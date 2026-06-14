using UnityEngine;

// Animador simples baseado em quadros (sprites). Usado para frutas, etc.
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] m_Frames;
    [SerializeField] private float m_FramesPerSecond = 12f;

    private SpriteRenderer m_SpriteRenderer;
    private int m_Index;
    private float m_Timer;

    public void SetFrames(Sprite[] frames, float fps)
    {
        m_Frames = frames;
        m_FramesPerSecond = fps;
    }

    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (m_Frames == null || m_Frames.Length == 0 || m_FramesPerSecond <= 0f)
        {
            return;
        }

        m_Timer += Time.deltaTime;

        if (m_Timer >= 1f / m_FramesPerSecond)
        {
            m_Timer = 0f;
            m_Index = (m_Index + 1) % m_Frames.Length;
            m_SpriteRenderer.sprite = m_Frames[m_Index];
        }
    }
}
