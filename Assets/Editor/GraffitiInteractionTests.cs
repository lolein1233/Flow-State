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

    [Test] public void WheelDepthControlsFootprintAtTheSameWallDistance()
    {
        Pintando paint = CreatePainter(out GraffitiPainter painter, out _);
        Set(painter, "physicalControlWasActive", true);

        Set(painter, "smoothedDepth", 1f);
        float closeWidth = paint.EstimateBrushWidth(3f);
        Assert.That(painter.SprayCone01, Is.EqualTo(0f).Within(0.001f));

        Set(painter, "smoothedDepth", -1f);
        float farWidth = paint.EstimateBrushWidth(3f);
        Assert.That(painter.SprayCone01, Is.EqualTo(1f).Within(0.001f));
        Assert.That(farWidth, Is.GreaterThan(closeWidth * 3.5f));
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

    [Test] public void CanOrientationDrivesSprayDespiteImportedMeshRotation()
    {
        var obj = Create("Rotated imported spray can");
        obj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        var animation = obj.AddComponent<GraffitiAnimation>();
        animation.enabled = false;
        // EditMode does not call Awake for ordinary MonoBehaviours.
        Invoke(animation, "Awake");
        obj.transform.rotation = Quaternion.Euler(0f, 22f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
        Ray ray = animation.GetNozzleRay(new Vector3(0f, 0.19f, 0.08f));
        Assert.That(Vector3.Angle(ray.direction, Quaternion.Euler(0f, 22f, 0f) * Vector3.forward), Is.LessThan(0.01f));
        Assert.That(Vector3.Distance(ray.origin, obj.transform.position), Is.GreaterThan(0.2f));
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

}
#endif
