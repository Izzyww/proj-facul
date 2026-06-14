using UnityEngine;

// Solta pequenas nuvens de poeira nos pes do player enquanto corre no chao.
public class RunDust : MonoBehaviour
{
    [SerializeField] private Sprite m_DustSprite;
    [SerializeField] private Transform m_FeetPoint;

    private LayerMask m_GroundLayer;
    private Rigidbody2D m_Rigidbody2D;
    private float m_Timer;
    private readonly float m_Interval = 0.12f;

    private void Awake()
    {
        m_GroundLayer = LayerMask.GetMask("Ground");
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (m_DustSprite == null)
        {
            return;
        }

        bool grounded = Physics2D.OverlapCircle(FeetPosition(), 0.25f, m_GroundLayer);
        bool moving = Mathf.Abs(m_Rigidbody2D.linearVelocity.x) > 1.5f;

        if (!grounded || !moving)
        {
            return;
        }

        m_Timer += Time.deltaTime;
        if (m_Timer >= m_Interval)
        {
            m_Timer = 0f;
            Sprite[] frames = { m_DustSprite };
            float drift = -Mathf.Sign(m_Rigidbody2D.linearVelocity.x) * 0.6f;
            OneShotVfx.Spawn(FeetPosition(), frames, 0f, 0.35f, new Vector2(drift, 0.4f), 8, 1.2f);
        }
    }

    private Vector3 FeetPosition()
    {
        return m_FeetPoint != null ? m_FeetPoint.position : transform.position + Vector3.down * 0.6f;
    }
}
