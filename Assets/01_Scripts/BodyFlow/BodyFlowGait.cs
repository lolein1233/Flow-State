using System.Collections.Generic;
using UnityEngine;

namespace FlowState.BodyFlow
{
    internal sealed class BodyFlowGait
    {
        readonly List<AnimatorClipInfo> clipInfo = new List<AnimatorClipInfo>(4);

        internal Vector2 Read(Animator animator, BodyFlowProfile profile, int idle, int walk, out float phase)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            phase = Mathf.Repeat(current.normalizedTime, 1f);
            Vector2 support = ReadState(animator, profile, current, idle, walk, false);
            if (!animator.IsInTransition(0))
                return support;

            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            float weight = Mathf.SmoothStep(0f, 1f, animator.GetAnimatorTransitionInfo(0).normalizedTime);
            return Vector2.Lerp(support, ReadState(animator, profile, next, idle, walk, true), weight);
        }

        Vector2 ReadState(Animator animator, BodyFlowProfile profile, AnimatorStateInfo state,
            int idle, int walk, bool next)
        {
            if (state.fullPathHash == idle)
                return Vector2.one;
            if (state.fullPathHash != walk)
                return Vector2.zero;

            if (next)
                animator.GetNextAnimatorClipInfo(0, clipInfo);
            else
                animator.GetCurrentAnimatorClipInfo(0, clipInfo);

            Vector2 support = Vector2.zero;
            float total = 0f;
            foreach (AnimatorClipInfo info in clipInfo)
            {
                BodyFlowProfile.GaitClip gait;
                if (info.clip == profile.walking.clip)
                    gait = profile.walking;
                else if (info.clip == profile.running.clip)
                    gait = profile.running;
                else
                    continue;

                float t = state.normalizedTime;
                support.x += info.weight * BodyFlowKinematics.SupportWeight(t, gait.leftSupport, profile.phaseFeather);
                support.y += info.weight * BodyFlowKinematics.SupportWeight(t, gait.rightSupport, profile.phaseFeather);
                total += info.weight;
            }
            return total > 0.0001f ? support / total : Vector2.zero;
        }

        internal readonly struct Contact
        {
            internal readonly Collider Surface;
            readonly Vector3 localPoint;
            readonly Quaternion localFrame;

            internal Contact(Collider surface, Vector3 point, Quaternion frame)
            {
                Surface = surface;
                Transform t = surface.transform;
                localPoint = t.InverseTransformPoint(point);
                localFrame = Quaternion.Inverse(t.rotation) * frame;
            }

            internal bool IsValid => Surface && Surface.enabled && Surface.gameObject.activeInHierarchy;

            internal Pose Read()
            {
                Transform t = Surface.transform;
                return new Pose(t.TransformPoint(localPoint), t.rotation * localFrame);
            }
        }
    }
}
