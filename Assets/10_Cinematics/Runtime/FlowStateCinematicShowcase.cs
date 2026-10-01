using FlowState.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

namespace FlowState.Cinematics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FlowStateCinematicShowcase : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;
        [SerializeField] private GameObject graffitiMenu;
        [SerializeField] private GameObject actor;
        [SerializeField] private Transform sprayCan;
        [SerializeField] private ParticleSystem sprayMist;
        [SerializeField] private GameObject[] paintGroups;
        [SerializeField] private Vector3[] paintPath;
        [SerializeField] private Renderer graffitiLogoRenderer;
        [SerializeField] private FlowStatePaletteController paletteController;
        [SerializeField] private CanvasGroup emotionGroup;
        [SerializeField] private TMP_Text emotionLabel;
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private RectTransform logoRect;

        private FlowEmotion appliedEmotion = (FlowEmotion)(-1);
        private MaterialPropertyBlock graffitiProperties;
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");

        public void Configure(
            PlayableDirector playableDirector,
            GameObject menu,
            GameObject character,
            Transform can,
            ParticleSystem mist,
            GameObject[] groups,
            Vector3[] path,
            Renderer logoRenderer,
            FlowStatePaletteController palette,
            CanvasGroup emotionCanvasGroup,
            TMP_Text emotionText,
            CanvasGroup finalLogoGroup,
            RectTransform finalLogoRect)
        {
            director = playableDirector;
            graffitiMenu = menu;
            actor = character;
            sprayCan = can;
            sprayMist = mist;
            paintGroups = groups;
            paintPath = path;
            graffitiLogoRenderer = logoRenderer;
            paletteController = palette;
            emotionGroup = emotionCanvasGroup;
            emotionLabel = emotionText;
            logoGroup = finalLogoGroup;
            logoRect = finalLogoRect;
            appliedEmotion = (FlowEmotion)(-1);
            EvaluateAt(0d);
        }

        private void OnEnable()
        {
            if (director != null)
                EvaluateAt(director.time);
        }

        private void LateUpdate()
        {
            if (director != null)
                EvaluateAt(director.time);
        }

        private void OnDisable()
        {
            if (sprayMist != null && sprayMist.isPlaying)
                sprayMist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void EvaluateAt(double playableTime)
        {
            float time = Mathf.Max(0f, (float)playableTime);
            bool menuVisible = time >= 4.5f && time < 18f;
            bool painting = time >= 18f && time < 56f;
            bool canVisible = painting;

            SetActive(graffitiMenu, menuVisible);
            SetActive(actor, painting);
            SetActive(sprayCan != null ? sprayCan.gameObject : null, canVisible);

            float paint01 = time < 4.5f ? 1f : Mathf.Clamp01((time - 18f) / 37f);
            int visibleGroups = paintGroups == null
                ? 0
                : Mathf.Clamp(Mathf.CeilToInt(paint01 * paintGroups.Length), 0, paintGroups.Length);
            if (paintGroups != null)
            {
                for (int index = 0; index < paintGroups.Length; index++)
                    SetActive(paintGroups[index], index < visibleGroups);
            }

            UpdateGraffitiLogo(time);

            UpdateSprayCan(time, paint01, painting);
            UpdateEmotion(time);
            UpdateLogo(time);
        }

        private void UpdateGraffitiLogo(float time)
        {
            if (graffitiLogoRenderer == null)
                return;
            if (graffitiProperties == null)
                graffitiProperties = new MaterialPropertyBlock();
            float reveal = time < 18f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - 18f) / 37f));
            graffitiLogoRenderer.GetPropertyBlock(graffitiProperties);
            graffitiProperties.SetFloat(RevealId, reveal);
            graffitiLogoRenderer.SetPropertyBlock(graffitiProperties);
        }

        private void UpdateSprayCan(float time, float paint01, bool painting)
        {
            if (sprayCan != null && paintPath != null && paintPath.Length > 0)
            {
                if (time < 4.5f)
                {
                    sprayCan.localPosition = new Vector3(4.25f, 1.85f + Mathf.Sin(time * 1.7f) * 0.08f, -2.4f);
                    sprayCan.localRotation = Quaternion.Euler(10f, -24f + Mathf.Sin(time * 1.2f) * 8f, 8f);
                    return;
                }

                float pathPosition = paint01 * (paintPath.Length - 1);
                int from = Mathf.Clamp(Mathf.FloorToInt(pathPosition), 0, paintPath.Length - 1);
                int to = Mathf.Min(from + 1, paintPath.Length - 1);
                float blend = pathPosition - from;
                Vector3 point = Vector3.Lerp(paintPath[from], paintPath[to], blend);
                float vibration = painting ? Mathf.Sin(time * 48f) * 0.025f : 0f;
                sprayCan.localPosition = point + new Vector3(vibration, -0.32f, -0.48f);
                sprayCan.localRotation = Quaternion.Euler(-5f, 0f, Mathf.Sin(time * 5.4f) * 3f);
                if (painting && actor != null)
                {
                    FlowStateCinematicPainter painter = actor.GetComponent<FlowStateCinematicPainter>();
                    if (painter != null) painter.Pose(point, sprayCan, time);
                }
                if (sprayMist != null)
                    sprayMist.transform.rotation = Quaternion.LookRotation(sprayCan.parent.TransformPoint(point) - sprayMist.transform.position);
            }

            if (sprayMist == null || !Application.isPlaying)
                return;

            if (painting && !sprayMist.isPlaying)
                sprayMist.Play();
            else if (!painting && sprayMist.isPlaying)
                sprayMist.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private void UpdateEmotion(float time)
        {
            FlowEmotion emotion = FlowEmotion.CreativeFlow;
            string caption = "CREATIVE FLOW // MAX";
            Color color = new Color(0.18f, 1f, 0.92f);

            if (paletteController != null && emotion != appliedEmotion)
            {
                paletteController.SetEmotion(emotion, Application.isPlaying ? 0.7f : 0f);
                appliedEmotion = emotion;
            }

            if (emotionGroup != null)
                emotionGroup.alpha = time < 63.5f ? Mathf.Clamp01(time * 2f) : Mathf.Clamp01((64f - time) * 2f);
            if (emotionLabel != null)
            {
                emotionLabel.text = "FSSRS // " + caption + "\nEMOTION SHADER";
                emotionLabel.color = color;
            }
        }

        private void UpdateLogo(float time)
        {
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - 64f) / 1.2f));
            if (logoGroup != null)
            {
                logoGroup.alpha = reveal;
                logoGroup.interactable = false;
                logoGroup.blocksRaycasts = false;
            }

            if (logoRect != null)
            {
                logoRect.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, reveal);
                logoRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-2.5f, 0f, reveal));
            }
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }
    }
}
