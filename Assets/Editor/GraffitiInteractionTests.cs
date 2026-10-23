#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class GraffitiInteractionTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    UnityEngine.Random.State randomState;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp] public void SetUp() => randomState = Random.state;

    [TearDown] public void TearDown()
    {
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
        Random.state = randomState;
    }

    GameObject Create(string name)
    {
        var obj = new GameObject(name);
        objects.Add(obj);
        return obj;
    }

    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);

    Pintando CreatePainter(out GraffitiPainter painter, out RaycastHit hit)
    {
        var owner = Create("Graffiti test painter");
        painter = owner.AddComponent<GraffitiPainter>();
        painter.enabled = false;
        painter.requireGraffitiMode = false;
        painter.useDecalMode = false;
        painter.cam = Create("Graffiti test camera").AddComponent<Camera>();
        painter.cam.enabled = false;
        painter.sprayResources = owner.AddComponent<SprayResourceSystem>();
        painter.sprayResources.enabled = false;
        Invoke(painter.sprayResources, "Awake");
        var paint = owner.AddComponent<Pintando>();
        paint.enabled = false;
        paint.mainPainter = painter;
        paint.cam = painter.cam;
        paint.distance = 6f;
        paint.paintLayer = ~0;
        Set(painter, "freePainter", paint);
        Set(painter, "isPainting", true);
        Set(painter, "emissionAllowed", true);
        var wall = Create("Graffiti test wall");
        wall.transform.position = new Vector3(0f, 10000f, 10003f);
        var collider = wall.AddComponent<BoxCollider>();
        collider.size = new Vector3(20f, 20f, 0.1f);
        Physics.SyncTransforms();
        Assert.IsTrue(collider.Raycast(new Ray(wall.transform.position - Vector3.forward * 3f, Vector3.forward), out hit, 6f));
        return paint;
    }

    [Test] public void ShortClickDepositsOneStrongStampImmediately()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out RaycastHit hit);
        // Validate the most dispersed case: even at maximum range a tap must read
        // immediately, without asking the player to hold the valve open.
        hit.distance = 6f;
        paint.BeginStrokeImmediately(hit);
        var canvas = hit.collider.GetComponentInChildren<GraffitiSurfaceCanvas>();
        Assert.That(canvas.GetStampCount(), Is.EqualTo(1));
        Invoke(canvas, "LateUpdate");
        Assert.That(canvas.GetComponent<MeshFilter>().sharedMesh.colors[0].a, Is.GreaterThan(0.7f));
    }

    int Simulate(int fps, float speed, out float lastAlpha)
    {
        Random.InitState(123);
        Pintando paint = CreatePainter(out GraffitiPainter painter, out RaycastHit hit);
        paint.BeginStrokeImmediately(hit);
        Vector3 start = hit.point;
        for (int i = 1; i <= fps; i++)
        {
            hit.point = start + Vector3.right * (speed * i / fps);
            Invoke(paint, "SampleStroke", hit, 1f / fps);
        }
        var canvas = hit.collider.GetComponentInChildren<GraffitiSurfaceCanvas>();
        Invoke(canvas, "LateUpdate");
        Color[] colors = canvas.GetComponent<MeshFilter>().sharedMesh.colors;
        lastAlpha = colors[colors.Length - 1].a;
        return canvas.GetStampCount();
    }

    [TestCase(0f)] [TestCase(1f)] [TestCase(8f)]
    public void DepositionIsStableAt30_60_144Fps(float speed)
    {
        int a = Simulate(30, speed, out float alphaA);
        int b = Simulate(60, speed, out float alphaB);
        int c = Simulate(144, speed, out float alphaC);
        Assert.That(Mathf.Max(a, b, c) - Mathf.Min(a, b, c), Is.LessThanOrEqualTo(1));
        Assert.That(alphaA, Is.EqualTo(alphaB).Within(0.015f));
        Assert.That(alphaA, Is.EqualTo(alphaC).Within(0.015f));
    }

    [Test] public void DistanceWidensEveryNozzleAndPreviewUsesItsAspect()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out _);
        foreach (GraffitiNozzleShape nozzle in System.Enum.GetValues(typeof(GraffitiNozzleShape)))
        {
            painter.currentNozzleShape = nozzle;
            Assert.That(paint.EstimateBrushWidth(6f), Is.GreaterThan(paint.EstimateBrushWidth(0.55f) * 2.5f));
            Vector2 preview = painter.GetPreviewSize();
            Vector2 aspect = painter.GetNozzleAspect();
            Assert.That(preview.x / preview.y, Is.EqualTo(aspect.x / aspect.y).Within(0.001f));
        }
    }

    [Test] public void AdaptiveGestureControlsFootprintAtTheSameWallDistance()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out _);
        Set(painter, "physicalControlWasActive", true);
        painter.useAdaptiveSprayDistance = true;

        Set(painter, "adaptiveCone", 0f);
        float closeWidth = paint.EstimateBrushWidth(3f);
        Assert.That(painter.SprayCone01, Is.EqualTo(0f).Within(0.001f));

        Set(painter, "adaptiveCone", 1f);
        float farWidth = paint.EstimateBrushWidth(3f);
        Assert.That(painter.SprayCone01, Is.EqualTo(1f).Within(0.001f));
        Assert.That(farWidth, Is.GreaterThan(closeWidth * 3.5f));
    }

    [Test] public void SlowPrecisionFootprintIsFineAndCoverageStillOpensWide()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out _);
        painter.brushMinSize = 0.08f;
        painter.brushMaxSize = 0.13f;

        float precisionWidth = paint.EstimateBrushWidth(0.55f);
        float coverageWidth = paint.EstimateBrushWidth(6f);

        Assert.That(precisionWidth, Is.LessThan(0.04f));
        Assert.That(coverageWidth, Is.GreaterThan(precisionWidth * 6f));
    }

    [Test] public void NozzleDiameterDoesNotRandomlyPulseBetweenSamples()
    {
        CreatePainter(out GraffitiPainter painter, out _);
        painter.brushMinSize = 0.08f;
        painter.brushMaxSize = 0.13f;

        float diameter = painter.GetBrushSize();
        for (int i = 0; i < 32; i++)
            Assert.That(painter.GetBrushSize(), Is.EqualTo(diameter).Within(0.000001f));
    }

    [Test] public void MovingStrokeStretchesSamplesAlongThePath()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out RaycastHit hit);
        painter.brushMinSize = painter.brushMaxSize = 0.1f;
        painter.brushJitter = 0f;
        paint.BeginStrokeImmediately(hit);

        hit.point += Vector3.right * 0.25f;
        Invoke(paint, "SampleStroke", hit, 1f / 60f);

        var canvas = hit.collider.GetComponentInChildren<GraffitiSurfaceCanvas>();
        Invoke(canvas, "LateUpdate");
        Vector3[] vertices = canvas.GetComponent<MeshFilter>().sharedMesh.vertices;
        Assert.That(vertices.Length, Is.GreaterThanOrEqualTo(8));
        float pathLength = Vector3.Distance(vertices[4], vertices[5]);
        float crossWidth = Vector3.Distance(vertices[5], vertices[6]);
        Assert.That(pathLength, Is.GreaterThan(crossWidth * 1.04f));
    }

    [Test] public void GestureIntentMakesFastSweepsWideAndSharpCornersPrecise()
    {
        float precise = GraffitiGestureDynamics.EvaluateCone(
            0.2f, 0f, true, 0.55f, 4.6f, 0.03f, 0.32f, 150f, 650f, 0.86f);
        float broad = GraffitiGestureDynamics.EvaluateCone(
            8f, 0f, true, 0.55f, 4.6f, 0.03f, 0.32f, 150f, 650f, 0.86f);
        float corner = GraffitiGestureDynamics.EvaluateCone(
            8f, 800f, true, 0.55f, 4.6f, 0.03f, 0.32f, 150f, 650f, 0.86f);

        Assert.That(precise, Is.LessThan(0.1f));
        Assert.That(broad, Is.GreaterThan(0.95f));
        Assert.That(corner, Is.LessThan(broad * 0.35f));
    }

    [Test] public void HandEdgeTransfersOutwardMotionToCameraButReturnsDirectly()
    {
        float centre = GraffitiGestureDynamics.GetCameraShare(0.2f, 0.3f, 1f, 0.68f, 0.94f);
        float edge = GraffitiGestureDynamics.GetCameraShare(0.92f, 1.02f, 1f, 0.68f, 0.94f);
        float returning = GraffitiGestureDynamics.GetCameraShare(0.92f, 0.82f, -1f, 0.68f, 0.94f);

        Assert.That(centre, Is.EqualTo(0f));
        Assert.That(edge, Is.GreaterThan(0.85f));
        Assert.That(returning, Is.EqualTo(0f));
    }

    [Test] public void FastSweepCompressionPreservesPrecisionDirectionAndLimitsTravel()
    {
        Vector2 precisionDelta = new Vector2(0.048f, 0f);
        Vector2 fastDelta = new Vector2(0.384f, 0.192f);

        Vector2 controlledPrecision = GraffitiGestureDynamics.CompressControlDelta(
            precisionDelta, 0.045f, 0.06f);
        Vector2 controlledFast = GraffitiGestureDynamics.CompressControlDelta(
            fastDelta, 0.045f, 0.06f);

        Assert.That(Vector2.Distance(controlledPrecision, precisionDelta), Is.LessThan(0.001f));
        Assert.That(controlledFast.magnitude, Is.LessThan(0.060001f));
        Assert.That(controlledFast.magnitude, Is.LessThan(fastDelta.magnitude * 0.15f));
        Assert.That(Vector2.Angle(controlledFast, fastDelta), Is.LessThan(0.001f));
    }

    [Test] public void FastSweepKeepsBroadIntentWhilePhysicalTravelIsCompressed()
    {
        const float deltaTime = 1f / 60f;
        Vector2 intentDelta = new Vector2(8f * 0.048f, 0f);
        Vector2 controlledDelta = GraffitiGestureDynamics.CompressControlDelta(
            intentDelta, 0.045f, 0.06f);
        float broadCone = GraffitiGestureDynamics.EvaluateCone(
            intentDelta.magnitude / deltaTime, 0f, true,
            0.55f, 4.6f, 0.03f, 0.32f, 150f, 650f, 0.86f);

        Assert.That(broadCone, Is.GreaterThan(0.95f));
        Assert.That(controlledDelta.x * 0.34f, Is.LessThan(0.021f));
        Assert.That(controlledDelta.x * 4f * 0.34f, Is.LessThan(0.083f));
    }

    [Test] public void EveryNozzleHasClearlySeparatedPrecisionAndCoverageWidths()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out _);
        Set(paint, "controlledCloseRadiusMultiplier", 0.55f);
        Set(paint, "controlledFarRadiusMultiplier", 3.25f);
        Set(painter, "physicalControlWasActive", true);
        painter.useAdaptiveSprayDistance = true;

        foreach (GraffitiNozzleShape nozzle in System.Enum.GetValues(typeof(GraffitiNozzleShape)))
        {
            painter.currentNozzleShape = nozzle;
            Set(painter, "adaptiveCone", 0.03f);
            float precisionWidth = paint.EstimateBrushWidth(3f);
            Set(painter, "adaptiveCone", 1f);
            float coverageWidth = paint.EstimateBrushWidth(3f);
            Assert.That(coverageWidth, Is.GreaterThan(precisionWidth * 5f), nozzle.ToString());
        }
    }

    [Test] public void GraffitiLookDeltaExtendsAimAndRespectsVerticalSafetyLimit()
    {
        var player = Create("Graffiti camera player");
        player.AddComponent<CharacterController>();
        var fps = player.AddComponent<FPSController>();
        Invoke(fps, "Awake");
        fps.EnterGraffitiMode();

        fps.ApplyGraffitiLookDelta(55f, -200f);
        Assert.That(Mathf.DeltaAngle(0f, player.transform.eulerAngles.y), Is.EqualTo(55f).Within(0.01f));
        float pitch = (float)typeof(FPSController).GetField("pitch", Private).GetValue(fps);
        Assert.That(pitch, Is.EqualTo(fps.graffitiMinPitch).Within(0.01f));
    }

    [Test] public void FullMixtureMaintainsMaximumOpacityAcrossNozzleProfiles()
    {
        CreatePainter(out GraffitiPainter painter, out _);
        painter.sprayResources.SetMixture(100f);
        painter.brushOpacity = 0.05f;
        float lowPreset = painter.GetPaintColorWithVariation(1f / 60f, 1f).a;
        painter.brushOpacity = 0.9f;
        float highPreset = painter.GetPaintColorWithVariation(1f / 60f, 1f).a;

        Assert.That(lowPreset, Is.GreaterThan(0.94f));
        Assert.That(highPreset, Is.EqualTo(lowPreset).Within(0.001f));

        painter.sprayResources.SetMixture(0f);
        float depletedMixture = painter.GetPaintColorWithVariation(1f / 60f, 1f).a;
        Assert.That(depletedMixture, Is.LessThan(lowPreset * 0.7f));
    }

    [Test] public void CloseToFarStrokeWidensContinuouslyInsideOneFrame()
    {
        Random.InitState(73);
        Pintando paint = CreatePainter(out GraffitiPainter painter, out RaycastHit hit);
        painter.brushMinSize = painter.brushMaxSize = 0.1f;
        painter.brushJitter = 0f;
        hit.distance = 0.55f;
        paint.BeginStrokeImmediately(hit);

        Vector3 start = hit.point;
        hit.point = start + Vector3.right * 1.2f;
        hit.distance = 6f;
        Invoke(paint, "SampleStroke", hit, 1f / 30f);

        var canvas = hit.collider.GetComponentInChildren<GraffitiSurfaceCanvas>();
        Invoke(canvas, "LateUpdate");
        Vector3[] vertices = canvas.GetComponent<MeshFilter>().sharedMesh.vertices;
        int quads = vertices.Length / 4;
        Assert.That(quads, Is.GreaterThan(6));

        float firstWidth = Vector3.Distance(vertices[0], vertices[1]);
        float previousWidth = firstWidth;
        for (int quad = 2; quad < quads; quad++)
        {
            float width = Vector3.Distance(vertices[quad * 4], vertices[quad * 4 + 1]);
            Assert.That(width + 0.0001f, Is.GreaterThanOrEqualTo(previousWidth));
            previousWidth = width;
        }
        Assert.That(previousWidth, Is.GreaterThan(firstWidth * 2.45f));
    }

    [Test] public void SprayRayUsesExactMeshNozzleAndRespectsHierarchyScale()
    {
        var parent = Create("Scaled camera parent");
        parent.transform.SetPositionAndRotation(new Vector3(2f, 1f, -3f), Quaternion.Euler(7f, 24f, -3f));
        parent.transform.localScale = Vector3.one * 1.7f;
        var obj = Create("Rotated imported spray can");
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = new Vector3(0.4f, -0.2f, 0.8f);
        obj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        obj.transform.localScale = Vector3.one * 8.017556f;
        var animation = obj.AddComponent<GraffitiAnimation>();
        animation.enabled = false;
        Ray ray = animation.GetNozzleRay();

        Vector3 expectedOrigin = obj.transform.TransformPoint(GraffitiAnimation.AuthoredNozzleLocalPosition);
        Vector3 expectedDirection = obj.transform.TransformDirection(Vector3.down).normalized;
        Assert.That(Vector3.Distance(ray.origin, expectedOrigin), Is.LessThan(0.00001f));
        Assert.That(Vector3.Angle(ray.direction, expectedDirection), Is.LessThan(0.001f));
        Assert.That(Vector3.Angle(animation.GetNozzleUp(),
            obj.transform.TransformDirection(Vector3.forward)), Is.LessThan(0.001f));
    }

    [Test] public void MenuRayFollowsPointerWithoutRotatingCamera()
    {
        var drawer = Create("Drawer").AddComponent<GraffitiMenuDrawer>();
        drawer.cam = Create("Camera").AddComponent<Camera>();
        drawer.cam.pixelRect = new Rect(0f, 0f, 800f, 600f);
        Quaternion initial = drawer.cam.transform.rotation;
        Assert.That(Vector3.Angle(drawer.GetSelectionRay(new Vector2(100f, 300f)).direction,
            drawer.GetSelectionRay(new Vector2(700f, 300f)).direction), Is.GreaterThan(20f));
        Assert.That(drawer.cam.transform.rotation, Is.EqualTo(initial));
    }

    [Test] public void FpsFindsMenuDrawerOnItsCameraChild()
    {
        var player = Create("Player");
        player.AddComponent<CharacterController>();
        var camera = Create("MainCamera");
        camera.transform.SetParent(player.transform);
        var drawer = camera.AddComponent<GraffitiMenuDrawer>();
        var fps = player.AddComponent<FPSController>();
        Invoke(fps, "Awake");
        var field = typeof(FPSController).GetField("graffitiMenu", Private);
        Assert.That(field.GetValue(fps), Is.SameAs(drawer));
    }

    [Test] public void DrawnMenuPresentationScalePreservesItsAnimatedTarget()
    {
        var obj = Create("Drawn Menu");
        obj.transform.localScale = Vector3.one * 0.06f;
        var menu = obj.AddComponent<GraffitiDrawMenu>();
        Invoke(menu, "Awake");
        menu.SetPresentationScale(5f);
        Assert.That(menu.visualRoot.localScale.x, Is.EqualTo(0.3f).Within(0.001f));
    }

    [Test] public void NozzlePreviewIsAComposedStrokeInsteadOfOneLargeDisk()
    {
        CreatePainter(out GraffitiPainter painter, out _);
        var obj = Create("Drawn Menu Preview");
        var menu = obj.AddComponent<GraffitiDrawMenu>();
        Invoke(menu, "Awake");
        menu.BindPainter(painter);

        Mesh mesh = (Mesh)typeof(GraffitiDrawMenu).GetField("previewMesh", Private).GetValue(menu);
        Assert.That(mesh, Is.Not.Null);
        Assert.That(mesh.vertexCount, Is.EqualTo(44));
        Assert.That(mesh.bounds.size.x, Is.GreaterThan(mesh.bounds.size.y * 2f));
    }

    [Test] public void GraffitiMenuUsesOriginalPresentationScaleByDefault()
    {
        var drawer = Create("Drawer at authored scale").AddComponent<GraffitiMenuDrawer>();
        Assert.That(drawer.menuScaleMultiplier, Is.EqualTo(1f));
    }

    [Test] public void WallCursorTracksPaintHitAndReplacesScreenMarker()
    {
        CreatePainter(out _, out RaycastHit hit);
        var cursor = Create("Wall cursor").AddComponent<GraffitiWallCursor>();
        Invoke(cursor, "Awake");
        cursor.SetTarget(hit, 0.2f, true);

        Assert.IsTrue(cursor.IsVisible);
        Assert.That(Vector3.Distance(cursor.transform.position, hit.point), Is.LessThan(0.03f));
        Assert.That(cursor.GetComponent<LineRenderer>().positionCount, Is.EqualTo(52));
        cursor.Hide();
        Assert.IsFalse(cursor.IsVisible);
    }

    [Test] public void EmptyAndReplacedCanNeverMutatesDepositedColors()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out RaycastHit hit);
        Invoke(painter.sprayResources, "Awake");
        paint.BeginStrokeImmediately(hit);
        var canvas = hit.collider.GetComponentInChildren<GraffitiSurfaceCanvas>();
        Invoke(canvas, "LateUpdate");
        Color32[] before = canvas.GetComponent<MeshFilter>().sharedMesh.colors32;
        painter.sprayResources.TickSpray(100f, GraffitiNozzleShape.FatCap);
        Assert.IsFalse(painter.sprayResources.HasPaint);
        Assert.IsTrue(painter.sprayResources.TryStartCanChange());
        Invoke(painter.sprayResources, "UpdateCanChange", 2f);
        Assert.That(painter.sprayResources.Paint, Is.EqualTo(100f));
        Assert.That(painter.sprayResources.OutputOpacity, Is.EqualTo(1f).Within(0.001f));
        CollectionAssert.AreEqual(before, canvas.GetComponent<MeshFilter>().sharedMesh.colors32);
    }

    [Test] public void CartoonAerosolBuildsFourBoundedBillboardLayersAndNeutralizesLegacyScale()
    {
        var owner = Create("Aerosol VFX owner");
        var root = Create("Aerosol VFX root");
        root.transform.localScale = Vector3.one * 0.22218f;
        var primary = root.AddComponent<ParticleSystem>();
        primary.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var vfx = owner.AddComponent<GraffitiSprayVFX>();

        Assert.That(vfx.Bind(primary), Is.SameAs(primary));
        Assert.IsTrue(vfx.IsConfigured);
        Assert.That(vfx.SystemCount, Is.EqualTo(4));
        Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));

        ParticleSystem[] layers = root.GetComponentsInChildren<ParticleSystem>(true);
        Assert.That(layers, Has.Length.EqualTo(4));
        int particleBudget = 0;
        foreach (ParticleSystem layer in layers)
        {
            particleBudget += layer.main.maxParticles;
            Assert.That(layer.main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Shape));
            var renderer = layer.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("FLOWSTATE/Graffiti/Cartoon Aerosol Particle"));
            Assert.That(renderer.renderMode, Is.EqualTo(ParticleSystemRenderMode.Billboard));
        }
        Assert.That(particleBudget, Is.LessThanOrEqualTo(402));
    }

    [Test] public void ChiselAerosolShapesThePuffsAndStartsAtTheNozzleRay()
    {
        var owner = Create("Chisel VFX owner");
        var root = Create("Chisel VFX root");
        var primary = root.AddComponent<ParticleSystem>();
        primary.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var vfx = owner.AddComponent<GraffitiSprayVFX>();
        vfx.Bind(primary);

        Ray ray = new Ray(new Vector3(2f, 3f, 4f), new Vector3(0.2f, -0.1f, 1f).normalized);
        vfx.UpdatePresentation(ray, 1.25f, 0.5f, Color.black,
            GraffitiNozzleShape.Chisel, 1f, Vector3.up);

        Assert.That(Vector3.Distance(vfx.EffectRoot.position, ray.origin), Is.LessThan(0.00001f));
        Assert.That(Vector3.Angle(vfx.EffectRoot.forward, ray.direction), Is.LessThan(0.001f));
        Assert.IsTrue(primary.main.startSize3D);
        Assert.That(primary.main.startSizeX.constantMin / primary.main.startSizeY.constantMin,
            Is.GreaterThan(3f));
    }

    [Test] public void AerosolNozzleProfilesPreserveReadableCapHierarchy()
    {
        Vector3 needle = GraffitiSprayVFX.GetNozzleVisualProfile(GraffitiNozzleShape.Needle);
        Vector3 soft = GraffitiSprayVFX.GetNozzleVisualProfile(GraffitiNozzleShape.SoftRound);
        Vector3 fat = GraffitiSprayVFX.GetNozzleVisualProfile(GraffitiNozzleShape.FatCap);

        Assert.That(needle.x, Is.LessThan(soft.x));
        Assert.That(fat.x, Is.GreaterThan(soft.x));
        Assert.That(fat.y, Is.GreaterThan(needle.y));
        Assert.That(fat.z, Is.GreaterThan(needle.z));
        Vector2 chisel = GraffitiSprayVFX.GetNozzleVisualAspect(GraffitiNozzleShape.Chisel);
        Assert.That(chisel.x / chisel.y, Is.GreaterThan(3f));
    }

}
#endif
