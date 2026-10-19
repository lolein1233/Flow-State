using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates and controls the gameplay pause menu at runtime.
/// Keeping the bootstrap here avoids adding more serialized state to the gameplay scene.
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class PauseMenuController : MonoBehaviour
{
    private const string GameplayScenePath = "Assets/02_Escenas/Gameplay.unity";
    private const string MainMenuScenePath = "Assets/02_Escenas/MainMenu_FlowState.unity";

    private static readonly Color Ink = new Color32(31, 18, 48, 255);
    private static readonly Color Paper = new Color32(238, 224, 199, 255);
    private static readonly Color PaperMuted = new Color32(205, 190, 168, 255);
    private static readonly Color Orange = new Color32(241, 91, 46, 255);
    private static readonly Color OrangeBright = new Color32(255, 130, 62, 255);

    public static PauseMenuController Instance { get; private set; }
    public bool IsPaused { get; private set; }

    private GameObject menuRoot;
    private CanvasGroup canvasGroup;
    private Button resumeButton;
    private FPSController fpsController;
    private GraffitiPainter graffitiPainter;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInitialScene()
    {
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void HandleActiveSceneChanged(Scene previous, Scene current)
    {
        EnsureForScene(current);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.isLoaded || scene.path != GameplayScenePath)
            return;

        if (Instance != null || Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include) != null)
            return;

        new GameObject("PauseMenuController").AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        fpsController = Object.FindFirstObjectByType<FPSController>(FindObjectsInactive.Include);
        graffitiPainter = Object.FindFirstObjectByType<GraffitiPainter>(FindObjectsInactive.Include);
        BuildMenu();
        menuRoot.SetActive(false);
    }

    private void Update()
    {
        if (IsPaused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ResumeGame();

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !AnotherGameplayModeUsesEscape())
            PauseGame();
    }

    private void LateUpdate()
    {
        if (!IsPaused)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PauseGame()
    {
        if (IsPaused)
            return;

        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        IsPaused = true;
        canvasGroup.alpha = 1f;
        menuRoot.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Prevent gameplay input from being buffered while time is frozen. In particular,
        // a Space press used on the menu must not become a jump on the first resumed frame.
        if (fpsController != null)
            fpsController.SetPauseInputSuspended(true);

        EventSystem.current?.SetSelectedGameObject(null);
        EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void ResumeGame()
    {
        if (!IsPaused)
            return;

        RestoreGameplayState();
        menuRoot.SetActive(false);
    }

    public void GoToMainMenu()
    {
        RestoreGameplayState();
        SceneManager.LoadScene(MainMenuScenePath);
    }

    public void QuitGame()
    {
        RestoreGameplayState();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool AnotherGameplayModeUsesEscape()
    {
        if (graffitiPainter != null && (graffitiPainter.menuOpen || graffitiPainter.IsPainting))
            return true;

        return fpsController != null && (fpsController.IsGraffitiMode() || fpsController.IsClimbing());
    }

    private void RestoreGameplayState()
    {
        IsPaused = false;
        Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
        AudioListener.pause = false;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;

        if (fpsController != null)
            fpsController.SetPauseInputSuspended(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (IsPaused)
            RestoreGameplayState();
    }

    private void BuildMenu()
    {
        EnsureEventSystem();

        menuRoot = new GameObject("PauseMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        menuRoot.transform.SetParent(transform, false);

        Canvas canvas = menuRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = menuRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = menuRoot.GetComponent<CanvasGroup>();

        RectTransform shade = CreateRect("NightShade", menuRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image shadeImage = shade.gameObject.AddComponent<Image>();
        shadeImage.color = new Color(0.035f, 0.018f, 0.06f, 0.86f);
        shadeImage.raycastTarget = true;

        RectTransform orangeWash = CreateRect("OrangeWash", shade, new Vector2(0f, 0f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero);
        orangeWash.gameObject.AddComponent<Image>().color = new Color(0.76f, 0.19f, 0.08f, 0.13f);

        RectTransform panelShadow = CreateRect("PanelShadow", shade, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(24f, -22f), new Vector2(710f, 760f));
        panelShadow.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        panelShadow.localEulerAngles = new Vector3(0f, 0f, -1.2f);

        RectTransform panel = CreateRect("GraffitiPaper", shade, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(710f, 760f));
        panel.gameObject.AddComponent<Image>().color = Paper;
        panel.localEulerAngles = new Vector3(0f, 0f, 0.65f);

        RectTransform topInk = CreateRect("TopInk", panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -34f), new Vector2(0f, 68f));
        topInk.gameObject.AddComponent<Image>().color = Ink;

        RectTransform topAccent = CreateRect("TopAccent", panel, new Vector2(0f, 1f), new Vector2(0.68f, 1f),
            new Vector2(0f, -74f), new Vector2(0f, 12f));
        topAccent.gameObject.AddComponent<Image>().color = Orange;

        RectTransform sideAccent = CreateRect("SideAccent", panel, new Vector2(0f, 0f), new Vector2(0f, 0.83f),
            new Vector2(18f, 0f), new Vector2(20f, 0f));
        sideAccent.gameObject.AddComponent<Image>().color = Orange;

        TMP_FontAsset font = ResolveGameFont();

        TextMeshProUGUI title = CreateText("Title", panel, font, "PAUSA", 100f, Ink,
            TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(62f, -238f), new Vector2(-62f, -104f));
        title.characterSpacing = -2f;

        RectTransform divider = CreateRect("Divider", panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(62f, -248f), new Vector2(-124f, 5f));
        divider.gameObject.AddComponent<Image>().color = Ink;

        resumeButton = CreateButton("ResumeButton", panel, font, "VOLVER", "01", new Vector2(0f, -326f));
        Button mainMenuButton = CreateButton("MainMenuButton", panel, font, "MENÚ PRINCIPAL", "02", new Vector2(0f, -456f));
        Button quitButton = CreateButton("QuitButton", panel, font, "SALIR", "03", new Vector2(0f, -586f));

        resumeButton.onClick.AddListener(ResumeGame);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
        quitButton.onClick.AddListener(QuitGame);

        TextMeshProUGUI hint = CreateText("Hint", panel, font, "ESC  ·  VOLVER AL JUEGO", 18f, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(54f, 23f), new Vector2(-54f, 32f));
        hint.alpha = 0.62f;

        Navigation resumeNavigation = resumeButton.navigation;
        resumeNavigation.mode = Navigation.Mode.Explicit;
        resumeNavigation.selectOnDown = mainMenuButton;
        resumeNavigation.selectOnUp = quitButton;
        resumeButton.navigation = resumeNavigation;

        Navigation mainNavigation = mainMenuButton.navigation;
        mainNavigation.mode = Navigation.Mode.Explicit;
        mainNavigation.selectOnDown = quitButton;
        mainNavigation.selectOnUp = resumeButton;
        mainMenuButton.navigation = mainNavigation;

        Navigation quitNavigation = quitButton.navigation;
        quitNavigation.mode = Navigation.Mode.Explicit;
        quitNavigation.selectOnDown = resumeButton;
        quitNavigation.selectOnUp = mainMenuButton;
        quitButton.navigation = quitNavigation;
    }

    private Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label, string number, Vector2 position)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position,
            new Vector2(580f, 102f));

        Image background = rect.gameObject.AddComponent<Image>();
        background.color = Ink;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = OrangeBright;
        colors.selectedColor = Orange;
        colors.pressedColor = new Color32(202, 61, 28, 255);
        colors.disabledColor = new Color32(91, 81, 95, 150);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        RectTransform stripe = CreateRect("Accent", rect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, 0f), new Vector2(16f, 0f));
        stripe.gameObject.AddComponent<Image>().color = Orange;

        TextMeshProUGUI numberText = CreateText("Number", rect, font, number, 20f, PaperMuted,
            TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(numberText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(31f, 0f), new Vector2(66f, 0f));

        TextMeshProUGUI buttonText = CreateText("Label", rect, font, label, 35f, Paper,
            TextAlignmentOptions.Left, FontStyles.Bold);
        SetRect(buttonText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(86f, 0f), new Vector2(-26f, 0f));
        buttonText.enableAutoSizing = true;
        buttonText.fontSizeMin = 24f;
        buttonText.fontSizeMax = 35f;
        buttonText.characterSpacing = 2f;

        return button;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string text,
        float size, Color color, TextAlignmentOptions alignment, FontStyles style)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = style;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private static TMP_FontAsset ResolveGameFont()
    {
        TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

        foreach (TMP_FontAsset loadedFont in loadedFonts)
        {
            if (loadedFont != null && loadedFont.name == "owned SDF")
                return loadedFont;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Object.DontDestroyOnLoad(eventSystem);
    }
}
