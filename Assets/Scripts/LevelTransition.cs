using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Transicao estilo desenho animado: um circulo preto que fecha sobre a tela
// (iris wipe) e depois reabre no proximo nivel. Gera a textura em tempo de
// execucao, entao nao precisa de nenhum asset arrastado no Inspector.
public class LevelTransition : MonoBehaviour
{
    public static LevelTransition Instance { get; private set; }

    public bool IsBusy { get; private set; }

    private const int TextureSize = 256;
    private const float CloseDuration = 0.55f;
    private const float OpenDuration = 0.55f;
    private const float ClosedRadius = -8f; // negativo garante tela 100% preta

    private RawImage m_Mask;
    private RectTransform m_MaskRect;
    private TextMeshProUGUI m_MessageText;
    private Texture2D m_Texture;
    private Color32[] m_Buffer;
    private float m_MaxRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildCanvas();
        SetMaskRadius(m_MaxRadius);
    }

    // Garante que exista uma instancia na cena.
    public static LevelTransition GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject obj = new GameObject("LevelTransition");
        return obj.AddComponent<LevelTransition>();
    }

    // Fecha a iris, executa onCovered (trocar de nivel), mostra a mensagem e reabre.
    public void Play(Action onCovered, string message)
    {
        if (IsBusy)
        {
            return;
        }

        StartCoroutine(TransitionRoutine(onCovered, message));
    }

    private IEnumerator TransitionRoutine(Action onCovered, string message)
    {
        IsBusy = true;
        ResizeMaskToScreen();

        yield return AnimateRadius(m_MaxRadius, ClosedRadius, CloseDuration);

        onCovered?.Invoke();

        if (!string.IsNullOrEmpty(message))
        {
            m_MessageText.text = message;
            m_MessageText.color = Color.white;
            yield return new WaitForSeconds(0.7f);
            m_MessageText.text = string.Empty;
        }
        else
        {
            yield return null;
        }

        yield return AnimateRadius(ClosedRadius, m_MaxRadius, OpenDuration);

        IsBusy = false;
    }

    private IEnumerator AnimateRadius(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // suaviza (smoothstep)
            SetMaskRadius(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetMaskRadius(to);
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        m_Texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        m_Buffer = new Color32[TextureSize * TextureSize];

        GameObject maskObj = new GameObject("IrisMask");
        maskObj.transform.SetParent(transform, false);
        m_Mask = maskObj.AddComponent<RawImage>();
        m_Mask.texture = m_Texture;
        m_Mask.raycastTarget = false;
        m_MaskRect = maskObj.GetComponent<RectTransform>();
        m_MaskRect.anchorMin = new Vector2(0.5f, 0.5f);
        m_MaskRect.anchorMax = new Vector2(0.5f, 0.5f);
        m_MaskRect.pivot = new Vector2(0.5f, 0.5f);

        GameObject textObj = new GameObject("Message");
        textObj.transform.SetParent(transform, false);
        m_MessageText = textObj.AddComponent<TextMeshProUGUI>();
        m_MessageText.text = string.Empty;
        m_MessageText.fontSize = 64;
        m_MessageText.fontStyle = FontStyles.Bold;
        m_MessageText.alignment = TextAlignmentOptions.Center;
        m_MessageText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.sizeDelta = new Vector2(0f, 240f);
        textRect.anchoredPosition = Vector2.zero;

        ResizeMaskToScreen();
    }

    private void ResizeMaskToScreen()
    {
        float side = Mathf.Max(Screen.width, Screen.height) * 1.1f;
        m_MaskRect.sizeDelta = new Vector2(side, side);
        m_MaxRadius = TextureSize * 0.72f; // cobre os cantos do quadrado
    }

    // Desenha a textura: transparente dentro do raio, preto solido fora.
    private void SetMaskRadius(float radius)
    {
        float center = TextureSize * 0.5f;
        const float edge = 4f;

        for (int y = 0; y < TextureSize; y++)
        {
            float dy = y - center;

            for (int x = 0; x < TextureSize; x++)
            {
                float dx = x - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((dist - radius) / edge);

                int i = y * TextureSize + x;
                m_Buffer[i] = new Color32(0, 0, 0, (byte)(alpha * 255f));
            }
        }

        m_Texture.SetPixels32(m_Buffer);
        m_Texture.Apply(false);
    }
}
