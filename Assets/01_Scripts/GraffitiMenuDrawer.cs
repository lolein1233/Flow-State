using UnityEngine;

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

    [Header("Tag opcional")]
    public bool requireWallTag = false;
    public string wallTag = "Wall";

    GraffitiDrawMenu currentMenu;
    GraffitiDrawnMenuButton currentHover;
    GraffitiColorWheel currentColorWheel;
    GraffitiColorValueSlider currentValueSlider;

    void Update()
    {
        if (currentMenu != null && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
            return;
        }

        if (Input.GetKeyDown(openMenuKey))
        {
            if (currentMenu != null)
                CloseMenu();
            else
                TryDrawMenu();
        }

        if (currentMenu != null)
            HandleMenuSelection();
    }

    void TryDrawMenu()
    {
        if (cam == null || menuPrefab == null) return;

        if (fps != null && !fps.IsGraffitiMode())
            fps.EnterGraffitiMode();

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, drawDistance, paintableLayer))
            return;

        if (requireWallTag && !hit.collider.CompareTag(wallTag))
            return;

        Vector3 spawnPos = hit.point + hit.normal * surfaceOffset;
        Quaternion spawnRot = GetSurfaceRotation(hit.normal);

        GameObject menuObj = Instantiate(menuPrefab, spawnPos, spawnRot);

        currentMenu = menuObj.GetComponent<GraffitiDrawMenu>();

        if (currentMenu == null)
            currentMenu = menuObj.AddComponent<GraffitiDrawMenu>();

        if (painter != null)
        {
            painter.menuOpen = true;
            painter.CancelPaintingForMenu();
        }

        RefreshColorControls();

        if (fps != null)
        {
            fps.canMove = false;
            fps.focusMode = false;
        }
    }

    void HandleMenuSelection()
    {
        if (cam == null || currentMenu == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        GraffitiDrawnMenuButton button = null;
        GraffitiColorWheel colorWheel = null;
        GraffitiColorValueSlider valueSlider = null;

        if (Physics.Raycast(ray, out hit, selectDistance, menuLayer))
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
        }
    }

    public void CloseMenu()
    {
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
            fps.canMove = true;
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
