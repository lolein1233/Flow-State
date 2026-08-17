using UnityEngine;

public class GraffitiColorValueSlider : MonoBehaviour
{
    public Renderer visualRenderer;
    public Renderer previewRenderer;
    public Transform handleTransform;
    public Renderer handleRenderer;
    public float hoverScale = 1.04f;
    public float handleDepth = -0.035f;
    public float handleWidth = 0.64f;

    Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;

        if (visualRenderer == null)
            visualRenderer = GetComponent<Renderer>();
    }

    public void SetHover(bool value)
    {
        transform.localScale = value ? originalScale * hoverScale : originalScale;
    }

    public bool TryEvaluate(RaycastHit hit, GraffitiPainter painter, out float value, out Color color)
    {
        Vector3 local = transform.InverseTransformPoint(hit.point);
        value = Mathf.Clamp01(local.y + 0.5f);

        float hue = painter != null ? painter.currentColorHue : 0f;
        float saturation = painter != null ? painter.currentColorSaturation : 0f;
        color = Color.HSVToRGB(hue, saturation, value);
        color.a = 1f;

        return local.x >= -0.65f && local.x <= 0.65f && local.y >= -0.55f && local.y <= 0.55f;
    }

    public void Preview(RaycastHit hit, GraffitiPainter painter)
    {
        UpdateSliderTint(painter);

        if (TryEvaluate(hit, painter, out float previewValue, out Color color))
        {
            GraffitiColorWheel.SetRendererColor(previewRenderer, color);
            UpdateHandle(color, previewValue);
        }
    }

    public void Apply(RaycastHit hit, GraffitiPainter painter)
    {
        if (painter == null)
            return;

        if (!TryEvaluate(hit, painter, out float value, out Color color))
            return;

        painter.SetFreePaintValue(value);
        UpdateSliderTint(painter);
        GraffitiColorWheel.SetRendererColor(previewRenderer, color);
        UpdateHandle(color, value);
    }

    public void UpdateSliderTint(GraffitiPainter painter)
    {
        if (painter == null || visualRenderer == null)
            return;

        GraffitiColorWheel.SetRendererColor(visualRenderer, painter.GetFullValueColor());
        UpdateHandle(painter.currentSprayColor, painter.currentColorValue);
    }

    void UpdateHandle(Color color, float value)
    {
        if (handleTransform != null)
        {
            handleTransform.localPosition = new Vector3(0f, Mathf.Clamp01(value) - 0.5f, handleDepth);
            handleTransform.localScale = new Vector3(handleWidth, 0.085f, 0.085f);
        }

        if (handleRenderer != null)
        {
            Color handleColor = color.grayscale > 0.55f ? Color.black : Color.white;
            GraffitiColorWheel.SetRendererColor(handleRenderer, handleColor);
        }
    }
}
