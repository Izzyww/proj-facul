using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela inicial: fundo preto, um paragrafo de historia aparece digitando, e um
// "continuar" embaixo. Enquanto a tela esta ativa o jogo fica pausado
// (Time.timeScale = 0); ao clicar, a fase comeca e a trilha entra.
public class IntroScreen : MonoBehaviour
{
    public static bool IsActive { get; private set; }
    private static bool s_AlreadyShown;

    private const string Story =
        "Uma gangue de ladroes invadiu o vale e esta roubando toda a comida da " +
        "aldeia. Foxy, a raposa mais corajosa da floresta, decidiu recuperar cada " +
        "fruta e por um fim na bagunca. Muitos perigos esperam pelo caminho: " +
        "armadilhas, abismos e ate o chefe da gangue. Boa sorte!";

    private AudioClip m_IntroMusic;
    private TextMeshProUGUI m_StoryText;
    private TextMeshProUGUI m_ContinueText;

    private float m_RevealTimer;
    private int m_Visible;
    private bool m_Done;

    public static void ShowOnce(AudioClip introMusic)
    {
        if (s_AlreadyShown)
        {
            return;
        }

        s_AlreadyShown = true;
        GameObject obj = new GameObject("IntroScreen");
        IntroScreen intro = obj.AddComponent<IntroScreen>();
        intro.m_IntroMusic = introMusic;
    }

    private void Awake()
    {
        IsActive = true;
        Time.timeScale = 0f;
        BuildUi();

        if (m_IntroMusic != null)
        {
            AudioManager.GetOrCreate().PlayBgm(m_IntroMusic);
        }
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Fundo preto cobrindo tudo.
        GameObject bg = new GameObject("Black");
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.SetParent(transform, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.black;

        m_StoryText = MakeText("Story", new Vector2(0.5f, 0.5f), new Vector2(1200f, 500f), 44f);
        m_StoryText.alignment = TextAlignmentOptions.Center;
        m_StoryText.text = Story;
        m_StoryText.maxVisibleCharacters = 0;

        m_ContinueText = MakeText("Continue", new Vector2(0.5f, 0.12f), new Vector2(900f, 90f), 38f);
        m_ContinueText.alignment = TextAlignmentOptions.Center;
        m_ContinueText.text = "clique para continuar";
        m_ContinueText.color = new Color(1f, 1f, 1f, 0f);
    }

    private TextMeshProUGUI MakeText(string name, Vector2 anchor, Vector2 size, float fontSize)
    {
        GameObject obj = new GameObject(name);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        return text;
    }

    private void Update()
    {
        // Tudo aqui roda em tempo "nao escalado" porque o jogo esta pausado.
        if (!m_Done)
        {
            m_RevealTimer += Time.unscaledDeltaTime;
            if (m_RevealTimer >= 0.025f)
            {
                m_RevealTimer = 0f;
                m_Visible++;
                m_StoryText.maxVisibleCharacters = m_Visible;

                if (m_Visible >= Story.Length)
                {
                    m_Done = true;
                }
            }

            // Deixa pular a digitacao com um clique/tecla.
            if (ContinuePressed())
            {
                m_Visible = Story.Length;
                m_StoryText.maxVisibleCharacters = m_Visible;
                m_Done = true;
            }

            return;
        }

        // Pisca o "continuar" e espera o clique pra comecar.
        float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
        m_ContinueText.color = new Color(1f, 1f, 1f, 0.35f + pulse * 0.65f);

        if (ContinuePressed())
        {
            Continue();
        }
    }

    private bool ContinuePressed()
    {
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    private void Continue()
    {
        AudioManager.Play(AudioManager.Sfx.Click);
        IsActive = false;
        Time.timeScale = 1f;

        if (LevelGenerator.Instance != null)
        {
            LevelGenerator.Instance.PlayCurrentMusic();
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        IsActive = false;
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
