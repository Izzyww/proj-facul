using UnityEngine;

// Bandeira de fim de fase. Quando o player encosta, dispara a transicao
// em circulo e avanca para o proximo nivel.
public class LevelFlag : MonoBehaviour
{
    private bool m_Triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_Triggered)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        m_Triggered = true;

        if (LevelGenerator.Instance != null)
        {
            LevelGenerator.Instance.GoToNextLevel();
        }
    }
}
