using UnityEngine;

[DisallowMultipleComponent]
public class CombatLockOnController : MonoBehaviour
{
    [Header("Referencias")]
    public FPSController fps;
    public Camera playerCamera;
    public DiegeticMusicPlayer musicPlayer;

    [Header("Input")]
    public int lockMouseButton = 1;
    public bool holdToLock = false;

    [Header("Busqueda")]
    public float maxLockDistance = 18f;
    public float keepLockDistance = 24f;
    [Range(5f, 120f)] public float maxLockAngle = 78f;
    [Range(0.05f, 0.8f)] public float maxScreenDistance = 0.48f;
    public bool requireLineOfSight = false;
    public LayerMask lineOfSightMask = ~0;

    [Header("Comportamiento")]
    public bool blockInGraffitiMode = true;
    public bool blockWhileMusicDeviceOpen = true;
    public float lockFovOffset = -2f;

    CombatTarget currentTarget;

    void Awake()
    {
        ResolveReferences();
    }

    void Update()
    {
        ResolveReferences();

        if (!CanUseLock())
        {
            ReleaseTarget();
            return;
        }

        if (Input.GetMouseButtonDown(lockMouseButton))
        {
            if (currentTarget != null && !holdToLock)
                ReleaseTarget();
            else
                TryAcquireTarget();
        }

        if (holdToLock && Input.GetMouseButtonUp(lockMouseButton))
            ReleaseTarget();

        if (currentTarget == null)
            return;

        if (!IsTargetStillValid(currentTarget))
        {
            ReleaseTarget();
            return;
        }

        Vector3 focusPoint = currentTarget.GetFocusPoint();

        if (fps != null)
        {
            fps.SetCombatLockPoint(focusPoint);
            fps.SetThirdPersonFovOffset(lockFovOffset);
        }
    }

    void OnDisable()
    {
        ReleaseTarget();
    }

    void ResolveReferences()
    {
        if (fps == null)
            fps = GetComponent<FPSController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (musicPlayer == null)
            musicPlayer = GetComponent<DiegeticMusicPlayer>();
    }

    bool CanUseLock()
    {
        if (fps != null)
        {
            if (blockInGraffitiMode && fps.IsGraffitiMode())
                return false;

            if (fps.IsClimbing() || fps.IsParkouring())
                return false;
        }

        if (blockWhileMusicDeviceOpen && musicPlayer != null && musicPlayer.IsOpen())
            return false;

        return true;
    }

    void TryAcquireTarget()
    {
        CombatTarget bestTarget = FindBestTarget();

        if (bestTarget == null)
            return;

        SetTarget(bestTarget);
    }

    CombatTarget FindBestTarget()
    {
        if (playerCamera == null)
            return null;

        CombatTarget[] targets = FindObjectsByType<CombatTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        CombatTarget bestTarget = null;
        float bestScore = float.MaxValue;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        foreach (CombatTarget target in targets)
        {
            if (!IsCandidate(target))
                continue;

            Vector3 focusPoint = target.GetFocusPoint();
            Vector3 toTarget = focusPoint - playerCamera.transform.position;
            float distance = toTarget.magnitude;

            if (distance > maxLockDistance)
                continue;

            Vector3 viewport = playerCamera.WorldToViewportPoint(focusPoint);
            if (viewport.z <= 0f || viewport.x < -0.05f || viewport.x > 1.05f || viewport.y < -0.05f || viewport.y > 1.05f)
                continue;

            float angle = Vector3.Angle(playerCamera.transform.forward, toTarget.normalized);
            if (angle > maxLockAngle)
                continue;

            Vector2 screenPosition = new Vector2(viewport.x, viewport.y);
            float screenDistance = Vector2.Distance(screenCenter, screenPosition);
            if (screenDistance > maxScreenDistance)
                continue;

            if (requireLineOfSight && IsObstructed(focusPoint, target))
                continue;

            float distanceScore = distance / Mathf.Max(0.01f, maxLockDistance);
            float score = screenDistance * 1.35f + distanceScore * 0.42f - target.lockPriority * 0.08f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    bool IsCandidate(CombatTarget target)
    {
        if (target == null || !target.isActiveAndEnabled || !target.canBeLocked)
            return false;

        return !target.transform.IsChildOf(transform);
    }

    bool IsTargetStillValid(CombatTarget target)
    {
        if (!IsCandidate(target))
            return false;

        float distance = Vector3.Distance(transform.position, target.GetFocusPoint());
        if (distance > keepLockDistance)
            return false;

        return !requireLineOfSight || !IsObstructed(target.GetFocusPoint(), target);
    }

    bool IsObstructed(Vector3 focusPoint, CombatTarget target)
    {
        if (playerCamera == null)
            return false;

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = focusPoint - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return false;

        RaycastHit hit;
        if (!Physics.Raycast(origin, direction.normalized, out hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
            return false;

        return !hit.transform.IsChildOf(target.transform);
    }

    void SetTarget(CombatTarget target)
    {
        ReleaseTarget();
        currentTarget = target;
        currentTarget.SetLocked(true);

        if (fps != null)
        {
            fps.SetCombatLockPoint(currentTarget.GetFocusPoint());
            fps.SetThirdPersonFovOffset(lockFovOffset);
        }
    }

    public void ReleaseTarget()
    {
        if (currentTarget != null)
            currentTarget.SetLocked(false);

        currentTarget = null;

        if (fps != null)
        {
            fps.ClearCombatLockPoint();
            fps.SetThirdPersonFovOffset(0f);
        }
    }

    public CombatTarget GetCurrentTarget()
    {
        return currentTarget;
    }
}
