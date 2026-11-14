#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// Makes a standalone Windows game so it can be played without opening Unity.
// Menu: Bouncing Bullet Baby > Build Windows Game. Batch: -executeMethod BuildGame.Windows
public static class BuildGame
{
    [MenuItem("Bouncing Bullet Baby/Build Windows Game")]
    public static void Windows()
    {
        LevelBuilder.BuildAll();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        PlayerSettings.productName = "Bouncing Bullet Baby";
        var report = BuildPipeline.BuildPlayer(scenes, "Builds/Windows/BouncingBulletBaby.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log($"BuildGame: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
    }
}
#endif
