using UnityEngine;

// Movimento da raposa: andar, pular (com pulo variavel e pulo duplo opcional),
// olhar pra cima (W) e rolar (S correndo). O roll mata inimigos e da invencibilidade.
// Quando machucado/morto o controle e travado e a animacao certa toca.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private float m_MoveSpeed = 7f;
    private float m_JumpForce = 14f;
    private float m_JumpCutMultiplier = 0.5f;

    private float m_RollSpeed = 12f;
    private float m_RollDuration = 0.4f;
    private float m_RollCooldown = 0.7f;

    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private float m_GroundCheckRadius = 0.2f;

    private Rigidbody2D m_Rigidbody2D;
    private SpriteRenderer m_SpriteRenderer;
    private PlayerAnimation m_Animation;
    private LayerMask m_GroundLayer;

    private float m_MoveInput;
    private bool m_FacingRight = true;
    private bool m_AllowDoubleJump;
    private int m_JumpsLeft;
    private bool m_JumpQueued;
    private bool m_JumpReleased;

    private bool m_Rolling;
    private float m_RollTimer;
    private float m_RollCooldownTimer;
    private int m_RollDir = 1;

    private float m_HurtTimer;
    private bool m_Dead;

    public bool IsRolling => m_Rolling;

    // Referencia ao player vivo na cena (traps e chefe usam isso sem Find).
    public static PlayerController Current { get; private set; }

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_Animation = GetComponent<PlayerAnimation>();
        m_GroundLayer = LayerMask.GetMask("Ground");
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    public void SetDoubleJump(bool allow)
    {
        m_AllowDoubleJump = allow;
    }

    public void ForceHurt(float stun)
    {
        if (m_Dead)
        {
            return;
        }

        m_HurtTimer = stun;
        m_Rolling = false;
        if (m_Animation != null)
        {
            m_Animation.SetState(PlayerAnimation.State.Hurt);
        }
    }

    public void ForceDead()
    {
        m_Dead = true;
        m_Rolling = false;
        if (m_Animation != null)
        {
            m_Animation.SetState(PlayerAnimation.State.Dead);
        }
    }

    private void Update()
    {
        if (m_RollCooldownTimer > 0f)
        {
            m_RollCooldownTimer -= Time.deltaTime;
        }

        if (m_Dead)
        {
            return;
        }

        if (m_HurtTimer > 0f)
        {
            m_HurtTimer -= Time.deltaTime;
            return;
        }

        if (m_Rolling)
        {
            m_RollTimer -= Time.deltaTime;
            if (m_RollTimer <= 0f)
            {
                m_Rolling = false;
            }
            else
            {
                m_Animation.SetState(PlayerAnimation.State.Roll);
                return;
            }
        }

        ReadInput();
        UpdateFacing();
        UpdateAnimationState();
    }

    private void ReadInput()
    {
        m_MoveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            m_JumpQueued = true;
        }

        if (Input.GetButtonUp("Jump"))
        {
            m_JumpReleased = true;
        }

        bool grounded = IsGrounded();
        bool rollPressed = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        if (rollPressed && grounded && Mathf.Abs(m_MoveInput) > 0.1f && m_RollCooldownTimer <= 0f)
        {
            StartRoll();
        }
    }

    private void StartRoll()
    {
        m_Rolling = true;
        m_RollTimer = m_RollDuration;
        m_RollCooldownTimer = m_RollCooldown;
        m_RollDir = m_FacingRight ? 1 : -1;
        m_Animation.SetState(PlayerAnimation.State.Roll);
    }

    private void UpdateFacing()
    {
        if (m_MoveInput > 0f)
        {
            m_FacingRight = true;
        }
        else if (m_MoveInput < 0f)
        {
            m_FacingRight = false;
        }

        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.flipX = !m_FacingRight;
        }
    }

    private void UpdateAnimationState()
    {
        if (m_Animation == null)
        {
            return;
        }

        bool grounded = IsGrounded();
        float vertical = Input.GetAxisRaw("Vertical");

        if (!grounded)
        {
            m_Animation.SetState(m_Rigidbody2D.linearVelocity.y > 0.1f
                ? PlayerAnimation.State.Jump
                : PlayerAnimation.State.Fall);
            return;
        }

        if (Mathf.Abs(m_MoveInput) > 0.1f)
        {
            m_Animation.SetState(PlayerAnimation.State.Run);
        }
        else if (vertical > 0.5f)
        {
            m_Animation.SetState(PlayerAnimation.State.LookUp);
        }
        else
        {
            m_Animation.SetState(PlayerAnimation.State.Idle);
        }
    }

    private void FixedUpdate()
    {
        if (m_Dead || m_HurtTimer > 0f)
        {
            return;
        }

        if (m_Rolling)
        {
            m_Rigidbody2D.linearVelocity = new Vector2(m_RollDir * m_RollSpeed, m_Rigidbody2D.linearVelocity.y);
            return;
        }

        m_Rigidbody2D.linearVelocity = new Vector2(m_MoveInput * m_MoveSpeed, m_Rigidbody2D.linearVelocity.y);

        bool grounded = IsGrounded();
        if (grounded && m_Rigidbody2D.linearVelocity.y <= 0.01f)
        {
            m_JumpsLeft = m_AllowDoubleJump ? 2 : 1;
        }

        if (m_JumpQueued && m_JumpsLeft > 0)
        {
            m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0f);
            m_Rigidbody2D.AddForce(Vector2.up * m_JumpForce, ForceMode2D.Impulse);
            m_JumpsLeft--;
        }
        m_JumpQueued = false;

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
        return m_GroundCheck != null && Physics2D.OverlapCircle(m_GroundCheck.position, m_GroundCheckRadius, m_GroundLayer);
    }
}
