#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ParkourDetectionTests
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<GameObject> objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }

        objects.Clear();
    }

    GameObject Create(string name)
    {
        var obj = new GameObject(name);
        objects.Add(obj);
        return obj;
    }

    FPSController CreateController(Vector3 position)
    {
        GameObject player = Create("Parkour test player");
        player.transform.position = position;
        CharacterController characterController = player.AddComponent<CharacterController>();
        var controller = player.AddComponent<FPSController>();
        controller.enabled = false;
        typeof(FPSController).GetField("controller", PrivateInstance).SetValue(controller, characterController);
        return controller;
    }

    static object Invoke(FPSController controller, string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(FPSController).GetMethod(methodName, PrivateInstance);
        Assert.That(method, Is.Not.Null, "Missing private parkour method " + methodName);
        return method.Invoke(controller, arguments);
    }

    [Test]
    public void ClimbDetectionAcceptsExplicitTriggerSensor()
    {
        int parkourLayer = LayerMask.NameToLayer("Parkour");
        Assert.That(parkourLayer, Is.GreaterThanOrEqualTo(0));

        Vector3 testOrigin = new Vector3(0f, 10000f, 0f);
        FPSController controller = CreateController(testOrigin);
        controller.parkourMask = 1 << parkourLayer;
        controller.requireClimbableMarker = true;

        GameObject sensor = Create("Parkour test climb sensor");
        sensor.layer = parkourLayer;
        sensor.transform.position = testOrigin + Vector3.forward;
        var box = sensor.AddComponent<BoxCollider>();
        box.size = new Vector3(3f, 4f, 0.18f);
        box.isTrigger = true;
        sensor.AddComponent<ClimbableSurface>();
        Physics.SyncTransforms();

        object[] arguments = { testOrigin, Vector3.forward, default(RaycastHit), null };
        bool found = (bool)Invoke(controller, "TryFindClimbSurface", arguments);

        Assert.That(found, Is.True);
        Assert.That(((RaycastHit)arguments[2]).collider, Is.SameAs(box));
        Assert.That(arguments[3], Is.SameAs(sensor.GetComponent<ClimbableSurface>()));
    }

    [Test]
    public void LandingProbeUsesMovementLayersInsteadOfParkourLayers()
    {
        Vector3 testPosition = new Vector3(30f, 20000f, 0f);
        FPSController controller = CreateController(testPosition + Vector3.up);
        controller.parkourMask = 1 << LayerMask.NameToLayer("Parkour");
        controller.movementCollisionMask = 1 << LayerMask.NameToLayer("Default");

        GameObject ground = Create("Parkour test default ground");
        ground.layer = LayerMask.NameToLayer("Default");
        ground.transform.position = testPosition;
        var box = ground.AddComponent<BoxCollider>();
        box.size = new Vector3(8f, 0.2f, 8f);
        Physics.SyncTransforms();

        object[] arguments = { testPosition + Vector3.up * 0.25f, Vector3.zero };
        bool found = (bool)Invoke(controller, "TryFindGround", arguments);

        Assert.That(found, Is.True);
        Assert.That(((Vector3)arguments[1]).y, Is.GreaterThan(box.bounds.max.y));
    }

    [Test]
    public void CompoundParkourBoundsIncludeSiblingColliders()
    {
        GameObject root = Create("Parkour test compound obstacle");
        root.transform.position = new Vector3(0f, 30000f, 0f);
        root.AddComponent<ClimbableSurface>();

        GameObject left = Create("Left detail");
        left.transform.SetParent(root.transform, false);
        left.transform.localPosition = new Vector3(-2f, 0f, 0f);
        var leftCollider = left.AddComponent<BoxCollider>();

        GameObject right = Create("Right detail");
        right.transform.SetParent(root.transform, false);
        right.transform.localPosition = new Vector3(2f, 2f, 0f);
        right.AddComponent<BoxCollider>();
        Physics.SyncTransforms();

        Bounds bounds = (Bounds)Invoke(CreateController(root.transform.position), "GetParkourSurfaceBounds", leftCollider);

        Assert.That(bounds.min.x, Is.LessThanOrEqualTo(leftCollider.bounds.min.x));
        Assert.That(bounds.max.x, Is.GreaterThanOrEqualTo(right.GetComponent<BoxCollider>().bounds.max.x));
        Assert.That(bounds.max.y, Is.GreaterThanOrEqualTo(right.GetComponent<BoxCollider>().bounds.max.y));
    }

    [Test]
    public void ThirdPersonCameraLookAheadIgnoresPureStrafe()
    {
        FPSController controller = CreateController(new Vector3(0f, 40000f, 0f));
        controller.thirdPersonLookAheadDistance = 0.65f;
        controller.thirdPersonLateralLookAheadMultiplier = 0f;
        controller.sprintSpeed = 8f;

        Vector3 lookAhead = (Vector3)Invoke(controller, "CalculateThirdPersonLookAhead", Vector3.right * 8f, 0f);

        Assert.That(lookAhead.sqrMagnitude, Is.LessThan(0.000001f));
    }

    [Test]
    public void ThirdPersonCameraLookAheadKeepsForwardAnticipation()
    {
        FPSController controller = CreateController(new Vector3(0f, 41000f, 0f));
        controller.thirdPersonLookAheadDistance = 0.65f;
        controller.sprintSpeed = 8f;

        Vector3 lookAhead = (Vector3)Invoke(controller, "CalculateThirdPersonLookAhead", Vector3.forward * 8f, 0f);

        Assert.That(lookAhead.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(lookAhead.z, Is.EqualTo(0.65f).Within(0.0001f));
    }

    [Test]
    public void GroundProbeBridgesAStairSizedDrop()
    {
        float groundTop = 42000f;
        FPSController controller = CreateController(new Vector3(0f, groundTop + 1.2f, 0f));
        controller.movementCollisionMask = 1 << LayerMask.NameToLayer("Default");
        controller.groundProbeRadiusScale = 0.74f;

        GameObject ground = Create("Ground probe test floor");
        ground.transform.position = new Vector3(0f, groundTop - 0.1f, 0f);
        var box = ground.AddComponent<BoxCollider>();
        box.size = new Vector3(6f, 0.2f, 6f);
        Physics.SyncTransforms();

        object[] arguments = { 0.52f, default(RaycastHit), 0f };
        bool found = (bool)Invoke(controller, "TryProbeWalkableGround", arguments);

        Assert.That(found, Is.True);
        Assert.That((float)arguments[2], Is.EqualTo(0.2f).Within(0.04f));
    }

    [Test]
    public void GroundedStepOffsetHasMarginForGameplayStairRise()
    {
        FPSController controller = CreateController(new Vector3(0f, 42500f, 0f));
        controller.groundedStepOffset = 0.52f;

        Invoke(controller, "SetControllerStepOffset", true);

        CharacterController characterController = controller.GetComponent<CharacterController>();
        Assert.That(characterController.stepOffset, Is.EqualTo(0.52f).Within(0.0001f));
        Assert.That(characterController.stepOffset, Is.GreaterThan(0.451f));
    }

    [Test]
    public void StairRiserIsHandledAsWalkableStepInsteadOfParkourWall()
    {
        float playerY = 43001f;
        FPSController controller = CreateController(new Vector3(0f, playerY, 0f));
        int parkourLayer = LayerMask.NameToLayer("Parkour");
        controller.movementCollisionMask = 1 << parkourLayer;
        controller.groundedStepOffset = 0.52f;
        controller.stepHeightTolerance = 0.06f;

        GameObject step = Create("Walkable stair test step");
        step.layer = parkourLayer;
        step.transform.position = new Vector3(0f, playerY - 1f + 0.2f, 1f);
        var box = step.AddComponent<BoxCollider>();
        box.size = new Vector3(3f, 0.4f, 1f);
        Physics.SyncTransforms();

        Assert.That(Physics.Raycast(new Vector3(0f, playerY - 0.8f, 0f), Vector3.forward, out RaycastHit wallHit, 2f, 1 << parkourLayer), Is.True);
        bool walkableStep = (bool)Invoke(controller, "IsWalkableStepAhead", wallHit, Vector3.forward);

        Assert.That(walkableStep, Is.True);
    }

    [Test]
    public void FallingAnimationRequiresMeaningfulVerticalDrop()
    {
        MethodInfo method = typeof(FPSController).GetMethod("ShouldPlayFallingAnimation", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        bool tinyDrop = (bool)method.Invoke(null, new object[] { true, false, 0.2f, 0.12f, 0.08f, 0.35f, -2f, -0.85f });
        bool realFall = (bool)method.Invoke(null, new object[] { true, false, 0.2f, 0.12f, 0.5f, 0.35f, -2f, -0.85f });

        Assert.That(tinyDrop, Is.False);
        Assert.That(realFall, Is.True);
    }

    [Test]
    public void InitialVisualGroundAlignmentWaitsUntilGroundContact()
    {
        FPSController controller = CreateController(new Vector3(0f, 44000f, 0f));
        FieldInfo attempts = typeof(FPSController).GetField("groundAlignmentFramesRemaining", PrivateInstance);
        Assert.That(attempts, Is.Not.Null);
        attempts.SetValue(controller, 12);

        for (int index = 0; index < 24; index++)
            Invoke(controller, "UpdateInitialGroundAlignment");

        Assert.That((int)attempts.GetValue(controller), Is.EqualTo(12));
    }

    [Test]
    public void TeleportRearmsVisualGroundAlignment()
    {
        FPSController controller = CreateController(new Vector3(0f, 45000f, 0f));
        FieldInfo attempts = typeof(FPSController).GetField("groundAlignmentFramesRemaining", PrivateInstance);
        Assert.That(attempts, Is.Not.Null);
        attempts.SetValue(controller, 0);
        controller.alignFeetToGroundOnStart = true;

        controller.TeleportTo(new Vector3(2f, 45000f, 3f), Quaternion.identity);

        Assert.That((int)attempts.GetValue(controller), Is.EqualTo(12));
    }
}
#endif
