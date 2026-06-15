using UnityEngine;

// Inimigo que patrulha andando de um lado pro outro. Vira quando encontra uma
// parede/lateral de plataforma na altura do corpo (nao atravessa) e na beirada
// (nao cai). Encostar de lado machuca o player; pular em cima OU rolar nele mata.
// m_FacesLeft = true quando a arte original olha pra esquerda (ex: o gambá),
// pra ele nao andar "de costas".
public class EnemyPatrol : MonoBehaviour
{
    public Sprite[] m_DeathFrames;
    public float m_PatrolSpeed = 2f;
    public bool m_FacesLeft;

    private Rigidbody2D m_Rigidbody2D;
    private Collider2D m_Collider;
    private Transform m_Transform;
    private LayerMask m_GroundLayer;
    private int m_Direction = 1;
    private bool m_Dead;

    private int BaseSign => m_FacesLeft ? -1 : 1;

    private void Awake()
    {
        m_Transform = transform;
        m_GroundLayer = LayerMask.GetMask("Ground");
        m_Collider = GetComponent<Collider2D>();

        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        if (m_Rigidbody2D == null)
        {
            m_Rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
        }
        m_Rigidbody2D.freezeRotation = true;
        m_Rigidbody2D.gravityScale = 3f;
    }

    private void Start()
    {
        m_Direction = 1;
        ApplyFacing();
    }

    private void FixedUpdate()
    {
        if (m_Dead)
        {
            return;
        }

        Bounds b = m_Collider != null ? m_Collider.bounds : new Bounds(m_Transform.position, Vector3.one);
        Vector2 dir = Vector2.right * m_Direction;
        float halfW = b.extents.x + 0.05f;

        Vector2 bodyOrigin = new Vector2(b.center.x, b.min.y + 0.25f);
        bool wallAhead = Physics2D.Raycast(bodyOrigin, dir, halfW + 0.15f, m_GroundLayer).collider != null;

        Vector2 ledgeOrigin = new Vector2(b.center.x + m_Direction * (halfW + 0.1f), b.min.y + 0.1f);
        bool groundAhead = Physics2D.Raycast(ledgeOrigin, Vector2.down, 0.6f, m_GroundLayer).collider != null;

        if (wallAhead || !groundAhead)
        {
            Flip();
        }

        m_Rigidbody2D.linearVelocity = new Vector2(m_Direction * m_PatrolSpeed, m_Rigidbody2D.linearVelocity.y);
    }

    private void Flip()
    {
        m_Direction *= -1;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        Vector3 scale = m_Transform.localScale;
        scale.x = Mathf.Abs(scale.x) * m_Direction * BaseSign;
        m_Transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandlePlayer(collision.collider);
    }

    private void HandlePlayer(Collider2D other)
    {
        if (m_Dead || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerController controller = other.GetComponent<PlayerController>();
        Rigidbody2D playerBody = other.attachedRigidbody;

        if (controller != null && controller.IsRolling)
        {
            Die(null);
            return;
        }

        float enemyMidY = m_Collider != null ? m_Collider.bounds.center.y : m_Transform.position.y;
        bool fromAbove = other.bounds.min.y >= enemyMidY;
        bool falling = playerBody == null || playerBody.linearVelocity.y < 1f;

        if (fromAbove && falling)
        {
            Die(playerBody);
            return;
        }

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(m_Transform.position);
        }
    }

    private void Die(Rigidbody2D playerBody)
    {
        m_Dead = true;
        OneShotVfx.Spawn(m_Transform.position, m_DeathFrames, 14f, 0.5f, Vector2.zero, 1, 1f);

        if (playerBody != null)
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 12f);
        }

        Destroy(gameObject);
    }
}
