using UnityEngine;

public class GraffitiAnimation : MonoBehaviour
{
    public Vector3 idlePosition;
    public Vector3 sprayOffset;

    public float moveSpeed = 10f;
    public float swayAmount = 2f;
    Quaternion initialRotation;


    Vector3 targetPos;

    void Start()
    {
        targetPos = idlePosition;
        initialRotation = transform.localRotation; // 👈 guardamos rotación original
    }

    void Update()
    {
        HandlePosition();
    }

    void LateUpdate()
    {
        float factor = Input.GetMouseButton(0) ? 1.2f : 1f; // 👈 más movimiento al pintar
        HandleRotation(factor);
    }   

    void HandlePosition()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * moveSpeed);
    }

    void HandleRotation(float intensity = 1f)
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 🎯 SWAY BASE
        Quaternion sway = Quaternion.Euler(
            -mouseY * swayAmount * intensity,
            mouseX * swayAmount * intensity,
            0f
        );

        // 🔥 MOVIMIENTO EXTRA AL PINTAR
        float paintNoise = 0f;

        if (Input.GetMouseButton(0))
        {
            transform.localPosition += new Vector3(0, 0, -0.002f);
        }

        Quaternion noiseRot = Quaternion.Euler(0, 0, paintNoise);

        Quaternion targetRot = initialRotation * sway * noiseRot;

        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            targetRot,
            Time.deltaTime * 12f
        );
    }

    public void StartSpray()
    {
        targetPos = idlePosition + sprayOffset;
    }

    public void StopSpray()
    {
        targetPos = idlePosition;
    }


}
