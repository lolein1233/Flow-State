using UnityEngine;

public class GraffitiColorWheel : MonoBehaviour
{
    public Renderer visualRenderer;
    public Renderer previewRenderer;
    public Transform selectorTransform;
    public Renderer selectorRenderer;
    public float hoverScale = 1.05f;
    public float selectorDepth = -0.035f;
    public float centerDeadZone = 0.035f;

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

    public bool TryEvaluate(RaycastHit hit, GraffitiPainter painter, out float hue, out float saturation, out Color color)
    {
        Vector3 local = transform.InverseTransformPoint(hit.point);
        Vector2 fromCenter = new Vector2(local.x, local.y) * 2f;
        float radius = fromCenter.magnitude;

        hue = 0f;
        saturation = 0f;
        color = Color.white;

        if (radius > 1.03f)
            return false;

        float angle = Mathf.Atan2(fromCenter.y, fromCenter.x) / (Mathf.PI * 2f);
        hue = radius <= centerDeadZone && painter != null ? painter.currentColorHue : Mathf.Repeat(angle, 1f);
        saturation = Mathf.Clamp01(radius);

        float value = painter != null ? painter.currentColorValue : 1f;
        color = Color.HSVToRGB(hue, saturation, value);
        color.a = 1f;
        return true;
    }

    public void Preview(RaycastHit hit, GraffitiPainter painter)
    {
        if (TryEvaluate(hit, painter, out _, out _, out Color color))
        {
            SetRendererColor(previewRenderer, color);
            UpdateSelectionVisual(painter, color);
        }
    }

    public void Apply(RaycastHit hit, GraffitiPainter painter)
    {
        if (painter == null)
            return;

        if (!TryEvaluate(hit, painter, out float hue, out float saturation, out Color color))
            return;

        painter.SetFreePaintHueSaturation(hue, saturation);
        SetRendererColor(previewRenderer, color);
        UpdateSelectionVisual(painter, color);
    }

    public void UpdateSelectionVisual(GraffitiPainter painter)
    {
        if (painter == null)
            return;

        Color color = Color.HSVToRGB(painter.currentColorHue, painter.currentColorSaturation, painter.currentColorValue);
        color.a = 1f;
        UpdateSelectionVisual(painter, color);
    }

    void UpdateSelectionVisual(GraffitiPainter painter, Color color)
    {
        if (painter == null)
            return;

        if (selectorTransform != null)
        {
            float angle = painter.currentColorHue * Mathf.PI * 2f;
            float radius = Mathf.Clamp01(painter.currentColorSaturation) * 0.5f;
            selectorTransform.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, selectorDepth);
        }

        if (selectorRenderer != null)
        {
            Color wheelSurfaceColor = Color.HSVToRGB(painter.currentColorHue, painter.currentColorSaturation, 1f);
            Color markerColor = wheelSurfaceColor.grayscale > 0.55f ? Color.black : Color.white;
            SetRendererColor(selectorRenderer, markerColor);
        }
    }

    public static void SetRendererColor(Renderer target, Color color)
    {
        if (target == null)
            return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        target.SetPropertyBlock(block);
    }
}
