using UnityEngine;

// In-level HUD in Ezra's font, drawn on the thick sand ceiling so no text covers the play area.
// His sheet has no digits, so the level number is spelled out and bullets and targets are shown as pips.
public sealed class GameplayUI : MonoBehaviour
{
    static readonly string[] Numbers = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty" };
    static readonly Color BulletDark = new Color(0.35f, 0.2f, 0.1f);
    static readonly Color TargetRed = new Color(0.85f, 0.15f, 0.15f);
    // The ceiling band is the top 46 game pixels (rows 0 and 1 of every level are wall); the 21 px font sits inside it.
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

        // Controls sit at the right end of the same band, if there is room left.
        int hintX = PixelUI.RefWidth - 12 - PixelUI.TextWidth(Hint, HintText);
        if (hintX > x + 16) PixelUI.Text(hintX, TextY, Hint, HintText, ink, false);
    }
}
