using UnityEngine;

// Efeito visual de uso unico: toca quadros (opcional), sobe um pouco, some
// (fade) e se destroi. Serve para poeira de corrida, coleta e morte de inimigo.
public class OneShotVfx : MonoBehaviour
{
    private SpriteRenderer m_SpriteRenderer;
    private Sprite[] m_Frames;
    private float m_Fps;
    private float m_Life;
    private Vector2 m_Velocity;
    private float m_Timer;
    private float m_FrameTimer;
    private int m_Index;
    private Color m_BaseColor;

    public static void Spawn(Vector3 position, Sprite[] frames, float fps, float life, Vector2 velocity, int sortingOrder, float scale)
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        GameObject obj = new GameObject("Vfx");
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = sortingOrder;

        OneShotVfx vfx = obj.AddComponent<OneShotVfx>();
        vfx.m_SpriteRenderer = sr;
        vfx.m_Frames = frames;
        vfx.m_Fps = fps;
        vfx.m_Life = life;
        vfx.m_Velocity = velocity;
        vfx.m_BaseColor = sr.color;
    }

    private void Update()
    {
        m_Timer += Time.deltaTime;
        transform.position += (Vector3)(m_Velocity * Time.deltaTime);

        if (m_Fps > 0f && m_Frames.Length > 1)
        {
            m_FrameTimer += Time.deltaTime;
            if (m_FrameTimer >= 1f / m_Fps)
            {
                m_FrameTimer = 0f;
                m_Index = Mathf.Min(m_Index + 1, m_Frames.Length - 1);
                m_SpriteRenderer.sprite = m_Frames[m_Index];
            }
        }

        float alpha = Mathf.Clamp01(1f - (m_Timer / m_Life));
        m_SpriteRenderer.color = new Color(m_BaseColor.r, m_BaseColor.g, m_BaseColor.b, alpha);

        if (m_Timer >= m_Life)
        {
            Destroy(gameObject);
        }
    }
}
