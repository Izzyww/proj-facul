using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HUD de coracoes desenhada por codigo (o pacote nao tem sprite de coracao).
public class HealthUI : MonoBehaviour
{
    public static HealthUI Instance { get; private set; }

    private Sprite m_FullHeart;
    private Sprite m_EmptyHeart;
    private readonly List<Image> m_Hearts = new List<Image>();
    private Transform m_Container;

    public static HealthUI GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject obj = new GameObject("HealthUI");
        return obj.AddComponent<HealthUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        m_FullHeart = BuildHeartSprite(new Color(0.9f, 0.15f, 0.2f));
        m_EmptyHeart = BuildHeartSprite(new Color(0.25f, 0.25f, 0.28f));
        BuildCanvas();
    }

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += Refresh;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= Refresh;
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject container = new GameObject("Hearts");
        container.transform.SetParent(transform, false);
        m_Container = container.transform;

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
    }

    private void Refresh(int current, int max)
    {
        while (m_Hearts.Count < max)
        {
            GameObject heart = new GameObject("Heart");
            heart.transform.SetParent(m_Container, false);
            Image img = heart.AddComponent<Image>();
            img.rectTransform.sizeDelta = new Vector2(44f, 44f);
            m_Hearts.Add(img);
        }

        for (int i = 0; i < m_Hearts.Count; i++)
        {
            bool active = i < max;
            m_Hearts[i].gameObject.SetActive(active);
            m_Hearts[i].sprite = i < current ? m_FullHeart : m_EmptyHeart;
        }
    }

    private Sprite BuildHeartSprite(Color color)
    {
        // Mascara de coracao 9x8 (1 = pixel preenchido).
        string[] mask =
        {
            "011011110",
            "111111111",
            "111111111",
            "111111111",
            "011111110",
            "001111100",
            "000111000",
            "000010000",
        };

        int w = mask[0].Length;
        int h = mask.Length;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < h; y++)
        {
            // Linha 0 da mascara e o topo do coracao; a textura cresce de baixo pra cima.
            string row = mask[h - 1 - y];

            for (int x = 0; x < w; x++)
            {
                bool filled = row[x] == '1';
                tex.SetPixel(x, y, filled ? color : new Color(0f, 0f, 0f, 0f));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
