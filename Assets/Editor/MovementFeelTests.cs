#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class MovementFeelTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<GameObject> objects = new List<GameObject>();

    GameObject Create(string name)
    {
        var obj = new GameObject(name);
        objects.Add(obj);
        return obj;
    }

    FPSController Player()
    {
        var obj = Create("Movement feel test player");
        var capsule = obj.AddComponent<CharacterController>();
        var player = obj.AddComponent<FPSController>();
        player.enabled = false;
        Set(player, "controller", capsule);
        return player;
    }

    static void Set(FPSController p, string field, object value) => typeof(FPSController).GetField(field, Private).SetValue(p, value);
    static T Get<T>(FPSController p, string field) => (T)typeof(FPSController).GetField(field, Private).GetValue(p);
    static object Call(FPSController p, string method, params object[] args) => typeof(FPSController).GetMethod(method, Private).Invoke(p, args);

    [TearDown] public void Cleanup()
    {
        foreach (var obj in objects) if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
    }

    [TestCase(30)] [TestCase(60)] [TestCase(144)]
    public void OrbitingDuringSprintKeepsVelocityAlignedWithFeet(int fps)
    {
        var p = Player();
        for (int i = 0; i < fps * 2; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, i * 120f / fps, 0f) * Vector3.forward;
            Call(p, "AdvancePlanarMovement", direction, p.sprintSpeed, true, true, 1f / fps);
            Vector3 velocity = Get<Vector3>(p, "currentPlanarVelocity");
            if (velocity.sqrMagnitude > 0.01f)
                Assert.That(Vector3.Angle(velocity, p.transform.forward), Is.LessThan(0.1f));
        }
        Assert.That(Get<Vector3>(p, "currentPlanarVelocity").magnitude, Is.EqualTo(p.sprintSpeed).Within(0.01f));
    }

    [Test] public void ReleasingSprintBrakesSmoothlyWithinAThirdOfASecond()
    {
        var p = Player();
        Set(p, "currentPlanarVelocity", Vector3.forward * p.sprintSpeed);
        for (int i = 0; i < 20; i++) Call(p, "AdvancePlanarMovement", Vector3.zero, 0f, false, true, 1f / 60f);
        Assert.That(Get<Vector3>(p, "currentPlanarVelocity").magnitude, Is.LessThan(0.001f));
    }

    [Test] public void ReversalBrakesBeforeAcceleratingIntoNewHeading()
    {
        var p = Player();
        Set(p, "currentPlanarVelocity", Vector3.forward * p.sprintSpeed);
        Call(p, "AdvancePlanarMovement", Vector3.back, p.sprintSpeed, true, true, 1f / 60f);
        Assert.That(Get<Vector3>(p, "currentPlanarVelocity").magnitude, Is.LessThan(p.sprintSpeed));
        for (int i = 0; i < 45; i++) Call(p, "AdvancePlanarMovement", Vector3.back, p.sprintSpeed, true, true, 1f / 60f);
        Assert.That(Vector3.Angle(p.transform.forward, Vector3.back), Is.LessThan(0.1f));
        Assert.That(Get<Vector3>(p, "currentPlanarVelocity").magnitude, Is.EqualTo(p.sprintSpeed).Within(0.01f));
    }

    [Test] public void AirborneReleasePreservesMomentum()
    {
        var p = Player();
        Set(p, "currentPlanarVelocity", Vector3.forward * p.sprintSpeed);
        Call(p, "AdvancePlanarMovement", Vector3.zero, 0f, false, false, 0.1f);
        Assert.That(Get<Vector3>(p, "currentPlanarVelocity").magnitude, Is.GreaterThan(p.sprintSpeed * 0.95f));
    }

    void Camera(FPSController p)
    {
        var cam = Create("Movement feel test camera").AddComponent<Camera>();
        cam.enabled = false;
        cam.transform.SetParent(p.transform, false);
        p.cameraOverride = cam;
        p.thirdPersonLookAheadDistance = 0f;
        p.cameraCollisionMask = 0;
        Call(p, "ResolveReferences");
        Call(p, "ApplyModeInstant");
    }

    [Test] public void CameraTracksHorizontalTravelWithoutFollowLag()
    {
        var p = Player();
        Camera(p);
        p.transform.position += new Vector3(3f, 0f, 4f);
        Vector3 target = (Vector3)Call(p, "GetDesiredThirdPersonTarget", 1f / 60f);
        Assert.That(target.x, Is.EqualTo(p.transform.position.x).Within(0.001f));
        Assert.That(target.z, Is.EqualTo(p.transform.position.z).Within(0.001f));
    }

    [Test] public void PlayerRotationDoesNotDragTheChildCameraOrbit()
    {
        var p = Player();
        Camera(p);
        var cam = p.cameraOverride.transform;
        Vector3 before = cam.position;
        Quaternion rotation = cam.rotation;
        p.transform.rotation = Quaternion.Euler(0f, 120f, 0f);
        Call(p, "UpdateThirdPersonCamera", 1f / 60f);
        Assert.That(Vector3.Distance(before, cam.position), Is.LessThan(0.001f));
        Assert.That(Quaternion.Angle(rotation, cam.rotation), Is.LessThan(0.1f));
    }

    [Test] public void MovementUsesPresentedOrbitInsteadOfUnfilteredMouseTarget()
    {
        var p = Player();
        Set(p, "cameraStateInitialized", true);
        Set(p, "cameraYaw", 30f);
        Set(p, "orbitYaw", 100f);
        Vector3 direction = (Vector3)Call(p, "GetThirdPersonMoveDirection", Vector2.up);
        Assert.That(Vector3.Angle(direction, Quaternion.Euler(0f, 30f, 0f) * Vector3.forward), Is.LessThan(0.1f));
    }

    [Test] public void CollisionStopsLocomotionAnimationSpeed()
    {
        var p = Player();
        p.transform.position = new Vector3(250f, 1001f, 0f);
        var floor = Create("Movement feel test floor").AddComponent<BoxCollider>();
        floor.transform.position = new Vector3(250f, 999.9f, 0f);
        floor.size = new Vector3(10f, 0.2f, 20f);
        var wall = Create("Movement feel test wall").AddComponent<BoxCollider>();
        wall.transform.position = new Vector3(250f, 1001f, 2f);
        wall.size = new Vector3(8f, 5f, 0.5f);
        Physics.SyncTransforms();
        for (int i = 0; i < 120; i++) Call(p, "StepMovement", Vector2.up, true, false, 1f / 60f);
        Assert.That(Get<float>(p, "currentPlanarSpeed"), Is.LessThan(0.15f));
        Assert.That(p.transform.position.z, Is.LessThan(1.4f));
    }

    [Test] public void FasterTraversalKeepsTheAcceptedAnimationCadence()
    {
        var p = Player();
        float fasterWalk = p.walkSpeed;
        float fasterRun = p.sprintSpeed;
        p.walkSpeed = 1.25f;
        p.sprintSpeed = 4f;
        float acceptedWalkRate = (float)Call(p, "GetLocomotionPlaybackRate", p.walkSpeed);
        float acceptedRunRate = (float)Call(p, "GetLocomotionPlaybackRate", p.sprintSpeed);
        p.walkSpeed = fasterWalk;
        p.sprintSpeed = fasterRun;
        Assert.That(fasterWalk, Is.GreaterThan(1.25f));
        Assert.That(fasterRun, Is.GreaterThan(4f));
        Assert.That((float)Call(p, "GetLocomotionPlaybackRate", fasterWalk), Is.EqualTo(acceptedWalkRate).Within(0.02f));
        Assert.That((float)Call(p, "GetLocomotionPlaybackRate", fasterRun), Is.EqualTo(acceptedRunRate).Within(0.02f));
    }

    [TestCase(1.25f)] [TestCase(4f)] [TestCase(8f)] [TestCase(100f)]
    public void LocomotionNeverAcceleratesTheAuthoredClipsBeyondNaturalCadence(float speed)
    {
        float rate = (float)Call(Player(), "GetLocomotionPlaybackRate", speed);
        Assert.That(rate, Is.LessThanOrEqualTo(1.15f));
    }
}
#endif
