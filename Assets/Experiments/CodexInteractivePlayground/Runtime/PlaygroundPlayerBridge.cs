using UnityEngine;

namespace FlowState.Playground
{
    // Added only to the experiment's player copy. FPSController remains the sole movement/camera/animation owner.
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(FPSController))]
    public sealed class PlaygroundPlayerBridge : MonoBehaviour
    {
        public Camera view;
        public KeyCode interactionKey = KeyCode.E;
        [Min(0.1f)] public float reach = 1.7f;
        public PlaygroundInteractable[] interactables;
        public Transform recovery;
        public PlaygroundInteractable Focused { get; private set; }
        public int AcceptedInteractions { get; private set; }
        FPSController player;
        CharacterController controller;
        void Awake() { player = GetComponent<FPSController>(); controller = GetComponent<CharacterController>(); }
        bool InputAllowed => player != null && player.canMove && Time.timeScale > 0f &&
            !PauseMenuController.GameplayInputBlocked && !player.IsGraffitiMode() && !player.IsClimbing() && !player.IsParkouring();
        void Update()
        {
            if (recovery != null && transform.position.y < -8f) player.TeleportTo(recovery.position, recovery.rotation);
            SetFocus(InputAllowed ? FindTarget() : null);
            if (Input.GetKeyDown(interactionKey)) TryUseFocused();
        }
        public bool TryUseFocused()
        {
            if (!InputAllowed) return false;
            SetFocus(FindTarget());
            if (Focused == null || !Focused.TryInteract()) return false;
            AcceptedInteractions++;
            return true;
        }
        public PlaygroundInteractable FindTarget()
        {
            if (view == null || controller == null || interactables == null) return null;
            PlaygroundInteractable best = null;
            float bestScore = float.MaxValue;
            // Reach is from the player's chest, not the third-person camera several metres behind them.
            Vector3 origin = controller.bounds.center + Vector3.up * 0.35f;
            foreach (var candidate in interactables)
            {
                if (candidate == null || !candidate.isActiveAndEnabled || !candidate.Available) continue;
                Vector3 point = candidate.InteractionPosition;
                Vector3 delta = point - origin;
                float distance = delta.magnitude;
                if (distance > reach || Vector3.Dot(view.transform.forward, (point - view.transform.position).normalized) < 0.84f) continue;
                Vector3 viewport = view.WorldToViewportPoint(point);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) continue;
                // The first body between player and control must belong to this interaction.
                if (Physics.Raycast(origin, delta.normalized, out var hit, distance + 0.05f, ~0, QueryTriggerInteraction.Ignore) &&
                    !hit.transform.IsChildOf(candidate.transform) && hit.transform != candidate.transform) continue;
                if (distance < bestScore) { best = candidate; bestScore = distance; }
            }
            return best;
        }
        void SetFocus(PlaygroundInteractable target)
        {
            if (Focused == target) return;
            if (Focused != null) Focused.SetFocus(false);
            Focused = target;
            if (Focused != null) Focused.SetFocus(true);
        }
        void OnDisable() { SetFocus(null); }
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!InputAllowed || hit.moveDirection.y < -0.3f || Mathf.Abs(hit.normal.y) > 0.4f) return;
            var pushable = hit.collider.GetComponentInParent<PushableBody>();
            if (pushable != null) pushable.Push(hit.moveDirection);
        }
    }
}
