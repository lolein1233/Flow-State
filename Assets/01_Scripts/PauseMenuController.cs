using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Uses the editable scene menus, with a runtime fallback for older scenes.
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
    public bool IsControlsOpen { get; private set; }
    public static bool GameplayInputBlocked => Instance != null && Instance.IsPaused;

    [Header("Menús editables de la escena")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button controlsCloseButton;
    private FPSController fpsController;
    private GraffitiPainter graffitiPainter;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool previousAudioPause;
    private bool closing;
    private readonly List<Canvas> hiddenHud = new List<Canvas>();

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

        if (Instance != null)
            return;

        var existing = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return;
        }

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
        if (menuRoot == null || controlsRoot == null)
            CreateEditableMenus();
        menuRoot.SetActive(false);
        controlsRoot.SetActive(false);
        WireMenuCallbacks();
    }

    public void CreateEditableMenus()
    {
        if (menuRoot != null || controlsRoot != null) return;
        fpsController = Object.FindFirstObjectByType<FPSController>(FindObjectsInactive.Include);
        graffitiPainter = Object.FindFirstObjectByType<GraffitiPainter>(FindObjectsInactive.Include);
        BuildMenu();
        menuRoot.SetActive(false);
        BuildControlsFromOriginal();
        EnhancePausePresentation();
        WireMenuCallbacks();
    }

    private void WireMenuCallbacks()
    {
        resumeButton.onClick.RemoveAllListeners();
        resumeButton.onClick.AddListener(ResumeGame);
        mainMenuButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.AddListener(GoToMainMenu);
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(QuitGame);
        controlsButton.onClick.RemoveAllListeners();
        controlsButton.onClick.AddListener(ShowControlsMenu);
        controlsCloseButton.onClick.RemoveAllListeners();
        controlsCloseButton.onClick.AddListener(ResumeGame);
        for (int i = 0; i < controlsTabs.Length; i++)
        {
            int page = i;
            controlsTabs[i].onClick.RemoveAllListeners();
            controlsTabs[i].onClick.AddListener(() => SetControlsPage(page));
        }
    }

    private void Update()
    {
        if (IsPaused)
        {
            if (closing)
                return;

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (IsControlsOpen) ResumeGame();
                else ShowControlsMenu();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                ResumeGame();

            if (IsControlsOpen && Input.GetKeyDown(KeyCode.RightArrow)) SetControlsPage(1);
            if (IsControlsOpen && Input.GetKeyDown(KeyCode.LeftArrow)) SetControlsPage(0);

            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ShowControlsMenu();
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
        if (closing)
            return;

        if (!IsPaused)
            BeginPause();

        IsControlsOpen = false;
        controlsRoot.SetActive(false);
        menuRoot.SetActive(true);
        SelectMenuButton(resumeButton);
    }

    private void BeginPause()
    {

        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        previousAudioPause = AudioListener.pause;

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Prevent gameplay input from being buffered while time is frozen. In particular,
        // a Space press used on the menu must not become a jump on the first resumed frame.
        if (fpsController != null)
            fpsController.SetPauseInputSuspended(true);

        if (graffitiPainter != null)
            graffitiPainter.CancelPaintingForMenu();

        hiddenHud.Clear();
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.enabled || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace ||
                canvas.transform.IsChildOf(transform))
                continue;
            hiddenHud.Add(canvas);
            canvas.enabled = false;
        }
    }

    public void ResumeGame()
    {
        if (!IsPaused || closing)
            return;

        StartCoroutine(CloseMenus());
    }

    private IEnumerator CloseMenus()
    {
        closing = true;
        SelectMenuButton(null);
        var motion = (IsControlsOpen ? controlsRoot : menuRoot).GetComponent<PunkMenuMotion>();
        if (motion != null) motion.PlayClose();
        yield return new WaitForSecondsRealtime(0.18f);
        RestoreGameplayState();
        menuRoot.SetActive(false);
        controlsRoot.SetActive(false);
        closing = false;
    }

    public void GoToMainMenu()
    {
        StopAllCoroutines();
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
        IsControlsOpen = false;
        Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;

        if (fpsController != null)
            fpsController.SetPauseInputSuspended(false);

        foreach (Canvas canvas in hiddenHud)
            if (canvas != null) canvas.enabled = true;
        hiddenHud.Clear();
    }

    private static void SelectMenuButton(Button button)
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        if (button != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
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
        this.mainMenuButton = mainMenuButton;
        this.quitButton = quitButton;

        resumeButton.onClick.AddListener(ResumeGame);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
        quitButton.onClick.AddListener(QuitGame);

        TextMeshProUGUI hint = CreateText("Hint", panel, font, "ESC  ·  VOLVER AL JUEGO", 18f, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(54f, 16f), new Vector2(-54f, 46f));
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
        if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        if (Application.isPlaying) Object.DontDestroyOnLoad(eventSystem);
    }
    [SerializeField] private GameObject controlsRoot;
    [SerializeField] private GameObject[] controlsPages;
    [SerializeField] private Button[] controlsTabs;
    [SerializeField] private TextMeshProUGUI controlsPageLabel;

    public void ShowControlsMenu()
    {
        if (closing) return;
        if (!IsPaused) BeginPause();
        IsControlsOpen = true;
        menuRoot.SetActive(false);
        controlsRoot.SetActive(true);
        SetControlsPage(0);
        SelectMenuButton(controlsTabs[0]);
    }

    public void SetControlsPage(int page)
    {
        page = Mathf.Clamp(page, 0, controlsPages.Length - 1);
        for (int i = 0; i < controlsPages.Length; i++)
        {
            controlsPages[i].SetActive(i == page);
            controlsTabs[i].transform.Find("Accent").GetComponent<UnityEngine.UI.Image>().color =
                i == page ? OrangeBright : PaperMuted;
        }
        controlsPageLabel.text = page == 0 ? "GUÍA DE CALLE / 01 · MOVIMIENTO + GRAFFITI" : "GUÍA DE CALLE / 02 · MÚSICA + PRESENTACIÓN";
    }

    private void BuildControlsFromOriginal()
    {
        // Duplicar antes de añadir animaciones: conservar el diseño original de pausa.
        controlsRoot = Instantiate(menuRoot, transform, false);
        controlsRoot.name = "ControlsMenu - Original Pause Design";
        controlsRoot.GetComponent<Canvas>().sortingOrder = 32000;
        var shade = (RectTransform)controlsRoot.transform.Find("NightShade");
        var panel = (RectTransform)shade.Find("GraffitiPaper");
        var shadow = (RectTransform)shade.Find("PanelShadow");
        panel.sizeDelta = shadow.sizeDelta = new Vector2(1460f, 940f);

        var title = panel.Find("Title").GetComponent<TextMeshProUGUI>();
        title.text = "MENÚ DE BOTONES";
        title.fontSize = 85f;
        SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(70f, -216f), new Vector2(-70f, -102f));
        panel.Find("Divider").GetComponent<RectTransform>().anchoredPosition = new Vector2(62f, -226f);
        panel.Find("MainMenuButton").gameObject.SetActive(false);
        panel.Find("QuitButton").gameObject.SetActive(false);

        var close = panel.Find("ResumeButton").GetComponent<UnityEngine.UI.Button>();
        controlsCloseButton = close;
        close.onClick.RemoveAllListeners();
        close.onClick.AddListener(ResumeGame);
        close.GetComponent<RectTransform>().anchoredPosition = new Vector2(455f, -838f);
        close.GetComponent<RectTransform>().sizeDelta = new Vector2(390f, 74f);
        close.transform.Find("Label").GetComponent<TMP_Text>().text = "VOLVER AL JUEGO";
        close.transform.Find("Number").GetComponent<TMP_Text>().text = "B";
        panel.Find("Hint").GetComponent<TMP_Text>().text = "B / ESC · CERRAR     |     ← → · CAMBIAR PÁGINA     |     EL JUEGO ESTÁ EN PAUSA";

        TMP_FontAsset bodyFont = ResolveReadableFont();
        controlsPageLabel = CreateText("PageLabel", panel.Find("TopInk"), bodyFont, string.Empty, 23f, Paper,
            TextAlignmentOptions.Left, FontStyles.Bold);
        SetRect(controlsPageLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(64f, 0f), new Vector2(-40f, 0f));

        controlsPages = new GameObject[2];
        controlsTabs = new Button[2];
        string[] tabs = { "MOVERSE / PINTAR", "MÚSICA / DEMO" };
        for (int i = 0; i < 2; i++)
        {
            controlsPages[i] = CreateRect("ControlsPage" + (i + 1), panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            controlsPages[i].transform.SetAsFirstSibling();
            // Detrás del texto fijo, pero sobre el papel del padre.
            controlsTabs[i] = CreateButton("ControlsTab" + i, panel, title.font, tabs[i], (i + 1).ToString("00"), new Vector2(-455f + i * 455f, -838f));
            controlsTabs[i].GetComponent<RectTransform>().sizeDelta = new Vector2(430f, 74f);
            int page = i;
            controlsTabs[i].onClick.AddListener(() => SetControlsPage(page));
        }
        WireNavigation(controlsTabs[0], controlsTabs[1], close);

        string jump = KeyLabel(fpsController != null ? fpsController.jumpKey : KeyCode.Space);
        string sprint = KeyLabel(fpsController != null ? fpsController.sprintKey : KeyCode.LeftShift);
        string graffiti = KeyLabel(fpsController != null ? fpsController.graffitiModeKey : KeyCode.Q);
        var drawer = fpsController != null ? fpsController.GetComponentInChildren<GraffitiMenuDrawer>(true) : null;
        var resources = graffitiPainter != null ? graffitiPainter.sprayResources : null;
        // Estos sistemas pueden crearse en Awake después del menú de pausa.
        string shake = KeyLabel(resources != null ? resources.ShakeKey : KeyCode.R);
        string can = KeyLabel(resources != null ? resources.ChangeCanKey : KeyCode.F);

        AddControlsColumn(controlsPages[0].transform, bodyFont, "01 / MUÉVETE POR LA CIUDAD", 0, new string[,]
        {
            { "W A S D", "Moverte" },
            { "MOUSE", "Mirar y orientar la cámara" },
            { sprint, "Correr" },
            { jump, "Saltar" },
            { jump + " / PARED", "Frente a un obstáculo: parkour / escalar" },
            { jump + " x2", "Doble toque rápido: rodada aérea" },
            { "W A S D / PARED", "Mientras cuelgas: moverte por la pared" },
            { jump + " / COLGADO", "Subir al borde o saltar entre paredes" },
            { "CTRL / ESC", "Soltarte de la pared" },
            { "ESC", "Pausa / volver al juego" }
        });
        AddControlsColumn(controlsPages[0].transform, bodyFont, "02 / DEJA TU MARCA", 1, new string[,]
        {
            { graffiti + " / PARED", "Entrar al modo graffiti" },
            { graffiti + " OTRA VEZ", "Abrir / cerrar colores y boquillas" },
            { "CLIC IZQUIERDO", "Mantener para pintar" },
            { KeyLabel(drawer != null ? drawer.selectKey : KeyCode.E) + " / CLIC", "Elegir color o boquilla en el menú" },
            { "TAB", "Cambiar de boquilla" },
            { shake, "Agitar y recuperar la mezcla" },
            { can, "Cambiar la lata cuando esté vacía" },
            { "RUEDA MOUSE", "Acercar / alejar la lata" },
            { "ESC", "Cerrar menú; otra vez: salir de graffiti" },
            { "B", "Abrir / cerrar esta ayuda" }
        });
        var music = fpsController != null ? fpsController.GetComponent<DiegeticMusicPlayer>() : null;
        AddControlsColumn(controlsPages[1].transform, bodyFont, "03 / LLEVA TU RITMO", 0, new string[,]
        {
            { KeyLabel(music != null ? music.toggleKey : KeyCode.M), "Abrir / guardar el reproductor" },
            { KeyLabel(music != null ? music.playKey : KeyCode.P), "Reproducir / pausar música (abierto)" },
            { "1 / 2 / 3 / 4", "Elegir pista (reproductor abierto)" },
            { "↑ / ↓", "Subir / bajar BPM (abierto)" },
            { "CLIC DERECHO", "Fijar / soltar un objetivo cercano" },
            { "F / FUERA GRAFFITI", "Sentarte / levantarte para una pose" }
        });
        AddControlsColumn(controlsPages[1].transform, bodyFont, "04 / PRESENTACIÓN", 1, new string[,]
        {
            { "F7", "Volver a Lugar 1" },
            { "F8", "Ir a Lugar 2" },
            { "F1", "Abrir / cerrar menú de emociones" },
            { "F2 / F3 / F4", "Neutral / duda / ira (menú abierto)" },
            { "F5 / F6", "Flow creativo / claridad (menú abierto)" },
            { "B / ESC", "Cerrar esta ayuda y volver al juego" }
        });
        var note = CreateText("ContextNote", controlsPages[1].transform, bodyFont,
            "LOS ATAJOS DE PINTURA FUNCIONAN EN MODO GRAFFITI.\nACÉRCATE A UNA SUPERFICIE PINTABLE Y MÍRALA PARA EMPEZAR.",
            25f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(note.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(75f, -770f), new Vector2(-75f, -670f));

        var motion = controlsRoot.AddComponent<PunkMenuMotion>();
        motion.Configure(panel, shadow, null, controlsRoot.GetComponent<CanvasGroup>(), false);
        controlsRoot.SetActive(false);
        SetControlsPage(0);
    }

    private static void AddControlsColumn(Transform parent, TMP_FontAsset font, string heading, int column, string[,] rows)
    {
        float left = 70f + column * 690f;
        var section = CreateText(heading, parent, font, heading, 28f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        SetRect(section.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -284f), new Vector2(left + 650f, -244f));
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = -316f - i * 47f;
            var key = CreateRect("Key" + i, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(left + 112f, y), new Vector2(224f, 37f));
            key.gameObject.AddComponent<UnityEngine.UI.Image>().color = Ink;
            var keyText = CreateText("KeyLabel", key, font, rows[i, 0], 22f, Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            keyText.enableAutoSizing = true;
            keyText.fontSizeMin = 16f;
            keyText.fontSizeMax = 22f;
            var description = CreateText("Action" + i, parent, font, rows[i, 1], 23f, Ink, TextAlignmentOptions.Left, FontStyles.Normal);
            SetRect(description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(left + 239f, y - 20f), new Vector2(left + 650f, y + 20f));
            description.enableAutoSizing = true;
            description.fontSizeMin = 19f;
            description.fontSizeMax = 23f;
        }
    }

    private static TMP_FontAsset ResolveReadableFont()
    {
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (font != null && font.name == "SpaceGrotesk-Bold SDF") return font;
        return TMP_Settings.defaultFontAsset;
    }

    private static string KeyLabel(KeyCode key)
    {
        if (key == KeyCode.Space) return "ESPACIO";
        if (key == KeyCode.LeftShift || key == KeyCode.RightShift) return "SHIFT";
        return key.ToString().ToUpperInvariant();
    }

    private static void WireNavigation(params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var navigation = buttons[i].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = navigation.selectOnLeft = buttons[(i + buttons.Length - 1) % buttons.Length];
            navigation.selectOnDown = navigation.selectOnRight = buttons[(i + 1) % buttons.Length];
            buttons[i].navigation = navigation;
        }
    }

