using System;
using System.Collections;
using UnityEngine;

// Vida do player: leva dano em vez de morrer na hora, com empurrao (knockback),
// 1 segundo de invencibilidade piscando e reinicio da fase quando zera.
public class PlayerHealth : MonoBehaviour
{
    public int m_MaxHealth = 3;

    public int CurrentHealth { get; private set; }

    // (atual, maximo) – a HUD escuta isso.
    public static event Action<int, int> OnHealthChanged;

    private Rigidbody2D m_Rigidbody2D;
    private SpriteRenderer m_SpriteRenderer;
    private bool m_Invincible;

    private readonly float m_KnockbackX = 8f;
    private readonly float m_KnockbackY = 9f;
    private readonly float m_InvincibleTime = 1f;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        CurrentHealth = m_MaxHealth;
        HealthUI.GetOrCreate();
        OnHealthChanged?.Invoke(CurrentHealth, m_MaxHealth);
    }

    public void TakeDamage(Vector2 sourcePosition)
    {
        if (m_Invincible || CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - 1);
        OnHealthChanged?.Invoke(CurrentHealth, m_MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        float dir = Mathf.Sign(transform.position.x - sourcePosition.x);
        if (dir == 0f)
        {
            dir = -1f;
        }

        m_Rigidbody2D.linearVelocity = new Vector2(dir * m_KnockbackX, m_KnockbackY);
        StartCoroutine(InvincibilityRoutine());
    }

    // Morte instantanea (cair no buraco).
    public void Kill()
    {
        if (CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth = 0;
        OnHealthChanged?.Invoke(CurrentHealth, m_MaxHealth);
        Die();
    }

    private IEnumerator InvincibilityRoutine()
    {
        m_Invincible = true;
        float elapsed = 0f;

        while (elapsed < m_InvincibleTime)
        {
            elapsed += Time.deltaTime;

            if (m_SpriteRenderer != null)
            {
                m_SpriteRenderer.enabled = ((int)(elapsed * 12f)) % 2 == 0;
            }

            yield return null;
        }

        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.enabled = true;
        }

        m_Invincible = false;
    }

    private void Die()
    {
        if (LevelGenerator.Instance != null)
        {
            LevelGenerator.Instance.KillPlayer();
        }
    }
}
