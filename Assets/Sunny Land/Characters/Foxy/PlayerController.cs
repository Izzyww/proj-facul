using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float m_MoveSpeed = 8f;
    private float m_JumpForce = 12f;

    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private float m_GroundCheckRadius = 0.2f;

    private Rigidbody2D m_Rigidbody2D;
    private LayerMask m_GroundLayer;
    private float m_MoveInput;
    private bool m_JumpPressed;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_GroundLayer = LayerMask.GetMask("Ground");
    }

    private void Update()
    {
        m_MoveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            m_JumpPressed = true;
        }
    }

    private void FixedUpdate()
    {
        m_Rigidbody2D.linearVelocity = new Vector2(m_MoveInput * m_MoveSpeed, m_Rigidbody2D.linearVelocity.y);

        if (m_JumpPressed && IsGrounded())
        {
            m_Rigidbody2D.AddForce(Vector2.up * m_JumpForce, ForceMode2D.Impulse);
            m_JumpPressed = false;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(m_GroundCheck.position, m_GroundCheckRadius, m_GroundLayer);
    }
}
