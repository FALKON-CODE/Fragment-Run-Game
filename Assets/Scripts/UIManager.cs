using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds and drives all UI from code (main menu, pause menu, HUD, banners and
/// the win screen) so the scene files stay tiny. In the MainMenu scene it shows
/// the main menu; in gameplay scenes it shows the HUD and menus on demand.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // Theme colours (sci-fi: dark panels, cyan accent).
    private static readonly Color Accent = new Color(0.20f, 0.90f, 1.00f);
    private static readonly Color Panel = new Color(0.04f, 0.06f, 0.12f, 0.92f);
    private static readonly Color ButtonColor = new Color(0.12f, 0.16f, 0.26f, 1f);

    private Font font;
    private Canvas canvas;

    private GameObject pausePanel;
    private GameObject winPanel;
    private Text levelLabel;
    private Text bannerText;

    private Image fadeImage;
    private const float FadeDuration = 0.4f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        BuildCanvas();

        // MainMenu scene has no GameManager: show the menu. Otherwise show the HUD.
        if (GameManager.Instance == null)
            BuildMainMenu();
        else
            BuildGameplayUI();

        BuildFade(); // Always on top of every panel.
    }

    void Start()
    {
        // Fade in from black whenever a scene starts.
        StartCoroutine(Fade(1f, 0f));
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ----- Public API (called by GameManager) ---------------------------

    public void SetLevel(int levelNumber)
    {
        if (levelLabel != null)
            levelLabel.text = "LEVEL " + levelNumber;
    }

    public void SetPauseVisible(bool visible)
    {
        if (pausePanel != null)
            pausePanel.SetActive(visible);
    }

    public void ShowBanner(string message)
    {
        if (bannerText != null)
            bannerText.text = message;
    }

    public void ShowWin()
    {
        if (winPanel != null)
            winPanel.SetActive(true);
    }

    // ----- UI construction ----------------------------------------------

    void BuildCanvas()
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // An EventSystem is required for buttons to receive clicks.
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildMainMenu()
    {
        GameObject panel = CreateFullScreenPanel("MainMenuPanel", new Color(0.04f, 0.06f, 0.12f, 1f));

        // Use one of the level backgrounds as the menu backdrop (dimmed).
        Sprite bg = Resources.Load<Sprite>("Art/bg_level3");
        if (bg != null)
        {
            Image bgImage = panel.GetComponent<Image>();
            bgImage.sprite = bg;
            bgImage.type = Image.Type.Simple;
            bgImage.color = new Color(0.45f, 0.5f, 0.6f, 1f);
        }

        CreateText(panel.transform, "FRAGMENT RUN", 96, Accent,
            new Vector2(0, 220), new Vector2(1200, 160), FontStyle.Bold);
        CreateText(panel.transform, "A short cyber platformer", 34,
            new Color(0.7f, 0.8f, 0.9f), new Vector2(0, 120), new Vector2(1000, 60), FontStyle.Normal);

        CreateButton(panel.transform, "START", new Vector2(0, -20), OnStart);
        CreateButton(panel.transform, "QUIT", new Vector2(0, -140), OnQuit);
    }

    void BuildGameplayUI()
    {
        // HUD level label (top-left).
        levelLabel = CreateText(canvas.transform, "LEVEL", 40, Accent,
            Vector2.zero, new Vector2(400, 70), FontStyle.Bold);
        RectTransform lr = levelLabel.rectTransform;
        lr.anchorMin = lr.anchorMax = new Vector2(0, 1);
        lr.pivot = new Vector2(0, 1);
        lr.anchoredPosition = new Vector2(40, -30);
        levelLabel.alignment = TextAnchor.UpperLeft;

        // Centre banner for "LEVEL COMPLETE" / "YOU DIED".
        bannerText = CreateText(canvas.transform, "", 80, Accent,
            new Vector2(0, 120), new Vector2(1400, 140), FontStyle.Bold);

        BuildPausePanel();
        BuildWinPanel();
    }

    void BuildPausePanel()
    {
        pausePanel = CreateFullScreenPanel("PausePanel", Panel);
        CreateText(pausePanel.transform, "PAUSED", 80, Accent,
            new Vector2(0, 200), new Vector2(800, 120), FontStyle.Bold);
        CreateButton(pausePanel.transform, "RESUME", new Vector2(0, 40), OnResume);
        CreateButton(pausePanel.transform, "RESTART", new Vector2(0, -80), OnRestart);
        CreateButton(pausePanel.transform, "MAIN MENU", new Vector2(0, -200), OnMainMenu);
        pausePanel.SetActive(false);
    }

    void BuildWinPanel()
    {
        winPanel = CreateFullScreenPanel("WinPanel", Panel);
        CreateText(winPanel.transform, "YOU ESCAPED!", 84, Accent,
            new Vector2(0, 200), new Vector2(1200, 140), FontStyle.Bold);
        CreateText(winPanel.transform, "Thanks for playing.", 34,
            new Color(0.7f, 0.8f, 0.9f), new Vector2(0, 90), new Vector2(900, 60), FontStyle.Normal);
        CreateButton(winPanel.transform, "PLAY AGAIN", new Vector2(0, -30), OnRestartFromWin);
        CreateButton(winPanel.transform, "MAIN MENU", new Vector2(0, -150), OnMainMenu);
        winPanel.SetActive(false);
    }

    // ----- Scene fade ---------------------------------------------------

    void BuildFade()
    {
        GameObject go = new GameObject("Fade", typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        fadeImage = go.GetComponent<Image>();
        fadeImage.color = Color.black;      // starts opaque; Start() fades it in.
        fadeImage.raycastTarget = false;    // never blocks the buttons underneath.

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Fades to black, then loads the requested scene.</summary>
    public void FadeAndLoad(string sceneName)
    {
        StartCoroutine(FadeOutThenLoad(sceneName));
    }

    IEnumerator FadeOutThenLoad(string sceneName)
    {
        yield return Fade(0f, 1f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;

        float t = 0f;
        Color c = fadeImage.color;
        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime; // works even while the game is paused.
            c.a = Mathf.Lerp(from, to, t / FadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }

    // ----- Button callbacks ---------------------------------------------

    void OnStart()
    {
        FadeAndLoad("Level1");
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnResume()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }

    void OnRestart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
    }

    void OnRestartFromWin()
    {
        FadeAndLoad("Level1");
    }

    void OnMainMenu()
    {
        FadeAndLoad("MainMenu");
    }

    // ----- Small UI factory helpers -------------------------------------

    GameObject CreateFullScreenPanel(string panelName, Color color)
    {
        GameObject go = new GameObject(panelName, typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        Image img = go.GetComponent<Image>();
        img.color = color;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    Text CreateText(Transform parent, string content, int size, Color color,
        Vector2 anchoredPos, Vector2 sizeDelta, FontStyle style)
    {
        GameObject go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rt = text.rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return text;
    }

    Button CreateButton(Transform parent, string label, Vector2 anchoredPos,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(label + "Button", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color = ButtonColor;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(420, 96);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.18f, 0.26f, 0.40f, 1f);
        colors.pressedColor = Accent;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        Text text = CreateText(go.transform, label, 40, Accent,
            Vector2.zero, new Vector2(420, 96), FontStyle.Bold);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        return button;
    }
}
