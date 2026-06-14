using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_ScoreText;

    private void Start()
    {
        GameManager.Instance.OnScoreChanged += UpdateScoreText;
        UpdateScoreText(GameManager.Instance.Score);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScoreText;
        }
    }

    private void UpdateScoreText(int score)
    {
        m_ScoreText.text = score.ToString();
    }
}
