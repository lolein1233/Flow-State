using System;
using UnityEngine;

namespace FlowState.BodyFlow
{
    [Serializable]
    public sealed class BodyFlowRig
    {
        [Serializable]
        public struct Limb
        {
            public Transform upper;
            public Transform lower;
            public Transform end;
            public Transform soleReference;

            internal bool IsChainUnder(Transform parent)
            {
                return upper && lower && end && upper != parent && upper.IsChildOf(parent) &&
                    lower.parent == upper && end.parent == lower;
            }
        }

        public Animator animator;
        public Transform pelvis;
        public Transform chest;
        public Limb leftLeg;
        public Limb rightLeg;
        public Limb leftArm;
        public Limb rightArm;

        public bool HasValidMapping(Transform player)
        {
            return animator && animator.transform != player && animator.transform.IsChildOf(player) &&
                pelvis && pelvis.parent == animator.transform && chest && chest.IsChildOf(pelvis) &&
                leftLeg.IsChainUnder(pelvis) && rightLeg.IsChainUnder(pelvis) &&
                leftArm.IsChainUnder(chest) && rightArm.IsChainUnder(chest) &&
                leftLeg.upper != rightLeg.upper && leftArm.upper != rightArm.upper &&
                leftLeg.soleReference && leftLeg.soleReference.IsChildOf(leftLeg.end) &&
                rightLeg.soleReference && rightLeg.soleReference.IsChildOf(rightLeg.end);
        }
    }
}
