using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tech images shared by the whole game: the starting techs' images (the same for every civilisation)
/// and the latest image any civilisation generated for each tech (used by the word keeper).
/// </summary>
public static class TechImageLibrary
{
    private static readonly Dictionary<string, Sprite> _start = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite> _latest = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite> _words = new Dictionary<string, Sprite>();

    /// <summary>The generated image for a word (shared by every civilisation), or its Resources image.</summary>
    public static Sprite GetWordSprite(string wordName)
    {
        if (_words.TryGetValue(wordName, out Sprite sprite) && sprite != null)
            return sprite;
        return Resources.Load<Sprite>(wordName);
    }

    /// <summary>
    /// Doesn't raise <see cref="Changed"/>: word cards are created when shown, so they pick it up next time.
    /// </summary>
    public static void SetWordSprite(string wordName, Sprite sprite)
    {
        _words[wordName] = sprite;
    }

    /// <summary>Raised when an image is added, so the UI can refresh.</summary>
    public static event Action Changed;

    /// <summary>Set once the starting images have been requested this session.</summary>
    public static bool StartImagesRequested { get; set; }

    /// <summary>The starting image for a tech, or its Resources image if there is none.</summary>
    public static Sprite GetStartSprite(string techName)
    {
        if (_start.TryGetValue(techName, out Sprite sprite) && sprite != null)
            return sprite;
        return Resources.Load<Sprite>(techName);
    }

    /// <summary>The latest image any civilisation made for a tech, else its starting image.</summary>
    public static Sprite GetLatestSprite(string techName)
    {
        if (_latest.TryGetValue(techName, out Sprite sprite) && sprite != null)
            return sprite;
        return GetStartSprite(techName);
    }

    public static void SetStartSprite(string techName, Sprite sprite)
    {
        _start[techName] = sprite;
        Changed?.Invoke();
    }

    public static void SetLatestSprite(string techName, Sprite sprite)
    {
        _latest[techName] = sprite;
        Changed?.Invoke();
    }

    /// <summary>
    /// Sprite over a whole runtime texture. With keep, it survives scene changes
    /// (loading a scene unloads assets nothing in the scene references).
    /// </summary>
    public static Sprite ToSprite(Texture2D texture, bool keep = false)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        if (keep)
        {
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }
        return sprite;
    }
}
