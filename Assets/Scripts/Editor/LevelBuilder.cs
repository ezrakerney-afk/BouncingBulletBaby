#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Builds the greybox levels from code so nobody hand-edits .unity files.
// Menu: Bouncing Bullet Baby > Build Greybox Levels. Batch: -executeMethod LevelBuilder.BuildAll
public static class LevelBuilder
{
    const string ArtDir = "Assets/Art/Placeholder";
    const string SceneDir = "Assets/Scenes/Levels";
    const string BulletPath = "Assets/Prefabs/Bullet.prefab";
    const int PixelsPerUnit = 16;

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
        public int shots;
        public Vector2[] targets;
        public int[] bounces;
        public bool boss;
        public List<(Vector2 center, Vector2 size)> blocks;
    }

    [MenuItem("Bouncing Bullet Baby/Build Greybox Levels")]
    public static void BuildAll()
    {
        EnsureAssets();
        var levels = Levels();
        var scenes = new List<EditorBuildSettingsScene>();
        Directory.CreateDirectory(SceneDir);
        foreach (int i in new[] { 0, 1, 2, 5, 3, 4 })
        {
            string path = $"{SceneDir}/Level{i + 1:00}.unity";
            BuildScene(levels[i], path);
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        scenes.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene($"{SceneDir}/Level01.unity");
        Debug.Log($"LevelBuilder: built {levels.Count} levels");
    }

    static List<Level> Levels()
    {
        // Arena is x -16..16, y -9..9. Outer walls are added for every level.
        var list = new List<Level>();
        list.Add(new Level { name = "Level01 - First Shot", baby = new Vector2(-12, -7.5f), target = new Vector2(12.5f, -7.5f), exit = new Vector2(12.5f, -2f),
            blocks = new List<(Vector2, Vector2)> { (new Vector2(0, -4f), new Vector2(1.5f, 10f)), (new Vector2(12.5f, -3.6f), new Vector2(5f, 0.6f)) } });
        // Build in priority order below; play order remains 1,2,3,4,5,boss.
        list.Add(new Level { name = "Level02 - Bank Shot", shots = 3, baby = new Vector2(-12, -7.5f),
            targets = new[] { new Vector2(8, -4), new Vector2(12, 3) }, bounces = new[] { 1, 1 },
            blocks = new List<(Vector2, Vector2)> { (new Vector2(0, -2), new Vector2(2, 14)) } });
        list.Add(new Level { name = "Level03 - Crib Climb", shots = 4, baby = new Vector2(-12, -7.5f),
            targets = new[] { new Vector2(-5, -6.3f), new Vector2(8, 0), new Vector2(-5, 6) },
            blocks = new List<(Vector2, Vector2)> {
                (new Vector2(-9, -7.5f), new Vector2(10, .6f)),
                (new Vector2(7, -1), new Vector2(12, .6f)),
                (new Vector2(-7, 4.5f), new Vector2(12, .6f)),
                (new Vector2(-5, -5.1f), new Vector2(3, .5f)),
                (new Vector2(8, 1.2f), new Vector2(3, .5f)),
                (new Vector2(-5, 7.2f), new Vector2(3, .5f)) } });
        list.Add(new Level { name = "Level04 - Mobile", shots = 4, baby = new Vector2(-12, -7.5f),
            targets = new[] { new Vector2(-6, 5), new Vector2(6, 5), new Vector2(-10, 0), new Vector2(10, 0) },
            blocks = new List<(Vector2, Vector2)>() });
        list.Add(new Level { name = "Level05 - Playpen Gauntlet", shots = 5, baby = new Vector2(-12, -7.5f),
            targets = new[] { new Vector2(-8, 6.3f), new Vector2(12, 5), new Vector2(3, -4), new Vector2(7, -4), new Vector2(11, -4) },
            bounces = new[] { 0, 2, 1, 1, 1 },
            blocks = new List<(Vector2, Vector2)> { (new Vector2(-9, 4.5f), new Vector2(10, .6f)),
                (new Vector2(0, 0), new Vector2(1, 9)) } });
        list.Add(new Level { name = "Boss - Big Teddy", shots = 6, boss = true, baby = new Vector2(-12, -7.5f),
            targets = new[] { new Vector2(3.8f, 0), new Vector2(-3.8f, 0), new Vector2(-4.2f, 1) },
            bounces = new[] { 1, 0, 0 }, blocks = new List<(Vector2, Vector2)>() });
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
        var pixelCamera = camGo.AddComponent<PixelPerfectCamera>();
        pixelCamera.assetsPPU = PixelsPerUnit;
        pixelCamera.refResolutionX = 544;
        pixelCamera.refResolutionY = 320;
        pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        Set(pixelCamera, "m_FilterMode", (int)PixelPerfectCamera.PixelPerfectFilterMode.Point);

        var level = new GameObject("Level");
        Block(level.transform, "Floor", new Vector2(0, -10), new Vector2(36, 2), Wood);
        Block(level.transform, "Ceiling", new Vector2(0, 10), new Vector2(36, 2), Wood);
        Block(level.transform, "WallL", new Vector2(-17, 0), new Vector2(2, 20), Wood);
        Block(level.transform, "WallR", new Vector2(17, 0), new Vector2(2, 20), Wood);
        int n = 0;
        foreach (var b in lv.blocks) Block(level.transform, $"Block{++n}", b.center, b.size, Rock);

        // Baby + revolver
        // Real art drops in by file name: Assets/Art/background.png and Assets/Art/baby.png (16 px = 1 unit).
        var backgroundArt = Art("background");
        if (backgroundArt != null)
        {
            var bg = Sprite("Background", null, Vector2.zero, Vector2.one, Color.white, backgroundArt);
            bg.GetComponent<SpriteRenderer>().sortingOrder = -100;
        }
        var babyArt = Art("baby");
        var baby = Sprite("Baby", null, lv.baby, Vector2.one, babyArt != null ? Color.white : BabyCol, babyArt != null ? babyArt : square);
        baby.GetComponent<SpriteRenderer>().sortingOrder = 10;
        var rb = baby.AddComponent<Rigidbody2D>(); rb.gravityScale = 3f; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = baby.AddComponent<CapsuleCollider2D>(); col.size = new Vector2(1f, 1f); col.sharedMaterial = slick;
        var bc = baby.AddComponent<BabyController>();
        var rocket = baby.AddComponent<DiaperRocket>();
        Set(rocket, "boostSpeed", 20f); Set(rocket, "boostDuration", 0.4f); Set(rocket, "cooldown", 0.3f);

        var gun = new GameObject("Revolver"); gun.transform.SetParent(baby.transform, false);
        var barrel = Sprite("Barrel", gun.transform, new Vector2(0.55f, 0), new Vector2(0.6f, 0.18f), new Color(0.2f, 0.2f, 0.22f), square);
        barrel.GetComponent<SpriteRenderer>().sortingOrder = 11;
        var muzzle = new GameObject("Muzzle"); muzzle.transform.SetParent(gun.transform, false); muzzle.transform.localPosition = new Vector3(1.0f, 0, 0);
        var rev = gun.AddComponent<Revolver>();
        Set(rev, "bulletPrefab", bulletPrefab); Set(rev, "muzzle", muzzle.transform); Set(rev, "owner", baby.transform); Set(rev, "aimCamera", cam);

        Set(rev, "capacity", lv.shots > 0 ? lv.shots : 6);

        // Target + exit
        var positions = lv.targets ?? new[] { lv.target };
        var targetObjects = new List<GameObject>();
        for (int j = 0; j < positions.Length; j++)
        {
            var target = Sprite($"Target{j + 1}", null, positions[j], Vector2.one * .9f, TargetCol, circle);
            target.AddComponent<CircleCollider2D>().isTrigger = true;
            var t = target.AddComponent<Target>(); Set(t, "baby", bc);
            t.requiredBounces = lv.bounces != null ? lv.bounces[j] : 0;
            t.requiresCeilingBounce = lv.boss && j == 0;
            t.penetrate = lv.name.Contains("Gauntlet") && j >= 2;
            targetObjects.Add(target);
            var bull = Sprite("Bullseye", target.transform, Vector2.zero, Vector2.one * .45f, Color.white, circle);
            if (lv.boss) target.AddComponent<GreyboxMotion>().phase = j + 1;
            if (lv.name.Contains("Mobile") && j < 2)
            {
                var motion = target.AddComponent<GreyboxMotion>();
                motion.amplitude = new Vector2(2.5f, .6f); motion.frequency = 1.2f; motion.offset = j * Mathf.PI;
                Sprite("Hanger", level.transform, new Vector2(positions[j].x, 7.7f), new Vector2(.1f, 2.4f), Wood, square);
            }
        }
        if (lv.name.Contains("Crib") || lv.name.Contains("Gauntlet"))
        {
            var pickup = Sprite("DiaperRocket Pickup (right click to boost)", null,
                lv.name.Contains("Crib") ? new Vector2(-11, -6.4f) : new Vector2(-11, -7.5f), Vector2.one, Color.cyan, circle);
            pickup.AddComponent<CircleCollider2D>().isTrigger = true;
            pickup.AddComponent<RocketPickup>();
        }
        if (lv.name.Contains("Mobile"))
        {
            foreach (float x in new[] { -10f, 10f })
            {
                var deflector = Block(level.transform, "45 degree deflector", new Vector2(x, -2), new Vector2(3, .5f), Rock);
                deflector.transform.rotation = Quaternion.Euler(0, 0, x < 0 ? 45 : -45);
                Block(level.transform, "Target cover", new Vector2(x - (x < 0 ? 1.4f : -1.4f), 0), new Vector2(.5f, 2), Rock);
            }
        }
        if (lv.boss) BuildTeddy(level.transform, targetObjects);
        if (lv.name.Contains("Crib"))
            foreach (var position in positions)
                Block(level.transform, "Crib alcove back", position + Vector2.right * 1.5f, new Vector2(.5f, 2.4f), Rock);

        // First Shot retains its rocket-to-exit tutorial; later rooms clear on all targets.
        if (lv.targets == null)
        {
            var exit = new GameObject("Exit"); exit.transform.position = lv.exit;
            var flag = Sprite("Flag", exit.transform, new Vector2(0, .2f), new Vector2(.9f, 1.6f), Color.gray, square);
            var trig = exit.AddComponent<BoxCollider2D>(); trig.size = new Vector2(1.2f, 2); trig.isTrigger = true;
            var le = exit.AddComponent<LevelExit>(); Set(le, "flag", flag.GetComponent<SpriteRenderer>());
        }

        var mgr = new GameObject("LevelManager");
        var manager = mgr.AddComponent<LevelManager>();
        manager.boss = lv.boss;
        manager.instructions = lv.name + (lv.boss ? ": ceiling bank, moving spot, shield gap; ammo refills per phase."
            : lv.name.Contains("Bank") ? ": both targets require a wall bank."
            : lv.name.Contains("Crib") ? ": jump onto tier 1, collect cyan rocket, boost between firing ledges."
            : lv.name.Contains("Mobile") ? ": time swinging targets; use the angled deflectors."
            : lv.name.Contains("Gauntlet") ? ": cyan rocket to high ledge; upper-right target needs 2 bounces; bank through the lower row."
            : ": hit the target, then rocket to the flag.");
        mgr.AddComponent<GameplayUI>();

        EditorSceneManager.SaveScene(scene, path);
    }

    static void BuildTeddy(Transform parent, List<GameObject> targets)
    {
        var teddy = Block(parent, "Big Teddy (6x6)", Vector2.zero, new Vector2(6, 6), Wood);
        Sprite("Left ear", teddy.transform, new Vector2(-.4f, .5f), Vector2.one * .35f, Rock, circle);
        Sprite("Right ear", teddy.transform, new Vector2(.4f, .5f), Vector2.one * .35f, Rock, circle);
        // Phase two motion is shared by body and exposed weak spot.
        var movement = teddy.AddComponent<GreyboxMotion>();
        movement.phase = 2; movement.amplitude = new Vector2(3, 0); movement.frequency = .8f;
        // Keep a stationary body in phases one and three.
        for (int phase = 1; phase <= 3; phase += 2)
            Block(parent, $"Teddy phase {phase}", Vector2.zero, new Vector2(6, 6), Wood).AddComponent<GreyboxMotion>().phase = phase;
        var spotMotion = targets[1].GetComponent<GreyboxMotion>();
        spotMotion.amplitude = new Vector2(3, 0); spotMotion.frequency = .8f;
        var thrown = Block(null, "Thrown block template", new Vector2(0, -30), Vector2.one, Rock);
        thrown.AddComponent<Rigidbody2D>().gravityScale = 1;
        thrown.SetActive(false);
        movement.throwBlocks = true; movement.blockPrefab = thrown;
        var shields = new GameObject("Rotating shields"); shields.transform.position = targets[2].transform.position;
        var spin = shields.AddComponent<GreyboxMotion>(); spin.phase = 3; spin.rotationSpeed = 65;
        Block(shields.transform, "Shield A", new Vector2(0, 1.5f), new Vector2(2, .3f), Color.gray);
        Block(shields.transform, "Shield B", new Vector2(0, -1.5f), new Vector2(2, .3f), Color.gray);
    }

    static GameObject Block(Transform parent, string name, Vector2 center, Vector2 size, Color color)
    {
        center = new Vector2(Mathf.Round(center.x), Mathf.Round(center.y));
        size = new Vector2(Mathf.Max(1, Mathf.Round(size.x)), Mathf.Max(1, Mathf.Round(size.y)));
        var go = Sprite(name, parent, center, size, color, square);
        go.AddComponent<BoxCollider2D>().sharedMaterial = slick;
        return go;
    }

    static GameObject Sprite(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        // Everything sits on the 16 px grid: positions and sizes are whole pixels.
        go.transform.localPosition = new Vector3(Snap(pos.x), Snap(pos.y), 0);
        go.transform.localScale = new Vector3(Mathf.Max(Pixel, Snap(size.x)), Mathf.Max(Pixel, Snap(size.y)), 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sharedMaterial = unlit;
        return go;
    }

    const float Pixel = 1f / PixelsPerUnit;
    static float Snap(float v) => Mathf.Round(v * PixelsPerUnit) / PixelsPerUnit;
    static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/{name}.png");

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
        go.transform.localScale = Vector3.one * 0.25f;
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
        // Regenerate our two owned placeholders so older 64px assets migrate too.
        {
            const int s = PixelsPerUnit;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - s / 2f + 0.5f, dy = y - s / 2f + 0.5f;
                    bool on = !round || dx * dx + dy * dy <= (s / 2f) * (s / 2f);
                    tex.SetPixel(x, y, on ? Color.white : Color.clear);
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single
            || imp.spritePixelsPerUnit != PixelsPerUnit
            || imp.filterMode != FilterMode.Point || imp.textureCompression != TextureImporterCompression.Uncompressed
            || imp.mipmapEnabled)
        {
            imp.textureType = TextureImporterType.Sprite;
            // Single mode: a stale Multiple-mode sheet from the old 64px texture leaves square_0 outside the 16px texture, so nothing renders.
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = PixelsPerUnit;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
