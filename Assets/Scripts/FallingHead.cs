using UnityEngine;

// Cabeca de espinhos no estilo Thwomp. Ela se move SOZINHA num ciclo, sem
// detectar o player: pausa no topo, desce com tudo, pausa embaixo, sobe devagar
// e repete. E um colisor solido (da pra ficar/subir em cima dela). Encostar por
// baixo ou pela lateral machuca; em cima e seguro (a cabeca te leva pra cima).
[RequireComponent(typeof(Rigidbody2D))]
public class FallingHead : MonoBehaviour
{
    public float m_FallSpeed = 24f;
    public float m_RiseSpeed = 3f;
    public float m_TopPause = 0.8f;
    public float m_BottomPause = 0.45f;
    public float m_FallDistance = 6f;

    private enum HeadState { TopPause, Falling, BottomPause, Rising }

    private Rigidbody2D m_Rigidbody2D;
    private Collider2D m_Collider;
    private LayerMask m_GroundLayer;

    private float m_TopY;
    private float m_BottomY;
    private HeadState m_State = HeadState.TopPause;
    private float m_Timer;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_Rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        m_Rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        m_Collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        m_GroundLayer = LayerMask.GetMask("Ground");
        m_TopY = transform.position.y;

        float half = m_Collider != null ? m_Collider.bounds.extents.y : 0.5f;
        Vector2 castFrom = new Vector2(transform.position.x, transform.position.y - half - 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(castFrom, Vector2.down, m_FallDistance + 2f, m_GroundLayer);
        m_BottomY = hit.collider != null ? hit.point.y + half + 0.05f : m_TopY - m_FallDistance;

        m_State = HeadState.TopPause;
        m_Timer = m_TopPause;
    }

    private void FixedUpdate()
    {
        Vector2 pos = m_Rigidbody2D.position;

        switch (m_State)
        {
            case HeadState.TopPause:
                m_Timer -= Time.fixedDeltaTime;
                if (m_Timer <= 0f)
                {
                    m_State = HeadState.Falling;
                }
                break;

            case HeadState.Falling:
                pos.y = Mathf.MoveTowards(pos.y, m_BottomY, m_FallSpeed * Time.fixedDeltaTime);
                m_Rigidbody2D.MovePosition(pos);
                if (pos.y <= m_BottomY + 0.001f)
                {
                    m_State = HeadState.BottomPause;
                    m_Timer = m_BottomPause;
                }
                break;

            case HeadState.BottomPause:
                m_Timer -= Time.fixedDeltaTime;
                if (m_Timer <= 0f)
                {
                    m_State = HeadState.Rising;
                }
                break;

            case HeadState.Rising:
                pos.y = Mathf.MoveTowards(pos.y, m_TopY, m_RiseSpeed * Time.fixedDeltaTime);
                m_Rigidbody2D.MovePosition(pos);
                if (pos.y >= m_TopY - 0.001f)
                {
                    m_State = HeadState.TopPause;
                    m_Timer = m_TopPause;
                }
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Damage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Damage(collision.collider);
    }

    private void Damage(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        float midY = m_Collider != null ? m_Collider.bounds.center.y : transform.position.y;
        bool onTop = other.bounds.min.y >= midY;
        if (onTop)
        {
            return; // em cima e seguro: pode "pegar carona"
        }

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(transform.position);
        }
    }
}
