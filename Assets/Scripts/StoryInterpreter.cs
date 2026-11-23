using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// The chronicler: before a technology's image is generated, a local language model (Ollama) writes how
/// this civilisation came to it, from where it lives on the map, its neighbours, what it values and its
/// earlier discoveries, then describes one image that symbolises it. That description becomes the image prompt.
/// </summary>
[Serializable]
public class StoryInterpreter
{
    [Tooltip("When off, or when Ollama can't be reached, image prompts are built from the words alone.")]
    public bool enabled = true;
    public string serverUrl = "http://127.0.0.1:11434";
    public string model = "llama3.2:3b";
    [Range(0f, 1.5f)] public float temperature = 0.9f;
    [Tooltip("Keep the GPU for image generation.")]
    public bool runOnCpu = true;
    public int timeoutSeconds = 180;
    [Tooltip("How long to keep retrying while the server is still starting up.")]
    public int serverStartupWaitSeconds = 120;

    private static readonly HashSet<string> _warnings = new HashSet<string>();

    private const string SystemPrompt =
        "You are the chronicler of an invented ancient civilisation in a strategy game. " +
        "You are told where this people lives, who its neighbours are, what it values, what it discovered before, " +
        "and the three words with which it was taught a new technology. " +
        "Answer in JSON with two fields. " +
        "\"story\": two or three sentences, in the past tense, telling how this people came to the new technology, " +
        "shaped by its land, its neighbours and its earlier discoveries. " +
        "\"image\": one sentence of at most 40 words describing a single symbolic scene or emblem this people " +
        "would paint for the discovery, with concrete objects, materials and colours from its land. " +
        "The image must contain no text, letters or numbers.";

    private const string OriginPrompt =
        "You are the chronicler of an invented ancient civilisation in a strategy game. " +
        "You are told where this people lives, who its neighbours are, what it values and what it first knew. " +
        "Answer in JSON. " +
        "\"story\": three or four sentences, in the past tense, telling how this people came to settle its land, " +
        "what it lived from and what it feared, how it saw its neighbours, and how its first knowledge was born of that land. " +
        "\"image\": one sentence of at most 40 words describing the emblem this people would paint to represent itself, " +
        "with concrete objects, materials and colours from its land. " +
        "Then, for each first knowledge, a field named after it: one sentence of at most 30 words describing " +
        "the symbol this people would paint for that knowledge, drawn from its land and way of life. " +
        "Images must contain no text, letters or numbers.";

    /// <summary>JSON schema Ollama must follow: story, image, and for an origin story one field per first knowledge.</summary>
    private static string AnswerFormat(List<string> knowledge)
    {
        StringBuilder properties = new StringBuilder("\"story\":{\"type\":\"string\"},\"image\":{\"type\":\"string\"}");
        StringBuilder required = new StringBuilder("\"story\",\"image\"");
        foreach (string name in knowledge)
        {
            properties.Append(',').Append(Quote(name)).Append(":{\"type\":\"string\"}");
            required.Append(',').Append(Quote(name));
        }
        return "{\"type\":\"object\",\"properties\":{" + properties + "},\"required\":[" + required + "]}";
    }

    /// <summary>
    /// What the chronicler knows when a civilisation starts researching a technology,
    /// or, for its origin story, at the start of a game (technology and words then list its first knowledge).
    /// </summary>
    public class Context
    {
        /// <summary>True for the origin story told at the start of a game.</summary>
        public bool origin;
        /// <summary>The civilisation's image folder name (its GameObject name).</summary>
        public string folder;
        public string civilisation;
        public string land;
        public string neighbours;
        public string values;
        public string technology;
        public string words;
        public List<string> history = new List<string>();
        /// <summary>Origin stories only: the starting technologies, each answered with its own scene.</summary>
        public List<string> knowledge = new List<string>();
    }

    [Serializable]
    public class Story
    {
        public string story;
        /// <summary>The scene for the technology's image, or for an origin story, the civilisation's emblem.</summary>
        public string image;
        /// <summary>Origin stories only: the scene for each starting technology.</summary>
        [NonSerialized] public Dictionary<string, string> knowledgeScenes = new Dictionary<string, string>();
    }

