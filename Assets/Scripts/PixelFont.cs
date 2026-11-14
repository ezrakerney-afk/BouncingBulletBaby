using System.Collections.Generic;
using UnityEngine;

// Bitmap text from PixelFontData. Letters only, anything else is a gap.
public static class PixelFont
{
    public const int Spacing = 2, SpaceWidth = 5;
    public static int Height => PixelFontData.Height;

    static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

    public static int Width(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int width = 0;
        foreach (char c in text)
            width += PixelFontData.Glyphs.TryGetValue(c, out var rows) ? rows[0].Length + Spacing : SpaceWidth;
        return Mathf.Max(1, width - Spacing);
    }

    public static Texture2D Texture(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (Cache.TryGetValue(text, out var cached) && cached != null) return cached;
        if (Cache.Count > 64)
        {
            foreach (var old in Cache.Values) if (old != null) Object.Destroy(old);
            Cache.Clear();
        }
        int width = Width(text) + Spacing, height = Height;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[width * height];
        var on = new Color32(255, 255, 255, 255);
        int x = 0;
        foreach (char c in text)
        {
            if (!PixelFontData.Glyphs.TryGetValue(c, out var rows)) { x += SpaceWidth; continue; }
            for (int row = 0; row < height && row < rows.Length; row++)
                for (int col = 0; col < rows[row].Length; col++)
                    if (rows[row][col] == '1' && x + col < width)
                        pixels[(height - 1 - row) * width + x + col] = on;
            x += rows[0].Length + Spacing;
        }
        tex.SetPixels32(pixels);
        tex.Apply(false);
        Cache[text] = tex;
        return tex;
    }
}
