using System;
using UnityEngine;

// Estado global da partida: pontuacao e VIDAS que persistem entre fases.
// As vidas nao zeram ao trocar de fase nem ao reiniciar a fase atual; so
// resetam quando o run recomeca (morreu de vez -> volta pra fase 1).
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int m_MaxLives = 3;

    public int Score { get; private set; }
    public int Lives { get; private set; }

    public event Action<int> OnScoreChanged;

    // (atual, maximo) – a HUD de vidas escuta isso.
    public static event Action<int, int> OnLivesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Lives = m_MaxLives;
    }

    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    // Tira uma vida e devolve quantas sobraram.
    public int LoseLife()
    {
        Lives = Mathf.Max(0, Lives - 1);
        OnLivesChanged?.Invoke(Lives, m_MaxLives);
        return Lives;
    }

    // Recomeca o run do zero (vidas cheias, pontuacao zerada).
    public void ResetRun()
    {
        Lives = m_MaxLives;
        Score = 0;
        OnLivesChanged?.Invoke(Lives, m_MaxLives);
        OnScoreChanged?.Invoke(Score);
    }

    // Devolve as vidas (usado quando a luta do chefe reinicia).
    public void RefillLives()
    {
        Lives = m_MaxLives;
        OnLivesChanged?.Invoke(Lives, m_MaxLives);
    }

    // Forca a HUD a se sincronizar (chamado quando ela aparece).
    public void BroadcastLives()
    {
        OnLivesChanged?.Invoke(Lives, m_MaxLives);
    }
}
