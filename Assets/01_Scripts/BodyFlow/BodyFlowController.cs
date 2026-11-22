using UnityEngine;

namespace FlowState.BodyFlow
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FPSController))]
    public sealed class BodyFlowController : MonoBehaviour
    {
        [SerializeField] bool useBodyFlow;
        [SerializeField] BodyFlowProfile profile;
        [SerializeField] BodyFlowRig rig = new BodyFlowRig();
        [SerializeField] string idleState = "Base Layer.Idle";
        [SerializeField] string locomotionState = "Base Layer.Walk";
        [SerializeField] bool showGizmos;

        static readonly int Jump = Animator.StringToHash("Jump");
        static readonly int FallLanding = Animator.StringToHash("FallLanding");
        static readonly int Hang = Animator.StringToHash("Hang");
        static readonly int ClimbUp = Animator.StringToHash("ClimbUp");
        static readonly int ClimbDown = Animator.StringToHash("ClimbDown");
        static readonly int VaultLow = Animator.StringToHash("VaultLow");
        static readonly int VaultMedium = Animator.StringToHash("VaultMedium");
        static readonly int VaultHigh = Animator.StringToHash("VaultHigh");
        static readonly int LedgeTopOut = Animator.StringToHash("LedgeTopOut");

        readonly BodyFlowGait gait = new BodyFlowGait();
        readonly RaycastHit[] hits = new RaycastHit[32];
        FPSController movement;
        CharacterController capsule;
        LimbCorrection leftLeg, rightLeg, leftArm, rightArm;
        Vector3 animatedPelvis;
        float pelvisOffset;
        float weight;
        float gaitPhase;
        Vector2 footSupport;
        int idleHash;
        int walkHash;
        bool initialized;
        bool pelvisWritten;
        BodyFlowAction previousAction;

        public bool UseBodyFlow { get => useBodyFlow; set => useBodyFlow = value; }
        public float PoseWeight => weight;
        public float StepPhase => gaitPhase;
        public BodyFlowAction Action => previousAction;
        public Vector2 FootSupport => footSupport;
        public Vector2 FootAdjustment => new Vector2(leftLeg.Applied, rightLeg.Applied);
        public Vector2 HandAdjustment => new Vector2(leftArm.Applied, rightArm.Applied);

        public float LocomotionBlendSpeed(float actual, float walk, float sprint)
        {
            return useBodyFlow && isActiveAndEnabled && profile && profile.walking.clip && profile.running.clip
                ? profile.MapLocomotionSpeed(actual, walk, sprint)
                : actual;
        }

        struct LimbCorrection
        {
            internal BodyFlowRig.Limb Bones;
            internal Vector3 Target;
            internal Vector3 Offset;
            internal float Influence;
            internal float Applied;
            Quaternion upperRotation, lowerRotation, endRotation;
            BodyFlowGait.Contact anchor;
            bool written;

            internal void Follow(RaycastHit hit, Vector3 marker, Vector3 axis, float padding, float phase,
                float normalLimit, float tangentLimit, float response)
            {
                Vector3 desired = Vector3.zero;
                if (hit.collider && phase > 0.001f)
                {
                    Vector3 point = hit.point + hit.normal * padding;
                    float distance = Vector3.Dot(point - marker, axis);
                    desired = axis * Mathf.Clamp(distance, -normalLimit, normalLimit);

                    if (phase >= 0.55f && tangentLimit > 0f)
                    {
                        if (!anchor.IsValid || anchor.Surface != hit.collider ||
                            Vector3.Distance(anchor.Read().position, point) > tangentLimit * 3f)
                        {
                            anchor = new BodyFlowGait.Contact(hit.collider, point, Quaternion.identity);
                        }
                        Vector3 tangent = Vector3.ProjectOnPlane(anchor.Read().position - marker, axis);
                        desired += Vector3.ClampMagnitude(tangent, tangentLimit);
                    }
                    else if (phase < 0.35f)
                    {
                        anchor = default;
                    }
                }
                else
                {
                    anchor = default;
                    phase = 0f;
                }

                Influence = Mathf.Lerp(Influence, phase, response);
                Offset = Vector3.Lerp(Offset, desired * phase, response);
            }

            internal void Apply(Vector3 offset, Vector3 pole, Quaternion soleTilt, BodyFlowProfile profile)
            {
                Transform upper = Bones.upper;
                Transform lower = Bones.lower;
                Transform end = Bones.end;
                Vector3 root = upper.position;
                Vector3 jointBefore = lower.position;
                Vector3 endBefore = end.position;
                Quaternion endWorldRotation = end.rotation;
                bool solve = BodyFlowKinematics.TryAdaptLimb(root, jointBefore, endBefore, offset, pole,
                    profile.kneeExtension, profile.extraBend, profile.maxJointCorrection,
                    out Vector3 joint, out Vector3 target);
                Target = endBefore + offset;
                Applied = 0f;
                if (!solve && Quaternion.Angle(Quaternion.identity, soleTilt) < 0.1f)
                    return;

                upperRotation = upper.localRotation;
                lowerRotation = lower.localRotation;
                endRotation = end.localRotation;
                written = true;
                if (solve)
                {
                    upper.rotation = Quaternion.FromToRotation(jointBefore - root, joint - root) * upper.rotation;
                    lower.rotation = Quaternion.FromToRotation(end.position - lower.position, target - lower.position) *
                        lower.rotation;
                    Applied = Vector3.Distance(endBefore, end.position);
                }
                end.rotation = soleTilt * endWorldRotation;
            }

            internal void Restore()
            {
                if (!written)
                    return;
                if (Bones.upper) Bones.upper.localRotation = upperRotation;
                if (Bones.lower) Bones.lower.localRotation = lowerRotation;
                if (Bones.end) Bones.end.localRotation = endRotation;
                written = false;
            }

            internal void ResetContact()
            {
                anchor = default;
                Offset = Vector3.zero;
                Influence = 0f;
                Applied = 0f;
            }
        }

        void Awake()
        {
            movement = GetComponent<FPSController>();
            capsule = GetComponent<CharacterController>();
        }

        void Update() => RestoreAnimatorPose();

        void LateUpdate()
        {
            if (!useBodyFlow && weight <= 0f)
                return;

            BodyFlowMotionIntent motion = movement.ReadBodyFlowIntent();
            BodyFlowAction action = motion.Traversal.Action;
            if (!motion.CanAdaptPose || action == BodyFlowAction.AirRoll || !rig.animator ||
                !rig.animator.isActiveAndEnabled || !rig.animator.isInitialized || !profile ||
                Time.deltaTime <= 0f || Time.deltaTime > 0.12f)
            {
                ResetContacts();
                weight = 0f;
                previousAction = action;
                return;
            }

            if (!initialized && !Initialize())
            {
                useBodyFlow = false;
                return;
            }

            if (action != previousAction && !(IsGroundContactAction(action) && IsGroundContactAction(previousAction)))
                ResetContacts();
            previousAction = action;
            float response = 1f - Mathf.Exp(-profile.response * Time.deltaTime);
            weight = Mathf.MoveTowards(weight, useBodyFlow ? 1f : 0f, Time.deltaTime / profile.blendSeconds);
            if (weight <= 0f)
            {
                ResetContacts();
                return;
            }

            AnimatorStateInfo state = rig.animator.GetCurrentAnimatorStateInfo(0);
            bool mantlePlaying = state.shortNameHash == LedgeTopOut;
            float downWeight = state.shortNameHash == ClimbDown ? 1f : 0f;
            if (rig.animator.IsInTransition(0))
            {
                AnimatorStateInfo next = rig.animator.GetNextAnimatorStateInfo(0);
                mantlePlaying |= next.shortNameHash == LedgeTopOut;
                float transition = Mathf.Clamp01(rig.animator.GetAnimatorTransitionInfo(0).normalizedTime);
                downWeight = Mathf.Lerp(downWeight, next.shortNameHash == ClimbDown ? 1f : 0f, transition);
                if (transition > 0.5f)
                    state = next;
            }
            gaitPhase = state.loop ? Mathf.Repeat(state.normalizedTime, 1f) : Mathf.Clamp01(state.normalizedTime);
            // Climb Down was authored in a higher vertical frame than Hang and Climb Up.
            float climbOffset = rig.animator.transform.TransformVector(Vector3.up * profile.climbDownRootOffset).y;
            MovePelvis(-climbOffset * downWeight * weight);

            if (action == BodyFlowAction.Climb || action == BodyFlowAction.WallJump)
            {
                AdaptWall(motion.Traversal, state, response);
                return;
            }

            if (action == BodyFlowAction.Vault || action == BodyFlowAction.TopOut)
            {
                AdaptObstacle(motion, state, response);
                return;
            }

            if (mantlePlaying)
                SettleOnDeck(motion.CapsuleBottom, 1f);

            footSupport = gait.Read(rig.animator, profile, idleHash, walkHash, out gaitPhase);
            if (motion.Grounded && state.shortNameHash == FallLanding)
                footSupport = Vector2.one * BodyFlowKinematics.Window(state.normalizedTime, 0.16f, 1.15f, 0.15f);
            else if (!motion.Grounded)
                footSupport = CanPrepareLanding(motion) ? Vector2.one * 0.45f : Vector2.zero;

            AdaptGround(footSupport, motion.Grounded, false, 0f, response);
        }

        bool Initialize()
        {
            if (!rig.HasValidMapping(transform))
            {
                Debug.LogWarning("BODYFLOW needs the MAIKOL skeleton mapping.", this);
                return false;
            }
            leftLeg.Bones = rig.leftLeg;
            rightLeg.Bones = rig.rightLeg;
            leftArm.Bones = rig.leftArm;
            rightArm.Bones = rig.rightArm;
            idleHash = Animator.StringToHash(idleState);
            walkHash = Animator.StringToHash(locomotionState);
            initialized = true;
            return true;
        }

        static bool IsGroundContactAction(BodyFlowAction action) =>
            action == BodyFlowAction.Ground || action == BodyFlowAction.Landing;

        bool CanPrepareLanding(BodyFlowMotionIntent motion)
        {
            if (motion.VerticalVelocity > -0.25f)
                return false;

            const float predictionTime = 0.14f;
            Vector3 expected = transform.position + motion.PlanarVelocity * predictionTime;
            expected.y = motion.CapsuleBottom + 0.08f;
            float drop = -motion.VerticalVelocity * predictionTime -
                0.5f * movement.gravity * predictionTime * predictionTime;
            return TrySurface(expected, Vector3.down, Mathf.Clamp(drop + 0.1f, 0.14f, 0.85f),
                Vector3.up, GroundNormalLimit(), null, out _);
        }

        void AdaptGround(Vector2 support, bool grounded, bool checkHeight, float expectedHeight, float response,
            Collider clearanceSurface = null)
        {
            Quaternion leftTilt = GroundFoot(ref leftLeg, support.x, checkHeight, expectedHeight, response, clearanceSurface);
            Quaternion rightTilt = GroundFoot(ref rightLeg, support.y, checkHeight, expectedHeight, response, clearanceSurface);
            float shared = grounded ? Mathf.Clamp((leftLeg.Offset.y + rightLeg.Offset.y) * 0.42f,
                -profile.pelvisCorrection, profile.pelvisCorrection) : 0f;
            pelvisOffset = Mathf.Lerp(pelvisOffset, shared, response);
            float height = pelvisOffset * weight;
            MovePelvis(height);
            ApplyGroundFoot(ref leftLeg, height, leftTilt);
            ApplyGroundFoot(ref rightLeg, height, rightTilt);
        }

        void MovePelvis(float height)
        {
            if (Mathf.Abs(height) < 0.0005f)
                return;
            if (!pelvisWritten)
                animatedPelvis = rig.pelvis.localPosition;
            rig.pelvis.position += Vector3.up * height;
            pelvisWritten = true;
        }

        void ApplyGroundFoot(ref LimbCorrection foot, float height, Quaternion tilt)
        {
            Vector3 offset = foot.Offset * weight;
            offset.y -= height * foot.Influence;
            foot.Apply(offset, transform.forward, tilt, profile);
        }

        Quaternion GroundFoot(ref LimbCorrection foot, float phase, bool checkHeight, float expectedHeight,
            float response, Collider clearanceSurface)
        {
            Vector3 marker = foot.Bones.soleReference.position;
            RaycastHit hit = default;
            float range = profile.groundProbeRange;
            if (phase > 0.001f && TrySurface(marker + Vector3.up * range, Vector3.down, 2f * range,
                Vector3.up, GroundNormalLimit(), clearanceSurface, out RaycastHit candidate) &&
                (!clearanceSurface || candidate.point.y > marker.y + 0.01f) &&
                (!checkHeight || Mathf.Abs(candidate.point.y - expectedHeight) < 0.23f))
            {
                hit = candidate;
                // A distant hit can be a tread that the authored swing will reach later.
                phase *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(range * 0.64f, range,
                    Mathf.Abs(hit.point.y - marker.y)));
            }
            foot.Follow(hit, marker, Vector3.up, profile.solePadding, phase,
                profile.footCorrection, clearanceSurface ? 0f : profile.horizontalAnchor, response);
            if (clearanceSurface)
                return Quaternion.identity;
            Vector3 normal = hit.collider ? hit.normal : Vector3.up;
            Quaternion tilt = Quaternion.FromToRotation(Vector3.up, normal);
            return Quaternion.RotateTowards(Quaternion.identity, tilt,
                profile.soleTilt * weight * foot.Influence);
        }

        float GroundNormalLimit() => Mathf.Cos(Mathf.Min(capsule.slopeLimit, 65f) * Mathf.Deg2Rad);

        void AdaptWall(BodyFlowTraversal traversal, AnimatorStateInfo state, float response)
        {
            if (!traversal.Surface)
                return;

            Vector2 hands = Vector2.zero;
            Vector2 feet = Vector2.zero;
            if (traversal.Action == BodyFlowAction.WallJump)
            {
                if (state.shortNameHash == Jump)
                    hands = Vector2.one * Mathf.SmoothStep(0f, 0.75f, Mathf.InverseLerp(0.76f, 0.97f, traversal.Progress));
            }
            else if (state.shortNameHash == Hang)
            {
                hands = Vector2.one;
                feet = Vector2.one * 0.65f;
            }
            else if (state.shortNameHash == ClimbUp)
            {
                float p = state.normalizedTime;
                hands = new Vector2(Cycle(p, 0.27f, 0.88f), Cycle(p, 0.91f, 0.73f));
                feet = new Vector2(Cycle(p, 0.91f, 0.65f), Cycle(p, 0.31f, 0.14f));
            }
            else if (state.shortNameHash == ClimbDown)
            {
                float p = state.normalizedTime;
                hands = new Vector2(Cycle(p, 0.93f, 0.76f), Cycle(p, 0.37f, 0.18f));
                feet = new Vector2(Cycle(p, 0.40f, 0.17f), Cycle(p, 0.91f, 0.73f));
            }
            footSupport = feet;
            WallLimb(ref leftArm, traversal, hands.x, true, -transform.right, response);
            WallLimb(ref rightArm, traversal, hands.y, true, transform.right, response);
            WallLimb(ref leftLeg, traversal, feet.x, false, transform.forward, response);
            WallLimb(ref rightLeg, traversal, feet.y, false, transform.forward, response);
        }

        float Cycle(float phase, float from, float to) =>
            BodyFlowKinematics.SupportWeight(phase, new Vector2(from, to), profile.phaseFeather);

        void WallLimb(ref LimbCorrection limb, BodyFlowTraversal traversal, float phase, bool hand,
            Vector3 pole, float response)
        {
            Vector3 normal = traversal.Normal.normalized;
            Vector3 marker = hand ? limb.Bones.end.position : limb.Bones.soleReference.position;
            float range = hand ? profile.wallProbeRange : profile.wallProbeRange * 0.63f;
            RaycastHit hit = default;
            if (phase > 0.001f && normal.sqrMagnitude > 0.5f &&
                TrySurface(marker + normal * range, -normal, 2f * range, normal, 0.68f, traversal.Surface, out hit))
            {
                float distance = Mathf.Abs(Vector3.Dot(hit.point - marker, normal));
                phase *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(range * 0.86f, range, distance));
            }
            limb.Follow(hit, marker, normal, hand ? 0.045f : 0.015f, phase,
                hand ? profile.handCorrection : profile.footCorrection * 0.7f, profile.horizontalAnchor, response);
            limb.Apply(limb.Offset * weight, pole, Quaternion.identity, profile);
        }

        void AdaptObstacle(BodyFlowMotionIntent motion, AnimatorStateInfo state, float response)
        {
            BodyFlowTraversal traversal = motion.Traversal;
            int clip = state.shortNameHash;
            float t = Mathf.Clamp01(state.normalizedTime);
            float expectedHeight = traversal.Destination.y - (transform.position.y - motion.CapsuleBottom);
            if (traversal.Action == BodyFlowAction.TopOut && clip == LedgeTopOut)
            {
                float settling = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.97f, traversal.Progress));
                SettleOnDeck(expectedHeight, settling);
            }

            Vector2 hands = Vector2.zero;
            if (clip == VaultLow)
                hands.x = BodyFlowKinematics.Window(t, 0.18f, 0.47f, 0.09f);
            else if (clip == VaultMedium)
                hands = new Vector2(BodyFlowKinematics.Window(t, 0.09f, 0.43f, 0.09f),
                    BodyFlowKinematics.Window(t, 0.14f, 0.49f, 0.09f));
            else if (clip == VaultHigh)
                hands = Vector2.one * BodyFlowKinematics.Window(t, 0.28f, 0.58f, 0.10f);
            else if (clip == LedgeTopOut)
                hands = Vector2.one * BodyFlowKinematics.Window(t, 0f, 0.57f, 0.13f);

            hands *= BodyFlowKinematics.Window(traversal.Progress, -0.06f, 0.83f, 0.12f);
            ObstacleHand(ref leftArm, traversal, hands.x, -transform.right, response);
            ObstacleHand(ref rightArm, traversal, hands.y, transform.right, response);

            if (traversal.Action == BodyFlowAction.Vault && traversal.Surface && traversal.Progress < 0.76f)
            {
                footSupport = Vector2.one * (0.6f * BodyFlowKinematics.Window(t, 0.1f, 0.79f, 0.18f));
                AdaptGround(footSupport, false, false, 0f, response, traversal.Surface);
                return;
            }

            float reception = Mathf.SmoothStep(0f, 0.7f, Mathf.InverseLerp(0.76f, 1f, traversal.Progress));
            footSupport = Vector2.one * reception;
            AdaptGround(footSupport, false, true, expectedHeight, response);
        }

        void SettleOnDeck(float expectedHeight, float blend)
        {
            if (blend <= 0f || profile.topOutSettleLimit <= 0f)
                return;

            Vector3 left = leftLeg.Bones.soleReference.position;
            Vector3 right = rightLeg.Bones.soleReference.position;
            Vector3 low = left.y <= right.y ? left : right;
            Vector3 high = left.y <= right.y ? right : left;
            if (!FindDeck(low, expectedHeight, out RaycastHit deck) && !FindDeck(high, expectedHeight, out deck))
                return;

            float drop = Mathf.Clamp(low.y - deck.point.y - profile.solePadding, 0f, profile.topOutSettleLimit);
            MovePelvis(-drop * blend * weight);
        }

        bool FindDeck(Vector3 marker, float expectedHeight, out RaycastHit hit)
        {
            return TrySurface(marker + Vector3.up * 0.16f, Vector3.down, profile.topOutSettleLimit + 0.42f,
                Vector3.up, GroundNormalLimit(), null, out hit) &&
                Mathf.Abs(hit.point.y - expectedHeight) < 0.19f;
        }

        void ObstacleHand(ref LimbCorrection hand, BodyFlowTraversal traversal, float phase, Vector3 pole, float response)
        {
            Vector3 wrist = hand.Bones.end.position;
            float range = profile.handProbeRange;
            RaycastHit hit = default;
            if (phase > 0.001f && traversal.Surface)
            {
                if (!TrySurface(wrist + Vector3.up * range, Vector3.down, 2f * range,
                    Vector3.up, GroundNormalLimit(), traversal.Surface, out hit))
                {
                    Vector3 towardLip = -Vector3.ProjectOnPlane(traversal.Normal, Vector3.up).normalized;
                    TrySurface(wrist + towardLip * 0.12f + Vector3.up * range, Vector3.down, 2f * range,
                        Vector3.up, GroundNormalLimit(), traversal.Surface, out hit);
                }
                if (!hit.collider && traversal.Action == BodyFlowAction.TopOut && traversal.Progress < 0.33f)
                {
                    WallLimb(ref hand, traversal, phase, true, pole, response);
                    return;
                }
            }
            hand.Follow(hit, wrist, Vector3.up, 0.05f, phase, profile.handCorrection,
                Mathf.Min(0.10f, profile.handCorrection), response);
            hand.Apply(hand.Offset * weight, pole, Quaternion.identity, profile);
        }

        bool TrySurface(Vector3 origin, Vector3 direction, float range, Vector3 normal, float minDot,
            Collider required, out RaycastHit hit)
        {
            int mask = movement.movementCollisionMask.value;
            if (required)
                mask |= movement.parkourMask.value;
            int count = Physics.RaycastNonAlloc(origin, direction, hits, range, mask,
                QueryTriggerInteraction.Ignore);
            hit = default;
            if (count == hits.Length)
                return false;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = hits[i];
                Collider surface = candidate.collider;
                if (!surface || candidate.distance >= nearest || surface.transform.IsChildOf(transform) ||
                    Vector3.Dot(candidate.normal, normal) < minDot || (required && !SharesSurface(required, surface)))
                    continue;
                nearest = candidate.distance;
                hit = candidate;
            }
            return hit.collider;
        }

        static bool SharesSurface(Collider expected, Collider candidate)
        {
            if (expected == candidate || (expected.attachedRigidbody && expected.attachedRigidbody == candidate.attachedRigidbody))
                return true;
            ClimbableSurface owner = expected.GetComponentInParent<ClimbableSurface>();
            return owner && candidate.GetComponentInParent<ClimbableSurface>() == owner;
        }

        void RestoreAnimatorPose()
        {
            leftLeg.Restore();
            rightLeg.Restore();
            leftArm.Restore();
            rightArm.Restore();
            if (pelvisWritten && rig.pelvis)
                rig.pelvis.localPosition = animatedPelvis;
            pelvisWritten = false;
        }

        void ResetContacts()
        {
            leftLeg.ResetContact();
            rightLeg.ResetContact();
            leftArm.ResetContact();
            rightArm.ResetContact();
            footSupport = Vector2.zero;
            pelvisOffset = 0f;
        }

        void OnDisable()
        {
            RestoreAnimatorPose();
            ResetContacts();
            weight = 0f;
        }

        [ContextMenu("Log BODYFLOW state")]
        void LogState() => Debug.Log($"BODYFLOW {Action}, phase {StepPhase:F2}, weight {PoseWeight:F2}, " +
            $"foot support {FootSupport:F2}, applied foot {FootAdjustment:F3} m, hand {HandAdjustment:F3} m.", this);

        void OnDrawGizmosSelected()
        {
            if (!showGizmos || !initialized || weight <= 0f)
                return;

            DrawContact(leftLeg, Color.cyan);
            DrawContact(rightLeg, Color.cyan);
            DrawContact(leftArm, Color.green);
            DrawContact(rightArm, Color.green);
        }

        static void DrawContact(LimbCorrection limb, Color color)
        {
            if (limb.Influence <= 0.05f || !limb.Bones.end)
                return;
            Gizmos.color = color;
            Gizmos.DrawLine(limb.Bones.end.position, limb.Target);
            Gizmos.DrawWireSphere(limb.Target, 0.035f);
        }
    }
}
