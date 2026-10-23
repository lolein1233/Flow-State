using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(150)]
public class Pintando : MonoBehaviour
{
    public Camera cam;
    public GameObject brushPrefab;
    public LayerMask paintLayer;
    public float distance = 3f;
    public float surfaceOffset = 0.012f;
    public int maxStampsPerFrame = 18;
    [SerializeField, Min(32)] int minimumInterpolationBudget = 96;
    [SerializeField, Range(0.25f, 1f)] float continuitySpacingScale = 0.5f;
    public GraffitiPainter mainPainter;

    [Header("Distancia y proyeccion")]
    [SerializeField] float closeDistance = 0.55f;
    [SerializeField, Range(0.2f, 0.9f)] float controlledCloseRadiusMultiplier = 0.3f;
    [SerializeField, Range(1.4f, 4.5f)] float controlledFarRadiusMultiplier = 3.25f;
    [SerializeField] float maximumAngleStretch = 2.25f;

    [Header("Ataque inicial")]
    [SerializeField, Range(1f, 4f)] float initialBurstExposureFrames = 3f;

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
    float strokeSeed;
    int strokeSampleIndex;
    Vector3 lastObservedPoint;
    float lastPaintCone01;
    int firstStampFrame = -1;
    public bool CapacityReached { get; private set; }
    Collider fullSurface;

    void Awake()
    {
        minimumInterpolationBudget = Mathf.Clamp(minimumInterpolationBudget, 64, 160);
        // Older scenes serialized a looser spacing value. Cap it here so every
        // existing wall receives the denser, continuous stroke without a scene migration.
        continuitySpacingScale = Mathf.Min(continuitySpacingScale, 0.5f);
        EnsureMaterial();
    }

