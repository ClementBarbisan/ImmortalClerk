using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

/// <summary>
/// Generates a civilisation's own image for each technology it researches, with FLUX.1 [schnell]
/// running in a local Forge server (started with --api). The images of the parent technologies are
/// blended and used as the img2img source, so every new technology grows out of the ones before it.
/// </summary>
[Serializable]
public class TechImageGenerator
{
    [Tooltip("When off, or when the server can't be reached, the shared images from Resources are used.")]
    public bool enabled = true;
    public string serverUrl = "http://127.0.0.1:7860";

    [Header("Model files (in Forge's models folders)")]
    public string checkpointFile = "flux1-schnell-Q3_K_S.gguf";
    [Tooltip("VAE and text encoders Forge needs next to a GGUF Flux model.")]
    public string[] moduleFiles = { "ae.safetensors", "clip_l.safetensors", "t5xxl_fp8_e4m3fn.safetensors" };

    [Header("Prompt")]
    [TextArea] public string styleDescription =
        "an ancient hand-painted symbol, one single centered motif on a plain background, no text or letters";
    [Tooltip("What a civilisation leaning towards religion values; used for its two strongest leanings.")]
    public string religionStyle = "sacred rituals, temples and mystic signs";
    public string socialStyle = "crowds, festivals and shared villages";
    public string scienceStyle = "instruments, measurements and precise diagrams";
    public string conquestStyle = "weapons, banners and fortifications";

    [Header("Generation")]
    [Tooltip("Tech images show at about 120 px in game.")]
    public int imageSize = 512;
    [Tooltip("FLUX.1 [schnell] is made for 1 to 4 steps.")]
    public int steps = 4;
    [Tooltip("How far a new image may move away from the blended parent images (0 = copy, 1 = ignore them).")]
    [Range(0f, 1f)] public float denoisingStrength = 0.75f;
    public int timeoutSeconds = 600;
    [Tooltip("How long to keep retrying while the server is still starting up.")]
    public int serverStartupWaitSeconds = 300;

    private static readonly HashSet<string> _warnings = new HashSet<string>();
    private static bool _modelSelected;

    public static string ImageFolder(string civName)
    {
        return Path.Combine(Application.persistentDataPath, "TechImages", civName);
    }

    public static string ImagePath(string civName, string techName)
    {
        return Path.Combine(ImageFolder(civName), techName + ".png");
    }

    // ---------------------------------------------------------------- Prompts

    /// <summary>
    /// Prompt for a new technology: the words that led to it, what its parents were made of,
    /// the civilisation's strongest leanings and the shared style.
    /// </summary>
    public string BuildPrompt(string techName, string phrase, List<string> parentPhrases, Vector4 civValues,
        bool hasReferences)
    {
        StringBuilder prompt = new StringBuilder("One symbol for ").Append(techName);
        if (!string.IsNullOrEmpty(phrase))
            prompt.Append(", showing ").Append(phrase);
        prompt.Append(". ");
        if (parentPhrases.Count > 0)
            prompt.Append("It grew out of ").Append(string.Join(" and ", parentPhrases)).Append(". ");
        prompt.Append(DescribeLeanings(civValues)).Append(' ');
        prompt.Append("Style: ").Append(styleDescription).Append('.');
        return prompt.ToString();
    }

    private string DescribeLeanings(Vector4 values)
    {
        List<KeyValuePair<float, string>> traits = new List<KeyValuePair<float, string>>
        {
            new KeyValuePair<float, string>(values.x, religionStyle),
            new KeyValuePair<float, string>(values.y, socialStyle),
            new KeyValuePair<float, string>(values.z, scienceStyle),
            new KeyValuePair<float, string>(values.w, conquestStyle)
        };
        traits.RemoveAll(x => string.IsNullOrEmpty(x.Value));
        if (traits.Count == 0)
            return "";
        traits.Sort((a, b) => b.Key.CompareTo(a.Key));
        string sentence = "The people who made it value " + traits[0].Value +
                          (traits[0].Key >= 70f ? " above all" : "");
        if (traits.Count > 1)
            sentence += ", and also " + traits[1].Value;
        return sentence + ".";
    }

