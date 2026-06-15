#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Constroi todos os prefabs (chao, plataformas, espinhos, frutas, inimigos,
// traps, checkpoint, aguia) a partir dos sprites reais e conecta tudo no
// LevelGenerator da cena. Tambem ajusta o prefab da raposa (fisica, vida,
// poeira, sombra) e preenche todas as animacoes (idle/run/jump/lookup/roll/hurt).
// Um clique: Tools/Level/Construir Nivel (Auto-Setup).
public static class LevelBuilder
{
    private const string GeneratedFolder = "Assets/_Generated";

    private const string PA = "Assets/Pixel Adventure/";
    private const string SL = "Assets/Sunny Land/";

    private const string TerrainSheet = PA + "Terrain/Terrain (16x16).png";
    private const string SpikeSheet = PA + "Traps/Spikes/Idle.png";
    private const string CheckpointSheet = PA + "Items/Checkpoints/Checkpoint/Checkpoint (No Flag).png";
    private const string FrogSheet = PA + "Main Characters/Ninja Frog/Idle (32x32).png";
    private const string SpikeHeadSheet = PA + "Traps/Spike Head/Idle.png";
    private const string BallSheet = PA + "Traps/Spiked Ball/Spiked Ball.png";
    private const string ChainSheet = PA + "Traps/Spiked Ball/Chain.png";
    private const string CherriesSheet = PA + "Items/Fruits/Cherries.png";

    private const string ShadowSheet = PA + "Other/Shadow.png";
    private const string DustSheet = PA + "Other/Dust Particle.png";

    private const string EagleSheet = SL + "Characters/eagle/Sprites/attack/eagle-attack-";
    private const string SkyBg = SL + "environment/Background/back.png";
    private const string PlayerPrefab = SL + "Characters/Foxy/player-idle-1_0.prefab";
    private const string FoxySprites = SL + "Characters/Foxy/Sprites/";

    private static readonly string[] FruitSheets =
    {
        PA + "Items/Fruits/Apple.png",
        PA + "Items/Fruits/Bananas.png",
        PA + "Items/Fruits/Orange.png",
        PA + "Items/Fruits/Strawberry.png",
        PA + "Items/Fruits/Pineapple.png",
        PA + "Items/Fruits/Melon.png",
        PA + "Items/Fruits/Kiwi.png",
    };