    /// <summary>A string field of a JSON object, unescaped, or null (JsonUtility can't read fields named at runtime).</summary>
    private static string ReadField(string json, string name)
    {
        Match match = Regex.Match(json, Regex.Escape(Quote(name)) + "\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        if (!match.Success)
            return null;
        return Regex.Unescape(match.Groups[1].Value);
    }

    /// <summary>Writes the story; onDone receives null if it couldn't.</summary>
    public IEnumerator Interpret(Context context, Action<Story> onDone)
    {
        if (!enabled)
        {
            onDone(null);
            yield break;
        }

        StringBuilder user = new StringBuilder();
        user.Append("Civilisation: ").Append(context.civilisation).Append('\n');
        user.Append("Land: ").Append(context.land).Append('\n');
        user.Append("Neighbours: ").Append(context.neighbours).Append('\n');
        user.Append("Values: ").Append(context.values).Append('\n');
        if (context.history.Count > 0)
            user.Append("Earlier chronicle:\n- ").Append(string.Join("\n- ", context.history)).Append('\n');
        if (context.origin)
        {
            user.Append("First knowledge: ").Append(context.technology).Append('\n');
            user.Append("Their first words: ").Append(context.words).Append('\n');
        }
        else
        {
            user.Append("New technology: ").Append(context.technology).Append('\n');
            user.Append("Words that taught it: ").Append(context.words).Append('\n');
        }

        string body = "{\"model\":" + Quote(model) +
                      ",\"stream\":false,\"keep_alive\":\"30m\"" +
                      ",\"format\":" + AnswerFormat(context.origin ? context.knowledge : new List<string>()) +
                      ",\"options\":{\"temperature\":" + temperature.ToString("R", CultureInfo.InvariantCulture) +
                      (runOnCpu ? ",\"num_gpu\":0" : "") + "}" +
                      ",\"messages\":[{\"role\":\"system\",\"content\":" +
                      Quote(context.origin ? OriginPrompt : SystemPrompt) + "}," +
                      "{\"role\":\"user\",\"content\":" + Quote(user.ToString()) + "}]}";

        string url = serverUrl.TrimEnd('/') + "/api/chat";
        float giveUpTime = Time.realtimeSinceStartup + serverStartupWaitSeconds;
        string error = null;
        string text = null;
        while (true)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;
                UnityWebRequestAsyncOperation operation;
                try
                {
                    operation = request.SendWebRequest();
                }
                catch (InvalidOperationException e)
                {
                    error = e.Message;
                    break;
                }
                yield return operation;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    text = request.downloadHandler.text;
                    break;
                }
                if (request.result != UnityWebRequest.Result.ConnectionError ||
                    Time.realtimeSinceStartup >= giveUpTime)
                {
                    error = request.error + " " + request.downloadHandler.text;
                    break;
                }
            }
            yield return new WaitForSecondsRealtime(3f);
        }

        if (text == null)
        {
            Warn("Ollama couldn't write the story of " + context.technology + " (" + error + "). " +
                 "Is it running at " + serverUrl + " with the model " + model + "?");
            onDone(null);
            yield break;
        }

        Story story = null;
        try
        {
            ChatReply reply = JsonUtility.FromJson<ChatReply>(text);
            story = JsonUtility.FromJson<Story>(reply.message.content);
            story.knowledgeScenes = new Dictionary<string, string>();
            foreach (string name in context.knowledge)
            {
                string scene = ReadField(reply.message.content, name);
                if (!string.IsNullOrWhiteSpace(scene))
                    story.knowledgeScenes[name] = scene;
            }
        }
        catch (Exception e)
        {
            Warn("Couldn't read the story Ollama wrote for " + context.technology + " (" + e.Message + ").");
        }
        if (story == null || string.IsNullOrWhiteSpace(story.image))
            story = null;
        onDone(story);
    }

    [Serializable]
    private class ChatReply
    {
        public ChatMessage message;
    }

    [Serializable]
    private class ChatMessage
    {
        public string content;
    }

    private static string Quote(string s)
    {
        StringBuilder quoted = new StringBuilder("\"");
        foreach (char c in s)
        {
            if (c == '"' || c == '\\') quoted.Append('\\').Append(c);
            else if (c == '\n') quoted.Append("\\n");
            else if (c < ' ') quoted.Append("\\u").Append(((int)c).ToString("x4"));
            else quoted.Append(c);
        }
        return quoted.Append('"').ToString();
    }

    private static void Warn(string message)
    {
        if (_warnings.Add(message))
            Debug.LogWarning("[StoryInterpreter] " + message + " Using the words alone for the image.");
    }

    // ---------------------------------------------------------------- Map

    /// <summary>
    /// The land at a point of the map (0..1 from the bottom left): north to south goes from cold to hot,
    /// the west edge is coast, the east is mountains, the middle is open plains.
    /// </summary>
    public static string DescribeLand(Vector2 position)
    {
        string climate;
        if (position.y > 0.75f) climate = "the cold far north, with tundra, long snowy winters and short summers";
        else if (position.y > 0.5f) climate = "temperate northern lands of pine forests, rivers and rain";
        else if (position.y > 0.25f) climate = "warm southern lands of dry hills, olive groves and grassland";
        else climate = "the hot far south, where desert meets steaming jungle";

        string terrain;
        if (position.x < 0.2f) terrain = "on the western coast, facing the open sea";
        else if (position.x > 0.8f) terrain = "far inland to the east, among high mountains";
        else if (position.x < 0.4f) terrain = "a few days' walk from the western sea";
        else if (position.x > 0.6f) terrain = "in the eastern foothills";
        else terrain = "in the open plains at the heart of the continent";

        return "They live in " + climate + ", " + terrain + ".";
    }

    /// <summary>Where the other civilisations are, seen from this one.</summary>
    public static string DescribeNeighbours(Vector2 position, List<KeyValuePair<string, Vector2>> others)
    {
        if (others.Count == 0)
            return "They know of no other people.";
        others.Sort((a, b) => (a.Value - position).sqrMagnitude.CompareTo((b.Value - position).sqrMagnitude));
        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, Vector2> other in others)
        {
            Vector2 offset = other.Value - position;
            float distance = offset.magnitude;
            string how = distance < 0.25f ? "close by" : distance < 0.55f ? "a long journey away" : "at the edge of the known world";
            parts.Add(other.Key + " lives " + how + " to the " + Compass(offset));
        }
        return string.Join("; ", parts) + ".";
    }

    private static string Compass(Vector2 offset)
    {
        string[] names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        int index = Mathf.RoundToInt(((angle + 360f) % 360f) / 45f) % 8;
        return names[index];
    }
}
