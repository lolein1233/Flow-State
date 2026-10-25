using UnityEngine;

/// <summary>Rebote, respiración y parallax de papel con el mismo ritmo del menú principal.</summary>
public sealed class PunkMenuMotion : MonoBehaviour
{
    [SerializeField] RectTransform panel, shadow, title;
    [SerializeField] CanvasGroup group;
    [SerializeField] UnityEngine.UI.Image backdropImage;
    [SerializeField] bool ambient;
    Material backdrop;
    Vector2 panelPosition, shadowPosition, titlePosition;
    Quaternion panelRotation, shadowRotation;
    static readonly int HoloTimeId = Shader.PropertyToID("_HoloTime");
    static readonly int PointerId = Shader.PropertyToID("_Pointer");
    float openedAt, closeAt;
    bool configured, closing;
    float motionScale = 1f;

    public void Configure(RectTransform paper, RectTransform paperShadow, RectTransform heading, CanvasGroup canvasGroup,
        bool animateAmbient, UnityEngine.UI.Image holographicBackdrop = null)
    {
        panel = paper;
        shadow = paperShadow;
        title = heading;
        backdropImage = holographicBackdrop;
        group = canvasGroup;
        ambient = animateAmbient;
        CachePose();
    }

    void CachePose()
    {
        if (panel == null || shadow == null || group == null) return;
        panelPosition = panel.anchoredPosition;
        panelRotation = panel.localRotation;
        shadowPosition = shadow.anchoredPosition;
        shadowRotation = shadow.localRotation;
        if (title != null) titlePosition = title.anchoredPosition;
        configured = true;
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!configured) CachePose();
        openedAt = Time.unscaledTime;
        closing = false;
        motionScale = PlayerPrefs.GetFloat("FS.Menu.Motion", 1f) < 0.8f ? 0.6f : 1f;
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = true;
        }
    }

    public void PlayClose()
    {
        closing = true;
        closeAt = Time.unscaledTime;
        group.interactable = false;
    }

    void Update()
    {
        if (!configured) return;
        if (backdrop == null && backdropImage != null)
        {
            backdrop = new Material(backdropImage.material) { name = "Pause Holographic Foil (Runtime)" };
            backdropImage.material = backdrop;
        }
        float elapsed = Time.unscaledTime - openedAt;
        float enter = Mathf.Clamp01(elapsed / (ambient ? 0.32f : 0.2f));
        float ease = 1f + 2.25f * Mathf.Pow(enter - 1f, 3f) + 1.25f * Mathf.Pow(enter - 1f, 2f);
        float close = closing ? Mathf.Clamp01((Time.unscaledTime - closeAt) / 0.17f) : 0f;
        float t = Time.unscaledTime;
        Vector2 pointer = new Vector2(
            Screen.width > 0 ? Mathf.Clamp(Input.mousePosition.x / Screen.width * 2f - 1f, -1f, 1f) : 0f,
            Screen.height > 0 ? Mathf.Clamp(Input.mousePosition.y / Screen.height * 2f - 1f, -1f, 1f) : 0f);
        Vector2 drift = ambient ? (new Vector2(Mathf.Sin(t * 1.17f) * 4f, Mathf.Sin(t * 1.83f) * 3f) + pointer * 9f) * motionScale : Vector2.zero;
        float rotation = ambient ? (Mathf.Sin(t * 1.41f) * 0.22f - pointer.x * 0.28f) * motionScale : 0f;
        float scale = Mathf.LerpUnclamped(1f - 0.18f * motionScale, 1f, ease) * Mathf.Lerp(1f, 0.94f, close);
        panel.anchoredPosition = panelPosition + drift + Vector2.down * (close * 14f);
        panel.localScale = Vector3.one * scale;
        panel.localRotation = panelRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(-3.5f * motionScale, 0f, Mathf.SmoothStep(0f, 1f, enter)) + rotation);
        shadow.anchoredPosition = shadowPosition + drift * 1.3f;
        shadow.localScale = panel.localScale;
        shadow.localRotation = shadowRotation * Quaternion.Euler(0f, 0f, rotation);
        group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.12f)) * (1f - close);
        if (title != null)
        {
            title.anchoredPosition = titlePosition + Vector2.up * (Mathf.Sin(t * 2.35f) * 1.8f * motionScale);
            title.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.35f) * 0.009f * motionScale);
        }
        if (backdrop != null)
        {
            backdrop.SetFloat(HoloTimeId, t * motionScale);
            backdrop.SetVector(PointerId, new Vector4(pointer.x * motionScale, pointer.y * motionScale, 0f, 0f));
        }
    }

    void OnDestroy()
    {
        if (backdrop != null) Destroy(backdrop);
    }
}
