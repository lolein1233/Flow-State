using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    public float speed = 5f;
    public ThirdPersonCameraMouse camScript;
    public CameraTransition camTransition;
    public FPSController unifiedController;

    void Awake()
    {
        if (unifiedController == null)
            unifiedController = GetComponent<FPSController>();
    }

    void Update()
    {
        if (unifiedController != null && unifiedController.enabled)
            return;

        if (camTransition != null && camTransition.IsInFirstPerson())
            return;

        if (camScript == null)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(h, 0, v);

        // 👉 Dirección basada en la cámara
        Vector3 moveDir = camScript.transform.forward * v + camScript.transform.right * h;
        moveDir.y = 0;
        moveDir.Normalize();

        transform.position += moveDir * speed * Time.deltaTime;
    }
}
