using UnityEngine;
using TMPro;
public class GraffitiRaycastUI : MonoBehaviour
{
    public float distance = 3f;
    public LayerMask wallLayer;
    public bool isPainting = false;
    public GameObject promptUI;
    public Camera cam;
    public FPSController fps;
    public bool enterGraffitiOnInteract = true;
    public KeyCode interactKey = KeyCode.E;

    bool canPaint = false;
    RaycastHit currentHit;

    void Awake()
    {
        if (fps == null)
            fps = GetComponent<FPSController>();
    }

    void Update()
    {
        if (cam == null)
            return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distance, wallLayer))
        {
            canPaint = true;
            currentHit = hit;

            if (promptUI != null && !isPainting)
                promptUI.SetActive(true);

            if (enterGraffitiOnInteract && fps != null && !fps.IsGraffitiMode() && !fps.IsClimbing() && Input.GetKeyDown(interactKey))
                fps.EnterGraffitiMode();
        }
        else
        {
            canPaint = false;

            if (promptUI != null)
                promptUI.SetActive(false);
        }
    }

    public bool CanPaint() => canPaint;
    public RaycastHit GetHit() => currentHit;
}
