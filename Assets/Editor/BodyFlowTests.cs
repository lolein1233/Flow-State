#if UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using FlowState.BodyFlow;
using NUnit.Framework;
using UnityEngine;

public sealed class BodyFlowTests
{
    [TestCase(0f, false)]
    [TestCase(0.01f, false)]
    [TestCase(0.5f, true)]
    [TestCase(1f, true)]
    [TestCase(2f, false)]
    public void TwoBoneSolverPreservesLengthsAtAndBeyondReach(float distance, bool expectedReached)
    {
        bool reached = BodyFlowKinematics.SolveTwoBone(Vector3.zero, Vector3.down * distance, Vector3.forward,
            0.57f, 0.45f, 8f, 125f, out Vector3 joint, out Vector3 end);
        Assert.That(joint.magnitude, Is.EqualTo(0.57f).Within(0.0001f));
        Assert.That(Vector3.Distance(joint, end), Is.EqualTo(0.45f).Within(0.0001f));
        Assert.That(joint.z, Is.GreaterThan(0f));
        Assert.That(reached, Is.EqualTo(expectedReached));
    }

    [Test]
    public void InvalidChainAndNonFiniteTargetReturnFiniteFallback()
    {
        Vector3 root = Vector3.one;
        bool reached = BodyFlowKinematics.SolveTwoBone(root, Vector3.one * float.NaN, Vector3.forward,
            0f, 0f, 8f, 125f, out Vector3 joint, out Vector3 end);
        Assert.That(reached, Is.False);
        Assert.That(joint, Is.EqualTo(root));
        Assert.That(end, Is.EqualTo(root));
    }

    [Test]
    public void WalkSupportWindowWrapsAndDoesNotLatchTheSwing()
    {
        var rightStance = new Vector2(0.86f, 0.39f);
        Assert.That(BodyFlowKinematics.SupportWeight(0.95f, rightStance, 0.06f), Is.EqualTo(1f));
        Assert.That(BodyFlowKinematics.SupportWeight(1.10f, rightStance, 0.06f), Is.EqualTo(1f));
        Assert.That(BodyFlowKinematics.SupportWeight(1.60f, rightStance, 0.06f), Is.Zero);
        Assert.That(BodyFlowKinematics.SupportWeight(0.89f, rightStance, 0.06f), Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void DownwardContactCannotStraightenTheAuthoredKnee()
    {
        Vector3 upper = new Vector3(0f, -0.45f, 0.27f);
        Vector3 ankle = Vector3.down * 0.9f;
        const float extensionLimit = 3f;
        bool adapted = BodyFlowKinematics.TryAdaptLimb(Vector3.zero, upper, ankle, Vector3.down * 0.20f,
            Vector3.forward, extensionLimit, 16f, 15f, out Vector3 joint, out Vector3 end);
        Assert.That(adapted, Is.True);
        float original = Vector3.Angle(upper, ankle - upper);
        float result = Vector3.Angle(joint, end - joint);
        Assert.That(result, Is.GreaterThanOrEqualTo(original - extensionLimit - 0.001f));
        Assert.That(joint.magnitude, Is.EqualTo(upper.magnitude).Within(0.0001f));
        Assert.That((end - joint).magnitude, Is.EqualTo((ankle - upper).magnitude).Within(0.0001f));
        Assert.That(joint.z, Is.Positive);
    }

    [Test]
    public void NoNearbySupportKeepsAuthoredJointAndAnkle()
    {
        Vector3 upper = new Vector3(0.04f, -0.44f, 0.21f);
        Vector3 ankle = Vector3.down * 0.88f;
        bool adapted = BodyFlowKinematics.TryAdaptLimb(Vector3.zero, upper, ankle, Vector3.zero,
            Vector3.forward, 3f, 16f, 15f, out Vector3 joint, out Vector3 end);
        Assert.That(adapted, Is.False);
        Assert.That(joint, Is.EqualTo(upper));
        Assert.That(end, Is.EqualTo(ankle));
    }

    [TestCase(0f, 0f)]
    [TestCase(3f, 1.8f)]
    [TestCase(4.5f, 3.65f)]
    [TestCase(6f, 5.5f)]
    public void LocomotionMapsActualWalkAndSprintToTheirAuthoredBlendKeys(float speed, float expected)
    {
        var profile = ScriptableObject.CreateInstance<BodyFlowProfile>();
        try
        {
            Assert.That(profile.MapLocomotionSpeed(speed, 3f, 6f), Is.EqualTo(expected).Within(0.001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void PlantedContactFollowsSurfacePoseAndExpiresWhenDisabled()
    {
        var surface = new GameObject("BODYFLOW test surface");
        try
        {
            var collider = surface.AddComponent<BoxCollider>();
            var gait = typeof(BodyFlowController).Assembly.GetType("FlowState.BodyFlow.BodyFlowGait");
            var contactType = gait.GetNestedType("Contact", BindingFlags.NonPublic);
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
            Vector3 localAnchor = new Vector3(0.4f, 0.5f, -0.3f);
            object contact = Activator.CreateInstance(contactType, instance, null,
                new object[] { collider, localAnchor, Quaternion.identity }, null);
            surface.transform.SetPositionAndRotation(new Vector3(2f, 0.3f, -4f), Quaternion.Euler(10f, 70f, 0f));

            var pose = (Pose)contactType.GetMethod("Read", instance).Invoke(contact, null);
            Assert.That(Vector3.Distance(pose.position, surface.transform.TransformPoint(localAnchor)), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(pose.rotation, surface.transform.rotation), Is.LessThan(0.05f));

            collider.enabled = false;
            Assert.That(contactType.GetProperty("IsValid", instance).GetValue(contact), Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(surface);
        }
    }
}
#endif
