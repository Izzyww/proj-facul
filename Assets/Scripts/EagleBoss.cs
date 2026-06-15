using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Chefe da fase 3. A aguia aparece numa ponta de uma "linha" (altura), espera
// ~1,5s telegrafando, e DISPARA bem rapido pro outro lado. Depois sobe/desce uma
// linha e repete. Apos algumas investidas ela descansa no meio (3s), e so AI da
// pra acerta-la pulando em cima. Precisa de 10 acertos. Em voo, encostar machuca.
public class EagleBoss : MonoBehaviour
{
    private enum BossState { Telegraph, Dashing, Resting, Dead }

    public bool m_FacesLeft = true; // a arte da aguia olha pra esquerda

    private float m_LeftX;
    private float m_RightX;
    private float m_MinY;
    private float m_MaxY;
    private Vector2 m_RestPosition;

    private readonly float m_DashSpeed = 28f;
    private readonly float m_FollowSpeed = 9f; // o quao rapido sobe/desce te seguindo
    private readonly float m_TelegraphTime = 1.5f;
    private readonly float m_RestDuration = 3f;
    private readonly int m_DashesBeforeRest = 5;
    private readonly int m_MaxHits = 10;

    private BossState m_State = BossState.Telegraph;
    private int m_Hits;
    private float m_StateTimer;

    private int m_DashDir = 1;
    private bool m_AtRightSide;
    private int m_DashesDone;
    private float m_DashY;

    private Transform m_PlayerTransform;
    private SpriteRenderer m_SpriteRenderer;
    private readonly List<Image> m_HpPips = new List<Image>();

