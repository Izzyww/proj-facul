using System.Collections.Generic;
using UnityEngine;

// Gera as fases. O chao vira UM colisor composto (CompositeCollider2D), entao
// nao existem mais "paredes invisiveis" nas emendas dos tiles. Plataformas "="
// sao atravessaveis por baixo e firmes por cima (PlatformEffector2D). Inimigos,
// frutas, espinhos e traps sao encaixados na superficie pra nao flutuar. A fase
// 3 e o chefe (aguia) montada na mao.
//
// Legenda dos mapas:
//   X  chao solido (grama, vira morro se empilhar)   =  plataforma atravessavel
//   ^  espinho        C fruta        E inimigo (sapo)  O inimigo (gambá)
//   H  cabeca que cai  S  bola que balanca
//   P  spawn do player  K  checkpoint (fim da fase)    . vazio
public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    [Header("Tiles / chao")]
    public GameObject m_SolidPrefab;
    public Sprite[] m_GrassTiles; // 9: TL,T,TR, ML,M,MR, BL,B,BR
    public Sprite m_ShadowSprite;

    [Header("Itens e inimigos")]
    public GameObject m_SpikePrefab;
    public GameObject[] m_FruitPrefabs;
    public GameObject m_FrogPrefab;
    public GameObject m_OpossumPrefab;
    public GameObject m_FallingHeadPrefab;
    public GameObject m_SwingPrefab;
    public GameObject m_CheckpointPrefab;
    public GameObject m_PlayerPrefab;
    public GameObject m_EaglePrefab;

    [Header("Cenario")]
    public Sprite m_BackgroundSprite;
    public Sprite m_SkySprite;
    public Sprite[] m_TreeSprites;
    public Sprite[] m_GroundProps;
    public Sprite m_SignSprite;

    [Header("HUD")]
    public Sprite m_LifeSprite;

    private const int OrderBackground = -100;
    private const int OrderTree = -70;
    private const int OrderShadow = -60;
    private const int OrderTile = -50;
    private const int OrderGroundProp = -45;

    private Transform m_BgTransform;
    private SpriteRenderer m_BgRenderer;

    private List<string[]> m_Levels;
    private int m_CurrentLevel;
    private int m_Width;
    private int m_Height;
    private float m_KillY;
    private bool m_IsBoss;
    private bool m_DeathHandled;

    private Transform m_PlayerTransform;
    private PlayerHealth m_PlayerHealth;

    private float m_DefaultCamSize = 5f;
    private bool m_CamSizeCaptured;
    private readonly Vector3 m_CameraOffset = new Vector3(4f, 0.5f, -10f);
    private Vector3 m_FixedCameraPos;

    private System.Random m_Rng;

    public bool IsBossLevel => m_IsBoss;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        if (Camera.main != null)
        {
            m_DefaultCamSize = Camera.main.orthographicSize;
            m_CamSizeCaptured = true;
        }

        BuildLevelData();
        LevelTransition.GetOrCreate();

        LivesHUD hud = LivesHUD.GetOrCreate();
        if (m_LifeSprite != null)
        {
            hud.m_LifeSprite = m_LifeSprite;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BroadcastLives();
        }

        if (m_SolidPrefab == null)
        {
            Debug.LogWarning("LevelGenerator: rode 'Tools/Level/Construir Nivel (Auto-Setup)' antes de dar Play.");
        }

        LoadLevel(0);
    }

    private void BuildLevelData()
    {
        m_Levels = new List<string[]>();

        // ── FASE 1 – Floresta (montada por codigo, espacamento certinho) ──
        m_Levels.Add(BuildForest());

        // ── FASE 2 – Colinas e cavernas (montada por codigo) ──────────
        m_Levels.Add(BuildCaves());

        // Fase 3 e o chefe; nao usa mapa de texto.
        m_Levels.Add(null);
    }

    // Monta a fase 1 a partir de segmentos de terreno + posicoes de entidades,
    // pra garantir tamanho e espacamento consistentes (sem contar caractere).
    private string[] BuildForest()
    {
        const int W = 78;
        const int H = 20; // alto: terreno fundo + folga pro topo (carona da cabeca)

        char[][] g = NewGrid(W, H);

        // Terreno: (xInicio, xFim, alturaDoTopo em worldY). Preenche ate worldY 0.
        AddTerrain(g, H, 0, 7, 8);     // A: inicio (inimigo na direita)
        AddTerrain(g, H, 8, 11, 10);   // degrau 1 do morro
        AddTerrain(g, H, 12, 18, 12);  // topo do morro
        AddTerrain(g, H, 24, 34, 10);  // plataforma do meio (5 espinhos)
        AddTerrain(g, H, 39, 46, 10);  // plataforma do gambá
        AddTerrain(g, H, 51, 57, 10);  // plataforma da fruta (pulo facil)
        AddTerrain(g, H, 58, 62, 8);   // descida
        AddTerrain(g, H, 63, 77, 6);   // chao final (espinhos + checkpoint)

        Set(g, H, 2, 9, 'P');          // player no comeco
        Set(g, H, 6, 9, 'E');          // sapo na ponta direita do chao inicial
        Set(g, H, 15, 13, 'C');        // fruta no topo do morro

        // Cabeca centralizada no buraco (GAP 19-23, centro 21). A plataforma alta
        // fica sobre o MEIO (em cima da plataforma do meio); so da pra alcancar
        // pegando carona na cabeca quando ela sobe.
        Set(g, H, 21, 17, 'H');
        for (int x = 25; x <= 33; x++) // plataforma centralizada sobre a do meio (24-34)
        {
            Set(g, H, x, 16, '=');
        }
        Set(g, H, 29, 17, 'C');

        // 5 espinhos no centro da plataforma do meio (24-34, centro 29).
        for (int x = 27; x <= 31; x++)
        {
            Set(g, H, x, 11, '^');
        }

        Set(g, H, 45, 11, 'O');        // gambá na direita da plataforma
        Set(g, H, 54, 11, 'C');        // fruta facil (centro de 51-57)

        // Faixa de espinhos antes do checkpoint: pula da pontinha por cima deles.
        for (int x = 66; x <= 69; x++)
        {
            Set(g, H, x, 7, '^');
        }
        Set(g, H, 73, 7, 'K');         // checkpoint

        return ToRows(g);
    }

    // Fase 2: plataformas separadas por buracos. Comeco simetrico com bola
    // balancando no buraco do meio; um inimigo por plataforma (nunca dois
    // juntos); cabecas e bolas sempre centradas nos buracos.
    private string[] BuildCaves()
    {
        const int W = 87;
        const int H = 18;

        char[][] g = NewGrid(W, H);

        AddTerrain(g, H, 0, 13, 10);   // L: plataforma inicial (esquerda)
        AddTerrain(g, H, 19, 32, 10);  // R: plataforma inicial (direita) - simetrica
        AddTerrain(g, H, 38, 49, 10);  // P3
        AddTerrain(g, H, 55, 66, 10);  // P4
        AddTerrain(g, H, 72, 86, 8);   // fim (espinhos + checkpoint)

        Set(g, H, 2, 11, 'P');         // player na esquerda
        Set(g, H, 7, 11, 'C');         // fruta na plataforma inicial esq
        Set(g, H, 11, 11, 'E');        // inimigo na direita da plataforma esq

        Set(g, H, 16, 16, 'S');        // bola balancando no buraco do meio (centro)

        Set(g, H, 26, 11, 'C');        // fruta na plataforma inicial dir
        Set(g, H, 30, 11, 'O');        // inimigo na direita

        Set(g, H, 35, 15, 'H');        // cabeca centrada no buraco (33-37, centro 35)

        Set(g, H, 43, 11, 'C');        // fruta P3
        Set(g, H, 47, 11, 'E');        // inimigo na direita de P3

        Set(g, H, 52, 16, 'S');        // bola no buraco (50-54, centro 52)

        Set(g, H, 60, 11, 'C');        // fruta P4
        Set(g, H, 64, 11, 'O');        // inimigo na direita de P4

        Set(g, H, 69, 15, 'H');        // cabeca centrada no buraco (67-71, centro 69)

        for (int x = 76; x <= 79; x++)
        {
            Set(g, H, x, 9, '^');      // espinhos antes do checkpoint
        }
        Set(g, H, 83, 9, 'K');         // checkpoint

        return ToRows(g);
    }

    private char[][] NewGrid(int width, int height)
    {
        char[][] g = new char[height][];
        for (int r = 0; r < height; r++)
        {
            g[r] = new char[width];
            for (int x = 0; x < width; x++)
            {
                g[r][x] = '.';
            }
        }

        return g;
    }

    private string[] ToRows(char[][] g)
    {
        string[] rows = new string[g.Length];
        for (int r = 0; r < g.Length; r++)
        {
            rows[r] = new string(g[r]);
        }

        return rows;
    }

    private void AddTerrain(char[][] g, int height, int x0, int x1, int topWorldY)
    {
        for (int x = x0; x <= x1; x++)
        {
            for (int wy = 0; wy <= topWorldY; wy++)
            {
                g[height - 1 - wy][x] = 'X';
            }
        }
    }

    private void Set(char[][] g, int height, int x, int worldY, char c)
    {
        g[height - 1 - worldY][x] = c;
    }

    public void LoadLevel(int levelIndex)
    {
        m_CurrentLevel = levelIndex;
        m_DeathHandled = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        m_Rng = new System.Random(levelIndex * 7919 + 17);
        m_IsBoss = m_Levels[levelIndex] == null;

        if (m_IsBoss)
        {
            BuildBossArena();
        }
        else
        {
            BuildTileLevel(m_Levels[levelIndex]);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  FASES NORMAIS
    // ─────────────────────────────────────────────────────────────────
    private void BuildTileLevel(string[] level)
    {
        m_Height = level.Length;
        m_Width = 0;
        for (int r = 0; r < level.Length; r++)
        {
            m_Width = Mathf.Max(m_Width, level[r].Length);
        }

        m_KillY = -4f;

        HashSet<Vector2Int> solids = new HashSet<Vector2Int>();
        bool[,] standable = new bool[m_Width, m_Height];

        for (int row = 0; row < m_Height; row++)
        {
            string line = level[row];
            int wy = m_Height - 1 - row;
            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];
                if (c == 'X' || c == '#')
                {
                    solids.Add(new Vector2Int(x, wy));
                    standable[x, wy] = true;
                }
                else if (c == '=')
                {
                    standable[x, wy] = true;
                }
            }
        }

        BuildSolids(solids);
        BuildOneWayPlatforms(level);

        GameObject spawnedPlayer = null;

        for (int row = 0; row < m_Height; row++)
        {
            string line = level[row];
            int wy = m_Height - 1 - row;

            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];
                switch (c)
                {
                    case '^':
                        SpawnSnapped(m_SpikePrefab, x, wy, standable, 0f);
                        break;
                    case 'C':
                        // Fruta tem muito espaco vazio no sprite; centraliza ~1
                        // tile acima da superficie pra ficar boiando e pegavel.
                        SpawnAt(RandomFruit(), x, SurfaceBelow(standable, x, wy) + 1.0f);
                        break;
                    case 'E':
                        SpawnSnapped(m_FrogPrefab, x, wy, standable, 0f);
                        break;
                    case 'O':
                        SpawnSnapped(m_OpossumPrefab, x, wy, standable, 0f);
                        break;
                    case 'H':
                        SpawnAt(m_FallingHeadPrefab, x, wy);
                        break;
                    case 'S':
                        SpawnAt(m_SwingPrefab, x, wy);
                        break;
                    case 'K':
                        SpawnSnapped(m_CheckpointPrefab, x, wy, standable, 0f);
                        break;
                    case 'P':
                        spawnedPlayer = SpawnSnapped(m_PlayerPrefab, x, wy, standable, 0f);
                        break;
                }
            }
        }

        ScatterProps(solids, standable);
        CreateBackground(m_BackgroundSprite, false);

        if (spawnedPlayer != null)
        {
            SetupPlayer(spawnedPlayer, false);
            SnapCameraToPlayer();
        }
    }

    private void BuildSolids(HashSet<Vector2Int> solids)
    {
        GameObject solidsRoot = new GameObject("Solids");
        solidsRoot.transform.SetParent(transform, false);

        Rigidbody2D body = solidsRoot.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite = solidsRoot.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            solidsRoot.layer = groundLayer;
        }

        foreach (Vector2Int cell in solids)
        {
            GameObject tile = Instantiate(m_SolidPrefab, new Vector3(cell.x, cell.y, 0f), Quaternion.identity);
            tile.transform.SetParent(solidsRoot.transform, true);
            if (groundLayer >= 0)
            {
                tile.layer = groundLayer;
            }

            BoxCollider2D col = tile.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.usedByComposite = true;
            }

            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite tileSprite = PickGrass(solids, cell.x, cell.y);
                sr.sprite = tileSprite;
                sr.sortingOrder = OrderTile;

                // So a fileira de cima (sem solido em cima) ganha sombra; o resto
                // e terra enterrada (sombra invisivel) - economiza objetos.
                if (!solids.Contains(new Vector2Int(cell.x, cell.y + 1)))
                {
                    AddShadow(tile.transform, tileSprite, OrderShadow, new Vector3(0.12f, -0.12f, 0f), 1f, 0.22f);
                }
            }
        }

        composite.GenerateGeometry();
    }

    private Sprite PickGrass(HashSet<Vector2Int> solids, int x, int y)
    {
        if (m_GrassTiles == null || m_GrassTiles.Length < 9)
        {
            return null;
        }

        bool up = solids.Contains(new Vector2Int(x, y + 1));
        bool down = solids.Contains(new Vector2Int(x, y - 1));
        bool left = solids.Contains(new Vector2Int(x - 1, y));
        bool right = solids.Contains(new Vector2Int(x + 1, y));

        int rowIdx = !up ? 0 : (!down ? 2 : 1);
        int colIdx = (!left && right) ? 0 : (left && !right ? 2 : 1);

        return m_GrassTiles[rowIdx * 3 + colIdx];
    }

    // Junta sequencias horizontais de "=" numa unica plataforma com 1 colisor
    // (sem emendas) e efeito de mao-unica.
    private void BuildOneWayPlatforms(string[] level)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");

        for (int row = 0; row < m_Height; row++)
        {
            string line = level[row];
            int wy = m_Height - 1 - row;
            int x = 0;

            while (x < line.Length)
            {
                if (line[x] != '=')
                {
                    x++;
                    continue;
                }

                int startX = x;
                while (x < line.Length && line[x] == '=')
                {
                    x++;
                }

                int run = x - startX;
                CreateOneWayPlatform(startX, wy, run, groundLayer);
            }
        }
    }

    private void CreateOneWayPlatform(int startX, int wy, int run, int groundLayer)
    {
        GameObject platform = new GameObject("Platform");
        platform.transform.SetParent(transform, false);
        float centerX = startX + (run - 1) * 0.5f;
        platform.transform.position = new Vector3(centerX, wy, 0f);
        if (groundLayer >= 0)
        {
            platform.layer = groundLayer;
        }

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
        col.size = new Vector2(run, 0.9f);
        col.usedByEffector = true;

        PlatformEffector2D effector = platform.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 160f;
        col.usedByEffector = true;

        for (int i = 0; i < run; i++)
        {
            int colIdx = run == 1 ? 1 : (i == 0 ? 0 : (i == run - 1 ? 2 : 1));
            Sprite tile = (m_GrassTiles != null && m_GrassTiles.Length >= 9) ? m_GrassTiles[colIdx] : null;

            GameObject visual = new GameObject("PlatTile");
            visual.transform.SetParent(platform.transform, false);
            visual.transform.localPosition = new Vector3(startX + i - centerX, 0f, 0f);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = tile;
            sr.sortingOrder = OrderTile;
            AddShadow(visual.transform, tile, OrderShadow, new Vector3(0.12f, -0.12f, 0f), 1f, 0.22f);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  CHEFE (FASE 3)
    // ─────────────────────────────────────────────────────────────────
    private void BuildBossArena()
    {
        m_Width = 24;
        m_Height = 15;
        m_KillY = -3f;

        // Plataforma principal pequena, solida, embaixo no centro.
        HashSet<Vector2Int> solids = new HashSet<Vector2Int>();
        for (int x = 9; x <= 15; x++)
        {
            solids.Add(new Vector2Int(x, 3));
        }
        BuildSolids(solids);

        // 3 niveis pra subir (pulo duplo so aqui): meio (laterais) e topo (centro).
        BuildBossPlatform(2, 6, 6);    // lateral esquerda (meio)
        BuildBossPlatform(18, 22, 6);  // lateral direita (meio)
        BuildBossPlatform(9, 15, 9);   // central (topo)

        CreateBackground(m_SkySprite != null ? m_SkySprite : m_BackgroundSprite, true);

        // Player no centro da plataforma principal.
        GameObject player = SpawnSnapped2(m_PlayerPrefab, 12f, 3.5f);
        if (player != null)
        {
            SetupPlayer(player, true); // pulo duplo so aqui
        }

        // Aguia: dispara em 3 alturas (uma por nivel) e descansa no centro.
        if (m_EaglePrefab != null)
        {
            GameObject eagleObj = SpawnAt(m_EaglePrefab, 12f, 11f);
            EagleBoss eagle = eagleObj.GetComponent<EagleBoss>();
            if (eagle != null)
            {
                float[] rows = { 5f, 8f, 11f };
                eagle.Setup(0f, 23f, rows, new Vector2(12f, 5.6f));
            }
        }

        // Camera fixa, longe o suficiente pra ver a arena inteira (qualquer aspect).
        if (Camera.main != null)
        {
            float aspect = Camera.main.aspect;
            float sizeForWidth = (m_Width * 0.5f + 1f) / Mathf.Max(0.2f, aspect);
            float sizeForHeight = m_Height * 0.5f + 1f;
            Camera.main.orthographicSize = Mathf.Max(sizeForWidth, sizeForHeight);
            m_FixedCameraPos = new Vector3(m_Width * 0.5f, m_Height * 0.5f, -10f);
            Camera.main.transform.position = m_FixedCameraPos;
        }
    }

    // Plataforma do chefe: atravessavel por baixo (PlatformEffector), firme em cima.
    private void BuildBossPlatform(int x0, int x1, int wy)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        CreateOneWayPlatform(x0, wy, x1 - x0 + 1, groundLayer);
    }

    // Como SpawnSnapped mas sem mapa de superficie: encaixa a base do colisor
    // dinamico (player) na altura "surface".
    private GameObject SpawnSnapped2(GameObject prefab, float x, float surface)
    {
        GameObject obj = SpawnAt(prefab, x, surface);
        if (obj == null)
        {
            return null;
        }

        BoxCollider2D box = obj.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            float scaleY = obj.transform.lossyScale.y;
            float worldBottom = obj.transform.position.y + (box.offset.y - box.size.y * 0.5f) * scaleY;
            obj.transform.position += new Vector3(0f, surface - worldBottom, 0f);
        }

        return obj;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Transicoes de fase / morte
    // ─────────────────────────────────────────────────────────────────
    public void GoToNextLevel()
    {
        int next = (m_CurrentLevel + 1) % m_Levels.Count;
        string message = "Fase " + (next + 1);
        LevelTransition.GetOrCreate().Play(() => LoadLevel(next), message);
    }

    // Caiu no vazio mas ainda tem vida: reinicia a fase atual.
    public void RestartCurrentLevel()
    {
        LevelTransition transition = LevelTransition.GetOrCreate();
        if (transition.IsBusy)
        {
            return;
        }

        transition.Play(() => LoadLevel(m_CurrentLevel), null);
    }

    // Vidas acabaram. No chefe a luta reinicia (vidas cheias); no resto, volta
    // pra fase 1 zerando o run.
    public void OnPlayerDeath()
    {
        LevelTransition transition = LevelTransition.GetOrCreate();
        if (transition.IsBusy)
        {
            return;
        }

        if (m_IsBoss)
        {
            transition.Play(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RefillLives();
                }
                LoadLevel(m_CurrentLevel);
            }, "A aguia venceu! De novo...");
        }
        else
        {
            transition.Play(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResetRun();
                }
                LoadLevel(0);
            }, "Voce perdeu! Fase 1");
        }
    }

    public void OnBossDefeated()
    {
        LevelTransition.GetOrCreate().Play(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetRun();
            }
            LoadLevel(0);
        }, "Voce venceu!");
    }

    // ─────────────────────────────────────────────────────────────────
    //  Cenario / camera / utilidades
    // ─────────────────────────────────────────────────────────────────
    private void SetupPlayer(GameObject player, bool boss)
    {
        m_PlayerTransform = player.transform;
        m_PlayerHealth = player.GetComponent<PlayerHealth>();

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SetDoubleJump(boss);
        }
    }

    // Fundo "back": uma imagem que acompanha a camera e cobre a tela (melhor
    // que o tile repetido). Reposicionado/escalado em LateUpdate.
    private void CreateBackground(Sprite sprite, bool boss)
    {
        if (sprite == null)
        {
            m_BgTransform = null;
            m_BgRenderer = null;
            return;
        }

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(transform, false);

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = OrderBackground;

        m_BgTransform = bg.transform;
        m_BgRenderer = sr;
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (m_BgTransform == null || m_BgRenderer == null || m_BgRenderer.sprite == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Sprite s = m_BgRenderer.sprite;
        float spriteW = s.rect.width / s.pixelsPerUnit;
        float spriteH = s.rect.height / s.pixelsPerUnit;
        float scale = Mathf.Max((halfW * 2f + 1f) / spriteW, (halfH * 2f + 1f) / spriteH);
        m_BgTransform.localScale = new Vector3(scale, scale, 1f);

        Vector3 camPos = cam.transform.position;
        m_BgTransform.position = new Vector3(camPos.x, camPos.y, 0f);
    }

    // Arvores so no chao de terra (nao em plataformas). Placa (com caveira)
    // perto de penhascos. Arbustos/cogumelos espalhados pelo chao.
    private void ScatterProps(HashSet<Vector2Int> solids, bool[,] standable)
    {
        int[] solidTop = new int[m_Width];
        int[] standTop = new int[m_Width];
        for (int x = 0; x < m_Width; x++)
        {
            solidTop[x] = -1;
            standTop[x] = -1;
            for (int y = m_Height - 1; y >= 0; y--)
            {
                if (standTop[x] < 0 && standable[x, y])
                {
                    standTop[x] = y;
                }
                if (solids.Contains(new Vector2Int(x, y)))
                {
                    solidTop[x] = y;
                    break;
                }
            }
        }

        int nextX = 2;
        for (int x = 1; x < m_Width - 1; x++)
        {
            if (solidTop[x] < 0)
            {
                continue;
            }

            bool cliff = solidTop[x - 1] < 0 || solidTop[x + 1] < 0;
            if (cliff && m_SignSprite != null)
            {
                // Placa um pouco pra dentro da beirada.
                PlaceProp(m_SignSprite, x, solidTop[x] + 0.5f, OrderGroundProp, 1f);
                nextX = x + 3;
                continue;
            }

            if (x < nextX)
            {
                continue;
            }

            bool terrainTop = standTop[x] == solidTop[x]; // topo e terra, nao plataforma
            if (!terrainTop)
            {
                continue;
            }

            double roll = m_Rng.NextDouble();
            if (roll < 0.35 && m_TreeSprites != null && m_TreeSprites.Length > 0)
            {
                Sprite tree = m_TreeSprites[m_Rng.Next(m_TreeSprites.Length)];
                PlaceProp(tree, x, solidTop[x] + 0.5f, OrderTree, 0.7f);
            }
            else if (roll < 0.75 && m_GroundProps != null && m_GroundProps.Length > 0)
            {
                Sprite gp = m_GroundProps[m_Rng.Next(m_GroundProps.Length)];
                PlaceProp(gp, x + (float)(m_Rng.NextDouble() * 0.3 - 0.15), solidTop[x] + 0.5f, OrderGroundProp, 1f);
            }

            nextX = x + 4 + m_Rng.Next(5);
        }
    }

    private void PlaceProp(Sprite sprite, float x, float surfaceY, int order, float scale)
    {
        if (sprite == null)
        {
            return;
        }

        GameObject prop = new GameObject("Prop");
        prop.transform.SetParent(transform, false);
        prop.transform.localScale = Vector3.one * scale;

        float h = (sprite.rect.height / sprite.pixelsPerUnit) * scale;
        prop.transform.position = new Vector3(x, surfaceY + h * 0.5f - 0.05f, 0f);

        SpriteRenderer sr = prop.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
    }

    private void AddShadow(Transform parent, Sprite sprite, int order, Vector3 offset, float scale, float alpha)
    {
        if (sprite == null)
        {
            return;
        }

        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = offset;
        shadow.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0f, 0f, 0f, alpha);
        sr.sortingOrder = order;
    }

    private float SurfaceBelow(bool[,] standable, int x, int fromY)
    {
        for (int y = fromY - 1; y >= 0; y--)
        {
            if (x >= 0 && x < m_Width && standable[x, y])
            {
                return y + 0.5f;
            }
        }

        return -0.5f;
    }

    private GameObject RandomFruit()
    {
        if (m_FruitPrefabs == null || m_FruitPrefabs.Length == 0)
        {
            return null;
        }

        return m_FruitPrefabs[m_Rng.Next(m_FruitPrefabs.Length)];
    }

    // Encosta a base do objeto na superficie e depois sobe "extraOffset" (ex.:
    // fruta flutuando). Inimigos/player (corpo dinamico) alinham pela base do
    // colisor (param exato, sem flutuar); o resto alinha pela base do sprite.
    private GameObject SpawnSnapped(GameObject prefab, int x, int wy, bool[,] standable, float extraOffset)
    {
        if (prefab == null)
        {
            return null;
        }

        float surface = SurfaceBelow(standable, x, wy);
        GameObject obj = SpawnAt(prefab, x, surface);
        float scaleY = obj.transform.lossyScale.y;

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        BoxCollider2D box = obj.GetComponent<BoxCollider2D>();
        float worldBottom;

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && box != null && !box.isTrigger)
        {
            worldBottom = obj.transform.position.y + (box.offset.y - box.size.y * 0.5f) * scaleY;
        }
        else
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            worldBottom = (sr != null && sr.sprite != null)
                ? obj.transform.position.y + sr.sprite.bounds.min.y * scaleY
                : obj.transform.position.y;
        }

        obj.transform.position += new Vector3(0f, surface - worldBottom + extraOffset, 0f);
        return obj;
    }

    private GameObject SpawnAt(GameObject prefab, float x, float y)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject obj = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity);
        obj.transform.SetParent(transform, true);
        return obj;
    }

    private void LateUpdate()
    {
        if (m_PlayerTransform == null)
        {
            return;
        }

        if (!m_IsBoss)
        {
            SnapCameraToPlayer();
        }

        UpdateBackground();

        if (!m_DeathHandled && m_PlayerTransform.position.y < m_KillY)
        {
            m_DeathHandled = true;
            if (m_PlayerHealth != null)
            {
                m_PlayerHealth.VoidFall();
            }
            else
            {
                RestartCurrentLevel();
            }
        }
    }

    private void SnapCameraToPlayer()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        if (m_CamSizeCaptured)
        {
            cam.orthographicSize = m_DefaultCamSize;
        }

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3 target = m_PlayerTransform.position + m_CameraOffset;

        float minX = halfW;
        float maxX = m_Width - halfW;
        target.x = minX <= maxX ? Mathf.Clamp(target.x, minX, maxX) : m_Width * 0.5f;

        // Base da camera nunca desce abaixo de worldY 0 (o terreno e fundo ate
        // la), entao nunca da pra ver onde as plataformas "terminam".
        float minY = halfH;
        float maxY = m_Height - halfH;
        target.y = minY <= maxY ? Mathf.Clamp(target.y, minY, maxY) : m_Height * 0.5f;

        target.z = m_CameraOffset.z;
        cam.transform.position = target;
    }
}
