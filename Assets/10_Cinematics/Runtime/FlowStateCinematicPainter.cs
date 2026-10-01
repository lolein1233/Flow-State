using System;
using UnityEngine;

namespace FlowState.Cinematics
{
    [DisallowMultipleComponent]
    public sealed class FlowStateCinematicPainter : MonoBehaviour
    {
        [SerializeField] private Transform[] bones;
        [SerializeField] private Quaternion[] restRotations;
        [SerializeField] private Vector3[] restPositions;
        private Transform Bone(string suffix)
        {
            foreach (Transform bone in bones)
                if (bone != null && bone.name.EndsWith(":" + suffix, StringComparison.Ordinal)) return bone;
            return null;
        }

        public void CaptureRestPose()
        {
            bones = GetComponentsInChildren<Transform>(true);
            restRotations = new Quaternion[bones.Length];
            restPositions = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                restRotations[i] = bones[i].localRotation;
                restPositions[i] = bones[i].localPosition;
            }
        }

        public void Pose(Vector3 paintPoint, Transform can, float time)
        {
            if (bones == null || bones.Length == 0 || can == null) return;
            for (int i = 1; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                bones[i].localRotation = restRotations[i];
                bones[i].localPosition = restPositions[i];
            }
            // Maikol's imported model faces local +Z. The wall is also in +Z.
            // Keep every joint target in that same frame instead of reaching behind him.
            float crouch = Mathf.Clamp((2.05f - paintPoint.y) * 0.55f, 0f, 0.48f);
            transform.localPosition = new Vector3(paintPoint.x - 0.48f, 0.025f - crouch, -0.9f);
            transform.localRotation = Quaternion.Euler(0f, -8f + Mathf.Sin(time * 0.7f) * 2f, 0f);
            Transform rightFoot = Bone("RightFoot");
            Transform leftFoot = Bone("LeftFoot");
            Quaternion rightFootRotation = rightFoot != null ? rightFoot.rotation : Quaternion.identity;
            Quaternion leftFootRotation = leftFoot != null ? leftFoot.rotation : Quaternion.identity;
            Transform spine = Bone("Spine1");
            if (spine != null) spine.rotation = Quaternion.AngleAxis(-4f - crouch * 12f, transform.right) * spine.rotation;

            Vector3 grip = can.position;
            Solve(Bone("RightArm"), Bone("RightForeArm"), Bone("RightHand"), grip,
                transform.parent.TransformPoint(paintPoint + new Vector3(0.8f, -0.6f, -0.9f)));
            Transform hand = Bone("RightHand");
            if (hand != null)
            {
                can.position = hand.position;
            }
            // Preserve the relaxed imported pose of the supporting arm and fingers.

            float stride = Mathf.Sin(time * 3.2f) * 0.09f;
            Solve(Bone("RightUpLeg"), Bone("RightLeg"), rightFoot,
                transform.parent.TransformPoint(new Vector3(paintPoint.x - 0.17f, 0.24f, -0.93f + stride)),
                transform.position + new Vector3(0.35f, 0.5f, 1.3f));
            Solve(Bone("LeftUpLeg"), Bone("LeftLeg"), leftFoot,
                transform.parent.TransformPoint(new Vector3(paintPoint.x - 0.81f, 0.24f, -1.09f - stride)),
                transform.position + new Vector3(-0.4f, 0.5f, 1.3f));
            if (rightFoot != null) rightFoot.rotation = rightFootRotation;
            if (leftFoot != null) leftFoot.rotation = leftFootRotation;
        }

        private static void Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 pole)
        {
            if (upper == null || lower == null || end == null) return;
            Vector3 origin = upper.position;
            float a = Vector3.Distance(origin, lower.position);
            float b = Vector3.Distance(lower.position, end.position);
            Vector3 direction = target - origin;
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);
            direction.Normalize();
            Vector3 bend = Vector3.ProjectOnPlane(pole - origin, direction).normalized;
            float along = (a * a - b * b + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            Vector3 elbow = origin + direction * along + bend * height;
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, elbow - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position, origin + direction * distance - lower.position) * lower.rotation;
        }
    }
}
