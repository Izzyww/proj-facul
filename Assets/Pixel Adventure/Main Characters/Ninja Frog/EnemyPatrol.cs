using UnityEngine;

// Inimigo que anda, vira na parede e na beirada do chao. Encostar de lado
// causa dano; pular em cima (estilo Mario) mata o inimigo e quica o player.
public class EnemyPatrol : MonoBehaviour
{
    public Sprite[] m_DeathFrames;

    private float m_PatrolSpeed = 2f;

    private Rigidbody2D m_Rigidbody2D;
    private Transform m_Transform;
    private LayerMask m_GroundLayer;
    private int m_Direction = 1;

    private void Awake()
    {
        m_Transform = transform;
        m_GroundLayer = LayerMask.GetMask("Ground");

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
        m_Direction = m_Transform.localScale.x >= 0f ? 1 : -1;
    }

    private void FixedUpdate()
    {
        Vector2 pos = m_Transform.position;
        Vector2 dir = Vector2.right * m_Direction;

        Vector2 wallOrigin = pos + dir * 0.5f + Vector2.up * (-0.3f);
        Vector2 ledgeOrigin = pos + dir * 0.6f + Vector2.up * (-0.95f);

        bool wallAhead = Physics2D.Raycast(wallOrigin, dir, 0.3f, m_GroundLayer).collider != null;
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

        Vector3 scale = m_Transform.localScale;
        scale.x = Mathf.Abs(scale.x) * m_Direction;
        m_Transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D playerBody = collision.collider.GetComponent<Rigidbody2D>();
        bool fromAbove = collision.collider.transform.position.y - m_Transform.position.y > 0.55f;
        bool falling = playerBody == null || playerBody.linearVelocity.y < 1f;

        if (fromAbove && falling)
        {
            Stomped(playerBody);
        }
        else
        {
            PlayerHealth health = collision.collider.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(m_Transform.position);
            }
        }
    }

    private void Stomped(Rigidbody2D playerBody)
    {
        OneShotVfx.Spawn(m_Transform.position, m_DeathFrames, 14f, 0.5f, Vector2.zero, 1, 1f);

        if (playerBody != null)
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 12f);
        }

        Destroy(gameObject);
    }
}
