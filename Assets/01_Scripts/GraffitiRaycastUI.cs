using UnityEngine;
using TMPro;
[DefaultExecutionOrder(200)]
public class GraffitiRaycastUI : MonoBehaviour
{
    [Header("Distancias")]
    [Min(0.1f)]
    public float distance = 3f;
    [Min(0.1f)]
    public float graffitiDetectionDistance = 9f;

    public LayerMask wallLayer;
    public bool isPainting = false;
    public GameObject promptUI;
    public Camera cam;
    public FPSController fps;
    public GraffitiPainter painter;
    public bool enterGraffitiOnInteract = true;
    public KeyCode interactKey = KeyCode.Q;

    [Header("Texto contextual")]
    [TextArea] public string enterPrompt = "PRESIONA [Q] PARA ENTRAR\nAL MODO GRAFFITI";
    [TextArea] public string graffitiPrompt = "MANTEN [CLICK] PARA PINTAR\n[Q] MENU  -  [ESC] SALIR";

    bool canPaint = false;
    bool canDetectGraffitiSurface = false;
    RaycastHit currentHit;
    TMP_Text promptText;
    int lastPromptState = -1;
    int surfaceFrame = -1;

    void Awake()
    {
        if (fps == null)
            fps = GetComponent<FPSController>();
        if (painter == null)
            painter = GetComponent<GraffitiPainter>();

        graffitiPrompt = graffitiPrompt.Replace("\n[RUEDA] DISTANCIA DE LA LATA", string.Empty);

        if (promptUI != null)
            promptText = promptUI.GetComponentInChildren<TMP_Text>(true);
    }

    public void RefreshSurface()
    {
        if (cam == null || surfaceFrame == Time.frameCount)
            return;
        surfaceFrame = Time.frameCount;

        bool graffitiModeActive = fps != null && fps.IsGraffitiMode();
        Ray ray = graffitiModeActive && painter != null ? painter.GetSprayRay() : new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        float detectionDistance = Mathf.Max(distance, graffitiDetectionDistance);

        if (Physics.Raycast(ray, out hit, detectionDistance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            canDetectGraffitiSurface = true;
            canPaint = hit.distance <= (painter != null ? painter.MaximumPaintDistance : distance);
            currentHit = hit;
        }
        else
        {
            canPaint = false;
            canDetectGraffitiSurface = false;
            currentHit = default(RaycastHit);
        }
    }

    void LateUpdate()
    {
        RefreshSurface();
        if (canDetectGraffitiSurface && enterGraffitiOnInteract && fps != null && !fps.IsGraffitiMode() &&
            !fps.IsClimbing() && Input.GetKeyDown(interactKey))
            fps.EnterGraffitiMode();
        UpdatePromptText();
        bool graffitiActive = fps != null && fps.IsGraffitiMode();
        SetPromptVisible(canDetectGraffitiSurface && !isPainting && !graffitiActive);
    }

    public bool CanPaint() => canPaint;
    public bool CanDetectGraffitiSurface() => canDetectGraffitiSurface;
    public RaycastHit GetHit() => currentHit;

    void SetPromptVisible(bool visible)
    {
        if (promptUI == null)
            return;

        if (promptText == null)
            promptText = promptUI.GetComponentInChildren<TMP_Text>(true);

        if (promptText != null && promptText.gameObject != promptUI)
        {
            if (!promptUI.activeSelf)
                promptUI.SetActive(true);
            if (promptText.gameObject.activeSelf != visible)
                promptText.gameObject.SetActive(visible);
            return;
        }

        if (promptUI.activeSelf != visible)
            promptUI.SetActive(visible);
    }

    void UpdatePromptText()
    {
        if (promptText == null && promptUI != null)
            promptText = promptUI.GetComponentInChildren<TMP_Text>(true);

        if (promptText == null)
            return;

        int promptState = fps != null && fps.IsGraffitiMode() ? 1 : 0;
        if (promptState == lastPromptState)
            return;

        promptText.text = promptState == 1 ? graffitiPrompt : enterPrompt;
        lastPromptState = promptState;
    }
}
