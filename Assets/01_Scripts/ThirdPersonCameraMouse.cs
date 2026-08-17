using UnityEngine;

public class ThirdPersonCameraMouse : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 200f;
    public float distance = 4f;
    public CameraTransition camTransition;
    public float minY = -30f;
    public float maxY = 60f;
    public Transform player;
    public FPSController playerController;

    float xRotation = 0f;
    float yRotation = 0f;

    void Start()
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<FPSController>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (camTransition != null && camTransition.IsInFirstPerson())
            return; // 🚫 NO mover cámara en primera persona

        if (playerController != null && playerController.IsGraffitiMode())
            return;

        if (target == null || player == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);
        Vector3 direction = rotation * new Vector3(0, 0, -distance);

        transform.position = target.position + direction;
        transform.LookAt(target);

        player.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    public float GetYRotation()
    {
        return yRotation;
    }
}
