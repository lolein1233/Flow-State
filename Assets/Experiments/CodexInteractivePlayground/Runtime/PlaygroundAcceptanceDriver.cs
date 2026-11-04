#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FlowState.Playground
{
    // Temporary QA component. Never placed in the saved scene or included in a Player build.
    // Drives the real FPSController.StepMovement seam with synthetic inputs; it does not replace collision or jump code.
    public sealed class PlaygroundAcceptanceDriver : MonoBehaviour
    {
        public string Phase { get; private set; } = "Starting";
        public bool Finished { get; private set; }
        public bool Passed { get; private set; }
        public string Failure { get; private set; }
        public List<string> evidence = new List<string>();
        FPSController player;
        PlaygroundPlayerBridge bridge;
        SlidingMechanism access, gate;
        MechanicalButton button;
        PushableBody crate;
        PlaygroundGoal goal;
        MethodInfo movement, animate, cameraUpdate;
        FieldInfo yaw, orbit;
        Vector2 input;
        bool jump;
        float direction;
        Coroutine routine;
        bool wasEnabled;
        string output;

        void Start()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Experiments/CodexInteractivePlayground/InteractivePlayground.unity") { Destroy(this); return; }
            output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs/CodexInteractivePlayground"); Directory.CreateDirectory(output);
            player = GetComponent<FPSController>(); bridge = GetComponent<PlaygroundPlayerBridge>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            movement = typeof(FPSController).GetMethod("StepMovement", flags); animate = typeof(FPSController).GetMethod("UpdateAnimator", flags);
            cameraUpdate = typeof(FPSController).GetMethod("UpdateCamera", flags);
            yaw = typeof(FPSController).GetField("cameraYaw", flags); orbit = typeof(FPSController).GetField("orbitYaw", flags);
            access = FindObjectsByType<SlidingMechanism>(FindObjectsSortMode.None).First(x => x.manual);
            gate = FindObjectsByType<SlidingMechanism>(FindObjectsSortMode.None).First(x => !x.manual);
            button = FindFirstObjectByType<MechanicalButton>(); crate = FindFirstObjectByType<PushableBody>(); goal = FindFirstObjectByType<PlaygroundGoal>();
            wasEnabled = player.enabled; player.enabled = false;
            routine = StartCoroutine(Run());
        }
        void Update()
        {
            if (Finished || player == null) return;
            try
            {
                yaw.SetValue(player, direction); orbit.SetValue(player, direction);
                movement.Invoke(player, new object[] { input, false, jump, Time.deltaTime }); jump = false;
                animate.Invoke(player, null);
            }
            catch (Exception error) { Fail(error.ToString()); }
        }
        void LateUpdate() { if (!Finished && player != null) cameraUpdate.Invoke(player, null); }
        void Require(bool condition, string message)
        {
            if (!condition) { Fail(message); throw new InvalidOperationException(message); }
            evidence.Add(message);
        }
        void Fail(string message)
        {
            if (Finished) return;
            Failure = message; Finished = true; Passed = false; input = Vector2.zero;
            if (routine != null) StopCoroutine(routine);
            if (player != null) player.enabled = wasEnabled;
            Save(); Debug.LogError("[Playground QA] " + message, this);
        }
        void Save() { File.WriteAllText(Path.Combine(output, "runtime-acceptance.json"), JsonUtility.ToJson(new Report { passed = Passed, phase = Phase, failure = Failure, evidence = evidence.ToArray() }, true)); }
        [Serializable] sealed class Report { public bool passed; public string phase; public string failure; public string[] evidence; }
        IEnumerator Walk(Vector3 target, float timeout = 12f)
        {
            float until = Time.time + timeout;
            while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.x, target.z)) > 0.12f && Time.time < until)
            {
                var delta = target - transform.position; direction = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                float distance = new Vector2(delta.x, delta.z).magnitude;
                input = Vector2.up * Mathf.Clamp(distance * 2f, 0.12f, 1f); yield return null;
            }
            input = Vector2.zero; yield return new WaitForSeconds(0.25f);
            Require(Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.x, target.z)) < 0.35f, "Reached " + target + " using FPSController.StepMovement");
        }
        IEnumerator Run()
        {
            yield return new WaitForSeconds(0.4f);
            Phase = "Access interaction";
            yield return Walk(new Vector3(2.9f, 0f, -8.75f)); direction = 0f; yield return new WaitForSeconds(0.35f);
            Capture("01-access");
            Require(bridge.TryUseFocused(), "Access accepted through reach, view and line-of-sight checks");
            Require(access.State == MechanismState.Opening, "Access enters Opening");
            Require(!bridge.TryUseFocused() && !access.TryInteract(), "Repeated input rejected while opening");
            yield return new WaitForSeconds(1.8f);
            Require(access.State == MechanismState.Open, "Access reaches Open");
            yield return Walk(new Vector3(0f, 0f, -8.8f)); yield return Walk(new Vector3(0f, 0f, -5f));
            Phase = "Close from inside";
            yield return Walk(new Vector3(2.9f, 0f, -5.2f)); direction = 180f; yield return new WaitForSeconds(0.35f);
            Require(bridge.TryUseFocused(), "Independent inside handle closes access");
            Require(!access.TryInteract(), "Repeated input rejected while closing");
            yield return new WaitForSeconds(1.8f); Require(access.State == MechanismState.Closed, "Access closes after crossing");

            Phase = "Crate push";
            yield return Walk(new Vector3(-4f, 0f, -4.2f)); direction = 0f; input = Vector2.up;
            float startZ = crate.transform.position.z, until = Time.time + 16f;
            while (crate.transform.position.z < 3.15f && Time.time < until) yield return null;
            input = Vector2.zero; yield return new WaitForSeconds(0.5f);
            Require(crate.transform.position.z - startZ > 4.3f && crate.PushContacts > 0, "Real CharacterController contacts moved crate to platform");
            Require(crate.GetComponent<Rigidbody>().linearVelocity.magnitude < 0.6f, "Crate settles after input release");
            Capture("02-crate-aligned");
            // No teleport between the starting point and the goal: both vertical changes use the original jump motor.
            yield return Walk(new Vector3(-4f, 0f, 0.55f)); direction = 0f; yield return new WaitForSeconds(0.25f);
            Phase = "Jump onto crate"; jump = true; input = Vector2.up;
            until = Time.time + 2f;
            while (transform.position.z < 3f && Time.time < until) yield return null;
            input = Vector2.zero; yield return new WaitForSeconds(0.7f);
            Require(transform.position.y > 2.85f && transform.position.y < 3.6f, "Original jump lands on movable crate");
            Phase = "Jump onto platform"; direction = 0f; jump = true; input = Vector2.up;
            until = Time.time + 2f;
            while (transform.position.z < 5.9f && Time.time < until) yield return null;
            input = Vector2.zero; yield return new WaitForSeconds(0.65f);
            Require(transform.position.y > 4.65f, "Original jump reaches maintenance platform using crate");
            yield return Walk(new Vector3(-4f, 0f, 6.65f)); direction = 0f; yield return new WaitForSeconds(0.35f);
            Phase = "Button and event"; Capture("03-button");
            Require(bridge.TryUseFocused(), "Elevated button reachable from platform");
            Require(!button.TryInteract(), "Button debounces repeated presses");
            yield return new WaitForSeconds(0.13f);
            Require(button.ActivationCount == 1 && gate.State == MechanismState.Opening, "One mechanical contact emits one event to independent gate");
            yield return new WaitForSeconds(2.3f);
            Require(gate.State == MechanismState.Open, "Gate fully opens after switch event");
            Require(button.TryInteract(), "Button recovers and can be pressed again"); yield return new WaitForSeconds(0.5f);
            Require(button.ActivationCount == 2 && gate.State == MechanismState.Open, "Repeated activation is idempotent for already-open gate");
            Phase = "Exit traversal";
            yield return Walk(new Vector3(0.7f, 0f, 6.5f)); yield return new WaitForSeconds(0.75f);
            yield return Walk(new Vector3(4.7f, 0f, 10.5f)); direction = 0f; yield return new WaitForSeconds(0.25f); Capture("04-gate-open");
            yield return Walk(new Vector3(4.7f, 0f, 19f));
            Require(goal.Complete, "Player crossed gate and triggered completion exactly through gameplay route");
            Phase = "Complete"; Capture("05-complete"); Passed = true; Finished = true; input = Vector2.zero; player.enabled = wasEnabled; Save();
            Debug.Log("[Playground QA] PASS: continuous route completed with original movement, jumping and collision.", this);
        }
        void Capture(string name) { ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png")); }
        void OnDestroy() { if (player != null) player.enabled = wasEnabled; }
    }
}
#endif