    void LateUpdate()
    {
        if (CapacityReached && mainPainter != null && mainPainter.raycastUI != null &&
            mainPainter.raycastUI.GetHit().collider != fullSurface)
            CapacityReached = false;
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

        RaycastHit hit = mainPainter.raycastUI != null ? mainPainter.raycastUI.GetHit() : default;
        if (hit.collider == null || hit.distance > distance || (paintLayer.value & (1 << hit.collider.gameObject.layer)) == 0)
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

        if (firstStampFrame == Time.frameCount)
            return;
        SampleStroke(hit, Time.deltaTime);
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

    void SampleStroke(RaycastHit hit, float deltaTime)
    {
        sampleTimer += deltaTime;
        PaintStroke(hit, deltaTime);
    }

    void PaintStroke(RaycastHit hit, float deltaTime)
    {
        float currentCone01 = GetEffectiveDistance01(hit.distance);
        float averageSize = Mathf.Max(0.001f, hasLastPaintPoint
            ? Mathf.Min(EstimateBrushWidthFromDistance01(lastPaintCone01), EstimateBrushWidthFromDistance01(currentCone01))
            : EstimateBrushWidthFromDistance01(currentCone01));
        float spacing = mainPainter.GetBrushSpacing(averageSize) * continuitySpacingScale;

        if (!hasLastPaintPoint || hit.collider != lastPaintCollider || Vector3.Angle(lastPaintNormal, hit.normal) > 40f)
        {
            float exposure = 1f / 60f;
            PaintStamp(hit, hit.point, hit.normal, Vector3.zero, 0f, currentCone01, exposure);
            sampleTimer = 0f;
            RememberHit(hit);
            return;
        }

        float distanceBetween = Vector3.Distance(lastPaintPoint, hit.point);
        float strokeSpeed = Vector3.Distance(lastObservedPoint, hit.point) / Mathf.Max(deltaTime, 0.0001f);
        lastObservedPoint = hit.point;
        int distanceSamples = Mathf.FloorToInt(distanceBetween / spacing);
        bool moving = strokeSpeed > 0.02f;
        // Spatial samples carry the unsampled remainder instead of stamping every
        // rendered frame. At rest use a fixed emission interval, independent of FPS.
        float interval = Mathf.Max(1f / 30f, mainPainter.freePaintRate);
        if (distanceSamples == 0 && (moving || sampleTimer < interval))
            return;

        int stampLimit = Mathf.Max(minimumInterpolationBudget, maxStampsPerFrame);
        int stampCount = Mathf.Clamp(distanceSamples > 0 ? distanceSamples : Mathf.FloorToInt(sampleTimer / interval), 1, stampLimit);
        float exposurePerStamp = distanceSamples > 0 ? mainPainter.MinimumStampExposure / 60f : interval;
        Vector3 startPoint = lastPaintPoint;
        Vector3 startNormal = lastPaintNormal;
        float startCone01 = lastPaintCone01;
        float sampledFraction = distanceSamples > 0 && distanceSamples <= stampLimit
            ? Mathf.Min(1f, distanceSamples * spacing / Mathf.Max(distanceBetween, 0.0001f)) : 1f;
        Vector3 previousSamplePoint = startPoint;
        for (int i = 1; i <= stampCount; i++)
        {
            float t = i / (float)stampCount * sampledFraction;
            Vector3 point = Vector3.Lerp(startPoint, hit.point, t);
            Vector3 normal = Vector3.Slerp(startNormal, hit.normal, t).normalized;
            Vector3 strokeDirection = point - previousSamplePoint;
            float segmentLength = strokeDirection.magnitude;
            float cone01 = Mathf.Lerp(startCone01, currentCone01, t);
            PaintStamp(hit, point, normal, strokeDirection, segmentLength, cone01, exposurePerStamp);
            previousSamplePoint = point;
            if (CapacityReached)
                break;
        }

        sampleTimer = distanceSamples > 0 ? 0f : Mathf.Max(0f, sampleTimer - stampCount * interval);
        lastPaintPoint = Vector3.Lerp(startPoint, hit.point, sampledFraction);
        lastPaintNormal = Vector3.Slerp(startNormal, hit.normal, sampledFraction).normalized;
        lastPaintCone01 = Mathf.Lerp(startCone01, currentCone01, sampledFraction);
    }

    void PaintStamp(RaycastHit hit, Vector3 point, Vector3 normal, Vector3 strokeDirection, float segmentLength, float distance01, float exposureSeconds)
    {
        Transform surface = hit.collider != null ? hit.collider.transform : null;
        GraffitiSurfaceCanvas canvas = GraffitiSurfaceCanvas.GetOrCreate(surface, sprayMaterial);
        if (canvas == null)
        {
            CapacityReached = surface != null;
            fullSurface = hit.collider;
            mainPainter.CancelPaintingForMenu();
            return;
        }

        float resourceRadius = mainPainter.sprayResources != null ? mainPainter.sprayResources.OutputRadius : 1f;
        float requestedSize = mainPainter.GetBrushSize();
        if (smoothedBrushSize <= 0f)
            smoothedBrushSize = (mainPainter.brushMinSize + mainPainter.brushMaxSize) * 0.5f;
        smoothedBrushSize = Mathf.Lerp(smoothedBrushSize, requestedSize, 0.14f);
        float size = smoothedBrushSize * resourceRadius * GetRadiusMultiplier(distance01);

        Vector3 tangent = GetSurfaceTangent(normal, strokeDirection);
        Vector3 sprayDirection = mainPainter.GetSprayRay().direction;
        Vector3 viewProjection = Vector3.ProjectOnPlane(sprayDirection, normal);
        float incidence = Mathf.Abs(Vector3.Dot(-sprayDirection, normal));
        float angleStretch = Mathf.Min(maximumAngleStretch, 1f / Mathf.Max(0.3f, incidence));
        // A stationary dab may align with the oblique projection. While moving,
        // keep the quad on the actual path so its bridge cannot rotate away and
        // reopen gaps on diagonal strokes.
        if (segmentLength <= 0.0001f && angleStretch > 1.05f && viewProjection.sqrMagnitude > 0.0001f)
            tangent = viewProjection.normalized;

        // Stretch a moving sample backwards across the distance it owns. The
        // spray halo still comes from the stamp shader, but the opaque spine is
        // now continuous instead of exposing a row of circular deposits.
        float bridgeLength = Mathf.Min(segmentLength * 1.2f, size * 0.8f);
        float width = size * angleStretch + bridgeLength;
        float height = size;
        ApplyNozzleAspect(ref width, ref height);

        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
        float precisionJitter = Mathf.Lerp(0.14f, 0.34f, Mathf.Clamp01(distance01));
        Vector2 jitter = Random.insideUnitCircle * size * mainPainter.brushJitter * precisionJitter;
        Vector3 position = point - tangent * (bridgeLength * 0.5f) + normal * surfaceOffset + tangent * jitter.x + bitangent * jitter.y;
        Color color = mainPainter.GetPaintColorWithVariation(exposureSeconds, 1f);
        if (strokeSeed <= 0f)
            strokeSeed = Random.Range(0.001f, 999f);
        float seed = strokeSeed + strokeSampleIndex++ * 0.013f;
        Vector4 profile = mainPainter.GetStampProfile(distance01, seed);
        Vector4 spray = mainPainter.GetSpraySettings();
        canvas.AddStamp(position, normal, tangent, width, height, color, profile, spray);
    }

    void ApplyNozzleAspect(ref float width, ref float height)
    {
        Vector2 aspect = mainPainter.GetNozzleAspect();
        width *= aspect.x;
        height *= aspect.y;
        if (mainPainter.currentNozzleShape == GraffitiNozzleShape.Splatter)
        {
            width *= Random.Range(0.82f, 1.28f);
            height *= Random.Range(0.82f, 1.28f);
        }
    }

    public void BeginStrokeImmediately(RaycastHit hit)
    {
        EnsureMaterial();
        if (mainPainter == null || sprayMaterial == null || hit.collider == null || !mainPainter.CanEmitPaint)
            return;

        ResetStroke();
        sampleTimer = initialBurstExposureFrames / 60f;
        PaintStamp(hit, hit.point, hit.normal, Vector3.zero, 0f, GetEffectiveDistance01(hit.distance), sampleTimer);
        sampleTimer = 0f;
        RememberHit(hit);
        firstStampFrame = Time.frameCount;
    }

    public float GetDistance01(float distanceToSurface)
    {
        return Mathf.Clamp01(Mathf.InverseLerp(closeDistance, Mathf.Max(closeDistance + 0.01f, distance), distanceToSurface));
    }

    public float EstimateBrushWidth(float distanceToSurface)
    {
        return EstimateBrushWidthFromDistance01(GetEffectiveDistance01(distanceToSurface));
    }

    public float GetRadiusMultiplier(float distance01)
    {
        return Mathf.Lerp(controlledCloseRadiusMultiplier, controlledFarRadiusMultiplier, Mathf.Clamp01(distance01));
    }

    float GetEffectiveDistance01(float distanceToSurface)
    {
        return mainPainter != null ? mainPainter.GetSprayDistance01(distanceToSurface) : GetDistance01(distanceToSurface);
    }

    float EstimateBrushWidthFromDistance01(float distance01)
    {
        if (mainPainter == null)
            return 0f;

        float resourceRadius = mainPainter.sprayResources != null ? mainPainter.sprayResources.OutputRadius : 1f;
        float baseSize = (mainPainter.brushMinSize + mainPainter.brushMaxSize) * 0.5f;
        Vector2 aspect = mainPainter.GetNozzleAspect();
        return baseSize * Mathf.Min(aspect.x, aspect.y) * resourceRadius * GetRadiusMultiplier(distance01);
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
            if (!canvas.AddStamp(point, normal, horizontal, baseWidth * taper, baseWidth * 1.8f, color, profile, spray))
            {
                Transform surface = canvas.transform.parent;
                canvas = GraffitiSurfaceCanvas.GetOrCreate(surface, sprayMaterial);
                if (canvas == null || !canvas.AddStamp(point, normal, horizontal, baseWidth * taper, baseWidth * 1.8f, color, profile, spray))
                    yield break;
            }
            yield return new WaitForSeconds(dripDuration / count);
        }
    }

    void RememberHit(RaycastHit hit)
    {
        hasLastPaintPoint = true;
        lastPaintPoint = hit.point;
        lastPaintNormal = hit.normal;
        lastPaintCollider = hit.collider;
        lastObservedPoint = hit.point;
        lastPaintCone01 = GetEffectiveDistance01(hit.distance);
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
        strokeSeed = 0f;
        strokeSampleIndex = 0;
    }

    void OnDestroy()
    {
        if (sprayMaterial != null)
        {
            if (Application.isPlaying) Destroy(sprayMaterial);
            else DestroyImmediate(sprayMaterial);
        }
    }
}
