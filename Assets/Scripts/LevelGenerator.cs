using System.Collections.Generic;
using UnityEngine;

// Gera os niveis a partir de mapas em texto, com autotiling do chao de grama
// (cantos arredondados + terra por baixo), sombras, fundo grande, camera presa
// dentro do nivel, transicao em circulo e queda mortal nos buracos.
//
// Legenda:
//   X (ou # B) solido (grama, autotile)   ^ espinho (dano)
//   C fruta (flutua, coleta)              E inimigo
//   P spawn do player                     F bandeira de fim
//   . vazio
//
// Preencha tudo pelo menu "Tools/Level/Construir Nivel (Auto-Setup)".
public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    [Header("Prefabs / sprites (preenchidos pelo builder)")]
    public GameObject m_SolidPrefab;
    public Sprite[] m_GrassTiles;       // 9: TL,T,TR, ML,M,MR, BL,B,BR
    public GameObject m_SpikePrefab;
    public GameObject m_FruitPrefab;
    public GameObject m_FlagPrefab;
    public GameObject m_EnemyPrefab;
    public GameObject m_PlayerPrefab;
    public Sprite m_BackgroundSprite;
    public Sprite m_ShadowSprite;

    private const int OrderBackground = -100;
    private const int OrderTileShadow = -60;
    private const int OrderTile = -50;

    private List<string[]> m_Levels;
    private int m_CurrentLevel;
    private int m_Width;
    private int m_Height;
    private Transform m_PlayerTransform;
    private PlayerHealth m_PlayerHealth;
    private bool m_FallHandled;

    private readonly Vector3 m_CameraOffset = new Vector3(4f, 0.5f, -10f);
    private readonly float m_KillY = -4f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BuildLevelData();
        LevelTransition.GetOrCreate();

        if (m_SolidPrefab == null)
        {
            Debug.LogWarning("LevelGenerator: rode 'Tools/Level/Construir Nivel (Auto-Setup)' antes de dar Play.");
        }

        LoadLevel(0);
    }

    private void BuildLevelData()
    {
        m_Levels = new List<string[]>();

        // ── FASE 1 – Floresta ─────────────────────────────────────────
        m_Levels.Add(new string[]
        {
            "........................................................",
            "........................................................",
            "...............CCC................CCC...................",
            "..............XXXXX............XXXXX.....................",
            "........CCC...........CCC...............CCC.............",
            ".......XXXXX..........XXXXX.............XXXXX............",
            "..P......E.....^^.............E........^.......E...^..F..",
            "XXXXXXXXXXXXXXXXXX..XXXXXXXXXXXXXXXX..XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX..XXXXXXXXXXXXXXXX..XXXXXXXXXXXXXXXXXX",
        });

        // ── FASE 2 – Colinas ──────────────────────────────────────────
        m_Levels.Add(new string[]
        {
            "........................................................",
            "...............CCC................CCC...................",
            "..............XXXXX............XXXXX.....................",
            "........CCC..........CCC................CCC.............",
            ".......XXXXX.........XXXXX.............XXXXX............",
            "...............^...........^...........................",
            "..P.....E....^^.....E....^^.....E.....^^....E.....F....",
            "XXXXXXXXXXXXXXXX..XXXXXXXXXXXX..XXXXXXXXXXXX..XXXXXXXXXX",
            "XXXXXXXXXXXXXXXX..XXXXXXXXXXXX..XXXXXXXXXXXX..XXXXXXXXXX",
        });

        // ── FASE 3 – Trilha difícil ───────────────────────────────────
        m_Levels.Add(new string[]
        {
            "...............CCC................CCC...................",
            "..............XXXXX............XXXXX.....................",
            ".........^...........^................^................",
            "........CCC..........CCC...............CCC.............",
            ".......XXXXX.........XXXXX.............XXXXX............",
            ".....^.......E....^.......E.......^.......E....^.......",
            "..P...E...^^...E...^^...E...^^...E...^^...E...^^...F...",
            "XXXXXXXXXXXXXX..XXXXXXXXXX..XXXXXXXXXX..XXXXXXXX..XXXXXX",
            "XXXXXXXXXXXXXX..XXXXXXXXXX..XXXXXXXXXX..XXXXXXXX..XXXXXX",
        });
    }

    public void LoadLevel(int levelIndex)
    {
        m_CurrentLevel = levelIndex;
        m_FallHandled = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        string[] level = m_Levels[levelIndex];
        m_Height = level.Length;
        m_Width = 0;
        for (int r = 0; r < level.Length; r++)
        {
            m_Width = Mathf.Max(m_Width, level[r].Length);
        }

        bool[,] solid = new bool[m_Width, m_Height];
        for (int row = 0; row < m_Height; row++)
        {
            string line = level[row];
            int worldY = m_Height - 1 - row;
            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];
                if (c == 'X' || c == '#' || c == 'B')
                {
                    solid[x, worldY] = true;
                }
            }
        }

        GameObject spawnedPlayer = null;

        for (int row = 0; row < m_Height; row++)
        {
            string line = level[row];
            int worldY = m_Height - 1 - row;

            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];

                switch (c)
                {
                    case 'X':
                    case '#':
                    case 'B':
                        SpawnSolid(x, worldY, solid);
                        break;
                    case '^':
                        SpawnAt(m_SpikePrefab, x, worldY);
                        break;
                    case 'C':
                        SpawnAt(m_FruitPrefab, x, worldY);
                        break;
                    case 'E':
                        SpawnAt(m_EnemyPrefab, x, worldY);
                        break;
                    case 'F':
                        SpawnAt(m_FlagPrefab, x, worldY + 0.9f);
                        break;
                    case 'P':
                        spawnedPlayer = SpawnAt(m_PlayerPrefab, x, worldY);
                        break;
                }
            }
        }

        CreateBackground();

        if (spawnedPlayer != null)
        {
            m_PlayerTransform = spawnedPlayer.transform;
            m_PlayerHealth = spawnedPlayer.GetComponent<PlayerHealth>();
            SnapCameraToPlayer();
        }
    }

    public void GoToNextLevel()
    {
        int next = (m_CurrentLevel + 1) % m_Levels.Count;
        string message = next == 0 ? "Voce zerou!" : "Fase " + (next + 1);
        LevelTransition.GetOrCreate().Play(() => LoadLevel(next), message);
    }

    public void KillPlayer()
    {
        LevelTransition transition = LevelTransition.GetOrCreate();
        if (transition.IsBusy)
        {
            return;
        }

        transition.Play(() => LoadLevel(m_CurrentLevel), null);
    }

    private void SpawnSolid(int x, int worldY, bool[,] solid)
    {
        if (m_SolidPrefab == null)
        {
            return;
        }

        GameObject obj = Instantiate(m_SolidPrefab, new Vector3(x, worldY, 0f), Quaternion.identity);
        obj.transform.SetParent(transform, true);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Sprite tile = PickGrass(x, worldY, solid);
            sr.sprite = tile;
            sr.sortingOrder = OrderTile;
            AddDropShadow(obj.transform, tile);
        }
    }

    private Sprite PickGrass(int x, int worldY, bool[,] solid)
    {
        if (m_GrassTiles == null || m_GrassTiles.Length < 9)
        {
            return null;
        }

        bool up = IsSolid(solid, x, worldY + 1);
        bool down = IsSolid(solid, x, worldY - 1);
        bool left = IsSolid(solid, x - 1, worldY);
        bool right = IsSolid(solid, x + 1, worldY);

        int rowIdx = !up ? 0 : (!down ? 2 : 1);
        int colIdx = (!left && right) ? 0 : (left && !right ? 2 : 1);

        return m_GrassTiles[rowIdx * 3 + colIdx];
    }

    private bool IsSolid(bool[,] solid, int x, int y)
    {
        if (x < 0 || y < 0 || x >= m_Width || y >= m_Height)
        {
            return false;
        }

        return solid[x, y];
    }

    private void AddDropShadow(Transform parent, Sprite sprite)
    {
        if (m_ShadowSprite == null && sprite == null)
        {
            return;
        }

        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = new Vector3(0.12f, -0.12f, 0f);

        SpriteRenderer sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0f, 0f, 0f, 0.22f);
        sr.sortingOrder = OrderTileShadow;
    }

    private void CreateBackground()
    {
        if (m_BackgroundSprite == null)
        {
            return;
        }

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(transform, false);
        bg.transform.position = new Vector3(m_Width * 0.5f, m_Height * 0.5f, 0f);

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = m_BackgroundSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(m_Width + 40f, m_Height + 40f);
        sr.sortingOrder = OrderBackground;
    }

    private void LateUpdate()
    {
        if (m_PlayerTransform == null)
        {
            return;
        }

        SnapCameraToPlayer();

        if (!m_FallHandled && m_PlayerTransform.position.y < m_KillY)
        {
            m_FallHandled = true;
            if (m_PlayerHealth != null)
            {
                m_PlayerHealth.Kill();
            }
            else
            {
                KillPlayer();
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

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3 target = m_PlayerTransform.position + m_CameraOffset;

        float minX = halfW;
        float maxX = m_Width - halfW;
        target.x = minX <= maxX ? Mathf.Clamp(target.x, minX, maxX) : m_Width * 0.5f;

        float minY = halfH - 1f;
        float maxY = m_Height - halfH;
        target.y = minY <= maxY ? Mathf.Clamp(target.y, minY, maxY) : m_Height * 0.5f;

        target.z = m_CameraOffset.z;
        cam.transform.position = target;
    }

    private GameObject SpawnAt(GameObject prefab, float x, float worldY)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject obj = Instantiate(prefab, new Vector3(x, worldY, 0f), Quaternion.identity);
        obj.transform.SetParent(transform, true);
        return obj;
    }
}
