#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Builds every level scene from the tile maps in Assets/Levels so nobody hand-edits .unity files.
// Menu: Bouncing Bullet Baby > Build Levels. Batch: -executeMethod LevelBuilder.BuildAll
public static class LevelBuilder
{
    const string ArtDir = "Assets/Art/Placeholder";
    const string SceneDir = "Assets/Scenes/Levels";
    const string BulletPath = "Assets/Prefabs/Bullet.prefab";
    // One pixel of Ezra's art is one game pixel: 32 px per unit, 960x540 view (2x on a 1080p screen).
    const int PixelsPerUnit = 32;

    static readonly Color Sand = new Color(0.93f, 0.80f, 0.56f);
    // Every surface is sand colored until the real tiles arrive; a touch deeper than the background sand so walls read.
    static readonly Color WallSand = new Color(0.90f, 0.75f, 0.50f);
    static readonly Color BabyCol = new Color(1f, 0.85f, 0.75f);
    static readonly Color TargetCol = new Color(0.9f, 0.12f, 0.12f);

    static Sprite square, circle, tile, pixel;
    static Material unlit;
    static PhysicsMaterial2D slick;
    static RicochetBullet bulletPrefab;

    const string LevelDir = "Assets/Levels";
    // Every level is a 30 x 18 tile map (one tile = one unit = 32 px). Tile (col, row) has its
    // centre at x = col - 14.5, y = 8.5 - row, so the outer ring of wall tiles frames the 960x540 view.
    const int Cols = 30, Rows = 18;

    struct Level
    {
        public string name;
        public int shots;
        public bool[,] solid;      // [col, row]
        public Vector2 baby;       // centre of the tile the baby's feet stand in (same tile in every level)
        public List<Vector2> targets;
    }

    static Vector2 Cell(int col, int row) => new Vector2(col - 14.5f, 8.5f - row);

    [MenuItem("Bouncing Bullet Baby/Build Levels")]
    public static void BuildAll()
    {
        EnsureAssets();
        var levels = Levels();
        var scenes = new List<EditorBuildSettingsScene>();
        Directory.CreateDirectory(SceneDir);
        // Build order: home screen (index 0), the levels, then the end screen and the game over screen.
        scenes.Add(BuildScreen(ScreenUI.Kind.Menu, ScreenUI.MenuScene));
        for (int i = 0; i < levels.Count; i++)
        {
            string path = $"{SceneDir}/Level{i + 1:00}.unity";
            BuildScene(levels[i], path, i);
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        scenes.Add(BuildScreen(ScreenUI.Kind.End, ScreenUI.EndScene));
        scenes.Add(BuildScreen(ScreenUI.Kind.GameOver, ScreenUI.GameOverScene));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene($"Assets/Scenes/{ScreenUI.MenuScene}.unity");
        Debug.Log($"LevelBuilder: built {levels.Count} levels");
    }

    // Level files: Assets/Levels/Level01.txt ... First line "NAME|SHOTS", then 18 rows of 30 characters:
    // '#' wall tile, '.' empty, 'B' baby start, 'T' target. tools/levels.py checks they can be beaten.
    static List<Level> Levels()
    {
        var list = new List<Level>();
        var files = new List<string>(Directory.GetFiles(LevelDir, "Level*.txt"));
        files.Sort(string.CompareOrdinal);
        foreach (string file in files)
        {
            var lines = new List<string>();
            foreach (string raw in File.ReadAllLines(file))
            {
                string line = raw.TrimEnd();
                if (line.Length > 0) lines.Add(line);
            }
            if (lines.Count != Rows + 1) { Debug.LogError($"LevelBuilder: {file} needs a title line and {Rows} rows"); continue; }
            string[] head = lines[0].Split('|');
            int shots = 6;
            if (head.Length > 1) int.TryParse(head[1], out shots);
            var lv = new Level { name = head[0].Trim(), shots = shots, solid = new bool[Cols, Rows], targets = new List<Vector2>() };
            bool ok = true;
            for (int row = 0; row < Rows; row++)
            {
                string text = lines[row + 1];
                if (text.Length != Cols) { Debug.LogError($"LevelBuilder: {file} row {row + 1} needs {Cols} characters"); ok = false; break; }
                for (int col = 0; col < Cols; col++)
                {
                    char c = text[col];
                    if (c == '#') lv.solid[col, row] = true;
                    else if (c == 'B') lv.baby = Cell(col, row);
                    else if (c == 'T') lv.targets.Add(Cell(col, row));
                }
            }
            if (ok) list.Add(lv);
        }
        return list;
    }

    // Home, game over and end screens: the same camera and background as the levels, plus ScreenUI.
    static EditorBuildSettingsScene BuildScreen(ScreenUI.Kind kind, string sceneName)
    {
        string path = $"Assets/Scenes/{sceneName}.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildCamera();
        BuildBackground();
        var go = new GameObject("Screen");
        var ui = go.AddComponent<ScreenUI>();
        ui.kind = kind;
        ui.face = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/face.png");
        ui.playButton = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/play.png");
        ui.exitButton = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/exit.png");
        go.AddComponent<GameShell>().music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundtrack.ogg");
        EditorSceneManager.SaveScene(scene, path);
        return new EditorBuildSettingsScene(path, true);
    }

    static Camera BuildCamera()
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 9.6f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Sand;
        camGo.transform.position = new Vector3(0, 0, -10);
        var pixelCamera = camGo.AddComponent<PixelPerfectCamera>();
        pixelCamera.assetsPPU = PixelsPerUnit;
        pixelCamera.refResolutionX = 960;
        pixelCamera.refResolutionY = 540;
        pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        Set(pixelCamera, "m_FilterMode", (int)PixelPerfectCamera.PixelPerfectFilterMode.Point);
        return cam;
    }

