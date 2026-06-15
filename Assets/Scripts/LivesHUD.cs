using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HUD de vidas com cerejas no canto superior esquerdo. Mostra 3 icones; ao
// perder uma vida a cereja correspondente fica cinza. Posicionamento manual
// (sem LayoutGroup) e refresh direto do GameManager pra nao depender de ordem
// de execucao. O sprite e preenchido pelo LevelBuilder/LevelGenerator.
public class LivesHUD : MonoBehaviour
{
    public static LivesHUD Instance { get; private set; }

    public Sprite m_LifeSprite;

    private const float IconSize = 46f;
    private const float Spacing = 10f;
    private const float MarginX = 24f;
    private const float MarginY = 24f;

    private readonly List<Image> m_Icons = new List<Image>();
    private RectTransform m_Container;

    public static LivesHUD GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject obj = new GameObject("LivesHUD");
        return obj.AddComponent<LivesHUD>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (m_LifeSprite != null)
            {
                Instance.m_LifeSprite = m_LifeSprite;
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildCanvas();
    }

    private void OnEnable()
    {
        GameManager.OnLivesChanged += Refresh;
    }

    private void OnDisable()
    {
        GameManager.OnLivesChanged -= Refresh;
    }

    private void Start()
    {
        // Garante que aparece mesmo se o evento ja tiver disparado antes.
        int current = GameManager.Instance != null ? GameManager.Instance.Lives : 3;
        int max = GameManager.Instance != null ? GameManager.Instance.m_MaxLives : 3;
        Refresh(current, max);
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        GameObject container = new GameObject("Cherries");
        m_Container = container.AddComponent<RectTransform>();
        m_Container.SetParent(transform, false);
        m_Container.anchorMin = new Vector2(0f, 1f);
        m_Container.anchorMax = new Vector2(0f, 1f);
        m_Container.pivot = new Vector2(0f, 1f);
        m_Container.anchoredPosition = Vector2.zero;
        m_Container.sizeDelta = new Vector2(400f, 80f);
    }

    private void Refresh(int current, int max)
    {
        if (m_Container == null)
        {
            return;
        }

        while (m_Icons.Count < max)
        {
            int index = m_Icons.Count;

            GameObject icon = new GameObject("Life" + index);
            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.SetParent(m_Container, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            rect.anchoredPosition = new Vector2(MarginX + index * (IconSize + Spacing), -MarginY);

            Image img = icon.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            m_Icons.Add(img);
        }

        for (int i = 0; i < m_Icons.Count; i++)
        {
            bool used = i < max;
            m_Icons[i].gameObject.SetActive(used);
            m_Icons[i].sprite = m_LifeSprite;

            bool alive = i < current;
            m_Icons[i].color = alive ? Color.white : new Color(0.15f, 0.15f, 0.18f, 0.8f);
        }
    }
}
