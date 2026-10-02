using FlowState.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DemoEmotionMenu : MonoBehaviour
{
    [SerializeField] FlowStatePaletteController paletteController;
    [SerializeField] TMP_FontAsset font;
    [SerializeField] KeyCode toggleKey = KeyCode.F1;
    [SerializeField, Min(0f)] float transitionDuration = 0.25f;

    static readonly FlowEmotion[] Emotions =
    {
        FlowEmotion.Neutral,
        FlowEmotion.Doubt,
        FlowEmotion.Anger,
        FlowEmotion.CreativeFlow,
        FlowEmotion.Clarity
    };

    static readonly KeyCode[] EmotionKeys =
    {
        KeyCode.F2,
        KeyCode.F3,
        KeyCode.F4,
        KeyCode.F5,
        KeyCode.F6
    };

    static readonly string[] EmotionLabels =
    {
        "NEUTRAL",
        "DUDA",
        "IRA",
        "FLOW CREATIVO",
        "CLARIDAD"
    };

    Canvas runtimeCanvas;
    GameObject panel;
    TMP_Text[] rows;
    bool visible;
    FlowEmotion displayedEmotion;

    void Awake()
    {
        if (paletteController == null)
            paletteController = FindFirstObjectByType<FlowStatePaletteController>(FindObjectsInactive.Include);

        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetVisible(!visible);
            return;
        }

        if (!visible)
            return;

        for (int i = 0; i < EmotionKeys.Length; i++)
        {
            if (Input.GetKeyDown(EmotionKeys[i]))
            {
                SelectEmotion(Emotions[i]);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            SetVisible(false);

        if (paletteController != null && displayedEmotion != paletteController.CurrentEmotion)
            RefreshSelection();
    }

    public void Configure(FlowStatePaletteController controller, TMP_FontAsset uiFont)
    {
        paletteController = controller;
        font = uiFont;
    }

    public void SelectEmotion(FlowEmotion emotion)
    {
        if (paletteController == null)
            return;

        paletteController.SetEmotion(emotion, transitionDuration);
        RefreshSelection();
    }

    void SetVisible(bool value)
    {
        visible = value;
        if (panel != null)
            panel.SetActive(value);

        if (value)
            RefreshSelection();
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("Demo Emotion Menu - Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        runtimeCanvas = canvasObject.GetComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.sortingOrder = 600;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Emotion Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.anchoredPosition = new Vector2(-28f, -28f);
        panelRect.sizeDelta = new Vector2(380f, 326f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.075f, 0.025f, 0.09f, 0.94f);

        CreateText("Title", "DEMO  /  EMOCION", 27f, new Vector2(20f, -17f), new Vector2(340f, 38f), new Color(1f, 0.34f, 0.72f), FontStyles.Bold);
        CreateText("Hint", "[F1] ABRIR / CERRAR", 15f, new Vector2(20f, -54f), new Vector2(340f, 24f), new Color(0.82f, 0.74f, 0.84f), FontStyles.Normal);

        rows = new TMP_Text[Emotions.Length];
        for (int i = 0; i < Emotions.Length; i++)
        {
            string label = "[F" + (i + 2) + "]  " + EmotionLabels[i];
            rows[i] = CreateText("Emotion " + (i + 1), label, 22f, new Vector2(28f, -91f - i * 42f), new Vector2(324f, 34f), Color.white, FontStyles.Bold);
        }
    }

    TMP_Text CreateText(string objectName, string value, float size, Vector2 position, Vector2 dimensions, Color color, FontStyles style)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    void RefreshSelection()
    {
        if (paletteController == null || rows == null)
            return;

        displayedEmotion = paletteController.CurrentEmotion;
        for (int i = 0; i < rows.Length; i++)
        {
            bool selected = Emotions[i] == displayedEmotion;
            rows[i].color = selected ? new Color(0.72f, 1f, 0.18f) : new Color(0.95f, 0.9f, 0.96f);
            rows[i].transform.localScale = selected ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
        }
    }
}
