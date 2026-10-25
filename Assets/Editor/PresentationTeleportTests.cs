#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class PresentationTeleportTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    readonly Vector3 origin = new Vector3(0f, 50000f, 0f);

    GameObject Create(string name, Vector3 position)
    {
        var obj = new GameObject(name);
        obj.transform.position = position;
        objects.Add(obj);
        return obj;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
    }

    FPSController CreatePlayer()
    {
        var player = Create("Presentation test player", origin + Vector3.up * 10f);
        player.transform.localScale = Vector3.one * 1.5f;
        var capsule = player.AddComponent<CharacterController>();
        capsule.height = 2f;
        capsule.radius = 0.5f;
        capsule.center = new Vector3(0f, 0.2f, 0f);
        var controller = player.AddComponent<FPSController>();
        controller.enabled = false;
        return controller;
    }

    BoxCollider CreateFloor()
    {
        var floor = Create("Presentation test floor", origin - Vector3.up * 0.5f);
        var collider = floor.AddComponent<BoxCollider>();
        collider.size = new Vector3(30f, 1f, 30f);
        return collider;
    }

    [Test]
    public void GroundMarkerAccountsForScaledCapsuleCenterAndFacing()
    {
        FPSController player = CreatePlayer();
        BoxCollider floor = CreateFloor();
        Transform marker = Create("Lugar", origin - Vector3.up * 0.4f).transform;
        marker.rotation = Quaternion.Euler(0f, 127f, 0f);

        Assert.That(player.TryTeleportToGround(marker), Is.True);
        var capsule = player.GetComponent<CharacterController>();
        Assert.That(capsule.bounds.min.y - floor.bounds.max.y, Is.EqualTo(player.landingClearance).Within(0.015f));
        Assert.That(Quaternion.Angle(player.transform.rotation, marker.rotation), Is.LessThan(0.01f));
        Assert.That(capsule.enabled, Is.True);
    }

    [Test]
    public void RepeatedRoundTripsDoNotAccumulateHeightOrPositionOffsets()
    {
        FPSController player = CreatePlayer();
        CreateFloor();
        var commands = player.gameObject.AddComponent<PresentationLocationCommands>();
        commands.player = player;
        commands.lugar1 = Create("Lugar 1", origin + Vector3.left * 5f).transform;
        commands.lugar2 = Create("Lugar 2", origin + Vector3.right * 5f).transform;

        Assert.That(commands.GoToLugar(1), Is.True);
        Vector3 firstArrival = player.transform.position;
        for (int i = 0; i < 5; i++)
        {
            Assert.That(commands.GoToLugar(2), Is.True);
            Assert.That(player.transform.position.x, Is.EqualTo(commands.lugar2.position.x).Within(0.001f));
            Assert.That(commands.GoToLugar(1), Is.True);
            Assert.That(Vector3.Distance(player.transform.position, firstArrival), Is.LessThan(0.01f));
        }
    }

    [Test]
    public void MissingGroundLeavesPlayerAtOriginalPosition()
    {
        FPSController player = CreatePlayer();
        Transform marker = Create("Unsupported marker", origin).transform;
        Vector3 before = player.transform.position;

        Assert.That(player.TryTeleportToGround(marker), Is.False);
        Assert.That(player.transform.position, Is.EqualTo(before));
        Assert.That(player.TryTeleportToGround(null), Is.False);
    }

    [Test]
    public void ObstructedArrivalLeavesPlayerAtOriginalPosition()
    {
        FPSController player = CreatePlayer();
        CreateFloor();
        Transform marker = Create("Blocked marker", origin).transform;
        var obstruction = Create("Arrival wall", origin + new Vector3(0.6f, 2f, 0f)).AddComponent<BoxCollider>();
        obstruction.size = new Vector3(0.2f, 4f, 4f);
        Vector3 before = player.transform.position;

        Assert.That(player.TryTeleportToGround(marker), Is.False);
        Assert.That(player.transform.position, Is.EqualTo(before));
    }
}
#endif
