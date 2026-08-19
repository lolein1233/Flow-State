using UnityEngine;

[DisallowMultipleComponent]
public class GraffitiAnimation : MonoBehaviour
{
    [Header("Base")]
    public Vector3 idlePosition;
    public Vector3 sprayOffset = new Vector3(0f, -0.025f, 0.035f);
    [SerializeField] float positionDamping = 15f;
    [SerializeField] float rotationDamping = 17f;

    [Header("Idle organico")]
    [SerializeField] Vector3 idlePositionAmplitude = new Vector3(0.0035f, 0.006f, 0.0026f);
    [SerializeField] Vector3 idleRotationAmplitude = new Vector3(0.65f, 0.8f, 0.72f);
    [SerializeField] float idleNoiseFrequency = 0.32f;
    [SerializeField, Range(1f, 3f)] float idlePresenceMultiplier = 2f;
    [SerializeField] Vector3 slowIdlePositionAmplitude = new Vector3(0.002f, 0.0045f, 0.0018f);
    [SerializeField] Vector3 slowIdleRotationAmplitude = new Vector3(0.35f, 0.45f, 1.1f);
    [SerializeField] float slowIdleNoiseFrequency = 0.095f;

    [Header("Sway de camara")]
    [SerializeField] Vector2 swayRotationAmount = new Vector2(1.8f, 2.25f);
    [SerializeField] Vector2 swayPositionAmount = new Vector2(0.0018f, 0.0012f);
    [SerializeField] float swayLag = 10f;
    [SerializeField] float maximumMouseDelta = 10f;

    [Header("Bobbing al caminar")]
    [SerializeField] float walkBobFrequency = 1.7f;
    [SerializeField] Vector3 walkBobAmplitude = new Vector3(0.005f, 0.0065f, 0.002f);
    [SerializeField] Vector3 walkBobRotation = new Vector3(0.7f, 0.5f, 0.9f);
    [SerializeField] float walkBlendSpeed = 7f;

    [Header("Pintado")]
    [SerializeField] float valveRecoilDistance = 0.012f;
    [SerializeField] float valveRecoilDuration = 0.16f;
    [SerializeField] Vector3 sprayVibrationAmplitude = new Vector3(0.0011f, 0.0014f, 0.0008f);
    [SerializeField] Vector3 sprayVibrationRotation = new Vector3(0.35f, 0.45f, 0.55f);
    [SerializeField] float sprayVibrationFrequency = 24f;
    [SerializeField] Vector3 wristDriftAmplitude = new Vector3(0.003f, 0.004f, 0.0015f);
    [SerializeField] float wristDriftFrequency = 0.55f;

    [Header("Agitado")]
    [SerializeField] Vector3 shakePositionAmplitude = new Vector3(0.095f, 0.018f, 0.025f);
    [SerializeField] Vector3 shakeRotationAmplitude = new Vector3(6f, 2.5f, 13f);

    [Header("Cambio de lata")]
    [SerializeField] Vector3 discardPosition = new Vector3(0.16f, -0.62f, 0.12f);
    [SerializeField] Vector3 discardRotation = new Vector3(28f, -36f, 72f);
    [SerializeField] Vector3 drawPosition = new Vector3(-0.2f, -0.58f, 0.14f);
    [SerializeField] Vector3 drawRotation = new Vector3(-18f, 24f, -38f);
    [SerializeField] float drawSettlingDistance = 0.035f;

    Quaternion baseRotation;
    CharacterController characterController;
    Vector3 currentPositionOffset;
    Vector3 positionVelocity;
    Vector3 currentRotationOffset;
    Vector3 rotationVelocity;
    Vector2 currentSway;
    float walkBlend;
    float walkCycle;
    float sprayBlend;
    float sprayBlendVelocity;
    float recoilTimer;
    bool spraying;
    bool shaking;
    bool changingCan;
    float shakeTravel;
    float canChangeProgress;
    float noiseSeed;

    void Awake()
    {
        if (idlePosition == Vector3.zero)
            idlePosition = transform.localPosition;

        baseRotation = transform.localRotation;
        characterController = GetComponentInParent<CharacterController>();
        noiseSeed = Random.Range(10f, 900f);
    }

