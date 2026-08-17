using UnityEngine;

public class GraffitiDrawnMenuButton : MonoBehaviour
{
    public enum ButtonType
    {
        Color,
        Nozzle,
        Close
    }

    public ButtonType buttonType;

    [Header("Color")]
    public Color colorValue = Color.black;

    [Header("Boquilla")]
    public string nozzleName = "Boquilla";
    public float nozzleMinSize = 0.05f;
    public float nozzleMaxSize = 0.1f;
    public float nozzlePaintRate = 0.02f;
    public float nozzleSpacingMultiplier = 0.22f;
    public float nozzleOpacity = 0.58f;
    public float nozzleJitter = 0.12f;
    public GraffitiNozzleShape nozzleShape = GraffitiNozzleShape.SoftRound;
    public Texture2D nozzleTexture;

    [Header("Visual")]
    public Renderer visualRenderer;
    public float hoverScale = 1.15f;

    Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;

        if (visualRenderer == null)
            visualRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        if (buttonType == ButtonType.Color)
            ApplyVisualColor();
        else if (buttonType == ButtonType.Nozzle)
            ApplyVisualNozzle();
    }

    void ApplyVisualColor()
    {
        if (visualRenderer == null) return;

        Material mat = visualRenderer.material;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", colorValue);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", colorValue);
    }

    void ApplyVisualNozzle()
    {
        if (visualRenderer == null) return;

        Material mat = visualRenderer.material;
        Color color = new Color(0.02f, 0.02f, 0.02f, 1f);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (nozzleTexture == null) return;

        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", nozzleTexture);

        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", nozzleTexture);
    }

    public void SetHover(bool value)
    {
        transform.localScale = value ? originalScale * hoverScale : originalScale;
    }

    public void Apply(GraffitiPainter painter, GraffitiMenuDrawer drawer)
    {
        if (painter == null) return;

        if (buttonType == ButtonType.Color)
        {
            painter.SetFreePaintColor(colorValue);
        }
        else if (buttonType == ButtonType.Nozzle)
        {
            painter.SetNozzleProfile(
                nozzleName,
                nozzleMinSize,
                nozzleMaxSize,
                nozzlePaintRate,
                nozzleSpacingMultiplier,
                nozzleOpacity,
                nozzleJitter,
                nozzleShape,
                nozzleTexture
            );
        }
        else if (buttonType == ButtonType.Close)
        {
            if (drawer != null)
                drawer.CloseMenu();
        }
    }
}
