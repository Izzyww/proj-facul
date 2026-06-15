using UnityEngine;

// Movimento da raposa: correr, andar (Shift = devagar), pular (com pulo variavel
// e pulo duplo opcional), olhar pra cima (W) e rolar (S correndo). O roll mata
// inimigos e da invencibilidade. Tambem agarra parede (wall-grab): encostando na
// parede + W ele gruda; segurando a direcao continua grudado; soltando a direcao
// ele cai; pulo enquanto grudado lanca pra longe da parede.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private float m_MoveSpeed = 7f;
    private float m_WalkSpeed = 3.2f; // com Shift
    private float m_JumpForce = 14f;
    private float m_JumpCutMultiplier = 0.5f;

    private float m_RollSpeed = 12f;
    private float m_RollDuration = 0.4f;
    private float m_RollCooldown = 0.7f;

    private float m_WallJumpX = 11f;
    private float m_WallJumpY = 14f;
    private float m_WallJumpLockTime = 0.22f;

    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private float m_GroundCheckRadius = 0.2f;

    private Rigidbody2D m_Rigidbody2D;
    private SpriteRenderer m_SpriteRenderer;
    private PlayerAnimation m_Animation;
    private BoxCollider2D m_Box;
    private LayerMask m_GroundLayer;
    private float m_DefaultGravity;

    private float m_MoveInput;
    private float m_CurrentSpeed;
    private bool m_FacingRight = true;
    private bool m_AllowDoubleJump;
    private int m_JumpsLeft;
    private bool m_JumpQueued;
    private bool m_JumpReleased;

    private bool m_Rolling;
    private float m_RollTimer;
    private float m_RollCooldownTimer;
    private int m_RollDir = 1;

    private bool m_WallGrabbing;
    private int m_WallSide; // +1 parede a direita, -1 parede a esquerda
    private float m_WallJumpLock;

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
        m_Box = GetComponent<BoxCollider2D>();
        m_GroundLayer = LayerMask.GetMask("Ground");
        m_DefaultGravity = m_Rigidbody2D.gravityScale;
        m_CurrentSpeed = m_MoveSpeed;
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
        m_WallGrabbing = false;
        if (m_Animation != null)
        {
            m_Animation.SetState(PlayerAnimation.State.Hurt);
        }
    }

    public void ForceDead()
    {
        m_Dead = true;
        m_Rolling = false;
        m_WallGrabbing = false;
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

        if (m_WallJumpLock > 0f)
        {
            m_WallJumpLock -= Time.deltaTime;
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
        UpdateWallGrab();

        if (m_WallGrabbing)
        {
            ApplyWallGrabVisual();
        }
        else
        {
            UpdateFacing();
            UpdateAnimationState();
        }
    }

    private void ReadInput()
    {
        m_MoveInput = Input.GetAxisRaw("Horizontal");

        bool walk = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        m_CurrentSpeed = walk ? m_WalkSpeed : m_MoveSpeed;

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

    // ── Wall grab ─────────────────────────────────────────────────────
    private void UpdateWallGrab()
    {
        bool grounded = IsGrounded();
        int wall = DetectWall();

        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetAxisRaw("Vertical") > 0.5f;
        bool pushingInto = (wall > 0 && m_MoveInput > 0.1f) || (wall < 0 && m_MoveInput < -0.1f);

        if (!m_WallGrabbing)
        {
            // Inicia agarrando: no ar, encostado na parede, empurrando contra ela e segurando W.
            if (!grounded && wall != 0 && pushingInto && up && m_WallJumpLock <= 0f)
            {
                m_WallGrabbing = true;
                m_WallSide = wall;
            }
        }
        else
        {
            // Mantem enquanto continuar empurrando pra parede (W ja nao e necessario).
            bool stillPushing = (m_WallSide > 0 && m_MoveInput > 0.1f) || (m_WallSide < 0 && m_MoveInput < -0.1f);
            if (grounded || DetectWall() != m_WallSide || !stillPushing)
            {
                m_WallGrabbing = false;
            }
        }
    }

    private void ApplyWallGrabVisual()
    {
        m_FacingRight = m_WallSide > 0;
        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.flipX = false; // os sprites wall-grab1/2 ja olham pro lado certo
        }

        m_Animation.SetState(m_WallSide > 0
            ? PlayerAnimation.State.WallGrab
            : PlayerAnimation.State.WallGrab2);
    }

    private int DetectWall()
    {
        if (m_Box == null)
        {
            return 0;
        }

        Bounds b = m_Box.bounds;
        float dist = b.extents.x + 0.08f;
        Vector2 mid = new Vector2(b.center.x, b.center.y);

        if (Physics2D.Raycast(mid, Vector2.right, dist, m_GroundLayer).collider != null)
        {
            return 1;
        }

        if (Physics2D.Raycast(mid, Vector2.left, dist, m_GroundLayer).collider != null)
        {
            return -1;
        }

        return 0;
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
            m_Rigidbody2D.gravityScale = m_DefaultGravity;
            m_Rigidbody2D.linearVelocity = new Vector2(m_RollDir * m_RollSpeed, m_Rigidbody2D.linearVelocity.y);
            return;
        }

        if (m_WallGrabbing)
        {
            WallGrabPhysics();
            return;
        }

        m_Rigidbody2D.gravityScale = m_DefaultGravity;
        m_Rigidbody2D.linearVelocity = new Vector2(m_MoveInput * m_CurrentSpeed, m_Rigidbody2D.linearVelocity.y);

        bool grounded = IsGrounded();
        if (grounded && m_Rigidbody2D.linearVelocity.y <= 0.01f)
        {
            m_JumpsLeft = m_AllowDoubleJump ? 2 : 1;
        }

        if (m_JumpQueued && m_JumpsLeft > 0)
        {
            bool isDouble = (m_AllowDoubleJump && m_JumpsLeft == 1);
            m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0f);
            m_Rigidbody2D.AddForce(Vector2.up * m_JumpForce, ForceMode2D.Impulse);
            m_JumpsLeft--;
            AudioManager.Play(isDouble ? AudioManager.Sfx.DoubleJump : AudioManager.Sfx.Jump);
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

    private void WallGrabPhysics()
    {
        // Pulo de parede: lanca pra cima e pro lado oposto da parede.
        if (m_JumpQueued)
        {
            m_WallGrabbing = false;
            m_WallJumpLock = m_WallJumpLockTime;
            m_Rigidbody2D.gravityScale = m_DefaultGravity;
            m_Rigidbody2D.linearVelocity = new Vector2(-m_WallSide * m_WallJumpX, m_WallJumpY);
            m_JumpsLeft = m_AllowDoubleJump ? 1 : 0;
            m_JumpQueued = false;
            AudioManager.Play(AudioManager.Sfx.WallJump);
            return;
        }

        // Grudado: sem gravidade, parado na parede.
        m_Rigidbody2D.gravityScale = 0f;
        m_Rigidbody2D.linearVelocity = Vector2.zero;
        m_JumpQueued = false;
        m_JumpReleased = false;
    }

    private bool IsGrounded()
    {
        return m_GroundCheck != null && Physics2D.OverlapCircle(m_GroundCheck.position, m_GroundCheckRadius, m_GroundLayer);
    }
}
