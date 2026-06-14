#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Constroi todos os prefabs do nivel a partir dos sprites reais (Pixel Adventure
// + Sunny Land), faz autotiling de grama, sombras, animacoes, ajusta o prefab da
// raposa (gravidade/collider/vida/poeira) e conecta tudo no LevelGenerator.
public static class LevelBuilder
{
    private const string GeneratedFolder = "Assets/_Generated";

    private const string TerrainSheet = "Assets/Pixel Adventure/Terrain/Terrain (16x16).png";
    private const string SpikeSheet = "Assets/Pixel Adventure/Traps/Spikes/Idle.png";
    private const string FlagSheet = "Assets/Pixel Adventure/Items/Checkpoints/End/End (Idle).png";
    private const string FruitSheet = "Assets/Pixel Adventure/Items/Fruits/Apple.png";
    private const string BackgroundSheet = "Assets/Pixel Adventure/Background/Green.png";
    private const string EnemySheet = "Assets/Pixel Adventure/Main Characters/Ninja Frog/Idle (32x32).png";
    private const string ShadowSheet = "Assets/Pixel Adventure/Other/Shadow.png";
    private const string DustSheet = "Assets/Pixel Adventure/Other/Dust Particle.png";
    private const string PlayerPrefab = "Assets/Sunny Land/Characters/Foxy/player-idle-1_0.prefab";

    [MenuItem("Tools/Level/Construir Nivel (Auto-Setup)")]
    public static void Build()
    {
        EnsureFolder();

        Sprite[] grass = LoadGrassTiles();
        Sprite spike = FirstSprite(SpikeSheet);
        Sprite flag = FirstSprite(FlagSheet);
        Sprite[] fruitFrames = SortedSprites(FruitSheet);
        Sprite[] enemyFrames = SortedSprites(EnemySheet);
        Sprite shadow = FirstSprite(ShadowSheet);
        Sprite dust = FirstSprite(DustSheet);
        Sprite background = LoadTiledBackground(BackgroundSheet);

        Sprite[] deathFrames = LoadSeparate("Assets/Sunny Land/Misc/Sunnyland FX/Sprites/enemy-death/enemy-death-", 6);
        Sprite[] pickupFrames = LoadSeparate("Assets/Sunny Land/Misc/Sunnyland FX/Sprites/item-feedback/item-feedback-", 4);

        GameObject solidPrefab = MakeSolid();
        GameObject spikePrefab = MakeSpike(spike);
        GameObject fruitPrefab = MakeFruit(fruitFrames, pickupFrames);
        GameObject flagPrefab = MakeFlag(flag);
        GameObject enemyPrefab = MakeEnemy(enemyFrames, deathFrames, shadow);
        GameObject playerPrefab = TweakPlayer(dust, shadow);

        LevelGenerator lg = Object.FindFirstObjectByType<LevelGenerator>();
        if (lg == null)
        {
            lg = new GameObject("LevelGenerator").AddComponent<LevelGenerator>();
        }

        lg.m_SolidPrefab = solidPrefab;
        lg.m_GrassTiles = grass;
        lg.m_SpikePrefab = spikePrefab;
        lg.m_FruitPrefab = fruitPrefab;
        lg.m_FlagPrefab = flagPrefab;
        lg.m_EnemyPrefab = enemyPrefab;
        lg.m_PlayerPrefab = playerPrefab;
        lg.m_BackgroundSprite = background;
        lg.m_ShadowSprite = shadow;

        EditorUtility.SetDirty(lg);
        EditorSceneManager.MarkSceneDirty(lg.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Nivel construido! Salve a cena (Ctrl+S) e de Play.");
    }

    // ── Prefabs ───────────────────────────────────────────────────────
    private static GameObject MakeSolid()
    {
        GameObject temp = new GameObject("Solid");
        temp.AddComponent<SpriteRenderer>(); // sprite definido por tile no LevelGenerator
        temp.AddComponent<BoxCollider2D>().size = Vector2.one;

        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0)
        {
            temp.layer = ground;
        }

        return SaveAndClean(temp, "Solid");
    }

    private static GameObject MakeSpike(Sprite sprite)
    {
        GameObject temp = new GameObject("Spike");
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.35f);
        col.offset = new Vector2(0f, -0.28f);

