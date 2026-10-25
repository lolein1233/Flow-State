using UnityEngine;
using UnityEngine.Rendering;

namespace FlowState.Rendering
{
    public enum FlowEmotion
    {
        Neutral,
        Doubt,
        Anger,
        CreativeFlow,
        Clarity
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FlowStatePaletteController : MonoBehaviour
    {
        [SerializeField] private FlowPaletteProfile initialPalette;
        [SerializeField] private FSSRSStylePreset initialStyle;
        [SerializeField] private Volume targetVolume;
        [SerializeField] private FlowEmotion initialEmotion = FlowEmotion.CreativeFlow;
        [SerializeField] private FlowPaletteProfile neutralPalette;
        [SerializeField] private FlowPaletteProfile doubtPalette;
        [SerializeField] private FlowPaletteProfile angerPalette;
        [SerializeField] private FlowPaletteProfile creativeFlowPalette;
        [SerializeField] private FlowPaletteProfile clarityPalette;
        [SerializeField, Min(0.01f)] private float defaultTransitionDuration = 0.65f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private FlowPaletteState current;
        private FlowPaletteState source;
        private FlowPaletteState target;
        private float transitionDuration;
        private float transitionTime;
        private bool transitioning;

        public FlowEmotion CurrentEmotion { get; private set; }

        private void OnEnable()
        {
            CurrentEmotion = initialEmotion;
            FlowPaletteProfile emotionPalette = PaletteFor(CurrentEmotion);
            if (emotionPalette != null)
                initialPalette = emotionPalette;

            if (initialPalette != null)
            {
                current = initialPalette.State;
                Apply(current);
            }

            ApplyEmotion(CurrentEmotion);
            ApplyStyle(initialStyle);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            CurrentEmotion = initialEmotion;
            FlowPaletteProfile previewPalette = PaletteFor(CurrentEmotion);
            if (previewPalette != null)
            {
                initialPalette = previewPalette;
                current = previewPalette.State;
                Apply(current);
            }

            ApplyEmotion(CurrentEmotion);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                FlowPaletteProfile previewPalette = PaletteFor(CurrentEmotion);
                if (previewPalette != null)
                    Apply(previewPalette.State);
                else if (initialPalette != null)
                    Apply(initialPalette.State);
                ApplyEmotion(CurrentEmotion);
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

        public void SetEmotion(FlowEmotion emotion)
        {
            SetEmotion(emotion, defaultTransitionDuration);
        }

        public void SetEmotion(FlowEmotion emotion, float duration)
        {
            CurrentEmotion = emotion;
            ApplyEmotion(emotion);
            SetPalette(PaletteFor(emotion), duration);
        }

        public void SetNormalState()
        {
            SetEmotion(FlowEmotion.Clarity);
        }

        public void SetMonochromeState()
        {
            SetEmotion(FlowEmotion.Neutral);
        }

        public void SetAngerState()
        {
            SetEmotion(FlowEmotion.Anger);
        }

        public void SetFlowMaximumState()
        {
            SetEmotion(FlowEmotion.CreativeFlow);
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

        public void ConfigureEmotionPalettes(
            FlowPaletteProfile neutral,
            FlowPaletteProfile doubt,
            FlowPaletteProfile anger,
            FlowPaletteProfile creativeFlow,
            FlowPaletteProfile clarity,
            FlowEmotion startingEmotion)
        {
            neutralPalette = neutral;
            doubtPalette = doubt;
            angerPalette = anger;
            creativeFlowPalette = creativeFlow;
            clarityPalette = clarity;
            initialEmotion = startingEmotion;
            CurrentEmotion = startingEmotion;

            FlowPaletteProfile palette = PaletteFor(startingEmotion);
            if (palette != null)
                initialPalette = palette;

            if (isActiveAndEnabled)
                OnEnable();
        }

        public FlowPaletteProfile PaletteFor(FlowEmotion emotion)
        {
            return emotion switch
            {
                FlowEmotion.Neutral => neutralPalette,
                FlowEmotion.Doubt => doubtPalette,
                FlowEmotion.Anger => angerPalette,
                FlowEmotion.CreativeFlow => creativeFlowPalette,
                FlowEmotion.Clarity => clarityPalette,
                _ => initialPalette
            };
        }

        public static float GetEmotionEnergy(FlowEmotion emotion)
        {
            return emotion switch
            {
                FlowEmotion.Doubt => 0.22f,
                FlowEmotion.Neutral => 0.42f,
                FlowEmotion.Clarity => 0.62f,
                FlowEmotion.CreativeFlow => 0.86f,
                FlowEmotion.Anger => 1f,
                _ => 0.42f
            };
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

        private static void ApplyEmotion(FlowEmotion emotion)
        {
            Shader.SetGlobalFloat(FSSRSShaderIDs.EmotionIndex, (float)emotion);
            Shader.SetGlobalFloat(FSSRSShaderIDs.EmotionEnergy, GetEmotionEnergy(emotion));
        }
    }
}
