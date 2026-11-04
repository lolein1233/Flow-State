using UnityEngine;

namespace FlowState.Playground
{
    // A stationary handle can operate a sliding leaf from either side of its frame.
    public sealed class MechanismHandle : PlaygroundInteractable
    {
        public SlidingMechanism mechanism;
        public override bool Available => mechanism != null && mechanism.Available;
        public override bool TryInteract() => Available && mechanism.TryInteract();
    }
}
