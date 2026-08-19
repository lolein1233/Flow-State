using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Legacy")]
    public float speed = 5f;
    public float gravity = -18f;
    public float mouseSensitivity = 200f;
    public Transform cameraHolder;
    public float minY = -80f;
    public float maxY = 80f;
    public float focusMinY = -40f;
    public float focusMaxY = 40f;
    public bool canMove = true;
    public bool focusMode = false;

    [Header("Modo graffiti")]
    public bool startInGraffitiMode = false;
    public KeyCode graffitiModeKey = KeyCode.E;
    public KeyCode exitGraffitiKey = KeyCode.Escape;
    public GraffitiRaycastUI graffitiRaycast;
    public bool requirePaintableToEnterGraffiti = true;
    public bool allowMovementInGraffitiMode = true;
    public float graffitiMoveSpeed = 2.25f;

    [Header("Movimiento tercera persona")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 8f;
    public float acceleration = 16f;
    public float airControl = 0.45f;
    public float rotationSmoothTime = 0.08f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpHeight = 1.35f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Camara")]
    public Camera cameraOverride;
    public Vector3 cameraTargetOffset = new Vector3(0f, 1.45f, 0f);
    public Vector3 thirdPersonOffset = new Vector3(0f, 0.35f, -4.25f);
    public Vector3 firstPersonOffset = new Vector3(0f, 1.55f, 0.1f);
    public float thirdPersonMinY = -35f;
    public float thirdPersonMaxY = 65f;
    public float cameraModeBlendSpeed = 12f;
    public float cameraCollisionRadius = 0.22f;
    public float cameraCollisionPadding = 0.15f;
    public LayerMask cameraCollisionMask = ~0;

    [Header("Camara tercera persona avanzada")]
    public Vector3 thirdPersonShoulderOffset = new Vector3(0.48f, 0.08f, 0f);
    public float thirdPersonTargetSmoothTime = 0.075f;
    public float thirdPersonPositionSmoothTime = 0.055f;
    public float thirdPersonYawSmoothTime = 0.055f;
    public float thirdPersonPitchSmoothTime = 0.045f;
    public float thirdPersonRotationSharpness = 18f;
    public float thirdPersonLookAheadDistance = 0.65f;
    public float thirdPersonLookAheadSmoothTime = 0.16f;
    public float thirdPersonMinCameraDistance = 0.28f;
    public float cameraCollisionSnapSmoothTime = 0.025f;
    public float cameraCollisionReturnSmoothTime = 0.18f;
    public float thirdPersonFov = 62f;
    public float sprintFov = 68f;
    public float graffitiFov = 60f;
    public float cameraFovSmoothTime = 0.14f;

    [Header("Combate / Lock-on")]
    public float combatLockLookSharpness = 11f;
    public float combatLockPlayerTurnSharpness = 13f;
    public float combatLockCameraFraming = 0.36f;
    public float combatLockPitchBias = 8f;

    [Header("Parkour")]
    public LayerMask parkourMask = ~0;
    public float wallCheckDistance = 1.15f;
    public float wallCheckHeight = 1.05f;
    public float vaultMaxHeight = 1.15f;
    public float climbMaxHeight = 2.6f;
    public float vaultForwardDistance = 1.35f;
    public float climbForwardOffset = 0.65f;
    public float vaultDuration = 0.42f;
    public float climbDuration = 0.72f;
    public float vaultArcHeight = 0.55f;
    public float climbArcHeight = 0.25f;
    public float landingProbeHeight = 2.4f;

    [Header("Subida al borde")]
    public float wallTopOutDuration = 0.58f;
    public float wallTopOutArcHeight = 0.18f;
    public float wallTopOutForwardOffset = 0.85f;

    [Header("Escalada continua")]
    public bool enableClimbing = true;
    public bool requireClimbableMarker = false;
    public KeyCode dropClimbKey = KeyCode.LeftControl;
    public float climbDetectDistance = 1.15f;
    public float climbMoveSpeed = 2.15f;
    public float climbSideSpeed = 1.1f;
    public float climbSurfaceOffset = 0.45f;
    public float climbSnapSpeed = 12f;
    public float climbTopProbeHeight = 1.8f;
    public float climbTopForwardOffset = 0.75f;
    public float climbAutoTopOutInput = 0.65f;
    public float climbMaxSurfaceAngle = 0.35f;

    [Header("Visual y animacion")]
    public Transform visualRoot;
    public Animator animator;
    public bool hideVisualInGraffitiMode = true;

    [Header("Apoyo al suelo")]
    public bool alignFeetToGroundOnStart = true;
    public float controllerSkinWidthOnGround = 0.03f;
    public float footGroundPadding = 0.015f;
    public float footGroundProbeHeight = 5f;
    public float footGroundProbeDistance = 20f;
    public float maxFootGroundSnap = 0.35f;

    CharacterController controller;
    Camera playerCamera;
    Transform cameraTransform;
    readonly HashSet<int> animatorParameters = new HashSet<int>();

    float pitch;
    float orbitYaw;
    float cameraYaw;
    float cameraPitch;
    float cameraYawVelocity;
    float cameraPitchVelocity;
    float cameraDistance = -1f;
    float cameraDistanceVelocity;
    float cameraFovVelocity;
    float externalThirdPersonFovOffset;
    float musicPlayerFovOffset;
    float combatLockFovOffset;
    float turnVelocity;
    float verticalVelocity;
    float currentPlanarSpeed;
    float lastGroundedTime = -999f;
    float jumpPressedTime = -999f;
    int groundAlignmentFramesRemaining;
    Vector3 previousCameraPlayerPosition;
    Vector3 smoothedCameraTarget;
    Vector3 cameraTargetVelocity;
    Vector3 cameraPositionVelocity;
    Vector3 cameraLookAhead;
    Vector3 cameraLookAheadVelocity;
    bool cameraStateInitialized;
    bool graffitiMode;
    bool isParkouring;
    bool isClimbing;
    bool climbAllowSideways = true;
    float climbSpeedMultiplier = 1f;
    float climbInput;
    Vector3 climbNormal;
    bool hasCombatLockPoint;
    Vector3 combatLockPoint;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    static readonly int GraffitiModeHash = Animator.StringToHash("GraffitiMode");
    static readonly int ParkourHash = Animator.StringToHash("Parkour");
    static readonly int ClimbingHash = Animator.StringToHash("Climbing");
    static readonly int ClimbInputHash = Animator.StringToHash("ClimbInput");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int VaultHash = Animator.StringToHash("Vault");
    static readonly int ClimbHash = Animator.StringToHash("Climb");
    static readonly int HangHash = Animator.StringToHash("Hang");
    static readonly int PaintHash = Animator.StringToHash("Paint");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        ResolveReferences();
        CacheAnimatorParameters();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        orbitYaw = transform.eulerAngles.y;
        cameraYaw = orbitYaw;
        cameraPitch = pitch;
        previousCameraPlayerPosition = transform.position;
        graffitiMode = startInGraffitiMode;
        jumpPressedTime = -999f;
        lastGroundedTime = -999f;
        ConfigureGroundContact();
        ApplyModeInstant();
        ResetMovementAnimator();
        groundAlignmentFramesRemaining = alignFeetToGroundOnStart ? 12 : 0;
        UpdateCursorState();
    }

    void Update()
    {
        ResolveReferences();
        HandleModeInput();
        UpdateCursorState();
        HandleLook();

        if (isClimbing)
            HandleClimbing();
        else if (!isParkouring)
            HandleMovement();

        UpdateAnimator();
        UpdateVisualState();
    }

    void LateUpdate()
    {
        UpdateCamera();
        UpdateInitialGroundAlignment();
    }

    void ResolveReferences()
    {
        if (cameraHolder == null && Camera.main != null)
            cameraHolder = Camera.main.transform;

        if (cameraOverride != null)
            playerCamera = cameraOverride;
        else if (cameraHolder != null)
            playerCamera = cameraHolder.GetComponent<Camera>();
        else
            playerCamera = Camera.main;

        if (playerCamera != null)
            cameraTransform = playerCamera.transform;
        else
            cameraTransform = cameraHolder;

        if (graffitiRaycast == null)
            graffitiRaycast = GetComponent<GraffitiRaycastUI>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void CacheAnimatorParameters()
    {
        animatorParameters.Clear();

        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            animatorParameters.Add(parameter.nameHash);
    }

    void ResetMovementAnimator()
    {
        if (animator == null)
            return;

        ResetAnimatorTrigger(JumpHash);
        ResetAnimatorTrigger(VaultHash);
        ResetAnimatorTrigger(ClimbHash);
        ResetAnimatorTrigger(HangHash);
        SetAnimatorBool(ParkourHash, false);
        SetAnimatorBool(ClimbingHash, false);
        SetAnimatorFloat(ClimbInputHash, 0f);
        SetAnimatorFloat(SpeedHash, 0f);
        SetAnimatorFloat(VerticalSpeedHash, 0f);

        if (animator.isActiveAndEnabled)
        {
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }
    }

    void ConfigureGroundContact()
    {
        if (controller == null || controllerSkinWidthOnGround <= 0f)
            return;

        controller.skinWidth = Mathf.Min(controller.skinWidth, controllerSkinWidthOnGround);
    }

    void UpdateInitialGroundAlignment()
    {
        if (groundAlignmentFramesRemaining <= 0)
            return;

        groundAlignmentFramesRemaining--;

        if (controller != null && !controller.isGrounded)
            return;

        if (AlignVisualFeetToGround())
            groundAlignmentFramesRemaining = 0;
    }

    bool AlignVisualFeetToGround()
    {
        if (visualRoot == null)
            return false;

        float groundY;
        float footY;

        if (!TryFindGroundBelowPlayer(out groundY) || !TryFindLowestFootY(out footY))
            return false;

        float targetFootY = groundY + footGroundPadding;
        float deltaY = Mathf.Clamp(targetFootY - footY, -maxFootGroundSnap, maxFootGroundSnap);

        if (Mathf.Abs(deltaY) < 0.001f)
            return true;

        visualRoot.position += Vector3.up * deltaY;
        return true;
    }

    bool TryFindGroundBelowPlayer(out float groundY)
    {
        groundY = 0f;

        Vector3 origin = transform.position + Vector3.up * footGroundProbeHeight;
        float distance = footGroundProbeHeight + footGroundProbeDistance;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        Collider[] ownColliders = GetComponentsInChildren<Collider>(true);

        foreach (RaycastHit hit in hits)
        {
            if (hit.normal.y < 0.35f || IsOwnCollider(hit.collider, ownColliders))
                continue;

            groundY = hit.point.y;
            return true;
        }

        return false;
    }

    bool TryFindLowestFootY(out float footY)
    {
        footY = float.PositiveInfinity;
        Transform root = animator != null ? animator.transform : visualRoot;
        bool foundFootBone = false;

        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
        {
            string boneName = bone.name.ToLowerInvariant();

            if (!boneName.Contains("foot") && !boneName.Contains("toe"))
                continue;

            footY = Mathf.Min(footY, bone.position.y);
            foundFootBone = true;
        }

        if (foundFootBone)
            return true;

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer visualRenderer in renderers)
            footY = Mathf.Min(footY, visualRenderer.bounds.min.y);

        return renderers.Length > 0;
    }

    bool IsOwnCollider(Collider candidate, Collider[] ownColliders)
    {
        foreach (Collider ownCollider in ownColliders)
        {
            if (candidate == ownCollider)
                return true;
        }

        return false;
    }

    void HandleModeInput()
    {
        if (Input.GetKeyDown(exitGraffitiKey) && graffitiMode)
        {
            ExitGraffitiMode();
            return;
        }

        if (isClimbing && (Input.GetKeyDown(dropClimbKey) || Input.GetKeyDown(exitGraffitiKey)))
        {
            ExitClimb(true);
            return;
        }

        if (!isClimbing && Input.GetKeyDown(graffitiModeKey) && !graffitiMode && CanEnterGraffitiMode())
            EnterGraffitiMode();
    }

    bool CanEnterGraffitiMode()
    {
        if (!requirePaintableToEnterGraffiti)
            return true;

        return graffitiRaycast == null || graffitiRaycast.CanPaint();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (graffitiMode)
        {
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, focusMode ? focusMinY : minY, focusMode ? focusMaxY : maxY);

        orbitYaw += mouseX;
        transform.rotation = Quaternion.Euler(0f, orbitYaw, 0f);
        return;
    }

        if (hasCombatLockPoint)
        {
            UpdateCombatLockLook();
            return;
        }

        orbitYaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, thirdPersonMinY, thirdPersonMaxY);
    }

    void UpdateCombatLockLook()
    {
        Vector3 origin = transform.position + cameraTargetOffset * 0.75f;
        Vector3 toTarget = combatLockPoint - origin;
        Vector3 planarToTarget = toTarget;
        planarToTarget.y = 0f;

        if (planarToTarget.sqrMagnitude <= 0.0001f)
            return;

        float desiredYaw = Mathf.Atan2(planarToTarget.x, planarToTarget.z) * Mathf.Rad2Deg;
        float planarDistance = Mathf.Max(0.1f, planarToTarget.magnitude);
        float desiredPitch = Mathf.Atan2(toTarget.y, planarDistance) * Mathf.Rad2Deg + combatLockPitchBias;
        desiredPitch = Mathf.Clamp(desiredPitch, thirdPersonMinY, thirdPersonMaxY);

        float blend = 1f - Mathf.Exp(-combatLockLookSharpness * Time.deltaTime);
        orbitYaw = Mathf.LerpAngle(orbitYaw, desiredYaw, blend);
        pitch = Mathf.Lerp(pitch, desiredPitch, blend);
    }

    void HandleMovement()
    {
        bool grounded = controller.isGrounded;

        if (grounded)
        {
            lastGroundedTime = Time.time;

            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 rawInput = Vector2.ClampMagnitude(new Vector2(h, v), 1f);

        if (Input.GetKeyDown(jumpKey))
        {
            Vector3 forward = GetPlanarCameraForward();

            if (!graffitiMode && TryStartParkour(rawInput.sqrMagnitude > 0.01f ? GetThirdPersonMoveDirection(rawInput) : forward))
                return;

            jumpPressedTime = Time.time;
        }

        Vector3 moveDirection = graffitiMode ? GetFirstPersonMoveDirection(rawInput) : GetThirdPersonMoveDirection(rawInput);
        bool canApplyInput = canMove && (!graffitiMode || allowMovementInGraffitiMode);
        float targetSpeed = 0f;

        if (canApplyInput && rawInput.sqrMagnitude > 0.01f)
        {
            targetSpeed = graffitiMode ? graffitiMoveSpeed : (Input.GetKey(sprintKey) ? sprintSpeed : Mathf.Max(speed, walkSpeed));

            if (!grounded)
                targetSpeed *= airControl;

            if (!graffitiMode && !hasCombatLockPoint)
                RotateToward(moveDirection);
        }

        if (!graffitiMode && hasCombatLockPoint)
            RotateTowardCombatLockPoint();

        currentPlanarSpeed = Mathf.MoveTowards(currentPlanarSpeed, targetSpeed, acceleration * Time.deltaTime);
        Vector3 planarVelocity = canApplyInput ? moveDirection * currentPlanarSpeed : Vector3.zero;

        if (Time.time - jumpPressedTime <= jumpBufferTime && Time.time - lastGroundedTime <= coyoteTime && canApplyInput && !graffitiMode)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressedTime = -999f;
            TriggerAnimator(JumpHash);
        }

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    Vector3 GetThirdPersonMoveDirection(Vector2 rawInput)
    {
        if (rawInput.sqrMagnitude <= 0.01f)
            return Vector3.zero;

        Vector3 forward = GetPlanarCameraForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 direction = forward * rawInput.y + right * rawInput.x;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    Vector3 GetFirstPersonMoveDirection(Vector2 rawInput)
    {
        if (rawInput.sqrMagnitude <= 0.01f)
            return Vector3.zero;

        Vector3 direction = transform.forward * rawInput.y + transform.right * rawInput.x;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    Vector3 GetPlanarCameraForward()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, orbitYaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
    }

    void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }

    void RotateTowardCombatLockPoint()
    {
        Vector3 direction = combatLockPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        float blend = 1f - Mathf.Exp(-combatLockPlayerTurnSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
    }

    bool TryStartParkour(Vector3 desiredForward)
    {
        if (!canMove || isParkouring)
            return false;

        desiredForward.y = 0f;

        if (desiredForward.sqrMagnitude <= 0.001f)
            desiredForward = transform.forward;

        desiredForward.Normalize();

        Vector3 origin = transform.position + Vector3.up * wallCheckHeight;

        if (!TryFindParkourWall(origin, desiredForward, out RaycastHit wallHit))
            return false;

        Bounds bounds = wallHit.collider.bounds;
        float obstacleHeight = bounds.max.y - transform.position.y;

        if (obstacleHeight <= vaultMaxHeight)
        {
            Vector3 landing = wallHit.point + desiredForward * vaultForwardDistance;

            if (TryFindGround(landing, out Vector3 groundPoint))
                landing = groundPoint;
            else
                landing.y = transform.position.y;

            StartCoroutine(ParkourMove(landing, wallTopOutDuration, wallTopOutArcHeight, VaultHash));
            return true;
        }

        if (obstacleHeight <= climbMaxHeight)
        {
            Vector3 topProbe = wallHit.point + desiredForward * wallTopOutForwardOffset;
            topProbe.y = bounds.max.y + 0.1f;

            if (TryFindGround(topProbe, out Vector3 climbPoint))
                topProbe = climbPoint;

            StartCoroutine(ParkourMove(topProbe, wallTopOutDuration, wallTopOutArcHeight, ClimbHash));
            return true;
        }

        if (TryEnterClimb(wallHit))
            return true;

        TriggerAnimator(HangHash);
        return false;
    }

    bool TryEnterClimb(RaycastHit wallHit)
    {
        if (!enableClimbing || graffitiMode || isClimbing || wallHit.collider == null)
            return false;

        if (Mathf.Abs(Vector3.Dot(wallHit.normal, Vector3.up)) > climbMaxSurfaceAngle)
            return false;

        ClimbableSurface climbable = wallHit.collider.GetComponentInParent<ClimbableSurface>();
        if (requireClimbableMarker && (climbable == null || !climbable.canClimb))
            return false;

        if (climbable != null && !climbable.canClimb)
            return false;

        isClimbing = true;
        isParkouring = false;
        climbNormal = wallHit.normal;
        climbAllowSideways = climbable == null || climbable.allowSideways;
        climbSpeedMultiplier = climbable != null ? Mathf.Max(0.1f, climbable.speedMultiplier) : 1f;
        verticalVelocity = 0f;
        currentPlanarSpeed = 0f;
        climbInput = 0f;

        transform.rotation = Quaternion.LookRotation(-climbNormal, Vector3.up);
        SnapToClimbSurface(wallHit.point);
        SetAnimatorBool(ClimbingHash, true);
        SetAnimatorFloat(ClimbInputHash, 0f);
        TriggerAnimator(HangHash);
        return true;
    }

    bool TryFindParkourWall(Vector3 origin, Vector3 desiredForward, out RaycastHit bestHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, desiredForward, wallCheckDistance, parkourMask, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        bestHit = default;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsSelf(hit.collider))
                continue;

            if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) > 0.35f)
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
            }
        }

        return bestDistance < float.MaxValue;
    }

    bool TryFindGround(Vector3 nearPoint, out Vector3 groundPoint)
    {
        Vector3 origin = nearPoint + Vector3.up * landingProbeHeight;
        float probeDistance = landingProbeHeight + climbMaxHeight + 0.5f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, probeDistance, parkourMask, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        groundPoint = nearPoint;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (IsSelf(hit.collider))
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                groundPoint = hit.point + Vector3.up * 0.05f;
                found = true;
            }
        }

        return found;
    }

    void HandleClimbing()
    {
        if (graffitiMode || !enableClimbing)
        {
            ExitClimb(false);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        climbInput = Mathf.Clamp(v, -1f, 1f);

        if (Input.GetKeyDown(jumpKey))
        {
            if (TryFindClimbTop(out Vector3 topOutPoint))
            {
                ExitClimb(false);
                StartCoroutine(ParkourMove(topOutPoint, wallTopOutDuration, wallTopOutArcHeight, ClimbHash));
                return;
            }

            ExitClimb(true);
            return;
        }

        if (climbInput > climbAutoTopOutInput && TryFindClimbTop(out Vector3 autoTopOutPoint))
        {
            ExitClimb(false);
            StartCoroutine(ParkourMove(autoTopOutPoint, wallTopOutDuration, wallTopOutArcHeight, ClimbHash));
            return;
        }

        Vector3 climbSide = Vector3.Cross(Vector3.up, climbNormal);
        if (climbSide.sqrMagnitude > 0.001f)
            climbSide.Normalize();

        float sideInput = climbAllowSideways ? h : 0f;
        Vector3 climbVelocity =
            Vector3.up * (climbInput * climbMoveSpeed * climbSpeedMultiplier) +
            climbSide * (sideInput * climbSideSpeed * climbSpeedMultiplier);

        controller.Move(climbVelocity * Time.deltaTime);

        if (!TryFindClimbSurface(transform.position + Vector3.up * wallCheckHeight, transform.forward, out RaycastHit surfaceHit, out ClimbableSurface climbable))
        {
            ExitClimb(false);
            return;
        }

        climbNormal = Vector3.Slerp(climbNormal, surfaceHit.normal, 1f - Mathf.Exp(-climbSnapSpeed * Time.deltaTime));
        climbAllowSideways = climbable == null || climbable.allowSideways;
        climbSpeedMultiplier = climbable != null ? Mathf.Max(0.1f, climbable.speedMultiplier) : 1f;

        transform.rotation = Quaternion.LookRotation(-climbNormal, Vector3.up);
        SnapToClimbSurface(surfaceHit.point, false);

        SetAnimatorBool(ClimbingHash, true);
        SetAnimatorFloat(ClimbInputHash, climbInput);
        SetAnimatorFloat(SpeedHash, Mathf.Abs(climbInput));
    }

    bool TryFindClimbSurface(Vector3 origin, Vector3 direction, out RaycastHit bestHit, out ClimbableSurface climbable)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, climbSurfaceOffset + climbDetectDistance, parkourMask, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        bestHit = default;
        climbable = null;

        foreach (RaycastHit hit in hits)
        {
            if (!IsValidClimbHit(hit, out ClimbableSurface candidate))
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                climbable = candidate;
            }
        }

        return bestDistance < float.MaxValue;
    }

    bool IsValidClimbHit(RaycastHit hit, out ClimbableSurface climbable)
    {
        climbable = null;

        if (hit.collider == null || IsSelf(hit.collider))
            return false;

        if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) > climbMaxSurfaceAngle)
            return false;

        climbable = hit.collider.GetComponentInParent<ClimbableSurface>();
        if (requireClimbableMarker && (climbable == null || !climbable.canClimb))
            return false;

        return climbable == null || climbable.canClimb;
    }

    void SnapToClimbSurface(Vector3 wallPoint, bool instant = true)
    {
        Vector3 target = wallPoint + climbNormal * climbSurfaceOffset;
        target.y = transform.position.y;
        Vector3 delta = target - transform.position;
        delta.y = 0f;

        if (!instant)
            delta *= 1f - Mathf.Exp(-climbSnapSpeed * Time.deltaTime);

        controller.Move(delta);
    }

    bool TryFindClimbTop(out Vector3 topOutPoint)
    {
        topOutPoint = transform.position;
        Vector3 highProbe = transform.position + Vector3.up * climbTopProbeHeight;

        if (TryFindClimbSurface(highProbe, -climbNormal, out _, out _))
            return false;

        Vector3 groundProbe = transform.position - climbNormal * climbTopForwardOffset + Vector3.up * (climbTopProbeHeight + 0.25f);
        if (!TryFindGround(groundProbe, out Vector3 groundPoint))
            return false;

        if (groundPoint.y < transform.position.y + 0.25f)
            return false;

        topOutPoint = groundPoint;
        return true;
    }

    void ExitClimb(bool pushAway)
    {
        if (!isClimbing)
            return;

        isClimbing = false;
        climbInput = 0f;
        verticalVelocity = -2f;
        currentPlanarSpeed = 0f;
        SetAnimatorBool(ClimbingHash, false);
        SetAnimatorFloat(ClimbInputHash, 0f);

        if (pushAway)
            controller.Move(climbNormal * 0.25f);
    }

    IEnumerator ParkourMove(Vector3 endPosition, float duration, float arcHeight, int triggerHash)
    {
        isParkouring = true;
        SetAnimatorBool(ParkourHash, true);
        TriggerAnimator(triggerHash);

        bool controllerWasEnabled = controller.enabled;
        controller.enabled = false;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 travel = endPosition - startPosition;
        travel.y = 0f;
        Quaternion endRotation = travel.sqrMagnitude > 0.01f ? Quaternion.LookRotation(travel.normalized, Vector3.up) : startRotation;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
            float eased = t * t * (3f - 2f * t);
            Vector3 arc = Vector3.up * Mathf.Sin(eased * Mathf.PI) * arcHeight;

            transform.position = Vector3.Lerp(startPosition, endPosition, eased) + arc;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, eased);

            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;

        controller.enabled = controllerWasEnabled;
        verticalVelocity = -2f;
        currentPlanarSpeed = 0f;
        isParkouring = false;
        SetAnimatorBool(ParkourHash, false);
    }

    bool IsSelf(Collider other)
    {
        return other != null && other.transform.IsChildOf(transform);
    }

    void UpdateCamera()
    {
        if (cameraTransform == null)
            return;

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

        if (!cameraStateInitialized)
            InitializeCameraState();

        if (graffitiMode)
        {
            UpdateGraffitiCamera(deltaTime);
            return;
        }

        UpdateThirdPersonCamera(deltaTime);
    }

    void UpdateGraffitiCamera(float deltaTime)
    {
        Vector3 desiredPosition = transform.TransformPoint(firstPersonOffset);
        Quaternion desiredRotation = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
        float blend = 1f - Mathf.Exp(-cameraModeBlendSpeed * deltaTime);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, blend);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, blend);
        UpdateCameraFov(graffitiFov, deltaTime);

        cameraYaw = orbitYaw;
        cameraPitch = pitch;
        cameraDistance = -1f;
        smoothedCameraTarget = transform.position + cameraTargetOffset;
        cameraLookAhead = Vector3.zero;
        previousCameraPlayerPosition = transform.position;
    }

    void UpdateThirdPersonCamera(float deltaTime)
    {
        Vector3 target = GetDesiredThirdPersonTarget(deltaTime);

        cameraYaw = Mathf.SmoothDampAngle(cameraYaw, orbitYaw, ref cameraYawVelocity, thirdPersonYawSmoothTime, Mathf.Infinity, deltaTime);
        cameraPitch = Mathf.SmoothDamp(cameraPitch, pitch, ref cameraPitchVelocity, thirdPersonPitchSmoothTime, Mathf.Infinity, deltaTime);

        Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 offset = thirdPersonOffset + thirdPersonShoulderOffset;
        Vector3 desiredPosition = target + orbit * offset;
        Vector3 resolvedPosition = ResolveCameraCollision(target, desiredPosition, deltaTime);
        Vector3 lookDirection = target - resolvedPosition;

        if (lookDirection.sqrMagnitude < 0.0001f)
            lookDirection = orbit * Vector3.forward;

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        float rotationBlend = 1f - Mathf.Exp(-thirdPersonRotationSharpness * deltaTime);

        cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, resolvedPosition, ref cameraPositionVelocity, thirdPersonPositionSmoothTime, Mathf.Infinity, deltaTime);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, rotationBlend);
        UpdateCameraFov(GetTargetThirdPersonFov(), deltaTime);
    }

    Vector3 GetDesiredThirdPersonTarget(float deltaTime)
    {
        Vector3 playerDelta = transform.position - previousCameraPlayerPosition;
        previousCameraPlayerPosition = transform.position;
        playerDelta.y = 0f;

        Vector3 desiredLookAhead = Vector3.zero;
        float playerSpeed = playerDelta.magnitude / deltaTime;

        if (playerSpeed > 0.05f)
        {
            float speedFactor = Mathf.InverseLerp(walkSpeed, sprintSpeed, playerSpeed);
            desiredLookAhead = playerDelta.normalized * thirdPersonLookAheadDistance * speedFactor;
        }

        cameraLookAhead = Vector3.SmoothDamp(cameraLookAhead, desiredLookAhead, ref cameraLookAheadVelocity, thirdPersonLookAheadSmoothTime, Mathf.Infinity, deltaTime);

        Vector3 desiredTarget = transform.position + cameraTargetOffset + cameraLookAhead;

        if (hasCombatLockPoint)
            desiredTarget = GetCombatCameraTarget(desiredTarget);

        smoothedCameraTarget = Vector3.SmoothDamp(smoothedCameraTarget, desiredTarget, ref cameraTargetVelocity, thirdPersonTargetSmoothTime, Mathf.Infinity, deltaTime);
        return smoothedCameraTarget;
    }

    Vector3 GetCombatCameraTarget(Vector3 playerTarget)
    {
        float framing = Mathf.Clamp01(combatLockCameraFraming);
        Vector3 target = Vector3.Lerp(playerTarget, combatLockPoint, framing);
        target.y = Mathf.Lerp(playerTarget.y, combatLockPoint.y, framing * 0.65f);
        return target;
    }

    float GetTargetThirdPersonFov()
    {
        float sprintAmount = Mathf.InverseLerp(walkSpeed, sprintSpeed, currentPlanarSpeed);
        float totalOffset = externalThirdPersonFovOffset + musicPlayerFovOffset + combatLockFovOffset;
        return Mathf.Clamp(Mathf.Lerp(thirdPersonFov, sprintFov, sprintAmount) + totalOffset, 35f, 95f);
    }

    void UpdateCameraFov(float targetFov, float deltaTime)
    {
        if (playerCamera == null)
            return;

        playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, targetFov, ref cameraFovVelocity, cameraFovSmoothTime, Mathf.Infinity, deltaTime);
    }

    Vector3 ResolveCameraCollision(Vector3 target, Vector3 desiredPosition, float deltaTime)
    {
        Vector3 direction = desiredPosition - target;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return desiredPosition;

        direction /= distance;
        RaycastHit[] hits = Physics.SphereCastAll(target, cameraCollisionRadius, direction, distance, cameraCollisionMask, QueryTriggerInteraction.Ignore);
        float bestDistance = distance;

        foreach (RaycastHit hit in hits)
        {
            if (IsSelf(hit.collider))
                continue;

            bestDistance = Mathf.Min(bestDistance, Mathf.Max(0.1f, hit.distance - cameraCollisionPadding));
        }

        bestDistance = Mathf.Max(thirdPersonMinCameraDistance, bestDistance);

        if (cameraDistance < 0f)
            cameraDistance = bestDistance;

        float smoothTime = bestDistance < cameraDistance ? cameraCollisionSnapSmoothTime : cameraCollisionReturnSmoothTime;
        cameraDistance = Mathf.SmoothDamp(cameraDistance, bestDistance, ref cameraDistanceVelocity, smoothTime, Mathf.Infinity, deltaTime);
        cameraDistance = Mathf.Clamp(cameraDistance, thirdPersonMinCameraDistance, distance);

        return target + direction * cameraDistance;
    }

    void ApplyModeInstant()
    {
        if (cameraTransform == null)
            return;

        InitializeCameraState();

        if (graffitiMode)
        {
            cameraTransform.position = transform.TransformPoint(firstPersonOffset);
            cameraTransform.rotation = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);

            if (playerCamera != null)
                playerCamera.fieldOfView = graffitiFov;
        }
        else
        {
            Vector3 target = transform.position + cameraTargetOffset;
            Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 offset = thirdPersonOffset + thirdPersonShoulderOffset;

            cameraTransform.position = target + orbit * offset;
            cameraTransform.rotation = Quaternion.LookRotation(target - cameraTransform.position, Vector3.up);
            cameraDistance = Vector3.Distance(target, cameraTransform.position);

            if (playerCamera != null)
                playerCamera.fieldOfView = thirdPersonFov;
        }

        ResetCameraVelocities();
    }

    void InitializeCameraState()
    {
        cameraYaw = orbitYaw;
        cameraPitch = pitch;
        previousCameraPlayerPosition = transform.position;
        cameraLookAhead = Vector3.zero;
        smoothedCameraTarget = transform.position + cameraTargetOffset;
        cameraDistance = -1f;
        cameraStateInitialized = true;
    }

    void ResetCameraVelocities()
    {
        cameraYawVelocity = 0f;
        cameraPitchVelocity = 0f;
        cameraDistanceVelocity = 0f;
        cameraFovVelocity = 0f;
        cameraTargetVelocity = Vector3.zero;
        cameraPositionVelocity = Vector3.zero;
        cameraLookAheadVelocity = Vector3.zero;
    }

    void UpdateAnimator()
    {
        if (animator == null)
            return;

        SetAnimatorFloat(SpeedHash, currentPlanarSpeed);
        SetAnimatorFloat(VerticalSpeedHash, verticalVelocity);
        SetAnimatorBool(GroundedHash, controller.isGrounded);
        SetAnimatorBool(GraffitiModeHash, graffitiMode);
        SetAnimatorBool(ParkourHash, isParkouring);
        SetAnimatorBool(ClimbingHash, isClimbing);
        SetAnimatorFloat(ClimbInputHash, climbInput);
    }

    void UpdateVisualState()
    {
        if (visualRoot == null || !hideVisualInGraffitiMode)
            return;

        bool shouldBeActive = !graffitiMode;

        if (visualRoot.gameObject.activeSelf != shouldBeActive)
            visualRoot.gameObject.SetActive(shouldBeActive);
    }

    void UpdateCursorState()
    {
        Cursor.lockState = graffitiMode ? CursorLockMode.Locked : CursorLockMode.Confined;
        Cursor.visible = graffitiMode;
    }

    void SetAnimatorFloat(int hash, float value)
    {
        if (animatorParameters.Contains(hash))
            animator.SetFloat(hash, value);
    }

    void SetAnimatorBool(int hash, bool value)
    {
        if (animatorParameters.Contains(hash))
            animator.SetBool(hash, value);
    }

    void TriggerAnimator(int hash)
    {
        if (animatorParameters.Contains(hash))
            animator.SetTrigger(hash);
    }

    public void SetThirdPersonFovOffset(float offset)
    {
        externalThirdPersonFovOffset = offset;
    }

    public void SetMusicPlayerFovOffset(float offset)
    {
        musicPlayerFovOffset = offset;
    }

    public void SetCombatLockFovOffset(float offset)
    {
        combatLockFovOffset = offset;
    }

    public void SetCombatLockPoint(Vector3 worldPoint)
    {
        hasCombatLockPoint = true;
        combatLockPoint = worldPoint;
    }

    public void ClearCombatLockPoint()
    {
        hasCombatLockPoint = false;
    }

    public bool HasCombatLockPoint()
    {
        return hasCombatLockPoint;
    }

    void ResetAnimatorTrigger(int hash)
    {
        if (animatorParameters.Contains(hash))
            animator.ResetTrigger(hash);
    }

    public void EnterGraffitiMode()
    {
        if (isClimbing)
            ExitClimb(false);

        graffitiMode = true;
        orbitYaw = transform.eulerAngles.y;
        cameraYaw = orbitYaw;
        cameraPitch = pitch;
        cameraDistance = -1f;
        ResetCameraVelocities();
        currentPlanarSpeed = 0f;
    }

    public void ExitGraffitiMode()
    {
        graffitiMode = false;
        focusMode = false;
        canMove = true;
        orbitYaw = transform.eulerAngles.y;
        cameraYaw = orbitYaw;
        cameraPitch = pitch;
        cameraDistance = -1f;
        smoothedCameraTarget = transform.position + cameraTargetOffset;
        previousCameraPlayerPosition = transform.position;
        ResetCameraVelocities();
    }

    public void SetGraffitiMode(bool value)
    {
        if (value)
            EnterGraffitiMode();
        else
            ExitGraffitiMode();
    }

    public bool IsGraffitiMode()
    {
        return graffitiMode;
    }

    public bool IsInFirstPerson()
    {
        return graffitiMode;
    }

    public bool IsThirdPersonMode()
    {
        return !graffitiMode;
    }

    public bool IsClimbing()
    {
        return isClimbing;
    }

    public bool IsParkouring()
    {
        return isParkouring;
    }

    public void SetPaintingAnimatorState(bool value)
    {
        SetAnimatorBool(PaintHash, value);
    }
}
