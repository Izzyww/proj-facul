using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float m_MoveSpeed = 7f;
    private float m_JumpForce = 14f;
    private float m_JumpCutMultiplier = 0.5f; // solta o botao = pulo mais baixo

    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private float m_GroundCheckRadius = 0.2f;

    private Rigidbody2D m_Rigidbody2D;
    private SpriteRenderer m_SpriteRenderer;
    private Animator m_Animator;
    private LayerMask m_GroundLayer;
    private float m_MoveInput;
    private bool m_JumpPressed;
    private bool m_JumpReleased;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_Animator = GetComponent<Animator>();
        m_GroundLayer = LayerMask.GetMask("Ground");
    }

    private void Update()
    {
        m_MoveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            m_JumpPressed = true;
        }

        if (Input.GetButtonUp("Jump"))
        {
            m_JumpReleased = true;
        }

        UpdateFacing();
        UpdateAnimator();
    }

    private void UpdateFacing()
    {
        if (m_MoveInput > 0f)
        {
            m_SpriteRenderer.flipX = false;
        }
        else if (m_MoveInput < 0f)
        {
            m_SpriteRenderer.flipX = true;
        }
    }

    private void UpdateAnimator()
    {
        if (m_Animator == null)
        {
            return;
        }

        m_Animator.SetFloat("Speed", Mathf.Abs(m_MoveInput));
        m_Animator.SetBool("Grounded", IsGrounded());
    }

    private void FixedUpdate()
    {
        m_Rigidbody2D.linearVelocity = new Vector2(m_MoveInput * m_MoveSpeed, m_Rigidbody2D.linearVelocity.y);

        if (m_JumpPressed && IsGrounded())
        {
            m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0f);
            m_Rigidbody2D.AddForce(Vector2.up * m_JumpForce, ForceMode2D.Impulse);
            m_JumpPressed = false;
        }

        if (m_JumpReleased)
        {
            if (m_Rigidbody2D.linearVelocity.y > 0f)
            {
                m_Rigidbody2D.linearVelocity = new Vector2(
                    m_Rigidbody2D.linearVelocity.x,
                    m_Rigidbody2D.linearVelocity.y * m_JumpCutMultiplier);
            }

            m_JumpReleased = false;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(m_GroundCheck.position, m_GroundCheckRadius, m_GroundLayer);
    }

    public bool PublicIsGrounded()
    {
        return IsGrounded();
    }
}
