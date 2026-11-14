using UnityEditor;

public static class CreateJamSkeleton
{
    public static void Run()
    {
        foreach (var name in new[] { "MainMenu", "Level01" })
        {
            var path = $"Assets/Scenes/{name}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", path);
        }
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level01.unity", true)
        };
        AssetDatabase.SaveAssets();
    }
}
