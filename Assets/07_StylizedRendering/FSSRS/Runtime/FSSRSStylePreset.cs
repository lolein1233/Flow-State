using UnityEngine;
using UnityEngine.Rendering;

namespace FlowState.Rendering
{
    [CreateAssetMenu(fileName = "FSSRS_NewStyle", menuName = "FLOW STATE/FSSRS/Style Preset")]
    public sealed class FSSRSStylePreset : ScriptableObject
    {
        [Range(0f, 1f)] public float outlineIntensity = 0.9f;
        [Range(0.5f, 4f)] public float outlineThickness = 1.25f;
        [Range(0.001f, 0.1f)] public float depthThreshold = 0.012f;
        [Range(0.01f, 1f)] public float normalThreshold = 0.22f;
        [Range(0.01f, 1f)] public float lumaThreshold = 0.16f;
        [Range(0, 16)] public int posterizeSteps = 6;
        [Range(0f, 1f)] public float grainIntensity = 0.12f;
        [Range(0f, 1f)] public float halftoneIntensity = 0.1f;
        [Range(1f, 12f)] public float halftoneScale = 3f;
        [Range(0f, 1f)] public float hatchIntensity = 0.12f;
        [Range(0f, 1f)] public float paletteInfluence = 0.28f;

        public void ApplyTo(FSSRSVolumeComponent target)
        {
            if (target == null)
                return;

            Set(target.outlineIntensity, outlineIntensity);
            Set(target.outlineThickness, outlineThickness);
            Set(target.depthThreshold, depthThreshold);
            Set(target.normalThreshold, normalThreshold);
            Set(target.lumaThreshold, lumaThreshold);
            Set(target.posterizeSteps, posterizeSteps);
            Set(target.grainIntensity, grainIntensity);
            Set(target.halftoneIntensity, halftoneIntensity);
            Set(target.halftoneScale, halftoneScale);
            Set(target.hatchIntensity, hatchIntensity);
            Set(target.paletteInfluence, paletteInfluence);
        }

        private static void Set<T>(VolumeParameter<T> parameter, T value)
        {
            parameter.overrideState = true;
            parameter.value = value;
        }
    }
}
