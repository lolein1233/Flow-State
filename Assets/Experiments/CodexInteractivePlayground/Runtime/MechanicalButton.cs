using UnityEngine;
using UnityEngine.Events;

namespace FlowState.Playground
{
    public sealed class MechanicalButton : PlaygroundInteractable
    {
        public Transform cap;
        public Vector3 localPress = new Vector3(0f, 0f, 0.09f);
        public DiegeticSignal signal;
        public UnityEvent activated = new UnityEvent();
        public bool Busy { get; private set; }
        public int ActivationCount { get; private set; }
        public override bool Available => !Busy && cap != null;
        Vector3 rest;
        float elapsed;
        bool fired;
        void Awake() { if (cap != null) rest = cap.localPosition; }
        public override bool TryInteract()
        {
            if (!Available) return false;
            Busy = true; elapsed = 0f; fired = false;
            return true;
        }
        void Update()
        {
            if (!Busy) return;
            if (cap == null) { Busy = false; return; }
            elapsed += Time.deltaTime;
            float compression = elapsed < 0.08f ? Mathf.SmoothStep(0f, 1f, elapsed / 0.08f) :
                elapsed < 0.16f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (elapsed - 0.16f) / 0.22f);
            cap.localPosition = rest + localPress * compression;
            if (!fired && elapsed >= 0.08f)
            {
                fired = true; ActivationCount++;
                if (signal != null) signal.Pulse();
                activated.Invoke();
            }
            if (elapsed >= 0.38f) { cap.localPosition = rest; Busy = false; }
        }
        void OnDisable() { if (cap != null) cap.localPosition = rest; Busy = false; }
    }
}
