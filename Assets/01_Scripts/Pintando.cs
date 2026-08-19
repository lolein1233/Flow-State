using System.Collections;
using UnityEngine;

public class Pintando : MonoBehaviour
{
    public Camera cam;
    public GameObject brushPrefab;
    public LayerMask paintLayer;
    public float distance = 3f;
    public float surfaceOffset = 0.012f;
    public int maxStampsPerFrame = 18;
    [SerializeField, Min(64)] int minimumInterpolationBudget = 384;
    [SerializeField, Range(0.35f, 1f)] float continuitySpacingScale = 0.58f;
    public GraffitiPainter mainPainter;

    [Header("Distancia y proyeccion")]
    [SerializeField] float closeDistance = 0.55f;
    [SerializeField] float farRadiusMultiplier = 1.42f;
    [SerializeField] float farOpacityMultiplier = 0.5f;
    [SerializeField] float maximumAngleStretch = 2.25f;

    [Header("Chorreos")]
    [SerializeField] bool enableDrips = true;
    [SerializeField] float dripDwellThreshold = 0.68f;
    [SerializeField] float dripLength = 0.24f;
    [SerializeField] float dripDuration = 0.48f;
    [SerializeField] int dripSegments = 11;

    const string SprayShaderName = "FLOWSTATE/Graffiti/LayeredSprayStamp";

    Material sprayMaterial;
    float sampleTimer;
    bool hasLastPaintPoint;
    Vector3 lastPaintPoint;
    Vector3 lastPaintNormal;
    Collider lastPaintCollider;
    Vector3 previousRayPoint;
    bool hasPreviousRayPoint;
    float dwellTimer;
    float dripCooldown;
    float smoothedBrushSize;

    void Awake()
    {
        minimumInterpolationBudget = Mathf.Max(384, minimumInterpolationBudget);
        EnsureMaterial();
    }

    void Update()
    {
        dripCooldown = Mathf.Max(0f, dripCooldown - Time.deltaTime);

        if (mainPainter == null || cam == null)
        {
            ResetStroke();
            return;
        }

        if (mainPainter.menuOpen || mainPainter.useDecalMode || !mainPainter.IsPainting || !Input.GetMouseButton(0))
        {
            ResetStroke();
            return;
        }

        if (mainPainter.requireGraffitiMode && mainPainter.fps != null && !mainPainter.fps.IsGraffitiMode())
        {
            ResetStroke();
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, distance, paintLayer, QueryTriggerInteraction.Ignore))
        {
            ResetStroke();
            return;
        }

        UpdateDwell(hit, Time.deltaTime);

        if (!mainPainter.CanEmitPaint || sprayMaterial == null)
        {
            SkipStroke(hit);
            return;
        }

