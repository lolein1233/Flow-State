using UnityEngine;

public class CameraTransition : MonoBehaviour
{
    public FPSController linkedController;
    public Transform mainCamera;

    public Transform thirdPersonPoint;
    public Transform firstPersonPoint;

    public float speed = 5f;

    private Transform target;
    private bool isMoving = false;
    private bool inFirstPerson = false;

    void Start()
    {
        target = thirdPersonPoint;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitFirstPerson();
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

    public void EnterFirstPerson()
    {
        if (linkedController != null)
        {
            linkedController.EnterGraffitiMode();
            inFirstPerson = true;
            return;
        }

        target = firstPersonPoint;
        isMoving = true;
        inFirstPerson = true;
    }

    public void ExitFirstPerson()
    {
        if (linkedController != null)
        {
            linkedController.ExitGraffitiMode();
            inFirstPerson = false;
            return;
        }

        target = thirdPersonPoint;
        isMoving = true;
        inFirstPerson = false;
    }

    public bool IsInFirstPerson()
    {
        if (linkedController != null)
            return linkedController.IsGraffitiMode();

        return inFirstPerson;
    }
}