    // Background: Assets/Art/background.png sits behind everything in every level and screen.
    static void BuildBackground()
    {
        var backgroundArt = Art("background");
        if (backgroundArt == null) return;
        var bg = Sprite("Background", null, Vector2.zero, Vector2.one, Color.white, backgroundArt);
        bg.GetComponent<SpriteRenderer>().sortingOrder = -100;
    }

    static void BuildScene(Level lv, string path, int index)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = BuildCamera();
        BuildBackground();

        BuildWalls(lv);

        // Baby + revolver
        var babyFrames = BabyFrames();
        bool hasBabyArt = babyFrames != null;
        // The baby sprite is 2x2 units with the feet on its bottom edge, so its centre is half a unit above the feet tile's centre.
        var baby = Sprite("Baby", null, lv.baby + Vector2.up * 0.5f, Vector2.one, hasBabyArt ? Color.white : BabyCol, hasBabyArt ? babyFrames[0] : square);
        baby.GetComponent<SpriteRenderer>().sortingOrder = 10;
        // The baby stands in the same spot all game and never moves, so it needs no physics.

        var gun = new GameObject("Revolver"); gun.transform.SetParent(baby.transform, false);
        if (hasBabyArt)
        {
            // The gun is drawn into the baby frames, so the frame follows the aim instead of a rotating barrel.
            var aim = baby.AddComponent<BabyAimSprite>();
            aim.body = baby.GetComponent<SpriteRenderer>(); aim.gun = gun.transform;
            aim.frames = babyFrames; aim.angles = new float[] { 0, 20, 30, 40, 55, 70, 85, 90 };
        }
        else
        {
            var barrel = Sprite("Barrel", gun.transform, new Vector2(0.55f, 0), new Vector2(0.6f, 0.18f), new Color(0.2f, 0.2f, 0.22f), square);
            barrel.GetComponent<SpriteRenderer>().sortingOrder = 11;
        }
        // 0.7 units matches MUZZLE in tools/levels.py.
        var muzzle = new GameObject("Muzzle"); muzzle.transform.SetParent(gun.transform, false); muzzle.transform.localPosition = new Vector3(0.7f, 0, 0);
        var rev = gun.AddComponent<Revolver>();
        Set(rev, "bulletPrefab", bulletPrefab); Set(rev, "muzzle", muzzle.transform); Set(rev, "owner", baby.transform); Set(rev, "aimCamera", cam);
        Set(rev, "capacity", lv.shots > 0 ? lv.shots : 6);

