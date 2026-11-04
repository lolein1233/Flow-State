using UnityEngine;

namespace FlowState.Playground
{
    public abstract class PlaygroundInteractable : MonoBehaviour
    {
        public Transform interactionPoint;
        public DiegeticSignal focusSignal;
        public virtual Vector3 InteractionPosition => interactionPoint != null ? interactionPoint.position : transform.position;
        public abstract bool Available { get; }
        public abstract bool TryInteract();
        public void SetFocus(bool focused) { if (focusSignal != null) focusSignal.SetFocus(focused); }
    }
}
