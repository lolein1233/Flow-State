using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class CombatTensionVignette : MonoBehaviour
{
    [Header("Referencias")]
    public CombatLockOnController lockOn;
    public Volume volume;

    [Header("Tension visual")]
    [Range(0f, 1f)] public float targetIntensity = 0.46f;
    [Range(0f, 1f)] public float targetSmoothness = 0.68f;
    public Color targetColor = new Color(0.015f, 0.012f, 0.018f, 1f);
    public float blendSharpness = 7.5f;

    Vignette vignette;
    VolumeProfile runtimeProfile;
    float baseIntensity;
    float baseSmoothness;
    Color baseColor;
    float amount;

    void Awake()
    {
        ResolveReferences();
        PrepareRuntimeProfile();
    }

    void Update()
    {
        ResolveReferences();

        if (vignette == null)
            PrepareRuntimeProfile();

        if (vignette == null)
            return;

        bool locked = lockOn != null && lockOn.GetCurrentTarget() != null;
        float targetAmount = locked ? 1f : 0f;
        float blend = 1f - Mathf.Exp(-blendSharpness * Time.deltaTime);
        amount = Mathf.Lerp(amount, targetAmount, blend);

        ApplyVignette(amount);
    }

    void OnDisable()
    {
        RestoreVignette();
    }

    void ResolveReferences()
    {
        if (lockOn == null)
            lockOn = GetComponent<CombatLockOnController>();

        if (volume == null)
            volume = FindFirstObjectByType<Volume>();
    }

    void PrepareRuntimeProfile()
    {
        if (volume == null)
            return;

        if (runtimeProfile == null)
        {
            VolumeProfile sourceProfile = volume.sharedProfile != null ? volume.sharedProfile : volume.profile;

            if (sourceProfile != null)
            {
                runtimeProfile = Instantiate(sourceProfile);
                volume.profile = runtimeProfile;
            }
            else
            {
                runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                volume.profile = runtimeProfile;
            }
        }

        if (!runtimeProfile.TryGet(out vignette))
            vignette = runtimeProfile.Add<Vignette>(true);

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;
        vignette.color.overrideState = true;

        baseIntensity = vignette.intensity.value;
        baseSmoothness = vignette.smoothness.value;
        baseColor = vignette.color.value;
    }

    void ApplyVignette(float value)
    {
        float eased = value * value * (3f - 2f * value);
        vignette.intensity.value = Mathf.Lerp(baseIntensity, targetIntensity, eased);
        vignette.smoothness.value = Mathf.Lerp(baseSmoothness, targetSmoothness, eased);
        vignette.color.value = Color.Lerp(baseColor, targetColor, eased);
    }

    void RestoreVignette()
    {
        if (vignette == null)
            return;

        vignette.intensity.value = baseIntensity;
        vignette.smoothness.value = baseSmoothness;
        vignette.color.value = baseColor;
    }
}
