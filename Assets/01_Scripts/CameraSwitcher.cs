using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Transform mainCamera;

    public Transform thirdPersonPoint;
    public Transform firstPersonPoint;

    public float speed = 5f;

    private Transform target;
    private bool isMoving = false;

    void Start()
    {
        target = thirdPersonPoint;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            target = firstPersonPoint;
            isMoving = true;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            target = thirdPersonPoint;
            isMoving = true;
        }

        if (isMoving)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.position, target.position, Time.deltaTime * speed);
            mainCamera.rotation = Quaternion.Lerp(mainCamera.rotation, target.rotation, Time.deltaTime * speed);

            if (Vector3.Distance(mainCamera.position, target.position) < 0.05f)
            {
                isMoving = false;
            }
        }
    }
}
