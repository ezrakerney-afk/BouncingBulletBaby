using UnityEngine;

// HUD on the ceiling band. No digits in the font, so numbers are words or pips.
public sealed class GameplayUI : MonoBehaviour
{
    static readonly string[] Numbers = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty" };
    static readonly Color BulletDark = new Color(0.35f, 0.2f, 0.1f);
    static readonly Color TargetRed = new Color(0.85f, 0.15f, 0.15f);
    const int TextY = 11, HintText = 1;
    const string Hint = "Aim with the mouse   Click to shoot   R restarts   Esc for menu";

    [SerializeField] LevelManager level;

    void Start()
    {
        if (level == null) level = FindFirstObjectByType<LevelManager>();
    }

    void OnGUI()
    {
        if (level == null || Event.current.type != EventType.Repaint) return;
        var ink = PixelUI.Ink;

        int n = level.LevelNumber;
        string number = n >= 0 && n < Numbers.Length ? Numbers[n] : "";
        int x = PixelUI.Text(12, TextY, $"Level {number}   {level.title}", 1, ink, false);

        x = PixelUI.Text(x + 20, TextY, "Bullets", 1, ink, false) + 7;
        for (int i = 0; i < level.ShotsLeft; i++, x += 7) PixelUI.Box(x, TextY + 6, 4, 9, BulletDark);

        x = PixelUI.Text(x + 16, TextY, "Targets", 1, ink, false) + 7;
        for (int i = 0; i < level.TargetsRemaining; i++, x += 10) PixelUI.Box(x, TextY + 7, 7, 7, TargetRed);

        int hintX = PixelUI.RefWidth - 12 - PixelUI.TextWidth(Hint, HintText);
        if (hintX > x + 16) PixelUI.Text(hintX, TextY, Hint, HintText, ink, false);

        string banner = level.Banner;
        if (banner != null) PixelUI.TextCentered(120, banner, 4, PixelUI.Paper);
    }
}