private void EnhancePausePresentation()
    {
        var shade = (RectTransform)menuRoot.transform.Find("NightShade");
        shade.GetComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.018f, 0.06f, 0.38f);
        var panel = (RectTransform)shade.Find("GraffitiPaper");
        var shadow = (RectTransform)shade.Find("PanelShadow");
        panel.sizeDelta = shadow.sizeDelta = new Vector2(900f, 900f);
        var title = panel.Find("Title").GetComponent<TextMeshProUGUI>();
        title.fontSize = 162f;
        SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(66f, -268f), new Vector2(-62f, -95f));

        // Ecos de registro de tinta, como el desdoblamiento cromático del menú principal.
        MakeTitleEcho(title, "CyanRegistration", new Vector2(-6f, 3f), new Color(0.03f, 0.67f, 0.72f, 0.28f));
        MakeTitleEcho(title, "MagentaRegistration", new Vector2(6f, -2f), new Color(0.83f, 0.08f, 0.31f, 0.22f));
        title.gameObject.AddComponent<HolographicTextEffect>();
        var subtitle = CreateText("PauseSubtitle", panel, ResolveReadableFont(), "RESPIRA. VUELVE A DEJAR TU MARCA.",
            22f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        SetRect(subtitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(70f, -300f), new Vector2(-60f, -264f));
        panel.Find("Divider").GetComponent<RectTransform>().anchoredPosition = new Vector2(62f, -312f);

        var edition = CreateText("Edition", panel.Find("TopInk"), ResolveReadableFont(), "FLOWSTATE / INTERMEDIO", 24f,
            Paper, TextAlignmentOptions.Left, FontStyles.Bold);
        SetRect(edition.rectTransform, Vector2.zero, Vector2.one, new Vector2(65f, 0f), new Vector2(-40f, 0f));

        var main = panel.Find("MainMenuButton").GetComponent<UnityEngine.UI.Button>();
        var quit = panel.Find("QuitButton").GetComponent<UnityEngine.UI.Button>();
        var controls = CreateButton("ControlsButton", panel, title.font, "MENÚ DE BOTONES", "B", new Vector2(0f, -500f));
        controlsButton = controls;
        controls.onClick.AddListener(ShowControlsMenu);
        var buttons = new[] { resumeButton, controls, main, quit };
        for (int i = 0; i < buttons.Length; i++)
        {
            var rect = buttons[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -386f - i * 118f);
            rect.sizeDelta = new Vector2(748f, 98f);
            var image = buttons[i].GetComponent<UnityEngine.UI.Image>();
            image.color = Color.white;
            var colors = buttons[i].colors;
            colors.normalColor = Ink;
            colors.highlightedColor = OrangeBright;
            colors.selectedColor = Orange;
            colors.pressedColor = PaperMuted;
            colors.fadeDuration = 0.1f;
            buttons[i].colors = colors;
            var label = buttons[i].transform.Find("Label").GetComponent<TextMeshProUGUI>();
            label.fontSizeMax = 41f;
            label.fontSize = 41f;
            buttons[i].gameObject.AddComponent<PunkMenuButton>().Configure(Paper, Ink);
        }
        main.transform.Find("Number").GetComponent<TMP_Text>().text = "02";
        quit.transform.Find("Number").GetComponent<TMP_Text>().text = "03";
        panel.Find("Hint").GetComponent<TMP_Text>().text = "ESC · VOLVER AL JUEGO";
        WireNavigation(buttons);

        var backdrop = shade.Find("OrangeWash").GetComponent<UnityEngine.UI.Image>();
        backdrop.name = "HolographicBackdrop";
        SetRect(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Material foilMaterial = Resources.Load<Material>("UI/PauseHolographicFoil");
        if (foilMaterial != null)
        {
            backdrop.material = foilMaterial;
            backdrop.color = Color.white;
        }

        var motion = menuRoot.AddComponent<PunkMenuMotion>();
        motion.Configure(panel, shadow, title.rectTransform, canvasGroup, true, backdrop);
        foreach (UnityEngine.UI.Graphic graphic in menuRoot.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            if (graphic.GetComponent<UnityEngine.UI.Button>() == null && graphic.name != "NightShade")
                graphic.raycastTarget = false;
    }

    private static void MakeTitleEcho(TextMeshProUGUI source, string name, Vector2 offset, Color color)
    {
        var echo = Instantiate(source, source.transform.parent, false);
        echo.name = name;
        echo.rectTransform.anchoredPosition += offset;
        echo.color = color;
        echo.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
    }

}
