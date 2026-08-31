using UnityEngine;
using UnityEngine.Rendering;

namespace FlowState.Rendering
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FlowStatePaletteController : MonoBehaviour
    {
        [SerializeField] private FlowPaletteProfile initialPalette;
        [SerializeField] private FSSRSStylePreset initialStyle;
        [SerializeField] private Volume targetVolume;
        [SerializeField, Min(0.01f)] private float defaultTransitionDuration = 0.65f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private FlowPaletteState current;
        private FlowPaletteState source;
        private FlowPaletteState target;
        private float transitionDuration;
        private float transitionTime;
        private bool transitioning;

        private void OnEnable()
        {
            if (initialPalette != null)
            {
                current = initialPalette.State;
                Apply(current);
            }

            ApplyStyle(initialStyle);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (initialPalette != null)
                    Apply(initialPalette.State);
                return;
            }

            if (!transitioning)
                return;

            transitionTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(transitionTime / transitionDuration);
            current = FlowPaletteState.Lerp(source, target, transitionCurve.Evaluate(normalizedTime));
            Apply(current);
            transitioning = normalizedTime < 1f;
        }

        public void SetPalette(FlowPaletteProfile palette)
        {
            SetPalette(palette, defaultTransitionDuration);
        }

        public void SetPalette(FlowPaletteProfile palette, float duration)
        {
            if (palette == null)
                return;

            if (duration <= 0f || !Application.isPlaying)
            {
                current = palette.State;
                transitioning = false;
                Apply(current);
                return;
            }

            source = current;
            target = palette.State;
            transitionTime = 0f;
            transitionDuration = Mathf.Max(0.01f, duration);
            transitioning = true;
        }

        public void ApplyStyle(FSSRSStylePreset preset)
        {
            if (preset == null || targetVolume == null || targetVolume.profile == null)
                return;

            if (targetVolume.profile.TryGet(out FSSRSVolumeComponent component))
                preset.ApplyTo(component);
        }

        public void Configure(FlowPaletteProfile palette, FSSRSStylePreset style, Volume volume)
        {
            initialPalette = palette;
            initialStyle = style;
            targetVolume = volume;
            if (isActiveAndEnabled)
                OnEnable();
        }

        private static void Apply(in FlowPaletteState state)
        {
            Shader.SetGlobalColor(FSSRSShaderIDs.PaperColor, state.paper);
            Shader.SetGlobalColor(FSSRSShaderIDs.InkColor, state.ink);
            Shader.SetGlobalColor(FSSRSShaderIDs.ShadowColor, state.shadow);
            Shader.SetGlobalColor(FSSRSShaderIDs.MidColor, state.mid);
            Shader.SetGlobalColor(FSSRSShaderIDs.HighlightColor, state.highlight);
            Shader.SetGlobalColor(FSSRSShaderIDs.AccentColor, state.accent);
        }
    }
}
