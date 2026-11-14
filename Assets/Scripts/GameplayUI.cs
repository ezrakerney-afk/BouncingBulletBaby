using UnityEngine;

// In-level HUD in Ezra's font. His sheet has no digits, so the level number is spelled out
// and bullets and targets are shown as little pips instead of numbers.
public sealed class GameplayUI : MonoBehaviour
{
    static readonly string[] Numbers = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty" };
    static readonly Color BulletGold = new Color(1f, 0.82f, 0.25f);
    static readonly Color TargetRed = new Color(0.9f, 0.2f, 0.2f);

    [SerializeField] LevelManager level;

    void Start()
    {
        if (level == null) level = FindFirstObjectByType<LevelManager>();
    }

    void OnGUI()
    {
        if (level == null || Event.current.type != EventType.Repaint) return;

        int n = level.LevelNumber;
        string number = n >= 0 && n < Numbers.Length ? Numbers[n] : "";
        int x = PixelUI.Text(10, 12, $"Level {number}   {level.title}", 1, PixelUI.Paper);

        x = PixelUI.Text(x + 24, 12, "Bullets", 1, PixelUI.Paper) + 8;
        for (int i = 0; i < level.ShotsLeft; i++, x += 7) Pip(x, 17, 4, 9, BulletGold);

        x = PixelUI.Text(x + 20, 12, "Targets", 1, PixelUI.Paper) + 8;
        for (int i = 0; i < level.TargetsRemaining; i++, x += 10) Pip(x, 18, 7, 7, TargetRed);

        PixelUI.Text(10, PixelUI.RefHeight - 34, "Aim with the mouse   Click to shoot   R restarts   Esc for menu", 1, PixelUI.Paper);

        if (level.LevelComplete) PixelUI.TextCentered(110, "Bullseye", 3, PixelUI.Paper);
        else if (level.OutOfShots) PixelUI.TextCentered(110, "Out of bullets", 3, PixelUI.Paper);
    }

    static void Pip(int x, int y, int width, int height, Color color)
    {
        PixelUI.Box(x + 1, y + 1, width, height, PixelUI.Ink);
        PixelUI.Box(x, y, width, height, color);
    }
}
