using UnityEngine;

// HUD/menu drawing in game pixels (960x540), scaled like the pixel perfect camera.
public static class PixelUI
{
    public const int RefWidth = 960, RefHeight = 540;
    public static readonly Color Ink = new Color(0.16f, 0.09f, 0.05f);
    public static readonly Color Paper = new Color(1f, 0.95f, 0.8f);

    public static int Scale => Mathf.Max(1, Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight));
    static int OriginX => (Screen.width - RefWidth * Scale) / 2;
    static int OriginY => (Screen.height - RefHeight * Scale) / 2;

    public static Rect ToScreen(float x, float y, float width, float height)
    {
        int s = Scale;
        return new Rect(OriginX + x * s, OriginY + y * s, width * s, height * s);
    }

    public static int TextWidth(string text, int size) => PixelFont.Width(text) * size;

    public static int Text(int x, int y, string text, int size, Color color, bool shadow = true)
    {
        var tex = PixelFont.Texture(text);
        if (tex == null) return x;
        var rect = ToScreen(x, y, tex.width * size, tex.height * size);
        float offset = Scale * size;
        var previous = GUI.color;
        if (shadow)
        {
            GUI.color = Ink;
            GUI.DrawTexture(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), tex);
        }
        GUI.color = color;
        GUI.DrawTexture(rect, tex);
        GUI.color = previous;
        return x + PixelFont.Width(text) * size;
    }

    public static void TextCentered(int y, string text, int size, Color color, bool shadow = true) =>
        Text((RefWidth - TextWidth(text, size)) / 2, y, text, size, color, shadow);

    public static void Box(float x, float y, float width, float height, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(ToScreen(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void Image(Texture2D tex, float x, float y, int size, Color tint)
    {
        if (tex == null) return;
        var previous = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(ToScreen(x, y, tex.width * size, tex.height * size), tex);
        GUI.color = previous;
    }
}