    private string BuildStartPrompt(JsonParser.Technos tech)
    {
        List<string> words = new List<string>();
        foreach (JsonParser.Word word in tech.words.subjects) words.Add(word.name);
        foreach (JsonParser.Word word in tech.words.verbs) words.Add(word.name);
        foreach (JsonParser.Word word in tech.words.objects) words.Add(word.name);
        string prompt = "One symbol for " + tech.name;
        if (words.Count > 0)
            prompt += ", evoking " + string.Join(", ", words);
        return prompt + ". Style: " + styleDescription + ".";
    }

    private string BuildWordPrompt(string wordName, string techName)
    {
        return "One symbol for the word \"" + wordName + "\", from the idea of " + techName +
               ". Style: " + styleDescription + ".";
    }

    // ---------------------------------------------------------------- Shared images

    /// <summary>
    /// Shared images (starting techs, words) are the same for every civilisation and kept between games.
    /// The settings' hash is in the name, so changing the prompt, model or size makes new ones.
    /// </summary>
    private string SharedImagePath(string folder, string name, string prompt)
    {
        uint hash = 2166136261;
        foreach (char c in prompt + "|" + checkpointFile + "|" + imageSize + "|" + steps)
            hash = (hash ^ c) * 16777619;
        return Path.Combine(Application.persistentDataPath, "TechImages", folder,
            name + "_" + hash.ToString("x8") + ".png");
    }

    /// <summary>
    /// Loads or generates the shared images: first the starting techs (no dependencies),
    /// then every word, those of the starting techs first since they can be used right away.
    /// </summary>
    public IEnumerator GenerateSharedImages(List<JsonParser.Technos> techs)
    {
        if (!enabled)
            yield break;

        foreach (JsonParser.Technos tech in techs)
        {
            if (tech.dependances.Count > 0)
                continue;
            string techName = tech.name;
            yield return LoadOrGenerate("_start", techName, BuildStartPrompt(tech),
                sprite => TechImageLibrary.SetStartSprite(techName, sprite));
        }

        List<JsonParser.Technos> ordered = techs.FindAll(x => x.dependances.Count == 0);
        ordered.AddRange(techs.FindAll(x => x.dependances.Count > 0));
        HashSet<string> done = new HashSet<string>();
        foreach (JsonParser.Technos tech in ordered)
        {
            List<JsonParser.Word> words = new List<JsonParser.Word>(tech.words.subjects);
            words.AddRange(tech.words.verbs);
            words.AddRange(tech.words.objects);
            foreach (JsonParser.Word word in words)
            {
                string wordName = word.name;
                if (!done.Add(wordName))
                    continue;
                yield return LoadOrGenerate("_words", wordName, BuildWordPrompt(wordName, tech.name),
                    sprite => TechImageLibrary.SetWordSprite(wordName, sprite));
            }
        }
    }

    private IEnumerator LoadOrGenerate(string folder, string name, string prompt, Action<Sprite> onSprite)
    {
        string path = SharedImagePath(folder, name, prompt);
        if (TryLoad(path, out Texture2D current))
        {
            onSprite(TechImageLibrary.ToSprite(current, true));
            yield break;
        }

        // Settings changed: show the previous version until the new one is ready.
        List<string> previous = PreviousVersions(path, name);
        if (previous.Count > 0 && TryLoad(previous[0], out Texture2D old))
            onSprite(TechImageLibrary.ToSprite(old, true));

        yield return Generate(name, prompt, new List<Sprite>(), path, texture =>
        {
            onSprite(TechImageLibrary.ToSprite(texture, true));
            foreach (string file in previous)
                File.Delete(file);
        });
    }

    private static bool TryLoad(string path, out Texture2D texture)
    {
        texture = null;
        if (!File.Exists(path))
            return false;
        texture = new Texture2D(2, 2);
        if (texture.LoadImage(File.ReadAllBytes(path)))
            return true;
        Object.Destroy(texture);
        texture = null;
        return false;
    }

