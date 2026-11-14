using System.Collections.Generic;
using UnityEngine;

// Tiny built-in 3x5 bitmap font so the HUD sits on the same pixel grid as the game. No font assets needed.
public static class PixelFont
{
    public const int GlyphWidth = 3, GlyphHeight = 5, Advance = 4;

    // Five rows of three bits each, top row first.
    static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['D'] = "110101101101110",
        ['E'] = "111100110100111", ['F'] = "111100110100100", ['G'] = "011100101101011", ['H'] = "101101111101101",
        ['I'] = "111010010010111", ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
        ['M'] = "101111111101101", ['N'] = "110101101101101", ['O'] = "010101101101010", ['P'] = "110101110100100",
        ['Q'] = "010101101110011", ['R'] = "110101110101101", ['S'] = "011100010001110", ['T'] = "111010010010010",
        ['U'] = "101101101101111", ['V'] = "101101101101010", ['W'] = "101101111111101", ['X'] = "101101010101101",
        ['Y'] = "101101010010010", ['Z'] = "111001010100111",
        ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "110001010100111", ['3'] = "110001010001110",
        ['4'] = "101101111001001", ['5'] = "111100110001110", ['6'] = "011100111101111", ['7'] = "111001010010010",
        ['8'] = "111101111101111", ['9'] = "111101111001110",
        [':'] = "000010000010000", ['.'] = "000000000000010", [','] = "000000000010100", ['!'] = "010010010000010",
        ['/'] = "001001010100100", ['-'] = "000000111000000", ['('] = "001010010010001", [')'] = "100010010010100",
        ['\''] = "010010000000000", [';'] = "000010000010100", ['+'] = "000010111010000", ['?'] = "110001010000010",
    };

    static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

    public static int Width(string text) => string.IsNullOrEmpty(text) ? 0 : text.Length * Advance - 1;

    // White-on-clear texture of the text, one texel per font pixel. Cached per string.
    public static Texture2D Texture(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (Cache.TryGetValue(text, out var cached) && cached != null) return cached;
        if (Cache.Count > 64)
        {
            foreach (var old in Cache.Values) if (old != null) Object.Destroy(old);
            Cache.Clear();
        }
        string upper = text.ToUpperInvariant();
        int width = Width(upper);
        var tex = new Texture2D(width, GlyphHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[width * GlyphHeight];
        var on = new Color32(255, 255, 255, 255);
        for (int i = 0; i < upper.Length; i++)
        {
            if (!Glyphs.TryGetValue(upper[i], out var rows)) continue;
            for (int row = 0; row < GlyphHeight; row++)
                for (int col = 0; col < GlyphWidth; col++)
                    if (rows[row * GlyphWidth + col] == '1')
                        pixels[(GlyphHeight - 1 - row) * width + i * Advance + col] = on;
        }
        tex.SetPixels32(pixels);
        tex.Apply(false);
        Cache[text] = tex;
        return tex;
    }
}