    [MenuItem("Tools/Level/Construir Nivel (Auto-Setup)")]
    public static void Build()
    {
        EnsureFolder();

        Sprite[] grass = LoadGrassTiles();
        Sprite shadow = FirstSprite(ShadowSheet);
        Sprite dust = FirstSprite(DustSheet);

        Sprite[] deathFrames = LoadSeq(SL + "Misc/Sunnyland FX/Sprites/enemy-death/enemy-death-", 6);
        Sprite[] pickupFrames = LoadSeq(SL + "Misc/Sunnyland FX/Sprites/item-feedback/item-feedback-", 4);

        Sprite block = LoadSingle(SL + "environment/Props/block.png");

        GameObject solidPrefab = MakeSolid();
        GameObject spikePrefab = MakeSpike(FirstSprite(SpikeSheet));
        GameObject[] fruitPrefabs = MakeFruits(pickupFrames);
        GameObject frogPrefab = MakeEnemy("Frog", SortedSprites(FrogSheet), deathFrames, shadow, 10f, false, 2.2f);
        GameObject opossumPrefab = MakeEnemy("Opossum", LoadSeq(SL + "Characters/Opossum/opossum/opossum-", 6), deathFrames, shadow, 14f, true, 4f);
        GameObject fallingHeadPrefab = MakeFallingHead(FirstSprite(SpikeHeadSheet));
        GameObject swingPrefab = MakeSwing(FirstSprite(ChainSheet), FirstSprite(BallSheet), block);
        GameObject checkpointPrefab = MakeCheckpoint(FirstSprite(CheckpointSheet));
        GameObject eaglePrefab = MakeEagle();
        GameObject playerPrefab = TweakPlayer(dust, shadow);

        // Fundo "back" (acompanha a camera) tanto na floresta quanto no chefe.
        Sprite back = FirstSprite(SkyBg);
        Sprite life = FirstSprite(CherriesSheet);

        Sprite[] trees = LoadSpriteList(
            SL + "environment/Props/tree.png",
            SL + "environment/Props/pine.png");
        Sprite[] groundProps = LoadSpriteList(
            SL + "environment/Props/bush.png",
            SL + "environment/Props/rock.png",
            SL + "environment/Props/shrooms.png");
        Sprite sign = LoadSingle(SL + "environment/Props/sign.png");

        // Trilha sonora (o pacote so tem musicas; os SFX sao sintetizados no AudioManager).
        const string Music = "Assets/SunnyLand Music/";
        AudioClip introMusic = LoadAudio(Music + "adventure pack 2 ogg/magic cliffs.ogg");
        AudioClip[] levelMusic =
        {
            LoadAudio(Music + "Adventure pack 1 ogg/happywalking.ogg"),
            LoadAudio(Music + "Adventure pack 1 ogg/exploration.ogg"),
            LoadAudio(Music + "adventure pack 2 ogg/Maniac.ogg"),
        };

        LevelGenerator lg = Object.FindFirstObjectByType<LevelGenerator>();
        if (lg == null)
        {
            lg = new GameObject("LevelGenerator").AddComponent<LevelGenerator>();
        }

        lg.m_SolidPrefab = solidPrefab;
        lg.m_GrassTiles = grass;
        lg.m_ShadowSprite = shadow;
        lg.m_SpikePrefab = spikePrefab;
        lg.m_FruitPrefabs = fruitPrefabs;
        lg.m_FrogPrefab = frogPrefab;
        lg.m_OpossumPrefab = opossumPrefab;
        lg.m_FallingHeadPrefab = fallingHeadPrefab;
        lg.m_SwingPrefab = swingPrefab;
        lg.m_CheckpointPrefab = checkpointPrefab;
        lg.m_PlayerPrefab = playerPrefab;
        lg.m_EaglePrefab = eaglePrefab;
        lg.m_BackgroundSprite = back;
        lg.m_SkySprite = back;
        lg.m_TreeSprites = trees;
        lg.m_GroundProps = groundProps;
        lg.m_SignSprite = sign;
        lg.m_LifeSprite = life;
        lg.m_IntroMusic = introMusic;
        lg.m_LevelMusic = levelMusic;

        EditorUtility.SetDirty(lg);
        EditorSceneManager.MarkSceneDirty(lg.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Nivel construido! Salve a cena (Ctrl+S) e de Play.");
    }

    // ── Tiles / chao ──────────────────────────────────────────────────
    private static GameObject MakeSolid()
    {
        GameObject temp = new GameObject("Solid");
        temp.AddComponent<SpriteRenderer>();

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.usedByComposite = true;

        SetLayer(temp, "Ground");
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
        col.size = new Vector2(0.8f, 0.4f);
        col.offset = new Vector2(0f, -0.25f);

        temp.AddComponent<Hazard>();
        return SaveAndClean(temp, "Spike");
    }

    private static GameObject[] MakeFruits(Sprite[] pickupFrames)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (string sheet in FruitSheets)
        {
            Sprite[] frames = SortedSprites(sheet);
            if (frames == null || frames.Length == 0)
            {
                continue;
            }

            string name = "Fruit_" + System.IO.Path.GetFileNameWithoutExtension(sheet);
            list.Add(MakeFruit(name, frames, pickupFrames));
        }

        return list.ToArray();
    }

    private static GameObject MakeFruit(string name, Sprite[] frames, Sprite[] pickupFrames)
    {
        GameObject temp = new GameObject(name);
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = -5;

        CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        Collectible collectible = temp.AddComponent<Collectible>();
        collectible.m_PickupFrames = pickupFrames;

        temp.AddComponent<SpriteAnimator>().SetFrames(frames, 15f);
        return SaveAndClean(temp, name);
    }

