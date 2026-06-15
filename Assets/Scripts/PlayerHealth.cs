using System.Collections;
using UnityEngine;

// Recebe o dano do player. Cada acerto tira 1 vida (no GameManager), com
// empurrao, animacao de hurt e invencibilidade piscando. Cair no vazio tambem
// tira 1 vida, mas sem a animacao de hurt. Quando as vidas zeram, o
// LevelGenerator decide o destino (volta pra fase 1, ou reinicia o chefe).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    private Rigidbody2D m_Rigidbody2D;
    private SpriteRenderer m_SpriteRenderer;
    private PlayerController m_Controller;

    private bool m_Invincible;
    private bool m_Dead;

    private readonly float m_KnockbackX = 8f;
    private readonly float m_KnockbackY = 9f;
    private readonly float m_HurtStun = 0.45f;
    private readonly float m_InvincibleTime = 1f;
    private readonly float m_DeathBlinkTime = 2.6f;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_Controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        LivesHUD.GetOrCreate();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BroadcastLives();
        }
    }

    // Dano de inimigo/espinho/trap. O roll protege contra inimigos (eles nem
    // chamam aqui), mas espinhos ainda machucam.
    public void TakeDamage(Vector2 sourcePosition)
    {
        if (m_Invincible || m_Dead)
        {
            return;
        }

        int remaining = GameManager.Instance != null ? GameManager.Instance.LoseLife() : 0;

        AudioManager.Play(AudioManager.Sfx.Hurt);

        float dir = Mathf.Sign(transform.position.x - sourcePosition.x);
        if (dir == 0f)
        {
            dir = -1f;
        }

        m_Rigidbody2D.linearVelocity = new Vector2(dir * m_KnockbackX, m_KnockbackY);

        if (remaining > 0)
        {
            if (m_Controller != null)
            {
                m_Controller.ForceHurt(m_HurtStun);
            }

            StartCoroutine(InvincibilityRoutine());
        }
        else
        {
            StartCoroutine(DeathByHitRoutine());
        }
    }

    // Caiu no vazio: perde vida, mas sem animacao de hurt.
    public void VoidFall()
    {
        if (m_Dead)
        {
            return;
        }

        int remaining = GameManager.Instance != null ? GameManager.Instance.LoseLife() : 0;

        if (remaining > 0)
        {
            m_Dead = true; // trava ate a fase reiniciar
            if (LevelGenerator.Instance != null)
            {
                LevelGenerator.Instance.RestartCurrentLevel();
            }
        }
        else
        {
            m_Dead = true;
            if (LevelGenerator.Instance != null)
            {
                LevelGenerator.Instance.OnPlayerDeath();
            }
        }
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

    // Terceiro acerto: animacao de morte (hurt-2) + piscada lenta e volta pra fase 1.
    private IEnumerator DeathByHitRoutine()
    {
        m_Dead = true;
        m_Invincible = true;

        if (m_Controller != null)
        {
            m_Controller.ForceDead();
        }

        float elapsed = 0f;
        while (elapsed < m_DeathBlinkTime)
        {
            elapsed += Time.deltaTime;
            if (m_SpriteRenderer != null)
            {
                m_SpriteRenderer.enabled = ((int)(elapsed * 5f)) % 2 == 0; // piscada lenta
            }

            yield return null;
        }

        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.enabled = true;
        }

        if (LevelGenerator.Instance != null)
        {
            LevelGenerator.Instance.OnPlayerDeath();
        }
    }
}