        sampleTimer += Time.deltaTime;
        PaintStroke(hit);
    }

    void EnsureMaterial()
    {
        if (sprayMaterial != null)
            return;

        Shader shader = Shader.Find(SprayShaderName);
        if (shader != null)
        {
            sprayMaterial = new Material(shader)
            {
                name = "Runtime Layered Graffiti Material",
                renderQueue = 3010
            };
        }
    }

    void PaintStroke(RaycastHit hit)
    {
        float averageSize = Mathf.Max(0.001f, (mainPainter.brushMinSize + mainPainter.brushMaxSize) * 0.5f);
        float spacing = mainPainter.GetBrushSpacing(averageSize) * continuitySpacingScale;

        if (!hasLastPaintPoint || hit.collider != lastPaintCollider || Vector3.Angle(lastPaintNormal, hit.normal) > 40f)
        {
            float exposure = Mathf.Max(sampleTimer, Time.deltaTime);
            PaintStamp(hit, hit.point, hit.normal, Vector3.zero, exposure);
            sampleTimer = 0f;
            RememberHit(hit);
            return;
        }

        float distanceBetween = Vector3.Distance(lastPaintPoint, hit.point);
        bool timeSampleDue = sampleTimer >= mainPainter.freePaintRate;
        int distanceSamples = Mathf.CeilToInt(distanceBetween / spacing);

        if (distanceSamples <= 0 && !timeSampleDue)
            return;

        int stampLimit = Mathf.Max(minimumInterpolationBudget, maxStampsPerFrame);
        int stampCount = Mathf.Clamp(Mathf.Max(1, distanceSamples), 1, stampLimit);
        float exposurePerStamp = Mathf.Max(0.0001f, sampleTimer / stampCount);
        if (distanceSamples > 0)
            exposurePerStamp = Mathf.Max(exposurePerStamp, mainPainter.freePaintRate * 0.75f);
        Vector3 startPoint = lastPaintPoint;
        Vector3 startNormal = lastPaintNormal;

        for (int i = 1; i <= stampCount; i++)
        {
            float t = i / (float)stampCount;
            Vector3 point = Vector3.Lerp(startPoint, hit.point, t);
            Vector3 normal = Vector3.Slerp(startNormal, hit.normal, t).normalized;
            Vector3 strokeDirection = point - startPoint;
            PaintStamp(hit, point, normal, strokeDirection, exposurePerStamp);
        }

        sampleTimer = 0f;
        RememberHit(hit);
    }

    void PaintStamp(RaycastHit hit, Vector3 point, Vector3 normal, Vector3 strokeDirection, float exposureSeconds)
    {
        Transform surface = hit.collider != null ? hit.collider.transform : null;
        GraffitiSurfaceCanvas canvas = GraffitiSurfaceCanvas.GetOrCreate(surface, sprayMaterial);
        if (canvas == null)
            return;

        float distance01 = Mathf.InverseLerp(closeDistance, Mathf.Max(closeDistance + 0.01f, distance), hit.distance);
        float resourceRadius = mainPainter.sprayResources != null ? mainPainter.sprayResources.OutputRadius : 1f;
        float requestedSize = mainPainter.GetBrushSize();
        if (smoothedBrushSize <= 0f)
            smoothedBrushSize = (mainPainter.brushMinSize + mainPainter.brushMaxSize) * 0.5f;
        smoothedBrushSize = Mathf.Lerp(smoothedBrushSize, requestedSize, 0.14f);
        float size = smoothedBrushSize * resourceRadius * Mathf.Lerp(0.86f, farRadiusMultiplier, distance01);
        float opacity = Mathf.Lerp(1.08f, farOpacityMultiplier, distance01);

        Vector3 tangent = GetSurfaceTangent(normal, strokeDirection);
        Vector3 viewProjection = Vector3.ProjectOnPlane(cam.transform.forward, normal);
        float incidence = Mathf.Abs(Vector3.Dot(-cam.transform.forward, normal));
        float angleStretch = Mathf.Min(maximumAngleStretch, 1f / Mathf.Max(0.3f, incidence));
        if (angleStretch > 1.05f && viewProjection.sqrMagnitude > 0.0001f)
            tangent = viewProjection.normalized;

        float width = size * angleStretch;
        float height = size;
        ApplyNozzleAspect(ref width, ref height);

        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
        Vector2 jitter = Random.insideUnitCircle * size * mainPainter.brushJitter;
        Vector3 position = point + normal * surfaceOffset + tangent * jitter.x + bitangent * jitter.y;
        Color color = mainPainter.GetPaintColorWithVariation(exposureSeconds, opacity);
        float seed = Random.Range(0.001f, 999f);
        Vector4 profile = mainPainter.GetStampProfile(distance01, seed);
        Vector4 spray = mainPainter.GetSpraySettings();
        canvas.AddStamp(position, normal, tangent, width, height, color, profile, spray);
    }

    void ApplyNozzleAspect(ref float width, ref float height)
    {
        switch (mainPainter.currentNozzleShape)
        {
            case GraffitiNozzleShape.Needle:
                width *= 0.72f;
                height *= 1.05f;
                break;
            case GraffitiNozzleShape.FatCap:
                width *= 1.18f;
                height *= 1.18f;
                break;
            case GraffitiNozzleShape.Chisel:
                width *= 1.58f;
                height *= 0.56f;
                break;
            case GraffitiNozzleShape.Splatter:
                width *= Random.Range(0.82f, 1.28f);
                height *= Random.Range(0.82f, 1.28f);
                break;
        }
    }

    Vector3 GetSurfaceTangent(Vector3 normal, Vector3 strokeDirection)
    {
        Vector3 tangent = Vector3.ProjectOnPlane(strokeDirection, normal);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.ProjectOnPlane(cam.transform.right, normal);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, Vector3.forward);
        return tangent.normalized;
    }

    void UpdateDwell(RaycastHit hit, float deltaTime)
    {
        float threshold = Mathf.Max(0.01f, (mainPainter.brushMinSize + mainPainter.brushMaxSize) * 0.16f);
        if (hasPreviousRayPoint && hit.collider == lastPaintCollider && Vector3.Distance(previousRayPoint, hit.point) <= threshold)
            dwellTimer += deltaTime;
        else
            dwellTimer = 0f;

        previousRayPoint = hit.point;
        hasPreviousRayPoint = true;

        if (!enableDrips || dripCooldown > 0f || dwellTimer < dripDwellThreshold || !mainPainter.CanEmitPaint || sprayMaterial == null)
            return;

        GraffitiSurfaceCanvas canvas = GraffitiSurfaceCanvas.GetOrCreate(hit.collider.transform, sprayMaterial);
        if (canvas != null)
        {
            StartCoroutine(CreateDrip(canvas, hit.point + hit.normal * surfaceOffset, hit.normal));
            dripCooldown = dripDwellThreshold * 1.4f;
            dwellTimer = 0f;
        }
    }

    IEnumerator CreateDrip(GraffitiSurfaceCanvas canvas, Vector3 start, Vector3 normal)
    {
        Vector3 down = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        if (down.sqrMagnitude < 0.0001f)
            yield break;

        Vector3 horizontal = Vector3.Cross(down, normal).normalized;
        int count = Mathf.Max(3, dripSegments);
        float baseWidth = Mathf.Lerp(mainPainter.brushMinSize, mainPainter.brushMaxSize, 0.35f) * 0.26f;

        for (int i = 0; i < count && canvas != null; i++)
        {
            float t = i / (float)(count - 1);
            float taper = Mathf.Lerp(1f, 0.28f, t);
            Vector3 point = start + down * (dripLength * t);
            Color color = mainPainter.GetPaintColorWithVariation(dripDuration / count, Mathf.Lerp(0.95f, 0.5f, t));
            Vector4 profile = new Vector4(0.72f, 0.92f, 0.1f, Random.Range(0.001f, 999f));
            Vector4 spray = mainPainter.GetSpraySettings();
            canvas.AddStamp(point, normal, horizontal, baseWidth * taper, baseWidth * 1.8f, color, profile, spray);
            yield return new WaitForSeconds(dripDuration / count);
        }
    }

    void RememberHit(RaycastHit hit)
    {
        hasLastPaintPoint = true;
        lastPaintPoint = hit.point;
        lastPaintNormal = hit.normal;
        lastPaintCollider = hit.collider;
    }

    void SkipStroke(RaycastHit hit)
    {
        sampleTimer = 0f;
        RememberHit(hit);
    }

    void ResetStroke()
    {
        sampleTimer = 0f;
        hasLastPaintPoint = false;
        lastPaintCollider = null;
        hasPreviousRayPoint = false;
        dwellTimer = 0f;
        smoothedBrushSize = 0f;
    }

    void OnDestroy()
    {
        if (sprayMaterial != null)
            Destroy(sprayMaterial);
    }
}
