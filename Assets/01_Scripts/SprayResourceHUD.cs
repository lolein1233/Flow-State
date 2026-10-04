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

    [Header("Animacion comic")]
    [SerializeField] bool animateHud = true;
    [SerializeField, Min(0.05f)] float entranceDuration = 0.5f;
    [SerializeField, Range(0f, 2f)] float comicMotion = 1f;
    [SerializeField] Color comicAccent = new Color(1f, 0.08f, 0.48f, 1f);
    [SerializeField, Range(0.8f, 1.6f)] float presentationScale = 1f;

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
    bool visible;
    float entranceTime;
    float responsiveScale = 1f;
    RectTransform comicEcho;
    RectTransform canFrameRect;
    RectTransform canPortrait;
    RectTransform paintRow;
    RectTransform mixtureRow;
    RectTransform paintEcho;
    RectTransform mixtureEcho;
    float motionActivity = 1f;
    float shakeBlend;
    float canRotationPhase;
    static readonly int HudTimeId = Shader.PropertyToID("_HudTime");
    static readonly int MotionId = Shader.PropertyToID("_MotionStrength");
    static readonly int SpriteUvId = Shader.PropertyToID("_SpriteUVRect");

    public void Bind(SprayResourceSystem value, Canvas canvas, TMP_FontAsset font)
    {
        resources = value;
        if (!built && canvas != null)
            Build(canvas, font);
    }

    public void SetVisible(bool visible)
    {
        if (visible && !this.visible)
            entranceTime = 0f;
        this.visible = visible;
        if (canvasGroup != null)
            canvasGroup.alpha = visible ? (animateHud ? Mathf.Clamp01(entranceTime / 0.08f) : 1f) : 0f;
    }

    void Update()
    {
        if (!built || resources == null)
            return;

        UpdateResponsiveScale();
        UpdateComicAnimation();
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

        paintValue.SetText("{0}%", Mathf.CeilToInt(resources.Paint));
        mixtureValue.SetText("{0}%", Mathf.CeilToInt(resources.Mixture));

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
            if (barTexture != null)
            {
                Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(barTexture);
                barMaterial.SetVector(SpriteUvId, new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
            }
        }

        GameObject rootObject = new GameObject("Spray Resources HUD", typeof(RectTransform), typeof(CanvasGroup));
        rootObject.transform.SetParent(canvas.transform, false);
        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(24f, 22f);
        root.sizeDelta = new Vector2(440f, 205f);
        canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        paintRow = BuildResourceRow("Paint", new Vector2(110f, 84f), paintColor, barTexture, font, "PINTURA", out paintFill, out paintValue, out paintEcho);
        mixtureRow = BuildResourceRow("Mixture", new Vector2(110f, 13f), mixtureColor, barTexture, font, "MEZCLA", out mixtureFill, out mixtureValue, out mixtureEcho);

        // Recorta la ilustracion original a la lata para animarla por separado de las barras.
        canPortrait = CreateRect(root, "Animated Can Portrait", new Vector2(0f, 8f), new Vector2(102f, 180f));
        canPortrait.gameObject.AddComponent<RectMask2D>();
        Image echo = CreateImage(canPortrait, "Comic Color Echo", new Vector2(10f, 5f), new Vector2(350f, 165f), canFrame, comicAccent, null);
        comicEcho = echo.rectTransform;
        Outline echoOutline = echo.gameObject.AddComponent<Outline>();
        echoOutline.effectColor = comicAccent;
        echoOutline.effectDistance = new Vector2(6f, -6f);
        Image paper = CreateImage(canPortrait, "Comic Paper Edge", new Vector2(5f, 9f), new Vector2(350f, 165f), canFrame, Color.white, null);
        Outline paperOutline = paper.gameObject.AddComponent<Outline>();
        paperOutline.effectColor = new Color(1f, 0.96f, 0.86f, 1f);
        paperOutline.effectDistance = new Vector2(3f, -3f);
        Image frame = CreateImage(canPortrait, "Spray Can Frame", new Vector2(5f, 9f), new Vector2(350f, 165f), canFrame, Color.white, null);
        canFrameRect = frame.rectTransform;
        Shadow frameShadow = frame.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        frameShadow.effectDistance = new Vector2(4f, -4f);

        warning = CreateText(root, string.Empty, new Vector2(110f, 162f), new Vector2(310f, 28f), 20f, font, TextAlignmentOptions.Left, criticalColor);
        UpdateResponsiveScale();
        built = true;
    }

    RectTransform BuildResourceRow(string name, Vector2 position, Color accent, Sprite texture, TMP_FontAsset font, string label,
        out Image fill, out TMP_Text value, out RectTransform echo)
    {
        RectTransform row = CreateRect(root, name + " Comic Panel", position, new Vector2(294f, 59f));
        echo = CreateImage(row, "Color Registration", new Vector2(7f, -6f), new Vector2(294f, 59f), null, accent, null).rectTransform;
        CreateImage(row, "Paper Border", new Vector2(-3f, -3f), new Vector2(300f, 65f), null, new Color(1f, 0.97f, 0.88f), null);
        CreateImage(row, "Ink Panel", Vector2.zero, new Vector2(294f, 59f), null, new Color(0.045f, 0.025f, 0.065f), null);
        CreateText(row, label, new Vector2(10f, 33f), new Vector2(175f, 23f), 18f, font, TextAlignmentOptions.Left, accent);
        value = CreateText(row, "100%", new Vector2(219f, 27f), new Vector2(70f, 30f), 23f, font, TextAlignmentOptions.Right, Color.white);
        CreateImage(row, "Empty Track", new Vector2(10f, 7f), new Vector2(274f, 24f), null, new Color(0.18f, 0.14f, 0.21f), null);
        fill = CreateBar(row, name, new Vector2(10f, 7f), new Vector2(274f, 24f), texture, accent);
        for (int i = 1; i < 10; i++)
            CreateImage(row, "Ink Tick " + i, new Vector2(10f + 27.4f * i, 7f), new Vector2(2f, 5f), null, new Color(0.02f, 0.01f, 0.03f, 0.65f), null);
        return row;
    }

    static RectTransform CreateRect(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    void UpdateResponsiveScale()
    {
        if (root == null || Screen.height == lastScreenHeight)
            return;

        lastScreenHeight = Screen.height;
        float scale = Mathf.Clamp(Screen.height / Mathf.Max(1f, referenceScreenHeight), 1f, maximumHudScale);
        responsiveScale = scale;
    }

    void UpdateComicAnimation()
    {
        if (!visible)
            return;

        entranceTime += Time.unscaledDeltaTime;
        float t = animateHud ? Mathf.Clamp01(entranceTime / Mathf.Max(0.05f, entranceDuration)) : 1f;
        float pop = Mathf.SmoothStep(0f, 1f, t);
        root.localScale = Vector3.one * (responsiveScale * presentationScale * Mathf.Lerp(0.88f, 1f, pop));
        root.anchoredPosition = (new Vector2(30f, 30f) + Vector2.down * (1f - pop) * 35f) * responsiveScale;
        canvasGroup.alpha = animateHud ? Mathf.Clamp01(entranceTime / 0.08f) : 1f;

        // Tiempo continuo y transiciones amortiguadas para evitar saltos al cambiar de accion.
        float time = Time.unscaledTime;
        float blend = 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime);
        float activity = resources.IsShaking ? 1.9f : resources.IsChangingCan ? 1.6f : resources.IsSpraying ? 1.4f : 1f;
        motionActivity = Mathf.Lerp(motionActivity, activity, blend);
        shakeBlend = Mathf.Lerp(shakeBlend, resources.IsShaking ? 1f : 0f, blend);
        canRotationPhase = (canRotationPhase + Mathf.Lerp(2.2f, 9f, shakeBlend) * Time.unscaledDeltaTime) % (Mathf.PI * 2f);
        float amount = animateHud ? comicMotion * motionActivity : 0f;
        comicEcho.anchoredPosition = new Vector2(10f + Mathf.Sin(time * 2.8f) * amount * 2.5f, 5f + Mathf.Cos(time * 2.2f) * amount * 2f);
        canPortrait.anchoredPosition = new Vector2(3f + Mathf.Sin(time * 1.8f) * amount * 3f, 12f + Mathf.Sin(time * 2.2f) * amount * 4f);
        canPortrait.localRotation = Quaternion.Euler(0f, 0f, -4f + Mathf.Sin(canRotationPhase) * amount * 2.6f);
        canPortrait.localScale = Vector3.one * (1f + Mathf.Sin(time * 2.2f) * amount * 0.018f);
        canFrameRect.localRotation = Quaternion.identity;
        AnimateRow(paintRow, paintEcho, new Vector2(110f, 84f), time, amount, 0f, 0.04f);
        AnimateRow(mixtureRow, mixtureEcho, new Vector2(110f, 13f), time, amount, 1.8f, 0.12f);
        if (barMaterial != null)
        {
            barMaterial.SetFloat(HudTimeId, Time.unscaledTime);
            barMaterial.SetFloat(MotionId, animateHud ? Mathf.Min(1.8f, amount) : 0f);
        }
    }

    void AnimateRow(RectTransform row, RectTransform echo, Vector2 position, float time, float amount, float phase, float delay)
    {
        float progress = animateHud ? Mathf.Clamp01((entranceTime - delay) / Mathf.Max(0.05f, entranceDuration)) : 1f;
        float slide = Mathf.SmoothStep(0f, 1f, progress);
        row.anchoredPosition = position + new Vector2(-(1f - slide) * 55f, Mathf.Sin(time * 1.8f + phase) * amount * 1.8f);
        row.localRotation = Quaternion.Euler(0f, 0f, (phase == 0f ? 1.5f : -1.5f) + Mathf.Sin(time * 1.6f + phase) * amount * 0.8f);
        echo.anchoredPosition = new Vector2(7f + Mathf.Sin(time * 2.4f + phase) * amount * 2.5f, -6f + Mathf.Cos(time * 2f + phase) * amount * 1.5f);
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
        if (root != null)
            Destroy(root.gameObject);
        if (barMaterial != null)
            Destroy(barMaterial);
    }
}
