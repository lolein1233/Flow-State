using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SprayResourceHUD : MonoBehaviour
{
    [SerializeField] SprayResourceSystem resources;
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] Color paintColor = new Color(0.78f, 1f, 0.28f, 1f);
    [SerializeField] Color mixtureColor = new Color(0.16f, 0.86f, 0.92f, 1f);
    [SerializeField] Color criticalColor = new Color(1f, 0.23f, 0.18f, 1f);

    RectTransform root;
    RectTransform paintFill;
    RectTransform mixtureFill;
    TMP_Text paintValue;
    TMP_Text mixtureValue;
    TMP_Text warning;
    CanvasGroup canvasGroup;
    float displayedPaint = 1f;
    float displayedMixture = 1f;
    bool built;

    public void Bind(SprayResourceSystem value, Canvas canvas, TMP_FontAsset font)
    {
        resources = value;
        if (!built && canvas != null)
            Build(canvas, font);
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = visible ? 1f : 0f;
    }

    void Update()
    {
        if (!built || resources == null)
            return;

        float interpolation = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        displayedPaint = Mathf.Lerp(displayedPaint, resources.Paint01, interpolation);
        displayedMixture = Mathf.Lerp(displayedMixture, resources.Mixture01, interpolation);
        SetFill(paintFill, displayedPaint);
        SetFill(mixtureFill, displayedMixture);

        bool paintCritical = resources.Paint <= resources.CriticalPaintLevel;
        float blink = 0.65f + Mathf.PingPong(Time.unscaledTime * 2.8f, 0.35f);
        Image paintImage = paintFill != null ? paintFill.GetComponent<Image>() : null;
        if (paintImage != null)
            paintImage.color = paintCritical ? Color.Lerp(criticalColor, Color.white, blink * 0.32f) : paintColor;

        paintValue.text = Mathf.CeilToInt(resources.Paint).ToString("00");
        mixtureValue.text = Mathf.CeilToInt(resources.Mixture).ToString("00");

        if (!resources.HasPaint)
        {
            warning.text = "SIN PINTURA";
            warning.color = criticalColor;
        }
        else if (resources.IsShaking)
        {
            warning.text = "AGITANDO";
            warning.color = mixtureColor;
        }
        else if (resources.NeedsShake)
        {
            warning.text = "[" + resources.ShakeKey + "] AGITAR";
            warning.color = Color.Lerp(criticalColor, Color.white, blink * 0.42f);
        }
        else
        {
            warning.text = string.Empty;
        }
    }

    void Build(Canvas canvas, TMP_FontAsset font)
    {
        GameObject rootObject = new GameObject("Spray Resources HUD", typeof(RectTransform), typeof(CanvasGroup));
        rootObject.transform.SetParent(canvas.transform, false);
        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(30f, 30f);
        root.sizeDelta = new Vector2(292f, 218f);
        canvasGroup = rootObject.GetComponent<CanvasGroup>();

        CreateText(root, "SPRAY", new Vector2(0f, 184f), new Vector2(250f, 30f), 26f, font, TextAlignmentOptions.Left, Color.white);
        paintFill = CreateBar(root, "Paint", new Vector2(12f, 44f), paintColor);
        mixtureFill = CreateBar(root, "Mixture", new Vector2(112f, 44f), mixtureColor);
        CreateText(root, "PINTURA", new Vector2(0f, 15f), new Vector2(88f, 24f), 16f, font, TextAlignmentOptions.Center, Color.white);
        CreateText(root, "MEZCLA", new Vector2(96f, 15f), new Vector2(88f, 24f), 16f, font, TextAlignmentOptions.Center, Color.white);
        paintValue = CreateText(root, "100", new Vector2(52f, 94f), new Vector2(54f, 30f), 20f, font, TextAlignmentOptions.Center, Color.white);
        mixtureValue = CreateText(root, "100", new Vector2(152f, 94f), new Vector2(54f, 30f), 20f, font, TextAlignmentOptions.Center, Color.white);
        warning = CreateText(root, string.Empty, new Vector2(0f, -10f), new Vector2(270f, 28f), 20f, font, TextAlignmentOptions.Left, criticalColor);
        built = true;
    }

    static RectTransform CreateBar(RectTransform parent, string objectName, Vector2 position, Color fillColor)
    {
        GameObject frameObject = new GameObject(objectName + " Frame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(parent, false);
        RectTransform frame = frameObject.GetComponent<RectTransform>();
        frame.anchorMin = frame.anchorMax = new Vector2(0f, 0f);
        frame.pivot = new Vector2(0f, 0f);
        frame.anchoredPosition = position;
        frame.sizeDelta = new Vector2(34f, 132f);
        frameObject.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.82f);

        GameObject fillObject = new GameObject(objectName + " Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(frame, false);
        RectTransform fill = fillObject.GetComponent<RectTransform>();
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 1f);
        fill.offsetMin = new Vector2(5f, 5f);
        fill.offsetMax = new Vector2(-5f, -5f);
        fillObject.GetComponent<Image>().color = fillColor;
        return fill;
    }

    static TMP_Text CreateText(RectTransform parent, string value, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject((string.IsNullOrEmpty(value) ? "Status" : value) + " Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    static void SetFill(RectTransform fill, float value)
    {
        if (fill == null)
            return;

        fill.anchorMax = new Vector2(1f, Mathf.Clamp01(value));
        fill.offsetMax = new Vector2(-5f, -5f);
    }
}