    public void Setup(float leftX, float rightX, float[] rowYs, Vector2 restPosition)
    {
        m_LeftX = leftX;
        m_RightX = rightX;
        m_RestPosition = restPosition;

        m_MinY = float.MaxValue;
        m_MaxY = float.MinValue;
        if (rowYs != null)
        {
            foreach (float y in rowYs)
            {
                m_MinY = Mathf.Min(m_MinY, y);
                m_MaxY = Mathf.Max(m_MaxY, y);
            }
        }

        if (m_MinY > m_MaxY)
        {
            m_MinY = 5f;
            m_MaxY = 11f;
        }
    }

    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        BuildHpBar();
        m_AtRightSide = false;
        BeginTelegraph();
    }

    private Transform Player()
    {
        if (m_PlayerTransform == null)
        {
            if (PlayerController.Current != null)
            {
                m_PlayerTransform = PlayerController.Current.transform;
            }
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    m_PlayerTransform = p.transform;
                }
            }
        }

        return m_PlayerTransform;
    }

    private void Update()
    {
        switch (m_State)
        {
            case BossState.Telegraph:
                TelegraphUpdate();
                break;
            case BossState.Dashing:
                DashUpdate();
                break;
            case BossState.Resting:
                RestUpdate();
                break;
        }
    }

    // ── Telegrafa antes do dash (seguindo o player) ───────────────────
    private void BeginTelegraph()
    {
        m_State = BossState.Telegraph;
        m_StateTimer = m_TelegraphTime;

        m_DashDir = m_AtRightSide ? -1 : 1;
        float x = m_AtRightSide ? m_RightX - 0.5f : m_LeftX + 0.5f; // na beirada, mas visivel
        transform.position = new Vector3(x, transform.position.y, 0f);
        Face(m_DashDir);

        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.color = Color.white;
        }
    }

    private void TelegraphUpdate()
    {
        // Enquanto se prepara, persegue a altura do player (preso na arena).
        float x = m_AtRightSide ? m_RightX - 0.5f : m_LeftX + 0.5f;
        Transform player = Player();
        float targetY = player != null ? Mathf.Clamp(player.position.y, m_MinY, m_MaxY) : transform.position.y;

        float y = Mathf.MoveTowards(transform.position.y, targetY, m_FollowSpeed * Time.deltaTime);
        float shake = Mathf.Sin(Time.time * 40f) * 0.05f; // tremidinha de aviso
        transform.position = new Vector3(x, y + shake, 0f);

        m_StateTimer -= Time.deltaTime;
        if (m_StateTimer <= 0f)
        {
            m_DashY = y; // trava a altura: a partir daqui NAO segue mais
            m_State = BossState.Dashing;
        }
    }

    // ── Dash rapidao (reto, sem seguir) ───────────────────────────────
    private void DashUpdate()
    {
        float x = transform.position.x + m_DashDir * m_DashSpeed * Time.deltaTime;
        transform.position = new Vector3(x, m_DashY, 0f);

        bool past = m_DashDir > 0 ? x > m_RightX + 1.5f : x < m_LeftX - 1.5f;
        if (!past)
        {
            return;
        }

        m_AtRightSide = m_DashDir > 0;
        m_DashesDone++;

        if (m_DashesDone >= m_DashesBeforeRest)
        {
            BeginResting();
            return;
        }

        BeginTelegraph();
    }

    // ── Descanso (vulneravel) ─────────────────────────────────────────
    private void BeginResting()
    {
        m_State = BossState.Resting;
        m_StateTimer = m_RestDuration;
        m_DashesDone = 0;
        transform.position = m_RestPosition;
        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.color = new Color(1f, 0.9f, 0.6f); // dourado: vulneravel
        }
    }

    private void RestUpdate()
    {
        float bob = Mathf.Sin(Time.time * 4f) * 0.12f;
        transform.position = new Vector3(m_RestPosition.x, m_RestPosition.y + bob, 0f);

        m_StateTimer -= Time.deltaTime;
        if (m_StateTimer <= 0f)
        {
            BeginTelegraph();
        }
    }

    private void Face(int dir)
    {
        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.flipX = m_FacesLeft ? dir > 0 : dir < 0;
        }
    }

    // ── Colisao com o player ──────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandlePlayer(other);
    }

    private void HandlePlayer(Collider2D other)
    {
        if (m_State == BossState.Dead || !other.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D playerBody = other.attachedRigidbody;
        bool fromAbove = other.transform.position.y - transform.position.y > 0.4f;
        bool falling = playerBody == null || playerBody.linearVelocity.y < 1f;

        if (m_State == BossState.Resting)
        {
            if (fromAbove && falling)
            {
                TakeHit(playerBody);
            }
            return;
        }

        // Voando (telegrafando ou dando dash): encostar machuca.
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(transform.position);
        }
    }

    private void TakeHit(Rigidbody2D playerBody)
    {
        m_Hits++;
        RefreshHpBar();

        if (playerBody != null)
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 13f);
        }

        if (m_Hits >= m_MaxHits)
        {
            Defeat();
        }
        else
        {
            m_StateTimer = Mathf.Min(m_StateTimer, 0.35f); // reage e volta a voar
        }
    }

    private void Defeat()
    {
        m_State = BossState.Dead;
        if (LevelGenerator.Instance != null)
        {
            LevelGenerator.Instance.OnBossDefeated();
        }

        Destroy(gameObject);
    }

    // ── HUD do chefe (pips no topo) ───────────────────────────────────
    private void BuildHpBar()
    {
        GameObject canvasObj = new GameObject("BossHpCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 850;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject row = new GameObject("BossHp");
        row.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < m_MaxHits; i++)
        {
            GameObject pip = new GameObject("Pip" + i);
            pip.transform.SetParent(row.transform, false);
            Image img = pip.AddComponent<Image>();
            img.color = new Color(0.85f, 0.2f, 0.25f);
            img.rectTransform.sizeDelta = new Vector2(20f, 20f);
            m_HpPips.Add(img);
        }

        RefreshHpBar();
    }

    private void RefreshHpBar()
    {
        for (int i = 0; i < m_HpPips.Count; i++)
        {
            bool remaining = i < (m_MaxHits - m_Hits);
            m_HpPips[i].color = remaining ? new Color(0.85f, 0.2f, 0.25f) : new Color(0.2f, 0.2f, 0.22f, 0.8f);
        }
    }

    private void OnDestroy()
    {
        if (m_HpPips.Count > 0 && m_HpPips[0] != null)
        {
            Canvas c = m_HpPips[0].GetComponentInParent<Canvas>();
            if (c != null)
            {
                Destroy(c.gameObject);
            }
        }
    }
}
