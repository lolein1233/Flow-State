using System.Collections;
using TMPro;
using UnityEngine;

namespace FlowState.Menu
{
    public sealed class MenuSubmenu : MonoBehaviour
    {
        public const float ReducedMotionScale = .6f;

        public GameObject presentation;
        public TMP_Text heading, body;
        public Renderer artwork;
        public Texture[] gallery;
        public GameObject galleryDecoration, settingsDecoration;
        public Renderer[] accentRenderers;
        public CanMotion motion;
        public MenuCameraFeedback cameraFeedback;
        public MenuAudio sound;
        [Min(.05f)] public float openDuration = .28f;

        MenuAction mode;
        int selected;
        Material artMaterial;
        Coroutine opening;
        Vector3 shownScale;
        Quaternion shownRotation;
        MaterialPropertyBlock accentProperties;
        static readonly Color PaperInk = new Color(.965f, .94f, .84f);

        public bool IsOpen => presentation && presentation.activeSelf;

        void Awake()
        {
            accentProperties = new MaterialPropertyBlock();
            artMaterial = new Material(artwork.sharedMaterial);
            artwork.sharedMaterial = artMaterial;
            shownScale = presentation.transform.localScale;
            shownRotation = presentation.transform.localRotation;

            heading.textWrappingMode = TextWrappingModes.NoWrap;
            heading.overflowMode = TextOverflowModes.Truncate;
            body.textWrappingMode = TextWrappingModes.NoWrap;
            // Layout is authored explicitly in world space; truncation can suppress the
            // entire mesh when TMP's font metrics exceed the physical card height.
            body.overflowMode = TextOverflowModes.Overflow;
            body.richText = true;

            float savedMotion = PlayerPrefs.GetFloat("FS.Menu.Motion", 1);
            float motionScale = savedMotion < .8f ? ReducedMotionScale : 1;
            motion.motionScale = cameraFeedback.motionScale = motionScale;
            // Older builds stored zero for the UI label "REDUCIDO", which disabled
            // the reactive camera completely. Persist the corrected reduced value.
            if (!Mathf.Approximately(savedMotion, motionScale))
                PlayerPrefs.SetFloat("FS.Menu.Motion", motionScale);

            AudioListener.volume = PlayerPrefs.GetFloat("FS.Audio.Master", 1);
            presentation.SetActive(false);
        }

        public void Open(MenuAction action)
        {
            mode = action;
            selected = 0;
            presentation.SetActive(true);
            Refresh();
            if (opening != null) StopCoroutine(opening);
            opening = StartCoroutine(AnimateOpen());
        }

        public void Navigate(int direction)
        {
            selected = GraffitiPlacement.Wrap(
                selected + direction,
                mode == MenuAction.Gallery ? Mathf.Max(1, gallery.Length) : 3);
            Refresh();
        }

        public void Confirm()
        {
            if (mode == MenuAction.Gallery)
            {
                Navigate(1);
                return;
            }

            if (selected == 0)
            {
                AudioListener.volume = AudioListener.volume > .99f ? 0 : Mathf.Min(1, AudioListener.volume + .25f);
                PlayerPrefs.SetFloat("FS.Audio.Master", AudioListener.volume);
            }
            else if (selected == 1)
            {
                float value = motion.motionScale > .8f ? ReducedMotionScale : 1;
                motion.motionScale = cameraFeedback.motionScale = value;
                PlayerPrefs.SetFloat("FS.Menu.Motion", value);
            }
            else Close();

            Refresh();
        }

        public void Close()
        {
            if (opening != null)
            {
                StopCoroutine(opening);
                opening = null;
            }

            presentation.transform.localScale = shownScale;
            presentation.transform.localRotation = shownRotation;
            presentation.SetActive(false);
            PlayerPrefs.Save();
        }

        void Refresh()
        {
            bool art = mode == MenuAction.Gallery;
            artwork.gameObject.SetActive(art && gallery.Length > 0);
            if (galleryDecoration) galleryDecoration.SetActive(art);
            if (settingsDecoration) settingsDecoration.SetActive(!art);

            Color accent = art ? new Color(.96f, .16f, .08f) : new Color(.18f, .42f, 1f);
            ApplyAccent(accent);

            heading.text = art ? "GALERÍA" : "CONFIG";
            heading.fontSize = art ? 5.35f : 5.1f;
            heading.color = new Color(.055f, .045f, .06f);
            heading.rectTransform.localPosition = new Vector3(-5.72f, 2.18f, -.31f);
            heading.rectTransform.sizeDelta = new Vector2(5.35f, .78f);

            body.color = PaperInk;
            body.alignment = TextAlignmentOptions.TopLeft;
            if (art)
            {
                body.fontSize = 1.9f;
                body.lineSpacing = 0;
                body.rectTransform.localPosition = new Vector3(-5.48f, -1.42f, -.34f);
                body.rectTransform.sizeDelta = new Vector2(4.95f, 1.25f);
                body.text = "ARCHIVO " + (selected + 1).ToString("00") + "/" + gallery.Length.ToString("00") +
                    "   ← → EXPLORAR\nESC / B  VOLVER";
                if (gallery.Length > 0) artMaterial.mainTexture = gallery[selected];
            }
            else
            {
                body.fontSize = 2.1f;
                body.lineSpacing = 20;
                body.rectTransform.localPosition = new Vector3(-5.38f, .84f, -.34f);
                body.rectTransform.sizeDelta = new Vector2(4.82f, 3.45f);
                string mark = "<color=#" + ColorUtility.ToHtmlStringRGB(accent) + ">●</color> ";
                body.text =
                    (selected == 0 ? mark : "    ") + "AUDIO        " + Mathf.RoundToInt(AudioListener.volume * 100) + "%\n" +
                    (selected == 1 ? mark : "    ") + "MOVIMIENTO   " + (motion.motionScale > .8f ? "SÍ" : "REDUCIDO") + "\n" +
                    (selected == 2 ? mark : "    ") + "VOLVER\n" +
                    "<color=#B8B09F>← → ELEGIR\nENTER / A CAMBIAR</color>";
            }
        }

        IEnumerator AnimateOpen()
        {
            Transform panel = presentation.transform;
            panel.localScale = shownScale * .82f;
            panel.localRotation = shownRotation * Quaternion.Euler(0, 0, -3.5f);
            float time = 0;
            while (time < 1)
            {
                time += Time.unscaledDeltaTime / Mathf.Max(.05f, openDuration);
                float t = Mathf.Clamp01(time);
                float overshoot = 1 + 2.25f * Mathf.Pow(t - 1, 3) + 1.25f * Mathf.Pow(t - 1, 2);
                panel.localScale = Vector3.LerpUnclamped(shownScale * .82f, shownScale, overshoot);
                panel.localRotation = Quaternion.Slerp(
                    shownRotation * Quaternion.Euler(0, 0, -3.5f),
                    shownRotation,
                    Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            panel.localScale = shownScale;
            panel.localRotation = shownRotation;
            opening = null;
        }

        void ApplyAccent(Color color)
        {
            accentProperties.Clear();
            accentProperties.SetColor("_BaseColor", color);
            accentProperties.SetColor("_Color", color);
            if (accentRenderers == null) return;
            foreach (Renderer renderer in accentRenderers)
                if (renderer) renderer.SetPropertyBlock(accentProperties);
        }

        void OnDestroy()
        {
            if (artMaterial) Destroy(artMaterial);
        }
    }
}