        // Targets
        var targetArt = Art("target");
        for (int j = 0; j < lv.targets.Count; j++)
        {
            var target = Sprite($"Target{j + 1}", null, lv.targets[j], targetArt != null ? Vector2.one : Vector2.one * .9f, targetArt != null ? Color.white : TargetCol, targetArt != null ? targetArt : circle);
            target.GetComponent<SpriteRenderer>().sortingOrder = 5;
            var hitArea = target.AddComponent<CircleCollider2D>();
            hitArea.isTrigger = true;
            hitArea.radius = 0.5f / target.transform.localScale.x;
            target.AddComponent<Target>();
            if (targetArt == null) Sprite("Bullseye", target.transform, Vector2.zero, Vector2.one * .45f, Color.white, circle).GetComponent<SpriteRenderer>().sortingOrder = 6;
        }

        var mgr = new GameObject("LevelManager");
        var manager = mgr.AddComponent<LevelManager>();
        manager.title = lv.name;
        mgr.AddComponent<GameplayUI>();
        var shell = mgr.AddComponent<GameShell>();
        shell.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundtrack.ogg");

        EditorSceneManager.SaveScene(scene, path);
    }

    // Tiles outside the map count as solid so the outer ring only shows its inner face.
    static bool Solid(Level lv, int col, int row) =>
        col < 0 || row < 0 || col >= Cols || row >= Rows || lv.solid[col, row];

    // One sprite per wall tile, picked by which neighbours are solid, and one box collider
    // per merged rectangle of tiles so bullets and the baby never catch on seams between tiles.
    static void BuildWalls(Level lv)
    {
        var walls = new GameObject("Walls");
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Cols; col++)
            {
                if (!lv.solid[col, row]) continue;
                // Which sides of this tile face open air: top = 1, right = 2, bottom = 4, left = 8.
                // Assets/Art/Tiles/tile_<mask>.png holds the matching piece, already rotated to fit.
                int mask = (Solid(lv, col, row - 1) ? 0 : 1) | (Solid(lv, col + 1, row) ? 0 : 2)
                         | (Solid(lv, col, row + 1) ? 0 : 4) | (Solid(lv, col - 1, row) ? 0 : 8);
                var tileArt = Art($"Tiles/tile_{mask}");
                var t = Sprite($"Tile {col},{row}", walls.transform, Cell(col, row), Vector2.one,
                    tileArt != null ? Color.white : WallSand, tileArt != null ? tileArt : tile);
                t.GetComponent<SpriteRenderer>().sortingOrder = 1;
            }

        var used = new bool[Cols, Rows];
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Cols; col++)
            {
                if (!lv.solid[col, row] || used[col, row]) continue;
                int w = 1;
                while (col + w < Cols && lv.solid[col + w, row] && !used[col + w, row]) w++;
                int h = 1;
                while (row + h < Rows)
                {
                    bool full = true;
                    for (int c = col; c < col + w; c++)
                        if (!lv.solid[c, row + h] || used[c, row + h]) { full = false; break; }
                    if (!full) break;
                    h++;
                }
                for (int r = row; r < row + h; r++)
                    for (int c = col; c < col + w; c++) used[c, r] = true;
                var box = walls.AddComponent<BoxCollider2D>();
                box.sharedMaterial = slick;
                box.size = new Vector2(w, h);
                box.offset = new Vector2(col - 15f + w / 2f, 9f - row - h / 2f);
            }

        // Thick backstops outside the view so nothing can ever leave the room.
        foreach (var (center, size) in new[] {
            (new Vector2(0, -11), new Vector2(40, 4)), (new Vector2(0, 11), new Vector2(40, 4)),
            (new Vector2(-17, 0), new Vector2(4, 26)), (new Vector2(17, 0), new Vector2(4, 26)) })
        {
            var box = walls.AddComponent<BoxCollider2D>();
            box.sharedMaterial = slick; box.size = size; box.offset = center;
        }
    }

    static GameObject Sprite(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        // Everything sits on the pixel grid: positions and sizes are whole game pixels.
        go.transform.localPosition = new Vector3(Snap(pos.x), Snap(pos.y), 0);
        go.transform.localScale = new Vector3(Mathf.Max(Pixel, Snap(size.x)), Mathf.Max(Pixel, Snap(size.y)), 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sharedMaterial = unlit;
        return go;
    }

    const float Pixel = 1f / PixelsPerUnit;
    static float Snap(float v) => Mathf.Round(v * PixelsPerUnit) / PixelsPerUnit;
    static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/{name}.png");

    // Assets/Art/Baby/baby_aim0.png (gun level) up to baby_aim7.png (gun straight up). Null until all eight exist.
    static Sprite[] BabyFrames()
    {
        var frames = new Sprite[8];
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Art($"Baby/baby_aim{i}");
            if (frames[i] == null) return null;
        }
        return frames;
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
        square = MakeSprite($"{ArtDir}/square.png", 0);
        circle = MakeSprite($"{ArtDir}/circle.png", 1);
        tile = MakeSprite($"{ArtDir}/tile.png", 2);
        pixel = MakeSprite($"{ArtDir}/bullet.png", 3);

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        const string matPath = ArtDir + "/Unlit.mat";
        unlit = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (unlit == null) { unlit = new Material(shader); AssetDatabase.CreateAsset(unlit, matPath); }

        const string physPath = ArtDir + "/Slick.physicsMaterial2D";
        slick = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physPath);
        if (slick == null) { slick = new PhysicsMaterial2D("Slick") { friction = 0f, bounciness = 0f }; AssetDatabase.CreateAsset(slick, physPath); }

        // The bullet is a 4x4 px square. Its physics body moves smoothly; the visible square is a child
        // that PixelSnap keeps on the same pixel grid as everything else, with a short square-pixel trail.
        var go = new GameObject("Bullet");
        var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0;
        var cc = go.AddComponent<CircleCollider2D>(); cc.radius = 2f / PixelsPerUnit; cc.sharedMaterial = slick;
        go.AddComponent<RicochetBullet>();
        var look = new GameObject("Pixel"); look.transform.SetParent(go.transform, false);
        var sr = look.AddComponent<SpriteRenderer>(); sr.sprite = pixel; sr.sharedMaterial = unlit; sr.sortingOrder = 8;
        sr.color = Art("bullet") != null ? Color.white : new Color(1f, 0.93f, 0.6f);
        if (Art("bullet") != null) sr.sprite = Art("bullet");
        look.AddComponent<PixelSnap>().pixelsPerUnit = PixelsPerUnit;
        bulletPrefab = PrefabUtility.SaveAsPrefabAsset(go, BulletPath).GetComponent<RicochetBullet>();
        Object.DestroyImmediate(go);
    }

    // kind 0 = solid square, 1 = disc, 2 = wall tile with a darker 1 px edge, 3 = 4x4 px bullet.
    static Sprite MakeSprite(string path, int kind)
    {
        // Regenerate our owned placeholders every build so old sizes migrate too.
        {
            int s = kind == 3 ? 4 : PixelsPerUnit;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - s / 2f + 0.5f, dy = y - s / 2f + 0.5f;
                    Color c = Color.white;
                    if (kind == 1 && dx * dx + dy * dy > (s / 2f) * (s / 2f)) c = Color.clear;
                    if (kind == 2 && (x == 0 || y == 0 || x == s - 1 || y == s - 1)) c = new Color(0.72f, 0.72f, 0.72f);
                    tex.SetPixel(x, y, c);
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
