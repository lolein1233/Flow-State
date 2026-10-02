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
    [SerializeField] GameObject selectionMarker;
    [SerializeField] Color idleColor = new Color(0.32f, 0.34f, 0.42f, 1f);
    [SerializeField] Color activeColor = new Color(1f, 0.08f, 0.48f, 1f);
    [SerializeField] float hoverLift = 0.055f;
    [SerializeField] float hoverDepth = 0.055f;
    [SerializeField] float hoverTilt = 12f;
    [SerializeField] float response = 12f;
    [Header("Movimiento holografico")]
    [SerializeField, Range(0f, 2f)] float idleMotion = 1f;
    [SerializeField] float animationPhase;
    [SerializeField] Renderer capRenderer;

    Vector3 restPosition;
    Vector3 restScale;
    Quaternion restRotation;
    MaterialPropertyBlock propertyBlock;
    float hoverAmount;
    float hoverTarget;
    float selectionPulse;
    bool selected;
    Color titleRestColor;
    MaterialPropertyBlock capProperties;
    float selectionAmount;
    float animationTime;
    bool restPoseCached;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int HighlightId = Shader.PropertyToID("_Highlight");
    static readonly int PhaseId = Shader.PropertyToID("_Phase");

    void Awake()
    {
        CacheRestPose();
        propertyBlock = new MaterialPropertyBlock();
        capProperties = new MaterialPropertyBlock();
        titleRestColor = title != null ? title.color : Color.white;
        ApplyColors(0f);
    }

    void OnEnable()
    {
        CacheRestPose();
        hoverAmount = 0f;
        hoverTarget = 0f;
        selectionPulse = 0f;
        animationTime = 0f;
    }

    void Update()
    {
        if (modelPivot == null)
            return;

        hoverAmount = Mathf.Lerp(hoverAmount, hoverTarget, 1f - Mathf.Exp(-response * Time.deltaTime));
        selectionAmount = Mathf.Lerp(selectionAmount, selected ? 1f : 0f, 1f - Mathf.Exp(-7f * Time.deltaTime));
        animationTime += Time.deltaTime;
        selectionPulse = Mathf.MoveTowards(selectionPulse, 0f, 2.5f * Time.deltaTime);

        float punch = Mathf.Sin(selectionPulse * Mathf.PI) * selectionPulse;
        float phase = animationTime * 1.25f + animationPhase;
        float motion = idleMotion * Mathf.SmoothStep(0f, 1f, animationTime * 2f);
        float lift = hoverAmount * hoverLift + Mathf.Sin(phase) * 0.006f * motion;
        float depth = hoverAmount * hoverDepth;

        modelPivot.localPosition = restPosition + new Vector3(0f, lift, -depth);
        modelPivot.localRotation = restRotation * Quaternion.Euler(-hoverTilt * hoverAmount,
            Mathf.Sin(phase * 0.7f) * (2f + selectionAmount * 2f) * motion,
            punch * 3f + Mathf.Sin(phase) * 0.8f * motion);
        modelPivot.localScale = restScale * (1f + hoverAmount * 0.045f + punch * 0.035f);

        ApplyColors(Mathf.Clamp01(hoverAmount + Mathf.Abs(punch)));
        if (capRenderer != null)
        {
            capRenderer.GetPropertyBlock(capProperties);
            capProperties.SetFloat(HighlightId, Mathf.Clamp01(selectionAmount * 0.75f + hoverAmount * 0.25f + punch * 0.3f));
            capProperties.SetFloat(PhaseId, animationPhase);
            capRenderer.SetPropertyBlock(capProperties);
        }
    }

    public void SetHover(bool value)
    {
        hoverTarget = value ? 1f : 0f;
    }

    public void PulseSelection()
    {
        selectionPulse = 1f;
    }

    public void SetSelected(bool value)
    {
        selected = value;
        if (selectionMarker != null)
            selectionMarker.SetActive(value);
        ApplyColors(hoverAmount);
    }

    void CacheRestPose()
    {
        if (modelPivot == null || restPoseCached)
            return;

        restPosition = modelPivot.localPosition;
        restRotation = modelPivot.localRotation;
        restScale = modelPivot.localScale;
        restPoseCached = true;
    }

    void ApplyColors(float amount)
    {
        Color color = Color.Lerp(idleColor, activeColor, Mathf.Max(selectionAmount * 0.7f, amount * 0.35f));

        if (haloRenderer != null)
        {
            haloRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(EmissionColorId, Color.black);
            propertyBlock.SetFloat(HighlightId, Mathf.Max(selectionAmount, amount * 0.5f));
            propertyBlock.SetFloat(PhaseId, animationPhase);
            haloRenderer.SetPropertyBlock(propertyBlock);
        }

        if (title != null)
            title.color = Color.Lerp(titleRestColor, activeColor, selected ? 1f : amount * 0.35f);
    }
}
