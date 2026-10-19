using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[DefaultExecutionOrder(300)]
public sealed class GraffitiWallCursor : MonoBehaviour
{
    const int SegmentCount = 52;

    [SerializeField] Color readyColor = new Color(0.82f, 1f, 0.08f, 0.95f);
    [SerializeField] Color blockedColor = new Color(1f, 0.1f, 0.42f, 0.92f);
    [SerializeField, Min(0.001f)] float surfaceOffset = 0.016f;
    [SerializeField, Range(1f, 20f)] float radiusResponse = 14f;
    [SerializeField, Range(0f, 0.15f)] float pulseAmount = 0.055f;

    LineRenderer ring;
    Material material;
    readonly Vector3[] points = new Vector3[SegmentCount];
    float targetRadius = 0.05f;
    float currentRadius = 0.05f;
    bool hasTarget;
    bool canPaint;

    public float CurrentRadius => currentRadius;
    public bool IsVisible => ring != null && ring.enabled;

    void Awake()
    {
        EnsureRenderer();
        Hide();
    }

    public void SetTarget(RaycastHit hit, float footprintDiameter, bool ready)
    {
        EnsureRenderer();
        if (hit.collider == null)
        {
            Hide();
            return;
        }

        Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.forward;
        transform.SetPositionAndRotation(hit.point + normal * surfaceOffset, Quaternion.LookRotation(normal));
        targetRadius = Mathf.Clamp(footprintDiameter * 0.5f, 0.024f, 0.72f);
        if (!hasTarget)
            currentRadius = targetRadius;
        hasTarget = true;
        canPaint = ready;
        ring.enabled = true;
        RefreshGeometry(0f);
    }

    public void Hide()
    {
        hasTarget = false;
        if (ring != null)
            ring.enabled = false;
    }

    void LateUpdate()
    {
        if (!hasTarget || ring == null || !ring.enabled)
            return;

        float blend = 1f - Mathf.Exp(-radiusResponse * Time.unscaledDeltaTime);
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, blend);
        RefreshGeometry(Time.unscaledTime);
    }

    void EnsureRenderer()
    {
        if (ring != null)
            return;

        ring = gameObject.GetComponent<LineRenderer>();
        if (ring == null)
            ring = gameObject.AddComponent<LineRenderer>();

        Shader shader = Shader.Find("FLOWSTATE/Graffiti/WallCursor");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            material = new Material(shader) { name = "Runtime Graffiti Wall Cursor" };
            ring.sharedMaterial = material;
        }

        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = SegmentCount;
        ring.alignment = LineAlignment.TransformZ;
        ring.textureMode = LineTextureMode.Stretch;
        ring.numCornerVertices = 3;
        ring.numCapVertices = 2;
        ring.shadowCastingMode = ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.allowOcclusionWhenDynamic = false;
        ring.sortingOrder = 120;
    }

    void RefreshGeometry(float time)
    {
        float pulse = 1f + Mathf.Sin(time * 4.8f) * pulseAmount;
        float radius = currentRadius * pulse;
        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SegmentCount;
            points[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }
        ring.SetPositions(points);

        float width = Mathf.Clamp(currentRadius * 0.075f, 0.004f, 0.018f);
        ring.startWidth = width;
        ring.endWidth = width;
        Color color = canPaint ? readyColor : blockedColor;
        ring.startColor = color;
        ring.endColor = color;
    }

    void OnDestroy()
    {
        if (material == null)
            return;
        if (Application.isPlaying) Destroy(material);
        else DestroyImmediate(material);
    }
}
