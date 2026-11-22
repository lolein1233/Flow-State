using System;
using UnityEngine;

namespace FlowState.BodyFlow
{
    [CreateAssetMenu(menuName = "FLOWSTATE/BODYFLOW Profile", fileName = "BodyFlowProfile")]
    public sealed class BodyFlowProfile : ScriptableObject
    {
        [Serializable]
        public struct GaitClip
        {
            public AnimationClip clip;
            public Vector2 leftSupport;
            public Vector2 rightSupport;

            internal GaitClip(Vector2 leftSupport, Vector2 rightSupport)
            {
                clip = null;
                this.leftSupport = leftSupport;
                this.rightSupport = rightSupport;
            }
        }

        [Header("Contact phases in the original clips")]
        public GaitClip walking = new GaitClip(new Vector2(0.36f, 0.87f), new Vector2(0.86f, 0.39f));
        public GaitClip running = new GaitClip(new Vector2(0.25f, 0.61f), new Vector2(0.75f, 0.115f));
        [Min(0.1f)] public float walkingBlendKey = 1.8f;
        [Min(0.2f)] public float runningBlendKey = 5.5f;
        [Range(0.015f, 0.15f)] public float phaseFeather = 0.06f;

        [Header("Contact adaptation (world metres)")]
        [Range(0.12f, 0.55f)] public float groundProbeRange = 0.32f;
        [Range(0.08f, 0.4f)] public float handProbeRange = 0.24f;
        [Range(0.2f, 0.65f)] public float wallProbeRange = 0.48f;
        [Range(0.02f, 0.24f)] public float footCorrection = 0.10f;
        [Range(0.02f, 0.22f)] public float handCorrection = 0.12f;
        [Range(0f, 0.09f)] public float horizontalAnchor = 0.035f;
        [Range(0f, 0.04f)] public float solePadding = 0.008f;
        [Range(0f, 0.10f)] public float pelvisCorrection = 0.055f;
        [Range(0f, 20f)] public float soleTilt = 10f;
        [Tooltip("Source hip height difference between Climb Down and Climb Up, in visual-root units.")]
        [Range(0f, 1.5f)] public float climbDownRootOffset = 1.03f;
        [Range(0f, 1.4f)] public float topOutSettleLimit = 1.1f;

        [Header("Limits relative to the evaluated pose")]
        [Range(0f, 8f)] public float kneeExtension = 3f;
        [Range(2f, 26f)] public float extraBend = 16f;
        [Range(4f, 25f)] public float maxJointCorrection = 15f;
        [Range(5f, 30f)] public float response = 19f;
        [Range(0.05f, 0.35f)] public float blendSeconds = 0.15f;

        public float MapLocomotionSpeed(float actual, float walk, float sprint)
        {
            if (actual <= walk)
                return Mathf.Min(actual, walkingBlendKey);
            return Mathf.Lerp(walkingBlendKey, runningBlendKey,
                Mathf.InverseLerp(walk, Mathf.Max(walk + 0.01f, sprint), actual));
        }
    }
}
