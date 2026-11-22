using UnityEngine;

namespace FlowState.BodyFlow
{
    public static class BodyFlowKinematics
    {
        internal static float Reach(float upper, float lower, float flexion)
        {
            return Mathf.Sqrt(Mathf.Max(0f, upper * upper + lower * lower + 2f * upper * lower *
                Mathf.Cos(flexion * Mathf.Deg2Rad)));
        }

        public static bool SolveTwoBone(Vector3 root, Vector3 target, Vector3 pole, float upper, float lower,
            float minimumFlexion, float maximumFlexion, out Vector3 joint, out Vector3 end)
        {
            Vector3 delta = target - root;
            float requested = delta.magnitude;
            if (!float.IsFinite(requested) || !float.IsFinite(upper + lower) || upper <= 0f || lower <= 0f ||
                !float.IsFinite(minimumFlexion + maximumFlexion) ||
                minimumFlexion < 0f || maximumFlexion >= 180f || minimumFlexion > maximumFlexion)
            {
                joint = root;
                end = root;
                return false;
            }

            Vector3 axis = requested > 0.00001f ? delta / requested : Vector3.down;
            Vector3 bend = Vector3.ProjectOnPlane(pole, axis).normalized;
            if (bend.sqrMagnitude < 0.5f)
            {
                Vector3 fallback = Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.forward;
                bend = Vector3.ProjectOnPlane(fallback, axis).normalized;
            }

            float min = Mathf.Max(0.00001f, Reach(upper, lower, maximumFlexion));
            float max = Reach(upper, lower, minimumFlexion);
            float distance = Mathf.Clamp(requested, min, max);
            float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
            joint = root + axis * along + bend * height;
            end = root + axis * distance;
            return requested >= min && requested <= max;
        }

        public static float SupportWeight(float phase, Vector2 interval, float feather)
        {
            float width = Mathf.Repeat(interval.y - interval.x, 1f);
            float elapsed = Mathf.Repeat(phase - interval.x, 1f);
            return Window(elapsed, 0f, width, feather);
        }

        internal static float Window(float phase, float from, float to, float feather)
        {
            float edge = Mathf.Min(phase - from, to - phase);
            return Mathf.SmoothStep(0f, 1f, edge / Mathf.Max(0.001f, feather));
        }

        public static bool TryAdaptLimb(Vector3 root, Vector3 authoredJoint, Vector3 authoredEnd,
            Vector3 offset, Vector3 fallbackPole, float extension, float extraBend, float jointLimit,
            out Vector3 joint, out Vector3 end)
        {
            joint = authoredJoint;
            end = authoredEnd;
            float offsetSquared = offset.sqrMagnitude;
            if (!float.IsFinite(offsetSquared) || offsetSquared < 0.00000025f)
                return false;

            Vector3 upper = authoredJoint - root;
            Vector3 lower = authoredEnd - authoredJoint;
            Vector3 axis = authoredEnd - root;
            float flexion = Vector3.Angle(upper, lower);
            if (!float.IsFinite(upper.sqrMagnitude + lower.sqrMagnitude) ||
                upper.sqrMagnitude < 0.0001f || lower.sqrMagnitude < 0.0001f ||
                axis.sqrMagnitude < 0.0001f || flexion > 175f)
                return false;

            Vector3 pole = Vector3.ProjectOnPlane(upper, axis);
            if (pole.sqrMagnitude < 0.000025f)
                pole = fallbackPole;

            float minFlexion = Mathf.Min(flexion, Mathf.Max(8f, flexion - extension));
            float maxFlexion = Mathf.Min(175f, flexion + extraBend);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                SolveTwoBone(root, authoredEnd + offset, pole, upper.magnitude, lower.magnitude,
                    minFlexion, maxFlexion, out Vector3 solvedJoint, out Vector3 solvedEnd);
                if (Vector3.Angle(upper, solvedJoint - root) <= jointLimit &&
                    Vector3.Angle(lower, solvedEnd - solvedJoint) <= jointLimit &&
                    (solvedEnd - authoredEnd).sqrMagnitude >= 0.00000025f)
                {
                    joint = solvedJoint;
                    end = solvedEnd;
                    return true;
                }
                offset *= 0.5f;
            }
            return false;
        }
    }

    public enum BodyFlowAction { Ground, Airborne, Landing, AirRoll, Climb, Vault, TopOut, WallJump }

    public struct BodyFlowTraversal
    {
        public BodyFlowAction Action;
        public Collider Surface;
        public Vector3 Normal;
        public Vector3 Destination;
        public float Progress;

        public BodyFlowTraversal(BodyFlowAction action, Collider surface, Vector3 normal, Vector3 destination)
        {
            Action = action;
            Surface = surface;
            Normal = normal;
            Destination = destination;
            Progress = 0f;
        }
    }

    public readonly struct BodyFlowMotionIntent
    {
        public readonly bool CanAdaptPose;
        public readonly bool Grounded;
        public readonly Vector3 PlanarVelocity;
        public readonly float VerticalVelocity;
        public readonly float CapsuleBottom;
        public readonly BodyFlowTraversal Traversal;

        public BodyFlowMotionIntent(bool canAdaptPose, bool grounded, Vector3 planarVelocity, float verticalVelocity,
            float capsuleBottom, BodyFlowTraversal traversal)
        {
            CanAdaptPose = canAdaptPose;
            Grounded = grounded;
            PlanarVelocity = planarVelocity;
            VerticalVelocity = verticalVelocity;
            CapsuleBottom = capsuleBottom;
            Traversal = traversal;
        }
    }
}
