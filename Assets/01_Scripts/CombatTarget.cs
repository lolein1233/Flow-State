using UnityEngine;

[DisallowMultipleComponent]
public class CombatTarget : MonoBehaviour
{
    [Header("Lock-on")]
    public string displayName = "ENEMIGO";
    public bool canBeLocked = true;
    public float lockPriority = 1f;
    public Transform focusPoint;
    [Range(0.25f, 0.95f)] public float focusHeightRatio = 0.62f;
    public Vector3 focusOffset = Vector3.zero;

    CombatTargetHighlighter highlighter;

    void Awake()
    {
        EnsureHighlighter();
    }

    void OnDisable()
    {
        SetLocked(false);
    }

    public Vector3 GetFocusPoint()
    {
        if (focusPoint != null)
            return focusPoint.position;

        Bounds bounds;
        if (TryGetWorldBounds(out bounds))
        {
            Vector3 point = bounds.center;
            point.y = Mathf.Lerp(bounds.min.y, bounds.max.y, focusHeightRatio);
            return point + focusOffset;
        }

        return transform.position + Vector3.up + focusOffset;
    }

    public bool TryGetWorldBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.one);

        foreach (Renderer targetRenderer in renderers)
        {
            if (ShouldIgnoreRenderer(targetRenderer))
                continue;

            if (!hasBounds)
            {
                bounds = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(targetRenderer.bounds);
            }
        }

        return hasBounds;
    }

    public float GetSelectionRadius()
    {
        Bounds bounds;
        if (!TryGetWorldBounds(out bounds))
            return 1f;

        return Mathf.Max(0.5f, Mathf.Max(bounds.extents.x, bounds.extents.z));
    }

    public void SetLocked(bool locked)
    {
        EnsureHighlighter();

        if (highlighter != null)
            highlighter.SetLocked(locked);
    }

    void EnsureHighlighter()
    {
        if (highlighter == null)
            highlighter = GetComponent<CombatTargetHighlighter>();

        if (highlighter == null)
            highlighter = gameObject.AddComponent<CombatTargetHighlighter>();
    }

    bool ShouldIgnoreRenderer(Renderer targetRenderer)
    {
        if (targetRenderer == null || targetRenderer is LineRenderer)
            return true;

        string objectName = targetRenderer.gameObject.name;
        if (objectName.Contains("Combat Outline Shell") || objectName.Contains("Combat Outline Shadow") || objectName.Contains("Combat Lock Ring"))
            return true;

        Material material = targetRenderer.sharedMaterial;
        return material != null && material.shader != null && material.shader.name.Contains("WhiteTargetOutline");
    }
}