    /// <summary>Older images of the same name (other settings hash), newest first.</summary>
    private static List<string> PreviousVersions(string path, string name)
    {
        List<string> files = new List<string>();
        string folder = Path.GetDirectoryName(path);
        if (!Directory.Exists(folder))
            return files;
        Regex sameName = new Regex("^" + Regex.Escape(name) + "_[0-9a-f]{8}\\.png$");
        foreach (string file in Directory.GetFiles(folder, "*.png"))
        {
            if (file != path && sameName.IsMatch(Path.GetFileName(file)))
                files.Add(file);
        }
        files.Sort((a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
        return files;
    }

    // ---------------------------------------------------------------- Forge

    /// <summary>
    /// Generates an image, saves it to outputPath and passes it to onDone.
    /// With parent images it runs img2img on their blend, otherwise txt2img.
    /// </summary>
    public IEnumerator Generate(string label, string prompt, List<Sprite> parents, string outputPath,
        Action<Texture2D> onDone)
    {
        string url = serverUrl.TrimEnd('/');
        float giveUpTime = Time.realtimeSinceStartup + serverStartupWaitSeconds;
        if (!_modelSelected)
        {
            yield return SelectModel(url, giveUpTime);
            if (!_modelSelected)
                yield break;
        }

        Txt2ImgRequest body;
        string endpoint;
        if (parents.Count > 0)
        {
            body = new Img2ImgRequest
            {
                init_images = new[] { BlendToBase64(parents) },
                denoising_strength = denoisingStrength
            };
            endpoint = "/sdapi/v1/img2img";
        }
        else
        {
            body = new Txt2ImgRequest();
            endpoint = "/sdapi/v1/txt2img";
        }
        body.prompt = prompt;
        body.steps = steps;
        body.width = imageSize;
        body.height = imageSize;

        Reply reply = new Reply();
        string json = JsonUtility.ToJson(body);
        yield return Send(() => PostJson(url + endpoint, json), reply, giveUpTime);
        if (!reply.ok)
        {
            Warn("Forge couldn't generate " + label + " (" + reply.Describe() + "). " +
                 "Is it running at " + serverUrl + " with --api?");
            yield break;
        }

        ForgeReply response = JsonUtility.FromJson<ForgeReply>(reply.text);
        if (response == null || response.images == null || response.images.Length == 0)
        {
            Warn("Forge returned no image for " + label + ".");
            yield break;
        }
        byte[] png = Convert.FromBase64String(response.images[0]);
        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(png))
        {
            Warn("Couldn't read the image Forge made for " + label + ".");
            Object.Destroy(texture);
            yield break;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllBytes(outputPath, png);
        onDone(texture);
    }

    /// <summary>
    /// Points Forge at the Flux schnell checkpoint and its VAE and text encoders, once per session.
    /// Forge wants the modules' full paths, so they're looked up from its module list.
    /// </summary>
    private IEnumerator SelectModel(string url, float giveUpTime)
    {
        Reply modules = new Reply();
        yield return Send(() => UnityWebRequest.Get(url + "/sdapi/v1/sd-modules"), modules, giveUpTime);
        if (!modules.ok)
        {
            Warn("Couldn't list Forge's modules (" + modules.Describe() + ").");
            yield break;
        }
        ModuleList list = JsonUtility.FromJson<ModuleList>("{\"items\":" + modules.text + "}");
        List<string> paths = new List<string>();
        foreach (string file in moduleFiles)
        {
            ModuleInfo found = Array.Find(list.items, m => Path.GetFileName(m.filename) == file);
            if (found == null)
            {
                Warn("Forge has no " + file + " in its VAE or text_encoder folders.");
                yield break;
            }
            paths.Add(found.filename);
        }

        StringBuilder options = new StringBuilder("{\"sd_model_checkpoint\":\"")
            .Append(EscapeJson(checkpointFile)).Append("\",\"forge_additional_modules\":[");
        for (int i = 0; i < paths.Count; i++)
            options.Append(i > 0 ? "," : "").Append('"').Append(EscapeJson(paths[i])).Append('"');
        options.Append("]}");
        Reply set = new Reply();
        string body = options.ToString();
        yield return Send(() => PostJson(url + "/sdapi/v1/options", body), set, 0f);
        if (!set.ok)
        {
            Warn("Forge refused the Flux schnell model settings (" + set.Describe() + ").");
            yield break;
        }
        _modelSelected = true;
    }

    private static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [Serializable]
    private class Txt2ImgRequest
    {
        public string prompt;
        public string negative_prompt = "";
        public int steps;
        // Flux schnell is guidance-distilled: no classifier-free guidance.
        public float cfg_scale = 1f;
        public float distilled_cfg_scale = 3.5f;
        public string sampler_name = "Euler";
        public string scheduler = "Simple";
        public int width;
        public int height;
        public long seed = -1;
    }

    [Serializable]
    private class Img2ImgRequest : Txt2ImgRequest
    {
        public string[] init_images;
        public float denoising_strength;
    }

    [Serializable]
    private class ForgeReply
    {
        public string[] images;
    }

    [Serializable]
    private class ModuleList
    {
        public ModuleInfo[] items;
    }

    [Serializable]
    private class ModuleInfo
    {
        public string model_name;
        public string filename;
    }

    // ---------------------------------------------------------------- HTTP

    private static UnityWebRequest PostJson(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    /// <summary>
    /// Sends a request, retrying while the server can't be reached until giveUpTime
    /// (it may still be starting: it's launched when entering Play mode).
    /// </summary>
    private IEnumerator Send(Func<UnityWebRequest> create, Reply reply, float giveUpTime)
    {
        while (true)
        {
            using (UnityWebRequest request = create())
            {
                request.timeout = timeoutSeconds;
                UnityWebRequestAsyncOperation operation;
                try
                {
                    operation = request.SendWebRequest();
                }
                catch (InvalidOperationException e)
                {
                    reply.error = "Unity refused the request: " + e.Message +
                                  ". Set Player Settings > Other Settings > Allow downloads over HTTP";
                    yield break;
                }
                yield return operation;

                // Forge answers 404 for a moment while its API is starting.
                bool notUpYet = request.result == UnityWebRequest.Result.ConnectionError ||
                                request.responseCode == 404;
                if (!notUpYet || Time.realtimeSinceStartup >= giveUpTime)
                {
                    reply.ok = request.result == UnityWebRequest.Result.Success;
                    reply.code = request.responseCode;
                    reply.error = request.error;
                    reply.text = request.downloadHandler != null ? request.downloadHandler.text : null;
                    yield break;
                }
            }
            yield return new WaitForSecondsRealtime(3f);
        }
    }

    private class Reply
    {
        public bool ok;
        public long code;
        public string error;
        public string text;

        public string Describe()
        {
            string details = string.IsNullOrEmpty(text) ? "" : ": " + (text.Length > 500 ? text.Substring(0, 500) : text);
            return (error ?? "HTTP " + code) + details;
        }
    }

    // ---------------------------------------------------------------- Images

    /// <summary>Averages the parent images (drawn over white) into one PNG, base64 encoded.</summary>
    private string BlendToBase64(List<Sprite> parents)
    {
        Color[] sum = new Color[imageSize * imageSize];
        foreach (Sprite parent in parents)
        {
            Texture2D copy = ReadableCopy(parent, imageSize);
            Color[] pixels = copy.GetPixels();
            for (int i = 0; i < sum.Length; i++)
            {
                Color c = pixels[i];
                sum[i] += new Color(c.r * c.a + 1f - c.a, c.g * c.a + 1f - c.a, c.b * c.a + 1f - c.a, 1f);
            }
            Object.Destroy(copy);
        }
        for (int i = 0; i < sum.Length; i++)
            sum[i] /= parents.Count;

        Texture2D blend = new Texture2D(imageSize, imageSize, TextureFormat.RGBA32, false);
        blend.SetPixels(sum);
        blend.Apply();
        string base64 = Convert.ToBase64String(blend.EncodeToPNG());
        Object.Destroy(blend);
        return base64;
    }

    /// <summary>Copies a sprite into a readable size x size texture, so it works without Read/Write enabled.</summary>
    private static Texture2D ReadableCopy(Sprite sprite, int size)
    {
        Texture source = sprite.texture;
        Rect rect = sprite.textureRect;
        Vector2 scale = new Vector2(rect.width / source.width, rect.height / source.height);
        Vector2 offset = new Vector2(rect.x / source.width, rect.y / source.height);

        RenderTexture target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, target, scale, offset);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D copy = new Texture2D(size, size, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        copy.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
        return copy;
    }

    private static void Warn(string message)
    {
        if (_warnings.Add(message))
            Debug.LogWarning("[TechImageGenerator] " + message + " Falling back to the shared images.");
    }
}
