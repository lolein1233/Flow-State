using TMPro;
using UnityEngine;

/// <summary>
/// Presentation-only feedback for the physical nozzle cards in the graffiti menu.
/// It does not own or modify any paint settings.
/// </summary>
public sealed class GraffitiNozzleVisual : MonoBehaviour
{
    [SerializeField] Transform modelPivot;
    [SerializeField] Renderer haloRenderer;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text descriptor;
    [SerializeField] Color idleColor = new Color(0.32f, 0.34f, 0.42f, 1f);
    [SerializeField] Color activeColor = new Color(1f, 0.08f, 0.48f, 1f);
    [SerializeField] float hoverLift = 0.055f;
    [SerializeField] float hoverDepth = 0.055f;
    [SerializeField] float hoverTilt = 12f;
    [SerializeField] float response = 12f;

    Vector3 restPosition;
    Vector3 restScale;
    Quaternion restRotation;
    MaterialPropertyBlock propertyBlock;
    float hoverAmount;
    float hoverTarget;
    float selectionPulse;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        CacheRestPose();
        propertyBlock = new MaterialPropertyBlock();
        ApplyColors(0f);
    }

    void OnEnable()
    {
        CacheRestPose();
        hoverAmount = 0f;
        hoverTarget = 0f;
        selectionPulse = 0f;
    }

    void Update()
    {
        if (modelPivot == null)
            return;

        hoverAmount = Mathf.MoveTowards(hoverAmount, hoverTarget, response * Time.deltaTime);
        selectionPulse = Mathf.MoveTowards(selectionPulse, 0f, 3.5f * Time.deltaTime);

        float punch = selectionPulse > 0f ? Mathf.Sin(selectionPulse * Mathf.PI * 4f) * selectionPulse : 0f;
        float lift = hoverAmount * hoverLift;
        float depth = hoverAmount * hoverDepth;

        modelPivot.localPosition = restPosition + new Vector3(0f, lift, -depth);
        modelPivot.localRotation = restRotation * Quaternion.Euler(-hoverTilt * hoverAmount, 0f, punch * 7f);
        modelPivot.localScale = restScale * (1f + hoverAmount * 0.1f + Mathf.Abs(punch) * 0.08f);

        ApplyColors(Mathf.Clamp01(hoverAmount + Mathf.Abs(punch)));
    }

    public void SetHover(bool value)
    {
        hoverTarget = value ? 1f : 0f;
    }

    public void PulseSelection()
    {
        selectionPulse = 1f;
    }

    void CacheRestPose()
    {
        if (modelPivot == null)
            return;

        restPosition = modelPivot.localPosition;
        restRotation = modelPivot.localRotation;
        restScale = modelPivot.localScale;
    }

    void ApplyColors(float amount)
    {
        Color color = Color.Lerp(idleColor, activeColor, amount);

        if (haloRenderer != null)
        {
            haloRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(EmissionColorId, color * Mathf.Lerp(0.4f, 2.2f, amount));
            haloRenderer.SetPropertyBlock(propertyBlock);
        }

        if (title != null)
            title.color = Color.Lerp(Color.white, activeColor, amount);

        if (descriptor != null)
            descriptor.color = Color.Lerp(new Color(0.64f, 0.66f, 0.72f, 1f), Color.white, amount);
    }
}
