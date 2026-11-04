#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowState.Playground.Editor
{
    public sealed class PlaygroundRuntimeTests
    {
        // EnterPlayMode keeps a direct reference to Assembly-CSharp without changing the project's assembly boundaries.
        [UnityTest]
        public IEnumerator ContinuousRouteAndCollisionSafetyInRealPlayMode()
        {
            EditorSceneManager.OpenScene(PlaygroundSceneBuilder.ScenePath);
            yield return new EnterPlayMode();
            bool runInBackground = Application.runInBackground;
            Application.runInBackground = true;
            var player = Object.FindFirstObjectByType<FPSController>();
            var bridge = player.GetComponent<PlaygroundPlayerBridge>();
            var driver = player.gameObject.AddComponent<PlaygroundAcceptanceDriver>();
            float deadline = Time.realtimeSinceStartup + 100f;
            while (!driver.Finished && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(driver.Finished && driver.Passed, Is.True, driver.Failure ?? driver.Phase);
            Assert.That(Object.FindFirstObjectByType<PlaygroundGoal>().Complete, Is.True);

            // Below are isolated collision probes, after the no-teleport acceptance route has completed.
            var gate = Object.FindObjectsByType<SlidingMechanism>(FindObjectsSortMode.None).First(d => !d.manual);
            player.enabled = false;
            player.TeleportTo(new Vector3(4.7f, 1.54f, 13f), Quaternion.identity);
            gate.RequestClose();
            yield return WaitGameTime(1.5f);
            Assert.That(gate.State == MechanismState.Opening || gate.State == MechanismState.Open, Is.True,
                "Closing gate must reverse when its next swept step meets the real character.");
            yield return WaitGameTime(1f);
            Assert.That(player.transform.position.y, Is.EqualTo(1.54f).Within(0.1f));
            player.TeleportTo(new Vector3(4.7f, 1.54f, 10f), Quaternion.identity);
            yield return WaitGameTime(2.4f);
            Assert.That(gate.State, Is.EqualTo(MechanismState.Open));
            gate.RequestClose(); yield return WaitGameTime(2.4f);
            Assert.That(gate.State, Is.EqualTo(MechanismState.Closed));
            var controller = player.GetComponent<CharacterController>();
            controller.Move(Vector3.forward * 7f);
            Assert.That(player.transform.position.z, Is.LessThan(12.2f), "Closed gate collider must stop the actual capsule.");

            player.TeleportTo(new Vector3(-4f, 1.54f, 7f), Quaternion.identity);
            Assert.That(bridge.TryUseFocused(), Is.False, "Ground-level interaction cannot reach the elevated switch through its platform.");
            player.TeleportTo(new Vector3(0f, 1.54f, -12f), Quaternion.identity);
            Assert.That(bridge.TryUseFocused(), Is.False, "Distant interaction cannot activate a door or switch.");
            var button = Object.FindFirstObjectByType<MechanicalButton>();
            int count = button.ActivationCount;
            Assert.That(button.TryInteract(), Is.True);
            Time.timeScale = 0f;
            float pauseUntil = Time.realtimeSinceStartup + 0.25f;
            while (Time.realtimeSinceStartup < pauseUntil) yield return null;
            Assert.That(button.ActivationCount, Is.EqualTo(count), "Paused button must not emit its contact event.");
            Time.timeScale = 1f; yield return WaitGameTime(0.45f);
            Assert.That(button.ActivationCount, Is.EqualTo(count + 1));
            Application.runInBackground = runInBackground;
            yield return new ExitPlayMode();
        }
        static IEnumerator WaitGameTime(float seconds)
        {
            // This is an EditMode test across EnterPlayMode. Its runner does not schedule WaitForSeconds.
            float until = Time.time + seconds;
            while (Time.time < until) yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Application.isPlaying) { Time.timeScale = 1f; yield return new ExitPlayMode(); }
        }
    }

    // EnterPlayMode reloads the domain, which can lose external tooling callbacks. Re-register a local XML reporter.
    [UnityEditor.InitializeOnLoad]
    public static class PlaygroundTestResults
    {
        static readonly TestRunnerApi api;
        static PlaygroundTestResults()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
        }
        sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.FullName.StartsWith("FlowState.Playground.")) return;
                Directory.CreateDirectory("Logs/CodexInteractivePlayground");
                File.WriteAllText("Logs/CodexInteractivePlayground/" + result.Name + ".txt", result.ResultState + "\n" + result.FullName + "\n" + result.Message + "\n" + result.StackTrace);
            }
            public void RunFinished(ITestResultAdaptor results)
            {
                if (!ContainsPlayground(results)) return;
                Directory.CreateDirectory("Logs/CodexInteractivePlayground");
                TestRunnerApi.SaveResultToFile(results, "Logs/CodexInteractivePlayground/playground-nunit.xml");
            }
            static bool ContainsPlayground(ITestResultAdaptor result) => result.FullName.StartsWith("FlowState.Playground.") || result.Children.Any(ContainsPlayground);
        }
    }
}
#endif