        temp.AddComponent<Hazard>();
        return SaveAndClean(temp, "Spike");
    }

    private static GameObject MakeFruit(Sprite[] frames, Sprite[] pickupFrames)
    {
        GameObject temp = new GameObject("Fruit");
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
        sr.sortingOrder = -5;

        CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        Collectible collectible = temp.AddComponent<Collectible>();
        collectible.m_PickupFrames = pickupFrames;

        temp.AddComponent<SpriteAnimator>().SetFrames(frames, 15f);

        return SaveAndClean(temp, "Fruit");
    }

    private static GameObject MakeFlag(Sprite sprite)
    {
        GameObject temp = new GameObject("Flag");
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -5;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 2.8f);

        temp.AddComponent<LevelFlag>();
        return SaveAndClean(temp, "Flag");
    }

    private static GameObject MakeEnemy(Sprite[] frames, Sprite[] deathFrames, Sprite shadow)
    {
        GameObject temp = new GameObject("Enemy");
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
        sr.sortingOrder = 0;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 1.5f);
        col.offset = new Vector2(0f, -0.25f);

        EnemyPatrol patrol = temp.AddComponent<EnemyPatrol>();
        patrol.m_DeathFrames = deathFrames;

        temp.AddComponent<SpriteAnimator>().SetFrames(frames, 10f);

        AddShadowChild(temp.transform, shadow, -0.95f, 1.2f);

        return SaveAndClean(temp, "Enemy");
    }

    // Ajusta o prefab da raposa existente.
    private static GameObject TweakPlayer(Sprite dust, Sprite shadow)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);

        Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        BoxCollider2D col = root.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(0.7f, 1.1f);
            col.offset = new Vector2(0f, -0.06f);
        }

        SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 10;
        }

        if (root.GetComponent<PlayerHealth>() == null)
        {
            root.AddComponent<PlayerHealth>();
        }

        Transform groundCheck = root.transform.Find("GroundCheck");

        RunDust runDust = root.GetComponent<RunDust>();
        if (runDust == null)
        {
            runDust = root.AddComponent<RunDust>();
        }

        SerializedObject so = new SerializedObject(runDust);
        so.FindProperty("m_DustSprite").objectReferenceValue = dust;
        if (groundCheck != null)
        {
            so.FindProperty("m_FeetPoint").objectReferenceValue = groundCheck;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        if (root.transform.Find("Shadow") == null)
        {
            AddShadowChild(root.transform, shadow, -0.7f, 1.1f);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
        PrefabUtility.UnloadPrefabContents(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
    }

    private static void AddShadowChild(Transform parent, Sprite shadow, float localY, float scale)
    {
        if (shadow == null)
        {
            return;
        }

        GameObject obj = new GameObject("Shadow");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(0f, localY, 0f);
        obj.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = shadow;
        sr.color = new Color(0f, 0f, 0f, 0.3f);
        sr.sortingOrder = -1;
    }

    private static GameObject SaveAndClean(GameObject temp, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{GeneratedFolder}/{name}.prefab");
        Object.DestroyImmediate(temp);
        return prefab;
    }

    // ── Sprites ────────────────────────────────────────────────────────
    private static Sprite[] LoadGrassTiles()
    {
        // 3x3 do bloco verde: cols 6,7,8 ; linhas (de cima) y160,y144,y128.
        int[] xs = { 96, 112, 128 };
        int[] ys = { 160, 144, 128 };

        Sprite[] tiles = new Sprite[9];
        Sprite fallback = FirstSprite(TerrainSheet);

        for (int row = 0; row < 3; row++)
        {
            for (int colTile = 0; colTile < 3; colTile++)
            {
                Sprite s = FindByRect(TerrainSheet, xs[colTile], ys[row]);
                tiles[row * 3 + colTile] = s != null ? s : fallback;
            }
        }

        return tiles;
    }

    private static Sprite FindByRect(string sheet, int x, int y)
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(sheet))
        {
            if (obj is Sprite sprite && (int)sprite.rect.x == x && (int)sprite.rect.y == y)
            {
                return sprite;
            }
        }

        return null;
    }

    private static Sprite FirstSprite(string sheetPath)
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath))
        {
            if (obj is Sprite sprite)
            {
                return sprite;
            }
        }

        Debug.LogWarning("LevelBuilder: nenhum sprite em " + sheetPath);
        return null;
    }

    private static Sprite[] SortedSprites(string sheetPath)
    {
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath))
        {
            if (obj is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort((a, b) => Trailing(a.name).CompareTo(Trailing(b.name)));
        return sprites.ToArray();
    }

    private static Sprite[] LoadSeparate(string prefix, int count)
    {
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 1; i <= count; i++)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{prefix}{i}.png");
            if (s != null)
            {
                sprites.Add(s);
            }
        }

        return sprites.ToArray();
    }

    private static int Trailing(string name)
    {
        Match m = Regex.Match(name, "(\\d+)$");
        return m.Success ? int.Parse(m.Value) : 0;
    }

    private static Sprite LoadTiledBackground(string sheetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.wrapMode = TextureWrapMode.Repeat;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        return FirstSprite(sheetPath);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets", "_Generated");
        }
    }
}
#endif
