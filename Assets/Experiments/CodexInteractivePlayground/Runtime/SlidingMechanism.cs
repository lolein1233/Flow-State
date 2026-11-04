using UnityEngine;
using UnityEngine.Events;

namespace FlowState.Playground
{
    public enum MechanismState { Closed, Opening, Open, Closing }

    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class SlidingMechanism : PlaygroundInteractable
    {
        public Vector3 localTravel = new Vector3(0f, 5.3f, 0f);
        [Min(0.1f)] public float duration = 1.4f;
        public bool manual = true;
        public DiegeticSignal stateSignal;
        public UnityEvent opened = new UnityEvent();
        public UnityEvent closed = new UnityEvent();
        public UnityEvent obstructed = new UnityEvent();
        public MechanismState State { get; private set; }
        public float Progress { get; private set; }
        public bool IsMoving => State == MechanismState.Opening || State == MechanismState.Closing;
        public override bool Available => manual && !IsMoving;
        Rigidbody body;
        BoxCollider shape;
        Vector3 closedPosition, travel;
        readonly Collider[] overlaps = new Collider[32];

        void Awake()
        {
            body = GetComponent<Rigidbody>(); shape = GetComponent<BoxCollider>();
            body.isKinematic = true; body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            closedPosition = body.position;
            travel = transform.TransformDirection(localTravel);
            State = MechanismState.Closed;
        }

        public override bool TryInteract()
        {
            if (!Available) return false;
            if (State == MechanismState.Closed) RequestOpen(); else RequestClose();
            return true;
        }
        public void RequestOpen()
        {
            if (State != MechanismState.Closed) return;
            State = MechanismState.Opening;
        }
        public void RequestClose()
        {
            if (State != MechanismState.Open) return;
            State = MechanismState.Closing;
            if (stateSignal != null) stateSignal.SetActive(false);
        }

        void FixedUpdate()
        {
            if (!IsMoving) return;
            float next = Mathf.MoveTowards(Progress, State == MechanismState.Opening ? 1f : 0f,
                Time.fixedDeltaTime / Mathf.Max(0.1f, duration));
            float eased = next * next * (3f - 2f * next);
            Vector3 target = closedPosition + travel * eased;
            if (Blocked(target))
            {
                if (State == MechanismState.Closing) { State = MechanismState.Opening; obstructed.Invoke(); }
                return;
            }
            Progress = next;
            body.MovePosition(target);
            if (next >= 1f)
            {
                State = MechanismState.Open;
                if (stateSignal != null) { stateSignal.SetActive(true); stateSignal.Pulse(); }
                opened.Invoke();
            }
            else if (next <= 0f) { State = MechanismState.Closed; closed.Invoke(); }
        }

        bool Blocked(Vector3 target)
        {
            // Include the whole next step, even when a caller selects a short duration.
            Vector3 delta = target - body.position;
            Vector3 half = Vector3.Scale(shape.size * 0.5f, transform.lossyScale);
            Vector3 localDelta = Quaternion.Inverse(transform.rotation) * delta;
            half += new Vector3(Mathf.Abs(localDelta.x), Mathf.Abs(localDelta.y), Mathf.Abs(localDelta.z)) * 0.5f;
            half += Vector3.one * 0.06f;
            Vector3 center = transform.TransformPoint(shape.center) + delta * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return true;
            for (int i = 0; i < count; i++)
            {
                var other = overlaps[i];
                if (other == shape || other.transform.IsChildOf(transform)) continue;
                if (other is CharacterController || (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)) return true;
            }
            return false;
        }
    }
}
