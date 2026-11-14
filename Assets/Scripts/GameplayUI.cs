using UnityEngine;

// Placeholder HUD drawn with PixelFont on the game's own pixel grid (960x540, integer upscaled).
public sealed class GameplayUI : MonoBehaviour
{
    const int RefWidth = 960, RefHeight = 540;
    static readonly Color Ink = new Color(0.16f, 0.09f, 0.05f);
    static readonly Color Paper = new Color(1f, 0.95f, 0.8f);

    [SerializeField] LevelManager level;
    int scale, originX, originY;

    void Start()
    {
        if (level == null) level = FindFirstObjectByType<LevelManager>();
    }

    void OnGUI()
    {
        if (level == null || Event.current.type != EventType.Repaint) return;
        // Match the Pixel Perfect Camera: whole-number zoom, centered.
        scale = Mathf.Max(1, Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight));
        originX = (Screen.width - RefWidth * scale) / 2;
        originY = (Screen.height - RefHeight * scale) / 2;

        Line(8, 4, $"{level.LevelName}  BULLETS: {level.ShotsLeft}  TARGETS: {level.TargetsRemaining}" + (level.boss ? $"  TEDDY PHASE {level.BossPhase}/3" : ""), 2);
        Line(8, 18, "A/D MOVE  SPACE JUMP  LEFT CLICK SHOOT  RIGHT CLICK ROCKET  R RESTART  ESC QUIT", 2);
        Line(8, 34, level.instructions, 2);

        string center = null;
        if (level.LevelComplete) center = (level.LevelName == "Level05" && level.ShotsLeft > 0 ? "SPARE SHOT BONUS!  " : "") + (level.HasNextLevel ? "YEEHAW!  ENTER FOR NEXT LEVEL" : "YOU BEAT IT!  ENTER TO PLAY AGAIN");
        else if (level.TargetHit) center = "TARGET HIT!  RIGHT CLICK: DIAPER ROCKET TO THE FLAG";
        else if (level.OutOfShots) center = "OUT OF BULLETS...  R TO RESTART";
        if (center != null) Line((RefWidth - PixelFont.Width(center) * 4) / 2, 120, center, 4);
    }

    // x, y and size are in game pixels, measured from the top left of the 960x540 view.
    void Line(int x, int y, string text, int size)
    {
        var tex = PixelFont.Texture(text);
        if (tex == null) return;
        int s = scale * size;
        var rect = new Rect(originX + x * scale, originY + y * scale, tex.width * s, tex.height * s);
        var previous = GUI.color;
        GUI.color = Ink;
        GUI.DrawTexture(new Rect(rect.x + s, rect.y + s, rect.width, rect.height), tex);
        GUI.color = Paper;
        GUI.DrawTexture(rect, tex);
        GUI.color = previous;
    }
}
