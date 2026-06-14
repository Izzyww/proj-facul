using UnityEngine;

public class Collectible : MonoBehaviour
{
    public Sprite[] m_PickupFrames;

    private float m_FloatAmplitude = 0.12f;
    private float m_FloatFrequency = 2.5f;

    private Vector3 m_StartPosition;
    private bool m_Collected;

    private void Start()
    {
        m_StartPosition = transform.position;
    }

    private void Update()
    {
        transform.position = m_StartPosition + Vector3.up * (Mathf.Sin(Time.time * m_FloatFrequency) * m_FloatAmplitude);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_Collected || !other.CompareTag("Player"))
        {
            return;
        }

        m_Collected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }

        OneShotVfx.Spawn(transform.position, m_PickupFrames, 16f, 0.4f, new Vector2(0f, 1f), 6, 1f);
        Destroy(gameObject);
    }
}