    void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        Vector2 mouseDelta = new Vector2(
            Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -maximumMouseDelta, maximumMouseDelta),
            Mathf.Clamp(Input.GetAxisRaw("Mouse Y"), -maximumMouseDelta, maximumMouseDelta));
        UpdateMotion(deltaTime, Time.time, mouseDelta, GetHorizontalSpeed());
    }

    void UpdateMotion(float deltaTime, float motionTime, Vector2 mouseDelta, float horizontalSpeed)
    {
        float swaySmooth = 1f - Mathf.Exp(-swayLag * deltaTime);
        currentSway = Vector2.Lerp(currentSway, mouseDelta, swaySmooth);

        float targetWalkBlend = horizontalSpeed > 0.08f ? Mathf.Clamp01(horizontalSpeed / 3f) : 0f;
        walkBlend = Mathf.MoveTowards(walkBlend, targetWalkBlend, walkBlendSpeed * deltaTime);
        walkCycle += deltaTime * walkBobFrequency * Mathf.PI * 2f * Mathf.Lerp(0.72f, 1.2f, walkBlend);

        bool sprayActive = spraying && !changingCan;
        sprayBlend = Mathf.SmoothDamp(sprayBlend, sprayActive ? 1f : 0f, ref sprayBlendVelocity, sprayActive ? 0.07f : 0.14f, Mathf.Infinity, deltaTime);
        recoilTimer = Mathf.Max(0f, recoilTimer - deltaTime);

        Vector3 targetPosition = BuildPositionOffset(motionTime);
        Vector3 targetRotation = BuildRotationOffset(motionTime);
        float positionTime = 1f / Mathf.Max(0.01f, positionDamping);
        float rotationTime = 1f / Mathf.Max(0.01f, rotationDamping);
        currentPositionOffset = Vector3.SmoothDamp(currentPositionOffset, targetPosition, ref positionVelocity, positionTime, Mathf.Infinity, deltaTime);
        currentRotationOffset = Vector3.SmoothDamp(currentRotationOffset, targetRotation, ref rotationVelocity, rotationTime, Mathf.Infinity, deltaTime);

        transform.localPosition = idlePosition + currentPositionOffset;
        transform.localRotation = baseRotation * Quaternion.Euler(currentRotationOffset);
    }

    Vector3 BuildPositionOffset(float time)
    {
        Vector3 idle = new Vector3(
            SignedPerlin(time * idleNoiseFrequency, noiseSeed),
            SignedPerlin(time * idleNoiseFrequency * 0.83f, noiseSeed + 17f),
            SignedPerlin(time * idleNoiseFrequency * 1.13f, noiseSeed + 31f));
        idle = Vector3.Scale(idle, idlePositionAmplitude) * idlePresenceMultiplier;

        Vector3 slowIdle = new Vector3(
            SignedPerlin(time * slowIdleNoiseFrequency, noiseSeed + 401f),
            SignedPerlin(time * slowIdleNoiseFrequency * 0.79f, noiseSeed + 431f),
            SignedPerlin(time * slowIdleNoiseFrequency * 1.17f, noiseSeed + 463f));
        idle += Vector3.Scale(slowIdle, slowIdlePositionAmplitude);

        Vector3 sway = new Vector3(-currentSway.x * swayPositionAmount.x, -currentSway.y * swayPositionAmount.y, 0f);
        Vector3 bob = new Vector3(
            Mathf.Sin(walkCycle) * walkBobAmplitude.x,
            Mathf.Sin(walkCycle * 2f) * walkBobAmplitude.y,
            (Mathf.Cos(walkCycle * 2f) - 1f) * 0.5f * walkBobAmplitude.z) * walkBlend;

        Vector3 drift = new Vector3(
            SignedPerlin(time * wristDriftFrequency, noiseSeed + 53f),
            SignedPerlin(time * wristDriftFrequency * 0.77f, noiseSeed + 71f),
            SignedPerlin(time * wristDriftFrequency * 1.21f, noiseSeed + 89f));
        drift = Vector3.Scale(drift, wristDriftAmplitude) * sprayBlend;

        Vector3 vibration = Vector3.zero;
        if (sprayBlend > 0.001f)
        {
            vibration = new Vector3(
                SignedPerlin(time * sprayVibrationFrequency, noiseSeed + 101f),
                SignedPerlin(time * sprayVibrationFrequency * 1.17f, noiseSeed + 127f),
                SignedPerlin(time * sprayVibrationFrequency * 0.91f, noiseSeed + 149f));
            vibration = Vector3.Scale(vibration, sprayVibrationAmplitude) * sprayBlend;
        }

        float recoil = recoilTimer > 0f ? Mathf.Sin((recoilTimer / valveRecoilDuration) * Mathf.PI) * valveRecoilDistance : 0f;
        Vector3 shake = shaking ? Vector3.Scale(shakePositionAmplitude, new Vector3(shakeTravel, Mathf.Abs(shakeTravel), -Mathf.Abs(shakeTravel))) : Vector3.zero;
        Vector3 canChange = changingCan ? BuildCanChangePosition(canChangeProgress) : Vector3.zero;
        return idle + sway + bob + (sprayOffset * sprayBlend) + drift + vibration + Vector3.back * recoil + shake + canChange;
    }

    Vector3 BuildRotationOffset(float time)
    {
        Vector3 idle = new Vector3(
            SignedPerlin(time * idleNoiseFrequency * 0.91f, noiseSeed + 191f),
            SignedPerlin(time * idleNoiseFrequency * 1.08f, noiseSeed + 223f),
            SignedPerlin(time * idleNoiseFrequency * 0.74f, noiseSeed + 251f));
        idle = Vector3.Scale(idle, idleRotationAmplitude) * idlePresenceMultiplier;

        Vector3 slowIdle = new Vector3(
            SignedPerlin(time * slowIdleNoiseFrequency * 0.83f, noiseSeed + 491f),
            SignedPerlin(time * slowIdleNoiseFrequency * 1.09f, noiseSeed + 521f),
            SignedPerlin(time * slowIdleNoiseFrequency * 0.61f, noiseSeed + 557f));
        idle += Vector3.Scale(slowIdle, slowIdleRotationAmplitude);

        Vector3 sway = new Vector3(-currentSway.y * swayRotationAmount.x, currentSway.x * swayRotationAmount.y, -currentSway.x * 0.18f);
        Vector3 bob = new Vector3(Mathf.Sin(walkCycle * 2f), Mathf.Sin(walkCycle), Mathf.Cos(walkCycle)) * walkBlend;
        bob = Vector3.Scale(bob, walkBobRotation);

        Vector3 vibration = Vector3.zero;
        if (sprayBlend > 0.001f)
        {
            vibration = new Vector3(
                SignedPerlin(time * sprayVibrationFrequency * 0.94f, noiseSeed + 277f),
                SignedPerlin(time * sprayVibrationFrequency * 1.19f, noiseSeed + 311f),
                SignedPerlin(time * sprayVibrationFrequency * 1.37f, noiseSeed + 347f));
            vibration = Vector3.Scale(vibration, sprayVibrationRotation) * sprayBlend;
        }

        Vector3 shake = shaking ? Vector3.Scale(shakeRotationAmplitude, new Vector3(-shakeTravel, shakeTravel, -shakeTravel)) : Vector3.zero;
        Vector3 canChange = changingCan ? BuildCanChangeRotation(canChangeProgress) : Vector3.zero;
        return idle + sway + bob + vibration + shake + canChange;
    }

    Vector3 BuildCanChangePosition(float progress)
    {
        if (progress < 0.48f)
        {
            float t = Mathf.SmoothStep(0f, 1f, progress / 0.48f);
            return Vector3.LerpUnclamped(Vector3.zero, discardPosition, t);
        }

        float enter = Mathf.InverseLerp(0.48f, 1f, progress);
        float easeOut = 1f - Mathf.Pow(1f - enter, 3f);
        float settling = Mathf.Sin(enter * Mathf.PI * 3f) * (1f - enter) * drawSettlingDistance;
        return Vector3.LerpUnclamped(drawPosition, Vector3.zero, easeOut) + Vector3.up * settling;
    }

    Vector3 BuildCanChangeRotation(float progress)
    {
        if (progress < 0.48f)
        {
            float t = Mathf.SmoothStep(0f, 1f, progress / 0.48f);
            return Vector3.LerpUnclamped(Vector3.zero, discardRotation, t);
        }

        float enter = Mathf.InverseLerp(0.48f, 1f, progress);
        float easeOut = 1f - Mathf.Pow(1f - enter, 3f);
        float settling = Mathf.Sin(enter * Mathf.PI * 3f) * (1f - enter);
        return Vector3.LerpUnclamped(drawRotation, Vector3.zero, easeOut) + new Vector3(0f, 0f, settling * 4f);
    }

    float GetHorizontalSpeed()
    {
        if (characterController != null)
        {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        Vector2 movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return movement.magnitude * 3f;
    }

    static float SignedPerlin(float x, float y)
    {
        return Mathf.PerlinNoise(x, y) * 2f - 1f;
    }

    public void StartSpray()
    {
        if (shaking)
            return;

        spraying = true;
        recoilTimer = valveRecoilDuration;
    }

    public void StopSpray()
    {
        spraying = false;
    }

    public void SetShake(float travel, bool active)
    {
        shaking = active && !changingCan;
        shakeTravel = active ? Mathf.Clamp(travel, -1f, 1f) : 0f;
        if (active)
            spraying = false;
    }

    public void SetCanChange(float progress, bool active)
    {
        changingCan = active;
        canChangeProgress = active ? Mathf.Clamp01(progress) : 0f;
        if (!active)
            return;

        spraying = false;
        shaking = false;
        shakeTravel = 0f;
    }
}
