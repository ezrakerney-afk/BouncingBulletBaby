#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// Player builds. Batch: -executeMethod BuildGame.Windows / BuildGame.WebGL
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

    // web build for itch, zip the contents of Builds/WebGL
    [MenuItem("Bouncing Bullet Baby/Build Web Game")]
    public static void WebGL()
    {
        LevelBuilder.BuildAll();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        PlayerSettings.productName = "Bouncing Bullet Baby";
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        // itch serves files as-is, so let the loader unpack compressed data itself
        PlayerSettings.WebGL.decompressionFallback = true;
        var report = BuildPipeline.BuildPlayer(scenes, "Builds/WebGL", BuildTarget.WebGL, BuildOptions.None);
        Debug.Log($"BuildGame: web {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
    }
}
#endif
