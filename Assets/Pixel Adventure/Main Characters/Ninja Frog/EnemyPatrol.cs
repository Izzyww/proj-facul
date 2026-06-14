using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    private float m_PatrolSpeed = 2f;

    [SerializeField] private Transform m_FrontCheck;

    private Rigidbody2D m_Rigidbody2D;
    private Transform m_Transform;
    private LayerMask m_GroundLayer;
    private int m_Direction = 1;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_Transform = transform;
        m_GroundLayer = LayerMask.GetMask("Ground");
    }

    private void Start()
    {
        m_Direction = m_Transform.localScale.x >= 0f ? 1 : -1;
    }

    private void FixedUpdate()
    {
        Vector2 moveDirection = Vector2.right * m_Direction;

        RaycastHit2D frontHit = Physics2D.Raycast(m_FrontCheck.position, moveDirection, 0.5f, m_GroundLayer);
        RaycastHit2D downHit = Physics2D.Raycast(m_FrontCheck.position, Vector2.down, 1f, m_GroundLayer);

        if (frontHit.collider != null || downHit.collider == null)
        {
            Flip();
        }

        m_Rigidbody2D.linearVelocity = new Vector2(m_Direction * m_PatrolSpeed, m_Rigidbody2D.linearVelocity.y);
    }

    private void Flip()
    {
        m_Direction *= -1;

        Vector3 scale = m_Transform.localScale;
        scale.x *= -1f;
        m_Transform.localScale = scale;
    }
}
