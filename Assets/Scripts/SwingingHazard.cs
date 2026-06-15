using UnityEngine;

// Obstaculo que balanca pendurado por uma corrente (pendulo). O ponto onde o
// prefab e colocado e o pivo (preso no ceu ou numa pedra). A bola/serra na
// ponta machuca o player. Corrente e bola sao montadas em tempo de execucao.
public class SwingingHazard : MonoBehaviour
{
    public Sprite m_ChainSprite;
    public Sprite m_BallSprite;
    public Sprite m_AnchorSprite; // pecinha no topo onde a corrente prende
    public float m_Length = 2.5f;
    public float m_MaxAngleDeg = 55f;
    public float m_Speed = 2f;
    public int m_ChainLinks = 5;
    public float m_PhaseOffset;
    public bool m_Spin; // serra gira no proprio eixo

    private Transform m_Ball;
    private Transform[] m_Links;
    private float m_LinkSpacing;

    private void Awake()
    {
        BuildVisuals();
    }

    private void BuildVisuals()
    {
        if (m_AnchorSprite != null)
        {
            GameObject anchor = new GameObject("Anchor");
            anchor.transform.SetParent(transform, false);
            SpriteRenderer anchorSr = anchor.AddComponent<SpriteRenderer>();
            anchorSr.sprite = m_AnchorSprite;
            anchorSr.sortingOrder = 0;
        }

        m_Links = new Transform[Mathf.Max(0, m_ChainLinks)];
        m_LinkSpacing = m_Length / Mathf.Max(1, m_ChainLinks);

        for (int i = 0; i < m_Links.Length; i++)
        {
            GameObject link = new GameObject("Link" + i);
            link.transform.SetParent(transform, false);
            SpriteRenderer sr = link.AddComponent<SpriteRenderer>();
            sr.sprite = m_ChainSprite;
            sr.sortingOrder = 1;
            m_Links[i] = link.transform;
        }

        GameObject ball = new GameObject("Ball");
        ball.transform.SetParent(transform, false);
        SpriteRenderer ballSr = ball.AddComponent<SpriteRenderer>();
        ballSr.sprite = m_BallSprite;
        ballSr.sortingOrder = 2;

        CircleCollider2D col = ball.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.55f;

        ball.AddComponent<Hazard>();
        m_Ball = ball.transform;
    }

    private void Update()
    {
        float angle = m_MaxAngleDeg * Mathf.Deg2Rad * Mathf.Sin(Time.time * m_Speed + m_PhaseOffset);
        Vector3 dir = new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f);

        if (m_Ball != null)
        {
            m_Ball.localPosition = dir * m_Length;
            if (m_Spin)
            {
                m_Ball.Rotate(0f, 0f, 360f * Time.deltaTime);
            }
        }

        if (m_Links != null)
        {
            for (int i = 0; i < m_Links.Length; i++)
            {
                m_Links[i].localPosition = dir * (m_LinkSpacing * (i + 0.5f));
            }
        }
    }
}
