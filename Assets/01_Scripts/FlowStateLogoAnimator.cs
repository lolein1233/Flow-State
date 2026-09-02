using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class FlowStateLogoAnimator : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] Shader animatedLogoShader;
    [SerializeField] bool playOnAwake = true;

    [Header("Movimiento")]
    [SerializeField, Min(0.01f)] float motionSpeed = 1f;
    [SerializeField] Vector2 floatAmplitude = new Vector2(10f, 6f);
    [SerializeField, Range(0f, 5f)] float rotationAmplitude = 0.9f;
    [SerializeField, Range(0f, 0.15f)] float breathingScale = 0.022f;
    [SerializeField, Min(0.25f)] float impactInterval = 1.85f;
    [SerializeField, Range(0f, 0.2f)] float impactScale = 0.055f;

    [Header("Impresion punk")]
    [SerializeField, Range(0f, 0.08f)] float chromaticSplit = 0.009f;
    [SerializeField, Range(0f, 0.08f)] float glitchAmount = 0.018f;
    [SerializeField, Range(0f, 2f)] float echoIntensity = 0.9f;
    [SerializeField, Range(0f, 3f)] float sweepIntensity = 1.25f;
    [SerializeField] Color cyanEcho = new Color(0f, 0.9f, 1f, 1f);
    [SerializeField] Color magentaEcho = new Color(1f, 0.02f, 0.48f, 1f);
    [SerializeField] Color sweepColor = new Color(1f, 0.92f, 0.24f, 1f);

    Image image;
    RectTransform rectTransform;
    Material originalMaterial;
    Material animatedMaterial;
    Vector3 baseAnchoredPosition;
    Vector3 baseScale;
    Quaternion baseRotation;
    float animationOrigin;
    bool isAnimating;

    static readonly int AnimationTimeId = Shader.PropertyToID("_AnimationTime");
    static readonly int BeatPulseId = Shader.PropertyToID("_BeatPulse");
    static readonly int ChromaticSplitId = Shader.PropertyToID("_ChromaticSplit");
    static readonly int GlitchAmountId = Shader.PropertyToID("_GlitchAmount");
    static readonly int EchoIntensityId = Shader.PropertyToID("_EchoIntensity");
    static readonly int SweepIntensityId = Shader.PropertyToID("_SweepIntensity");
    static readonly int CyanEchoId = Shader.PropertyToID("_CyanEcho");
    static readonly int MagentaEchoId = Shader.PropertyToID("_MagentaEcho");
    static readonly int SweepColorId = Shader.PropertyToID("_SweepColor");

    public bool IsAnimating => isAnimating;

    void Awake()
    {
        CacheReferencesAndPose();
    }

    void OnEnable()
    {
        if (Application.isPlaying && playOnAwake)
            Play();
    }

    void Update()
    {
        if (!isAnimating)
            return;

        float time = (Time.unscaledTime - animationOrigin) * motionSpeed;
        float impact = EvaluateImpact(time);
        float breath = Mathf.Sin(time * 2.35f) * breathingScale;
        float horizontal = Mathf.Sin(time * 1.17f) * floatAmplitude.x;
        float vertical = Mathf.Sin(time * 1.83f + 0.7f) * floatAmplitude.y;
        float rotation = Mathf.Sin(time * 1.41f) * rotationAmplitude;

        rectTransform.anchoredPosition3D = baseAnchoredPosition + new Vector3(horizontal, vertical, 0f);
        rectTransform.localScale = baseScale * (1f + breath + impact * impactScale);
        rectTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rotation + impact * 1.35f);

        if (animatedMaterial != null)
        {
            animatedMaterial.SetFloat(AnimationTimeId, time);
            animatedMaterial.SetFloat(BeatPulseId, impact);
        }
    }

    void OnDisable()
    {
        StopAndRestore();
    }

    void OnDestroy()
    {
        ReleaseMaterial();
    }

    public void Configure(Shader shader)
    {
        animatedLogoShader = shader;
    }

    [ContextMenu("Play Logo Animation")]
    public void Play()
    {
        CacheReferencesAndPose();

        if (!EnsureMaterial())
            return;

        animationOrigin = Time.unscaledTime;
        isAnimating = true;
    }

    [ContextMenu("Stop Logo Animation")]
    public void StopAndRestore()
    {
        isAnimating = false;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D = baseAnchoredPosition;
            rectTransform.localScale = baseScale;
            rectTransform.localRotation = baseRotation;
        }

        if (image != null && animatedMaterial != null && image.material == animatedMaterial)
            image.material = originalMaterial;

        ReleaseMaterial();
    }

    float EvaluateImpact(float time)
    {
        float phase = Mathf.Repeat(time, impactInterval) / impactInterval;
        float primary = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phase / 0.11f));
        float reboundPhase = Mathf.Abs(phase - 0.17f);
        float rebound = Mathf.Clamp01(1f - reboundPhase / 0.055f) * 0.32f;
        return Mathf.Clamp01(primary + rebound);
    }

    void CacheReferencesAndPose()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            baseAnchoredPosition = rectTransform.anchoredPosition3D;
            baseScale = rectTransform.localScale;
            baseRotation = rectTransform.localRotation;
        }
    }

    bool EnsureMaterial()
    {
        if (image == null)
            return false;

        if (animatedLogoShader == null)
            animatedLogoShader = Shader.Find("FLOW STATE/UI/Animated Punk Logo");

        if (animatedLogoShader == null)
        {
            Debug.LogWarning("Flow State logo shader was not found.", this);
            return false;
        }

        if (animatedMaterial == null)
        {
            originalMaterial = image.material;
            animatedMaterial = new Material(animatedLogoShader)
            {
                name = "Flow State Logo (Runtime)",
                hideFlags = HideFlags.DontSave
            };
        }

        animatedMaterial.SetFloat(ChromaticSplitId, chromaticSplit);
        animatedMaterial.SetFloat(GlitchAmountId, glitchAmount);
        animatedMaterial.SetFloat(EchoIntensityId, echoIntensity);
        animatedMaterial.SetFloat(SweepIntensityId, sweepIntensity);
        animatedMaterial.SetColor(CyanEchoId, cyanEcho);
        animatedMaterial.SetColor(MagentaEchoId, magentaEcho);
        animatedMaterial.SetColor(SweepColorId, sweepColor);
        image.material = animatedMaterial;
        return true;
    }

    void ReleaseMaterial()
    {
        if (animatedMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(animatedMaterial);
        else
            DestroyImmediate(animatedMaterial);

        animatedMaterial = null;
    }
}
