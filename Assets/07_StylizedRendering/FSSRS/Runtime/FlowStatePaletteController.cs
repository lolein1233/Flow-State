using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace FlowState.Rendering
{
    public enum FlowEmotion
    {
        // Keep the serialized and shader indices of the existing looks.
        Comic = 0,
        Neutral = 1,
        Anger = 2,
        CreativeFlow = 3
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FlowStatePaletteController : MonoBehaviour
    {
        [SerializeField] private FlowPaletteProfile initialPalette;
        [SerializeField] private FSSRSStylePreset initialStyle;
        [SerializeField] private Volume targetVolume;
        [SerializeField] private FlowEmotion initialEmotion = FlowEmotion.Neutral;
        [SerializeField, FormerlySerializedAs("neutralPalette")] private FlowPaletteProfile comicPalette;
        [SerializeField, FormerlySerializedAs("doubtPalette"), InspectorName("Neutral Palette")] private FlowPaletteProfile neutralStatePalette;
        [SerializeField] private FlowPaletteProfile angerPalette;
        [SerializeField] private FlowPaletteProfile creativeFlowPalette;
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
            NormalizeInitialEmotion();
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

            NormalizeInitialEmotion();
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
            if (!System.Enum.IsDefined(typeof(FlowEmotion), emotion))
                emotion = FlowEmotion.Neutral;
            CurrentEmotion = emotion;
            ApplyEmotion(emotion);
            SetPalette(PaletteFor(emotion), duration);
        }

        public void SetNormalState()
        {
            SetEmotion(FlowEmotion.Neutral);
        }

        public void SetMonochromeState()
        {
            SetEmotion(FlowEmotion.Comic);
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
            FlowPaletteProfile comic,
            FlowPaletteProfile neutral,
            FlowPaletteProfile anger,
            FlowPaletteProfile creativeFlow,
            FlowEmotion startingEmotion)
        {
            comicPalette = comic;
            neutralStatePalette = neutral;
            angerPalette = anger;
            creativeFlowPalette = creativeFlow;
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
                FlowEmotion.Comic => comicPalette,
                FlowEmotion.Neutral => neutralStatePalette,
                FlowEmotion.Anger => angerPalette,
                FlowEmotion.CreativeFlow => creativeFlowPalette,
                _ => neutralStatePalette != null ? neutralStatePalette : initialPalette
            };
        }

        public static float GetEmotionEnergy(FlowEmotion emotion)
        {
            return emotion switch
            {
                FlowEmotion.Neutral => 0.22f,
                FlowEmotion.Comic => 0.42f,
                FlowEmotion.CreativeFlow => 0.86f,
                FlowEmotion.Anger => 1f,
                _ => 0.42f
            };
        }

        private void NormalizeInitialEmotion()
        {
            // Old scenes may still serialize the removed Clarity value (4).
            if (!System.Enum.IsDefined(typeof(FlowEmotion), initialEmotion))
                initialEmotion = FlowEmotion.Neutral;
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
