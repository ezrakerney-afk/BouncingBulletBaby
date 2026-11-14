#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry point (omit -quit): builds scenes, enters Play, checks Level01 flow, exits.
[InitializeOnLoad]
public static class GreyboxSmokeCheck
{
    const string Key = "GreyboxSmokeCheck.Pending";
    static double began;
    static bool failed;
    static GreyboxSmokeCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                began = EditorApplication.timeSinceStartup;
                Application.logMessageReceived += Capture;
                EditorApplication.update += Check;
            }
        };
    }
    public static void Run()
    {
        LevelBuilder.BuildAll();
        EditorSceneManager.OpenScene("Assets/Scenes/Levels/Level01.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    static void Capture(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
    }
    static void Check()
    {
        if (EditorApplication.timeSinceStartup - began < 3) return;
        EditorApplication.update -= Check;
        var baby = Object.FindFirstObjectByType<BabyController>();
        var gun = Object.FindFirstObjectByType<Revolver>();
        var target = Object.FindFirstObjectByType<Target>();
        var level = Object.FindFirstObjectByType<LevelManager>();
        bool ok = baby != null && gun != null && target != null && level != null;
        if (ok)
        {
            ok &= gun.TryFire(Vector2.up) && gun.ShotsLeft == 5;
            target.Hit(baby);
            ok &= level.TargetHit && baby.RocketUnlocked && !gun.CanShoot;
            var exit = Object.FindFirstObjectByType<LevelExit>();
            ok &= exit != null && exit.Open;
        }
        SessionState.SetBool(Key, false);
        Debug.Log(ok && !failed ? "GREYBOX_SMOKE_PASS: Level01 Play, firing, target, rocket and exit unlock" : "GREYBOX_SMOKE_FAIL");
        EditorApplication.Exit(ok && !failed ? 0 : 1);
    }
}
#endif
