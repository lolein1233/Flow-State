using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace FlowState.Rendering
{
    public enum FSSRSDebugMode
    {
        None,
        LinearDepth,
        Normals,
        Edges,
        Posterized
    }

    [Serializable]
    public sealed class FSSRSDebugModeParameter : VolumeParameter<FSSRSDebugMode>
    {
        public FSSRSDebugModeParameter(FSSRSDebugMode value, bool overrideState = false)
            : base(value, overrideState)
        {
        }
    }

    [Serializable, VolumeComponentMenu("FLOW STATE/FSSRS Composite")]
    public sealed class FSSRSVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enabledEffect = new(true);
        public ColorParameter outlineColor = new(new Color(0.025f, 0.022f, 0.03f, 1f), true, false, true);
        public ClampedFloatParameter outlineIntensity = new(0.9f, 0f, 1f);
        public ClampedFloatParameter outlineThickness = new(1.25f, 0.5f, 4f);
        public ClampedFloatParameter depthThreshold = new(0.012f, 0.001f, 0.1f);
        public ClampedFloatParameter normalThreshold = new(0.22f, 0.01f, 1f);
        public ClampedFloatParameter lumaThreshold = new(0.16f, 0.01f, 1f);
        public ClampedIntParameter posterizeSteps = new(6, 0, 16);
        [FormerlySerializedAs("grainIntensity")]
        public ClampedFloatParameter inkFleckIntensity = new(0.015f, 0f, 1f);
        public ClampedFloatParameter halftoneIntensity = new(0.1f, 0f, 1f);
        public ClampedFloatParameter halftoneScale = new(3f, 1f, 12f);
        public ClampedFloatParameter hatchIntensity = new(0.12f, 0f, 1f);
        public ClampedFloatParameter paletteInfluence = new(0.28f, 0f, 1f);
        public ClampedFloatParameter paperLift = new(0.3f, 0f, 1f);
        public ClampedFloatParameter colorSaturation = new(0.25f, 0f, 1f);
        public ClampedFloatParameter accentBoost = new(0.25f, 0f, 1f);
        public FSSRSDebugModeParameter debugMode = new(FSSRSDebugMode.None);

        public bool IsActive() => enabledEffect.value && outlineIntensity.value + inkFleckIntensity.value +
            halftoneIntensity.value + hatchIntensity.value + paletteInfluence.value + paperLift.value +
            colorSaturation.value + accentBoost.value > 0.001f;

        public bool IsTileCompatible() => false;
    }
}
