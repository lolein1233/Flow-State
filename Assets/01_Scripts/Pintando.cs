using UnityEngine;

public class Pintando : MonoBehaviour
{
    public Camera cam;
    public GameObject brushPrefab;
    public LayerMask paintLayer;

    public float distance = 3f;
    public float surfaceOffset = 0.012f;
    public int maxStampsPerFrame = 18;

    public GraffitiPainter mainPainter;

    float sampleTimer;
    bool hasLastPaintPoint;
    Vector3 lastPaintPoint;
    Vector3 lastPaintNormal;
    Collider lastPaintCollider;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    void Update()
    {
        if (mainPainter == null || cam == null || brushPrefab == null)
        {
            ResetStroke();
            return;
        }

        if (mainPainter.menuOpen || mainPainter.useDecalMode)
        {
            ResetStroke();
            return;
        }

        if (mainPainter.requireGraffitiMode && mainPainter.fps != null && !mainPainter.fps.IsGraffitiMode())
        {
            ResetStroke();
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            ResetStroke();
            return;
        }

        sampleTimer += Time.deltaTime;
        if (sampleTimer < mainPainter.freePaintRate)
            return;

        sampleTimer = 0f;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, distance, paintLayer, QueryTriggerInteraction.Ignore))
        {
            ResetStroke();
            return;
        }

        PaintStroke(hit);
    }

    void PaintStroke(RaycastHit hit)
    {
        float averageSize = Mathf.Max(0.001f, (mainPainter.brushMinSize + mainPainter.brushMaxSize) * 0.5f);
        float spacing = mainPainter.GetBrushSpacing(averageSize);

        if (!hasLastPaintPoint || hit.collider != lastPaintCollider || Vector3.Angle(lastPaintNormal, hit.normal) > 40f)
        {
            PaintStamp(hit.point, hit.normal, Vector3.zero, hit.collider.transform);
            RememberHit(hit);
            return;
        }

        float distanceBetween = Vector3.Distance(lastPaintPoint, hit.point);
        int stampCount = Mathf.Clamp(Mathf.CeilToInt(distanceBetween / spacing), 1, maxStampsPerFrame);

        for (int i = 1; i <= stampCount; i++)
        {
            float t = i / (float)stampCount;
            Vector3 point = Vector3.Lerp(lastPaintPoint, hit.point, t);
            Vector3 normal = Vector3.Slerp(lastPaintNormal, hit.normal, t).normalized;
            Vector3 strokeDirection = point - lastPaintPoint;

            PaintStamp(point, normal, strokeDirection, hit.collider.transform);
        }

        RememberHit(hit);
    }

    void PaintStamp(Vector3 point, Vector3 normal, Vector3 strokeDirection, Transform parent)
    {
        float size = mainPainter.GetBrushSize();
        Vector3 tangent = GetSurfaceTangent(normal, strokeDirection);
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
        Vector2 jitter = Random.insideUnitCircle * size * mainPainter.brushJitter;

        Vector3 pos = point + normal * surfaceOffset + tangent * jitter.x + bitangent * jitter.y;
        Quaternion rot = Quaternion.LookRotation(-normal, tangent);

        float zRotation = GetNozzleRotation();
        rot *= Quaternion.Euler(0f, 0f, zRotation);

        GameObject brush = Instantiate(brushPrefab, pos, rot);
        brush.transform.localScale = Vector3.one * size;

        if (parent != null)
            brush.transform.SetParent(parent, true);

        ApplyBrushVisual(brush);
    }

    Vector3 GetSurfaceTangent(Vector3 normal, Vector3 strokeDirection)
    {
        Vector3 tangent = Vector3.ProjectOnPlane(strokeDirection, normal);

        if (tangent.sqrMagnitude < 0.0001f && cam != null)
            tangent = Vector3.ProjectOnPlane(cam.transform.right, normal);

        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, Vector3.up);

        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, Vector3.forward);

        return tangent.normalized;
    }

    float GetNozzleRotation()
    {
        switch (mainPainter.currentNozzleShape)
        {
            case GraffitiNozzleShape.Chisel:
                return Random.Range(-8f, 8f);
            case GraffitiNozzleShape.Needle:
                return Random.Range(-18f, 18f);
            default:
                return Random.Range(0f, 360f);
        }
    }

    void ApplyBrushVisual(GameObject brush)
    {
        Renderer renderer = brush.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        Color color = mainPainter.GetPaintColorWithVariation();
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);

        if (mainPainter.brushTexture != null)
        {
            block.SetTexture(BaseMapId, mainPainter.brushTexture);
            block.SetTexture(MainTexId, mainPainter.brushTexture);
        }

        renderer.SetPropertyBlock(block);
    }

    void RememberHit(RaycastHit hit)
    {
        hasLastPaintPoint = true;
        lastPaintPoint = hit.point;
        lastPaintNormal = hit.normal;
        lastPaintCollider = hit.collider;
    }

    void ResetStroke()
    {
        sampleTimer = 0f;
        hasLastPaintPoint = false;
        lastPaintCollider = null;
    }
}
