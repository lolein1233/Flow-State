#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FlowState.Playground.Editor
{
    public sealed class PlaygroundMechanicsTests
    {
        readonly List<GameObject> created = new List<GameObject>();
        GameObject New(string name) { var obj = new GameObject(name); created.Add(obj); return obj; }
        static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        SlidingMechanism Door()
        {
            var obj = New("Test door"); obj.transform.position = new Vector3(1000f, 1000f, 1000f);
            var door = obj.AddComponent<SlidingMechanism>(); door.localTravel = new Vector3(0f, 5f, 0f); door.duration = 0.5f;
            Call(door, "Awake"); return door;
        }
        [TearDown] public void Cleanup() { foreach (var obj in created) if (obj != null) Object.DestroyImmediate(obj); created.Clear(); }

        [Test] public void DoorRejectsRepeatedInputsDuringBothTransitions()
        {
            var door = Door(); int opened = 0, closed = 0;
            door.opened.AddListener(() => opened++); door.closed.AddListener(() => closed++);
            Assert.That(door.TryInteract(), Is.True);
            for (int i = 0; i < 10; i++) Assert.That(door.TryInteract(), Is.False);
            Assert.That(door.State, Is.EqualTo(MechanismState.Opening));
            for (int i = 0; i < 40; i++) Call(door, "FixedUpdate");
            Assert.That(door.State, Is.EqualTo(MechanismState.Open)); Assert.That(opened, Is.EqualTo(1));
            Assert.That(door.TryInteract(), Is.True);
            Assert.That(door.TryInteract(), Is.False);
            for (int i = 0; i < 40; i++) Call(door, "FixedUpdate");
            Assert.That(door.State, Is.EqualTo(MechanismState.Closed)); Assert.That(closed, Is.EqualTo(1));
        }
        [Test] public void CloseReopensInsteadOfCrushingACharacter()
        {
            var door = Door();
            door.RequestOpen(); for (int i = 0; i < 40; i++) Call(door, "FixedUpdate");
            // Kinematic MovePosition is applied at the physics tick; put the body at the evaluated endpoint for this EditMode overlap check.
            door.transform.position = new Vector3(1000f, 1005f, 1000f);
            var player = New("Obstructing controller"); player.AddComponent<CharacterController>(); player.transform.position = new Vector3(1000f, 1004.8f, 1000f);
            Physics.SyncTransforms();
            door.RequestClose(); Call(door, "FixedUpdate");
            Assert.That(door.State, Is.EqualTo(MechanismState.Opening));
            Assert.That(door.Progress, Is.EqualTo(1f));
        }
        [Test] public void ButtonWithoutCapAndUnassignedHandleFailSafely()
        {
            var button = New("Button").AddComponent<MechanicalButton>();
            Assert.That(button.TryInteract(), Is.False);
            var handle = New("Handle").AddComponent<MechanismHandle>();
            Assert.That(handle.TryInteract(), Is.False);
        }
        [Test] public void IndependentHandleDoesNotAcceptCommandsDuringMotion()
        {
            var door = Door(); var handle = New("Handle").AddComponent<MechanismHandle>(); handle.mechanism = door;
            Assert.That(handle.TryInteract(), Is.True); Assert.That(handle.TryInteract(), Is.False);
            Assert.That(door.State, Is.EqualTo(MechanismState.Opening));
        }
        [Test] public void PushRequestsAreBoundedAndCannotAddVerticalImpulse()
        {
            var obj = New("Crate"); var body = obj.AddComponent<Rigidbody>(); body.useGravity = false;
            var push = obj.AddComponent<PushableBody>(); Call(push, "Awake");
            body.linearVelocity = new Vector3(20f, 0f, 20f);
            for (int i = 0; i < 200; i++) push.Push(new Vector3(0f, 90f, 1f));
            Call(push, "FixedUpdate");
            Assert.That(Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude, Is.LessThanOrEqualTo(push.maximumSpeed + 0.001f));
            Assert.That(body.linearVelocity.y, Is.EqualTo(0f));
        }
    }
}
#endif
