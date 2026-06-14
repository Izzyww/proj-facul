using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] m_IdleSprites;
    [SerializeField] private Sprite[] m_RunSprites;
    [SerializeField] private Sprite[] m_JumpSprites;
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private float m_GroundCheckRadius = 0.2f;

    private SpriteRenderer m_SpriteRenderer;
    private LayerMask m_GroundLayer;
    private int m_FrameIndex;
    private float m_FrameTimer;
    private PlayerAnimState m_CurrentState;

    private enum PlayerAnimState
    {
        Idle,
        Run,
        Jump
    }

    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_GroundLayer = LayerMask.GetMask("Ground");
    }

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool isGrounded = Physics2D.OverlapCircle(m_GroundCheck.position, m_GroundCheckRadius, m_GroundLayer);

        if (moveInput > 0f)
        {
            m_SpriteRenderer.flipX = false;
        }
        else if (moveInput < 0f)
        {
            m_SpriteRenderer.flipX = true;
        }

        PlayerAnimState targetState = GetTargetState(isGrounded, moveInput);

        if (targetState != m_CurrentState)
        {
            m_CurrentState = targetState;
            m_FrameIndex = 0;
            m_FrameTimer = 0f;
        }

        Sprite[] currentSprites = GetSpritesForState(m_CurrentState);
        if (currentSprites == null || currentSprites.Length == 0)
        {
            return;
        }

        float frameRate = m_CurrentState == PlayerAnimState.Run ? 12f : 8f;
        m_FrameTimer += Time.deltaTime;

        if (m_FrameTimer >= 1f / frameRate)
        {
            m_FrameTimer = 0f;

            if (m_CurrentState == PlayerAnimState.Jump)
            {
                m_FrameIndex = Mathf.Min(m_FrameIndex + 1, currentSprites.Length - 1);
            }
            else
            {
                m_FrameIndex = (m_FrameIndex + 1) % currentSprites.Length;
            }
        }

        m_SpriteRenderer.sprite = currentSprites[m_FrameIndex];
    }

    private PlayerAnimState GetTargetState(bool isGrounded, float moveInput)
    {
        if (!isGrounded)
        {
            return PlayerAnimState.Jump;
        }

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            return PlayerAnimState.Run;
        }

        return PlayerAnimState.Idle;
    }

    private Sprite[] GetSpritesForState(PlayerAnimState state)
    {
        switch (state)
        {
            case PlayerAnimState.Run:
                return m_RunSprites;
            case PlayerAnimState.Jump:
                return m_JumpSprites;
            default:
                return m_IdleSprites;
        }
    }
}