    private static GameObject MakeEnemy(string name, Sprite[] frames, Sprite[] deathFrames, Sprite shadow, float fps, bool facesLeft, float speed)
    {
        GameObject temp = new GameObject(name);
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        Sprite first = (frames != null && frames.Length > 0) ? frames[0] : null;
        sr.sprite = first;
        sr.sortingOrder = 0;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.mass = 0.3f; // leve: nao empurra o player pra fora das plataformas
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Colisor calculado a partir do tamanho real do sprite, com a base do
        // colisor no pe do sprite (assim o inimigo nao fica flutuando).
        float ph = first != null ? first.rect.height / first.pixelsPerUnit : 2f;
        float pw = first != null ? first.rect.width / first.pixelsPerUnit : 1f;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.size = new Vector2(pw * 0.55f, ph * 0.72f);
        col.offset = new Vector2(0f, -ph * 0.14f);

        EnemyPatrol patrol = temp.AddComponent<EnemyPatrol>();
        patrol.m_DeathFrames = deathFrames;
        patrol.m_FacesLeft = facesLeft;
        patrol.m_PatrolSpeed = speed;

        if (frames != null && frames.Length > 0)
        {
            temp.AddComponent<SpriteAnimator>().SetFrames(frames, fps);
        }

        AddShadowChild(temp.transform, shadow, -ph * 0.5f + 0.05f, 1.0f, -1);
        return SaveAndClean(temp, name);
    }

    private static GameObject MakeFallingHead(Sprite sprite)
    {
        GameObject temp = new GameObject("FallingHead");
        temp.transform.localScale = Vector3.one * 0.82f; // ~1,5x maior que antes

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        // Solido (da pra subir em cima) e cinematico (movido por codigo).
        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = new Vector2(2.6f, 2.6f);

        temp.AddComponent<FallingHead>();
        SetLayer(temp, "Ground");
        return SaveAndClean(temp, "FallingHead");
    }

    private static GameObject MakeSwing(Sprite chain, Sprite ball, Sprite anchor)
    {
        GameObject temp = new GameObject("Swing");
        SwingingHazard swing = temp.AddComponent<SwingingHazard>();
        swing.m_ChainSprite = chain;
        swing.m_BallSprite = ball;
        swing.m_AnchorSprite = anchor;
        swing.m_Length = 3f;
        swing.m_MaxAngleDeg = 55f;
        swing.m_Speed = 2f;
        swing.m_ChainLinks = 6;
        return SaveAndClean(temp, "Swing");
    }

    private static GameObject MakeCheckpoint(Sprite sprite)
    {
        GameObject temp = new GameObject("Checkpoint");
        temp.transform.localScale = Vector3.one * 0.6f;

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -4;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.4f, 4f);

