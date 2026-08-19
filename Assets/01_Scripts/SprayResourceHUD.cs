using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SprayResourceHUD : MonoBehaviour
{
    const string CanFrameResource = "UI/HUD/Barra_de_lata_sin_slider";
    const string BarTextureResource = "UI/HUD/Solo_slider_Lata";

    [SerializeField] SprayResourceSystem resources;
    [SerializeField] float smoothSpeed = 10f;
    [SerializeField] Color paintColor = new Color(0.82f, 0.98f, 0.08f, 1f);
    [SerializeField] Color mixtureColor = new Color(0.04f, 0.72f, 1f, 1f);
    [SerializeField] Color criticalColor = new Color(1f, 0.16f, 0.1f, 1f);
    [SerializeField] float referenceScreenHeight = 1080f;
    [SerializeField, Range(1f, 1.5f)] float maximumHudScale = 1.34f;

    RectTransform root;
    Image paintFill;
    Image mixtureFill;
    TMP_Text paintValue;
    TMP_Text mixtureValue;
    TMP_Text warning;
    CanvasGroup canvasGroup;
    Material barMaterial;
    float displayedPaint = 1f;
    float displayedMixture = 1f;
    int lastScreenHeight;
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

        UpdateResponsiveScale();
        float interpolation = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        displayedPaint = Mathf.Lerp(displayedPaint, resources.Paint01, interpolation);
        displayedMixture = Mathf.Lerp(displayedMixture, resources.Mixture01, interpolation);
        SetFill(paintFill, displayedPaint);
        SetFill(mixtureFill, displayedMixture);

        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 8f) * 0.5f;
        bool paintCritical = resources.Paint <= resources.CriticalPaintLevel;
        bool mixtureCritical = resources.Mixture <= resources.UnstableMixtureLevel;
        paintFill.color = paintCritical ? Color.Lerp(criticalColor, Color.white, pulse * 0.28f) : paintColor;
        mixtureFill.color = mixtureCritical ? Color.Lerp(criticalColor, mixtureColor, pulse * 0.3f) : mixtureColor;

        paintValue.text = Mathf.CeilToInt(resources.Paint).ToString("00");
        mixtureValue.text = Mathf.CeilToInt(resources.Mixture).ToString("00");

        if (resources.IsChangingCan)
        {
            warning.text = "CAMBIANDO LATA";
            warning.color = Color.white;
        }
        else if (!resources.HasPaint)
        {
            warning.text = "[" + resources.ChangeCanKey + "] LATA NUEVA";
            warning.color = Color.Lerp(criticalColor, Color.white, pulse * 0.35f);
        }
        else if (resources.IsShaking)
        {
            warning.text = "AGITANDO";
            warning.color = mixtureColor;
        }
        else if (resources.NeedsShake)
        {
            warning.text = "[" + resources.ShakeKey + "] AGITAR";
            warning.color = Color.Lerp(criticalColor, Color.white, pulse * 0.35f);
        }
        else
        {
            warning.text = string.Empty;
        }
    }

    void Build(Canvas canvas, TMP_FontAsset font)
    {
        DisableLegacyResourceMockups(canvas);
        Sprite canFrame = LoadResourceSprite(CanFrameResource);
        Sprite barTexture = LoadResourceSprite(BarTextureResource);
        Shader shader = Shader.Find("FLOWSTATE/UI/GrungeResourceBar");
        if (shader != null)
        {
            barMaterial = new Material(shader)
            {
                name = "Runtime Spray HUD Grunge Bar"
            };
        }

        GameObject rootObject = new GameObject("Spray Resources HUD", typeof(RectTransform), typeof(CanvasGroup));
        rootObject.transform.SetParent(canvas.transform, false);
        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(24f, 22f);
        root.sizeDelta = new Vector2(390f, 154f);
        canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image frame = CreateImage(root, "Spray Can Frame", Vector2.zero, new Vector2(318f, 150f), canFrame, Color.white, null);
        Shadow frameShadow = frame.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        frameShadow.effectDistance = new Vector2(4f, -4f);

        CreateImage(root, "Mixture Backing", new Vector2(96f, 4f), new Vector2(202f, 36f), barTexture, new Color(0.005f, 0.008f, 0.012f, 0.94f), null);
        paintFill = CreateBar(root, "Paint", new Vector2(103f, 39f), new Vector2(188f, 32f), barTexture, paintColor);
        mixtureFill = CreateBar(root, "Mixture", new Vector2(103f, 8f), new Vector2(188f, 28f), barTexture, mixtureColor);

        paintValue = CreateText(root, "100", new Vector2(302f, 42f), new Vector2(44f, 25f), 17f, font, TextAlignmentOptions.Left, Color.white);
        mixtureValue = CreateText(root, "100", new Vector2(302f, 10f), new Vector2(44f, 25f), 17f, font, TextAlignmentOptions.Left, Color.white);
        warning = CreateText(root, string.Empty, new Vector2(104f, 76f), new Vector2(250f, 28f), 18f, font, TextAlignmentOptions.Left, criticalColor);
        UpdateResponsiveScale();
        built = true;
    }

    void UpdateResponsiveScale()
    {
        if (root == null || Screen.height == lastScreenHeight)
            return;

        lastScreenHeight = Screen.height;
        float scale = Mathf.Clamp(Screen.height / Mathf.Max(1f, referenceScreenHeight), 1f, maximumHudScale);
        root.localScale = Vector3.one * scale;
        root.anchoredPosition = new Vector2(24f, 22f) * scale;
    }

    static void DisableLegacyResourceMockups(Canvas destination)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas candidate in canvases)
        {
            if (candidate == null || candidate == destination)
                continue;

            Transform paintSlider = candidate.transform.Find("Slider");
            Transform mixtureSlider = candidate.transform.Find("F");
            if (paintSlider == null || mixtureSlider == null)
                continue;
            if (paintSlider.GetComponent<Slider>() == null || mixtureSlider.GetComponent<Slider>() == null)
                continue;

            candidate.gameObject.SetActive(false);
        }
    }

    Image CreateBar(RectTransform parent, string objectName, Vector2 position, Vector2 size, Sprite sprite, Color color)
    {
        Image image = CreateImage(parent, objectName + " Fill", position, size, sprite, color, barMaterial);
        image.type = sprite != null ? Image.Type.Filled : Image.Type.Simple;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillClockwise = true;
        image.fillAmount = 1f;
        return image;
    }

    static Image CreateImage(RectTransform parent, string objectName, Vector2 position, Vector2 size, Sprite sprite, Color color, Material material)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.material = material;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return image;
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
        shadow.effectColor = new Color(0f, 0f, 0f, 0.96f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    static Sprite LoadResourceSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    static void SetFill(Image fill, float value)
    {
        if (fill != null)
            fill.fillAmount = Mathf.Clamp01(value);
    }

    void OnDestroy()
    {
        if (barMaterial != null)
            Destroy(barMaterial);
    }
}
