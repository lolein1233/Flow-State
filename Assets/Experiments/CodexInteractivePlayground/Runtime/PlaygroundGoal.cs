using UnityEngine;
using UnityEngine.Events;

namespace FlowState.Playground
{
    public sealed class PlaygroundGoal : MonoBehaviour
    {
        public SlidingMechanism gate;
        public MechanicalButton button;
        public DiegeticSignal signal;
        public UnityEvent completed = new UnityEvent();
        public bool Complete { get; private set; }
        void OnTriggerEnter(Collider other) { TryComplete(other); }
        void OnTriggerStay(Collider other) { if (!Complete) TryComplete(other); }
        void TryComplete(Collider other)
        {
            if (Complete || other.GetComponentInParent<PlaygroundPlayerBridge>() == null ||
                gate == null || gate.State != MechanismState.Open || button == null || button.ActivationCount == 0) return;
            Complete = true;
            if (signal != null) { signal.SetActive(true); signal.Pulse(); }
            completed.Invoke();
            Debug.Log("[Codex Interactive Playground] Ruta completada.", this);
        }
    }
}