        temp.AddComponent<LevelFlag>();
        return SaveAndClean(temp, "Checkpoint");
    }

    private static GameObject MakeEagle()
    {
        Sprite[] frames = LoadSeqAbs(EagleSheet, 1, 4);

        GameObject temp = new GameObject("Eagle");
        temp.transform.localScale = Vector3.one * 1.2f;

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = (frames != null && frames.Length > 0) ? frames[0] : null;
        sr.sortingOrder = 20;

        CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.9f;

        temp.AddComponent<EagleBoss>();

        if (frames != null && frames.Length > 0)
        {
            temp.AddComponent<SpriteAnimator>().SetFrames(frames, 12f);
        }

        return SaveAndClean(temp, "Eagle");
    }

    // ── Player ────────────────────────────────────────────────────────
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
            col.size = new Vector2(0.62f, 1.1f);
            col.offset = new Vector2(0f, -0.06f);
            col.edgeRadius = 0.03f; // ajuda a nao "grudar" nas quinas
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

        AssignPlayerAnimations(root);

        if (root.transform.Find("Shadow") == null)
        {
            AddShadowChild(root.transform, shadow, -0.62f, 1.0f, 5);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
        PrefabUtility.UnloadPrefabContents(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
    }

    private static void AssignPlayerAnimations(GameObject root)
    {
        PlayerAnimation anim = root.GetComponent<PlayerAnimation>();
        if (anim == null)
        {
            anim = root.AddComponent<PlayerAnimation>();
        }

        Sprite[] idle = LoadSeqAbs(FoxySprites + "idle/player-idle-", 1, 4);
        Sprite[] run = LoadSeqAbs(FoxySprites + "run/player-run-", 1, 6);
        Sprite[] jump = LoadSeqAbs(FoxySprites + "jump/player-jump-", 1, 2);
        Sprite jump2 = LoadSingle(FoxySprites + "jump/player-jump-2.png");
        Sprite lookUp = LoadSingle(FoxySprites + "LookUp/lookUp.png");
        Sprite[] roll = LoadSeqAbs(FoxySprites + "Roll/Roll", 1, 4);
        Sprite[] hurt = LoadSeqAbs(FoxySprites + "hurt/player-hurt-", 1, 2);
        Sprite dead = LoadSingle(FoxySprites + "Hurt2/hurt-2.png");
        Sprite wallGrab1 = LoadSingle(FoxySprites + "WallGrab/wall-grab1.png");
        Sprite wallGrab2 = LoadSingle(FoxySprites + "WallGrab/wall-grab2.png");

        SerializedObject so = new SerializedObject(anim);
        SetSprites(so, "m_IdleSprites", idle);
        SetSprites(so, "m_RunSprites", run);
        SetSprites(so, "m_JumpSprites", jump);
        SetSprites(so, "m_FallSprites", jump2 != null ? new[] { jump2 } : null);
        SetSprites(so, "m_LookUpSprites", lookUp != null ? new[] { lookUp } : null);
        SetSprites(so, "m_RollSprites", roll);
        SetSprites(so, "m_HurtSprites", hurt);
        SetSprites(so, "m_DeadSprites", dead != null ? new[] { dead } : null);
        SetSprites(so, "m_WallGrabSprites", wallGrab1 != null ? new[] { wallGrab1 } : null);
        SetSprites(so, "m_WallGrab2Sprites", wallGrab2 != null ? new[] { wallGrab2 } : null);
        so.ApplyModifiedPropertiesWithoutUndo();

        SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
        if (sr != null && idle != null && idle.Length > 0)
        {
            sr.sprite = idle[0];
        }
    }

    private static void SetSprites(SerializedObject so, string propName, Sprite[] sprites)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null)
        {
            return;
        }

        int count = sprites != null ? sprites.Length : 0;
        prop.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    // ── Helpers de sprite / prefab ────────────────────────────────────
    private static void AddShadowChild(Transform parent, Sprite shadow, float localY, float scale, int order)
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
        sr.sortingOrder = order;
    }

    private static GameObject SaveAndClean(GameObject temp, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{GeneratedFolder}/{name}.prefab");
        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static void SetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            go.layer = layer;
        }
    }

    private static Sprite[] LoadGrassTiles()
    {
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

    private static Sprite[] LoadSpriteList(params string[] paths)
    {
        List<Sprite> list = new List<Sprite>();
        foreach (string path in paths)
        {
            Sprite s = LoadSingle(path);
            if (s != null)
            {
                list.Add(s);
            }
        }

        return list.ToArray();
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

        Sprite single = LoadSingle(sheetPath);
        if (single == null)
        {
            Debug.LogWarning("LevelBuilder: nenhum sprite em " + sheetPath);
        }

        return single;
    }

    private static Sprite LoadSingle(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AudioClip LoadAudio(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning("LevelBuilder: musica nao encontrada em " + path);
        }

        return clip;
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

        if (sprites.Count == 0)
        {
            Sprite single = LoadSingle(sheetPath);
            if (single != null)
            {
                sprites.Add(single);
            }
        }

        sprites.Sort((a, b) => Trailing(a.name).CompareTo(Trailing(b.name)));
        return sprites.ToArray();
    }

    // Carrega arquivos separados tipo prefix1.png .. prefixN.png
    private static Sprite[] LoadSeq(string prefix, int count)
    {
        return LoadSeqAbs(prefix, 1, count);
    }

    private static Sprite[] LoadSeqAbs(string prefix, int start, int end)
    {
        List<Sprite> sprites = new List<Sprite>();
        for (int i = start; i <= end; i++)
        {
            Sprite s = LoadSingle($"{prefix}{i}.png");
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

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets", "_Generated");
        }
    }
}
#endif
