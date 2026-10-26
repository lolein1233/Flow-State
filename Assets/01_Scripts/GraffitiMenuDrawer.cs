using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GraffitiMenuDrawer : MonoBehaviour
{
    [Header("Referencias")]
    public Camera cam;
    public GraffitiPainter painter;
    public FPSController fps;

    [Header("Menú")]
    public GameObject menuPrefab;
    public LayerMask paintableLayer;
    public LayerMask menuLayer;

    [Header("Control")]
    public KeyCode openMenuKey = KeyCode.Q;
    public KeyCode selectKey = KeyCode.E;
    public bool allowMouseClick = true;

    [Header("Raycast")]
    public float drawDistance = 3f;
    public float selectDistance = 3f;
    public float surfaceOffset = 0.02f;
    [Range(1f, 8f)] public float menuScaleMultiplier = 1f;

    [Header("Tag opcional")]
    public bool requireWallTag = false;
    public string wallTag = "Wall";

    GraffitiDrawMenu currentMenu;
    GraffitiDrawnMenuButton currentHover;
    GraffitiColorWheel currentColorWheel;
    GraffitiColorValueSlider currentValueSlider;
    bool previousCanMove;
    int closedFrame = -1;
    GraffitiQuickPainter quickPainter;

    public bool IsMenuOpen => currentMenu != null;
    public bool ClosedThisFrame => closedFrame == Time.frameCount;

    void Awake() => quickPainter = GetComponentInParent<GraffitiQuickPainter>();

    void Update()
    {
        if (PauseMenuController.GameplayInputBlocked) return;
        if (quickPainter != null && quickPainter.IsQuickMode)
        {
            if (currentMenu != null) CloseMenu();
            return;
        }
        if (currentMenu != null && fps != null && !fps.IsGraffitiMode())
        {
            CloseMenu();
            return;
        }
        if (currentMenu != null && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
            return;
        }

        if (Input.GetKeyDown(openMenuKey))
        {
            if (currentMenu != null)
                CloseMenu();
            else if (fps != null && fps.EnteredGraffitiModeThisFrame())
                return;
            else if (fps != null && !fps.IsGraffitiMode())
            {
                RaycastHit entryHit;
                if (TryGetPaintableHit(out entryHit))
                    fps.EnterGraffitiMode();
                return;
            }
            else
                TryDrawMenu();
        }

        if (currentMenu != null)
            HandleMenuSelection();
    }

    void TryDrawMenu()
    {
        if (cam == null || menuPrefab == null) return;

        RaycastHit hit;
        if (!TryGetPaintableHit(out hit))
            return;

        Vector3 spawnPos = hit.point + hit.normal * surfaceOffset;
        Quaternion spawnRot = GetSurfaceRotation(hit.normal);

        GameObject menuObj = Instantiate(menuPrefab, spawnPos, spawnRot);

        currentMenu = menuObj.GetComponent<GraffitiDrawMenu>();

        if (currentMenu == null)
            currentMenu = menuObj.AddComponent<GraffitiDrawMenu>();

        currentMenu.SetPresentationScale(menuScaleMultiplier);
        currentMenu.BindPainter(painter);

        if (painter != null)
        {
            painter.menuOpen = true;
            painter.CancelPaintingForMenu();
        }

        RefreshColorControls();
        RefreshNozzleSelection();

        if (fps != null)
        {
            previousCanMove = fps.canMove;
            fps.canMove = false;
            fps.focusMode = false;
        }
    }

    bool TryGetPaintableHit(out RaycastHit hit)
    {
        hit = default(RaycastHit);
        if (cam == null)
            return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        float effectiveDistance = painter != null ? Mathf.Max(drawDistance, painter.MaximumPaintDistance) : drawDistance;
        if (!Physics.Raycast(ray, out hit, effectiveDistance, paintableLayer))
            return false;

        return !requireWallTag || hit.collider.CompareTag(wallTag);
    }

    void HandleMenuSelection()
    {
        if (cam == null || currentMenu == null) return;

        Ray ray = GetSelectionRay(Input.mousePosition);
        RaycastHit hit;

        GraffitiDrawnMenuButton button = null;
        GraffitiColorWheel colorWheel = null;
        GraffitiColorValueSlider valueSlider = null;

        float effectiveDistance = painter != null ? Mathf.Max(selectDistance, painter.MaximumPaintDistance) : selectDistance;
        if (Physics.Raycast(ray, out hit, effectiveDistance, menuLayer))
        {
            button = hit.collider.GetComponentInParent<GraffitiDrawnMenuButton>();
            if (button == null)
                colorWheel = hit.collider.GetComponentInParent<GraffitiColorWheel>();

            if (button == null && colorWheel == null)
                valueSlider = hit.collider.GetComponentInParent<GraffitiColorValueSlider>();
        }

        if (button != currentHover)
        {
            if (currentHover != null)
                currentHover.SetHover(false);

            currentHover = button;

            if (currentHover != null)
                currentHover.SetHover(true);
        }

        if (colorWheel != currentColorWheel)
        {
            if (currentColorWheel != null)
                currentColorWheel.SetHover(false);

            currentColorWheel = colorWheel;

            if (currentColorWheel != null)
                currentColorWheel.SetHover(true);
        }

        if (valueSlider != currentValueSlider)
        {
            if (currentValueSlider != null)
                currentValueSlider.SetHover(false);

            currentValueSlider = valueSlider;

            if (currentValueSlider != null)
                currentValueSlider.SetHover(true);
        }

        if (!currentMenu.IsReady) return;

        bool selected = Input.GetKeyDown(selectKey) || (allowMouseClick && Input.GetMouseButtonDown(0));
        bool selectingContinuous = selected || Input.GetKey(selectKey) || (allowMouseClick && Input.GetMouseButton(0));

        if (currentColorWheel != null)
        {
            currentColorWheel.Preview(hit, painter);

            if (selectingContinuous)
                currentColorWheel.Apply(hit, painter);

            return;
        }

        if (currentValueSlider != null)
        {
            currentValueSlider.Preview(hit, painter);

            if (selectingContinuous)
                currentValueSlider.Apply(hit, painter);

            return;
        }

        if (currentHover == null) return;

        if (selected)
        {
            currentHover.Apply(painter, this);
            RefreshNozzleSelection();
        }
    }

    public void CloseMenu()
    {
        if (currentMenu == null)
            return;

        closedFrame = Time.frameCount;
        if (currentHover != null)
        {
            currentHover.SetHover(false);
            currentHover = null;
        }

        if (currentColorWheel != null)
        {
            currentColorWheel.SetHover(false);
            currentColorWheel = null;
        }

        if (currentValueSlider != null)
        {
            currentValueSlider.SetHover(false);
            currentValueSlider = null;
        }

        if (currentMenu != null)
        {
            currentMenu.CloseAndDestroy();
            currentMenu = null;
        }

        if (painter != null)
            painter.menuOpen = false;

        if (fps != null)
            fps.canMove = previousCanMove;
    }

    public Ray GetSelectionRay(Vector2 screenPosition) => cam.ScreenPointToRay(screenPosition);

    void OnDisable()
    {
        CloseMenu();
    }

    void RefreshNozzleSelection()
    {
        if (currentMenu == null || painter == null) return;
        foreach (GraffitiDrawnMenuButton button in currentMenu.GetComponentsInChildren<GraffitiDrawnMenuButton>(true))
        {
            GraffitiNozzleVisual visual = button.GetComponent<GraffitiNozzleVisual>();
            if (visual != null)
                visual.SetSelected(button.buttonType == GraffitiDrawnMenuButton.ButtonType.Nozzle &&
                    button.nozzleShape == painter.currentNozzleShape);
        }
    }

    void RefreshColorControls()
    {
        if (currentMenu == null || painter == null)
            return;

        GraffitiColorWheel[] wheels = currentMenu.GetComponentsInChildren<GraffitiColorWheel>(true);
        foreach (GraffitiColorWheel wheel in wheels)
            wheel.UpdateSelectionVisual(painter);

        GraffitiColorValueSlider[] sliders = currentMenu.GetComponentsInChildren<GraffitiColorValueSlider>(true);
        foreach (GraffitiColorValueSlider slider in sliders)
            slider.UpdateSliderTint(painter);
    }

    Quaternion GetSurfaceRotation(Vector3 normal)
    {
        Vector3 forward = -normal;

        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, forward);

        if (up.sqrMagnitude < 0.001f)
            up = Vector3.ProjectOnPlane(cam.transform.forward, forward);

        return Quaternion.LookRotation(forward, up);
    }
}
