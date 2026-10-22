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
}
#endif
