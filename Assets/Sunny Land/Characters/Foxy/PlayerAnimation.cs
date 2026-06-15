using UnityEngine;

// Animador da raposa, baseado em quadros. Quem manda no estado e o
// PlayerController (ele chama SetState toda frame). Esta classe so cuida de
// trocar os sprites; o flip horizontal fica no PlayerController.
public class PlayerAnimation : MonoBehaviour
{
    public enum State
    {
        Idle,
        Run,
        Jump,   // subindo
        Fall,   // caindo
        LookUp,
        Roll,
        Hurt,
        Dead
    }

    [SerializeField] private Sprite[] m_IdleSprites;
    [SerializeField] private Sprite[] m_RunSprites;
    [SerializeField] private Sprite[] m_JumpSprites;
    [SerializeField] private Sprite[] m_FallSprites;
    [SerializeField] private Sprite[] m_LookUpSprites;
    [SerializeField] private Sprite[] m_RollSprites;
    [SerializeField] private Sprite[] m_HurtSprites;
    [SerializeField] private Sprite[] m_DeadSprites;

    private SpriteRenderer m_SpriteRenderer;
    private State m_State = State.Idle;
    private int m_FrameIndex;
    private float m_FrameTimer;

    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetState(State state)
    {
        if (state != m_State)
        {
            m_State = state;
            m_FrameIndex = 0;
            m_FrameTimer = 0f;
            ApplyFrame();
        }
    }

    private void Update()
    {
        Sprite[] frames = FramesFor(m_State);
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        m_FrameTimer += Time.deltaTime;
        float fps = FpsFor(m_State);

        if (m_FrameTimer >= 1f / fps)
        {
            m_FrameTimer = 0f;

            if (Loops(m_State))
            {
                m_FrameIndex = (m_FrameIndex + 1) % frames.Length;
            }
            else
            {
                m_FrameIndex = Mathf.Min(m_FrameIndex + 1, frames.Length - 1);
            }

            ApplyFrame();
        }
    }

    private void ApplyFrame()
    {
        Sprite[] frames = FramesFor(m_State);
        if (frames != null && frames.Length > 0)
        {
            m_FrameIndex = Mathf.Clamp(m_FrameIndex, 0, frames.Length - 1);
            m_SpriteRenderer.sprite = frames[m_FrameIndex];
        }
    }

    private bool Loops(State state)
    {
        return state == State.Idle || state == State.Run || state == State.Roll;
    }

    private float FpsFor(State state)
    {
        switch (state)
        {
            case State.Run: return 14f;
            case State.Roll: return 16f;
            case State.Hurt: return 8f;
            default: return 9f;
        }
    }

    private Sprite[] FramesFor(State state)
    {
        switch (state)
        {
            case State.Run: return m_RunSprites;
            case State.Jump: return m_JumpSprites;
            case State.Fall: return Pick(m_FallSprites, m_JumpSprites);
            case State.LookUp: return Pick(m_LookUpSprites, m_IdleSprites);
            case State.Roll: return Pick(m_RollSprites, m_RunSprites);
            case State.Hurt: return Pick(m_HurtSprites, m_IdleSprites);
            case State.Dead: return Pick(m_DeadSprites, m_HurtSprites);
            default: return m_IdleSprites;
        }
    }

    private Sprite[] Pick(Sprite[] primary, Sprite[] fallback)
    {
        return (primary != null && primary.Length > 0) ? primary : fallback;
    }
}
