#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the greybox levels from code so nobody hand-edits .unity files.
// Menu: Bouncing Bullet Baby > Build Greybox Levels. Batch: -executeMethod LevelBuilder.BuildAll
public static class LevelBuilder
{
    const string ArtDir = "Assets/Art/Placeholder";
    const string SceneDir = "Assets/Scenes/Levels";
    const string BulletPath = "Assets/Prefabs/Bullet.prefab";

    static readonly Color Sand = new Color(0.93f, 0.80f, 0.56f);
    static readonly Color Wood = new Color(0.45f, 0.28f, 0.16f);
    static readonly Color Rock = new Color(0.62f, 0.42f, 0.30f);
    static readonly Color BabyCol = new Color(1f, 0.85f, 0.75f);
    static readonly Color TargetCol = new Color(0.9f, 0.12f, 0.12f);

    static Sprite square, circle;
    static Material unlit;
    static PhysicsMaterial2D slick;
    static RicochetBullet bulletPrefab;

    struct Level
    {
        public string name;
        public Vector2 baby, target, exit;
        public List<(Vector2 center, Vector2 size)> blocks;
    }

    [MenuItem("Bouncing Bullet Baby/Build Greybox Levels")]
    public static void BuildAll()
    {
        EnsureAssets();
        var levels = Levels();
        var scenes = new List<EditorBuildSettingsScene>();
        Directory.CreateDirectory(SceneDir);
        for (int i = 0; i < levels.Count; i++)
        {
            string path = $"{SceneDir}/Level{i + 1:00}.unity";
            BuildScene(levels[i], path);
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log($"LevelBuilder: built {levels.Count} levels");
    }

    static List<Level> Levels()
    {
        // Arena is x -16..16, y -9..9. Outer walls are added for every level.
        var list = new List<Level>();
        list.Add(new Level { name = "Level01 - First Shot", baby = new Vector2(-12, -7.5f), target = new Vector2(12.5f, -7.5f), exit = new Vector2(12.5f, -2f),
            blocks = new List<(Vector2, Vector2)> { (new Vector2(0, -4f), new Vector2(1.5f, 10f)), (new Vector2(12.5f, -3.6f), new Vector2(5f, 0.6f)) } });
        list.Add(new Level { name = "Level02 - Bank Shot", baby = new Vector2(-13, -7.5f), target = new Vector2(14.3f, 5.5f), exit = new Vector2(-12f, 4.6f),
            blocks = new List<(Vector2, Vector2)> { (new Vector2(10f, 4f), new Vector2(1f, 8f)), (new Vector2(-12f, 3f), new Vector2(5f, 0.6f)), (new Vector2(2f, -6f), new Vector2(6f, 0.6f)) } });
        list.Add(new Level { name = "Level03 - Low Ceiling", baby = new Vector2(-13, -7.5f), target = new Vector2(13f, -7.8f), exit = new Vector2(0f, 6.6f),
            blocks = new List<(Vector2, Vector2)> { (new Vector2(6f, -5.5f), new Vector2(18f, 0.6f)), (new Vector2(-4f, -7f), new Vector2(1f, 3.4f)), (new Vector2(0f, 5f), new Vector2(4f, 0.6f)) } });
        list.Add(new Level { name = "Level04 - Zigzag", baby = new Vector2(-13.5f, -7.5f), target = new Vector2(13.5f, 7f), exit = new Vector2(13.5f, -2.4f),
            blocks = new List<(Vector2, Vector2)> { (new Vector2(-5f, -3f), new Vector2(1f, 12f)), (new Vector2(5f, 3f), new Vector2(1f, 12f)), (new Vector2(13.5f, -4f), new Vector2(4f, 0.6f)) } });
        list.Add(new Level { name = "Level05 - The Vault", baby = new Vector2(-13f, -7.5f), target = new Vector2(0f, 4.5f), exit = new Vector2(13f, 6.6f),
            blocks = new List<(Vector2, Vector2)> { (new Vector2(0f, 6.2f), new Vector2(6f, 0.6f)), (new Vector2(-2.7f, 4.2f), new Vector2(0.6f, 4f)), (new Vector2(2.7f, 4.2f), new Vector2(0.6f, 4f)),
                (new Vector2(-8f, -2f), new Vector2(5f, 0.6f)), (new Vector2(13f, 5f), new Vector2(5f, 0.6f)) } });
        return list;
    }

    static void BuildScene(Level lv, string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 9.6f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Sand;
        camGo.transform.position = new Vector3(0, 0, -10);

        var level = new GameObject("Level");
        Block(level.transform, "Floor", new Vector2(0, -9.5f), new Vector2(34, 1), Wood);
        Block(level.transform, "Ceiling", new Vector2(0, 9.5f), new Vector2(34, 1), Wood);
        Block(level.transform, "WallL", new Vector2(-16.5f, 0), new Vector2(1, 20), Wood);
        Block(level.transform, "WallR", new Vector2(16.5f, 0), new Vector2(1, 20), Wood);
        int n = 0;
        foreach (var b in lv.blocks) Block(level.transform, $"Block{++n}", b.center, b.size, Rock);

        // Baby + revolver
        var baby = Sprite("Baby", null, lv.baby, new Vector2(0.9f, 1.1f), BabyCol, square);
        var rb = baby.AddComponent<Rigidbody2D>(); rb.gravityScale = 3f; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = baby.AddComponent<CapsuleCollider2D>(); col.size = new Vector2(1f, 1f); col.sharedMaterial = slick;
        var bc = baby.AddComponent<BabyController>();
        var rocket = baby.AddComponent<DiaperRocket>();
        Set(rocket, "boostSpeed", 20f); Set(rocket, "boostDuration", 0.22f); Set(rocket, "cooldown", 0.3f);

        var gun = new GameObject("Revolver"); gun.transform.SetParent(baby.transform, false);
        var barrel = Sprite("Barrel", gun.transform, new Vector2(0.55f, 0), new Vector2(0.6f, 0.18f), new Color(0.2f, 0.2f, 0.22f), square);
        barrel.transform.localScale = new Vector3(0.6f / 1.0f / 0.9f, 0.18f / 1.1f, 1);
        var muzzle = new GameObject("Muzzle"); muzzle.transform.SetParent(gun.transform, false); muzzle.transform.localPosition = new Vector3(1.0f, 0, 0);
        var rev = gun.AddComponent<Revolver>();
        Set(rev, "bulletPrefab", bulletPrefab); Set(rev, "muzzle", muzzle.transform); Set(rev, "owner", baby.transform); Set(rev, "aimCamera", cam);

        // Target + exit
        var target = Sprite("Target", null, lv.target, new Vector2(0.9f, 0.9f), TargetCol, circle);
        target.AddComponent<CircleCollider2D>().radius = 0.5f;
        var t = target.AddComponent<Target>(); Set(t, "baby", bc);
        var bull = Sprite("Bullseye", target.transform, Vector2.zero, Vector2.one * 0.45f, Color.white, circle);
        bull.transform.localScale = Vector3.one * 0.45f;

        var exit = new GameObject("Exit"); exit.transform.position = lv.exit;
        var flag = Sprite("Flag", exit.transform, new Vector2(0, 0.2f), new Vector2(0.9f, 1.6f), Color.gray, square);
        var trig = exit.AddComponent<BoxCollider2D>(); trig.size = new Vector2(1.2f, 2f); trig.isTrigger = true;
        var le = exit.AddComponent<LevelExit>(); Set(le, "flag", flag.GetComponent<SpriteRenderer>());

        var mgr = new GameObject("LevelManager");
        mgr.AddComponent<LevelManager>();
        mgr.AddComponent<GameplayUI>();

        EditorSceneManager.SaveScene(scene, path);
    }

    static GameObject Block(Transform parent, string name, Vector2 center, Vector2 size, Color color)
    {
        var go = Sprite(name, parent, center, size, color, square);
        go.AddComponent<BoxCollider2D>().sharedMaterial = slick;
        return go;
    }

    static GameObject Sprite(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sharedMaterial = unlit;
        return go;
    }

    static void Set(Object obj, string field, object value)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"LevelBuilder: {obj.GetType().Name}.{field} not found"); return; }
        switch (value)
        {
            case float f: p.floatValue = f; break;
            case int i: p.intValue = i; break;
            case Object o: p.objectReferenceValue = o; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureAssets()
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory("Assets/Prefabs");
        square = MakeSprite($"{ArtDir}/square.png", false);
        circle = MakeSprite($"{ArtDir}/circle.png", true);

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        const string matPath = ArtDir + "/Unlit.mat";
        unlit = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (unlit == null) { unlit = new Material(shader); AssetDatabase.CreateAsset(unlit, matPath); }

        const string physPath = ArtDir + "/Slick.physicsMaterial2D";
        slick = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physPath);
        if (slick == null) { slick = new PhysicsMaterial2D("Slick") { friction = 0f, bounciness = 0f }; AssetDatabase.CreateAsset(slick, physPath); }

        var go = new GameObject("Bullet");
        go.transform.localScale = Vector3.one * 0.28f;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = circle; sr.color = new Color(1f, 0.85f, 0.2f); sr.sharedMaterial = unlit;
        var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0;
        var cc = go.AddComponent<CircleCollider2D>(); cc.radius = 0.5f; cc.sharedMaterial = slick;
        go.AddComponent<TrailRenderer>().time = 0.12f;
        var tr = go.GetComponent<TrailRenderer>(); tr.startWidth = 0.15f; tr.endWidth = 0f; tr.sharedMaterial = unlit;
        tr.startColor = new Color(1f, 0.9f, 0.4f); tr.endColor = new Color(1f, 0.5f, 0.1f, 0f);
        go.AddComponent<RicochetBullet>();
        bulletPrefab = PrefabUtility.SaveAsPrefabAsset(go, BulletPath).GetComponent<RicochetBullet>();
        Object.DestroyImmediate(go);
    }

    static Sprite MakeSprite(string path, bool round)
    {
        if (!File.Exists(path))
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - s / 2f + 0.5f, dy = y - s / 2f + 0.5f;
                    bool on = !round || dx * dx + dy * dy <= (s / 2f) * (s / 2f);
                    tex.SetPixel(x, y, on ? Color.white : Color.clear);
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp.textureType != TextureImporterType.Sprite || imp.spritePixelsPerUnit != 64)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 64;
            imp.filterMode = FilterMode.Bilinear;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
