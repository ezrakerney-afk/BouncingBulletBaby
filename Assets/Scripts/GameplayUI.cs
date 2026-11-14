using UnityEngine;

// Placeholder HUD: no Canvas, font assets, or scene serialization required.
public sealed class GameplayUI : MonoBehaviour
{
    [SerializeField] LevelManager level;
    GUIStyle big, small;

    void Start()
    {
        if (level == null) level = FindFirstObjectByType<LevelManager>();
    }

    void OnGUI()
    {
        if (level == null) return;
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        }

        GUI.Label(new Rect(16, 10, 600, 24), $"{level.LevelName}   Bullets: {new string('|', level.ShotsLeft)} ({level.ShotsLeft})", small);
        GUI.Label(new Rect(16, 32, 900, 24), "A/D move   Space jump   Left click shoot (bullets ricochet)   R restart", small);

        string center = null;
        if (level.LevelComplete) center = level.HasNextLevel ? "YEEHAW!  Enter for next level" : "YOU BEAT IT!  Enter to play again";
        else if (level.TargetHit) center = "TARGET HIT!  Right click: DIAPER ROCKET to the green flag";
        else if (level.OutOfShots) center = "Out of bullets...";
        if (center != null) GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 60), center, big);
    }
}
