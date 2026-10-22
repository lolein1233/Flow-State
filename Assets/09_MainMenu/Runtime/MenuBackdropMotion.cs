using UnityEngine;

namespace FlowState.Menu
{
    [DisallowMultipleComponent]
    public sealed class MenuBackdropMotion : MonoBehaviour
    {
        [Header("Composition")]
        public Renderer wall;
        public CanMotion canMotion;
        public FlowStateMenuCameraController cameraController;
        public MenuCameraFeedback accessibility;

        [Header("Holographic mask")]
        [Range(0f, 1f)] public float wallMotion = 0.82f;
        [Range(0f, 1f)] public float hologramIntensity = 0.88f;
        [Range(0f, 1f)] public float foilStrength = 0.74f;
        [Range(0f, 1f)] public float foilDarkness = 0.28f;

        public float CanImpulse => canImpulse;

        static readonly int MotionAmountId = Shader.PropertyToID("_BackdropMotion");
        static readonly int CanImpulseId = Shader.PropertyToID("_BackdropImpulse");
        static readonly int CanDirectionId = Shader.PropertyToID("_BackdropDirection");
        static readonly int ParallaxId = Shader.PropertyToID("_BackdropParallax");
        static readonly int HologramIntensityId = Shader.PropertyToID("_BackdropHoloIntensity");
        static readonly int FoilStrengthId = Shader.PropertyToID("_BackdropFoilStrength");
        static readonly int FoilDarknessId = Shader.PropertyToID("_BackdropFoilDarkness");

        MaterialPropertyBlock wallProperties;
        float canImpulse;
        float canDirection;
        float impulseAge = float.PositiveInfinity;

        void Awake()
        {
            wallProperties = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            if (canMotion)
                canMotion.RotationStarted += OnCanRotationStarted;
        }

        void OnDisable()
        {
            if (canMotion)
                canMotion.RotationStarted -= OnCanRotationStarted;
        }

        void OnCanRotationStarted(int direction, float duration)
        {
            canDirection = Mathf.Sign(direction);
            canImpulse = 1f;
            impulseAge = 0f;
        }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            impulseAge += dt;
            if (!float.IsPositiveInfinity(impulseAge))
            {
                float decay = Mathf.Exp(-impulseAge * 3.45f);
                float graffitiSnap = 0.82f + Mathf.Sin(impulseAge * 24f) * 0.18f;
                canImpulse = Mathf.Max(0f, decay * graffitiSnap);
                if (canImpulse < 0.002f)
                {
                    canImpulse = 0f;
                    impulseAge = float.PositiveInfinity;
                }
            }

            float reducedMotion = accessibility ? accessibility.motionScale : 1f;
            Vector2 parallax = cameraController ? cameraController.Response : Vector2.zero;

            if (!wall)
                return;

            wall.GetPropertyBlock(wallProperties);
            wallProperties.SetFloat(MotionAmountId, wallMotion * reducedMotion);
            wallProperties.SetFloat(HologramIntensityId, hologramIntensity);
            wallProperties.SetFloat(FoilStrengthId, foilStrength);
            wallProperties.SetFloat(FoilDarknessId, foilDarkness);
            wallProperties.SetFloat(CanImpulseId, canImpulse * reducedMotion);
            wallProperties.SetFloat(CanDirectionId, canDirection);
            wallProperties.SetVector(ParallaxId, new Vector4(parallax.x, parallax.y, 0f, 0f));
            wall.SetPropertyBlock(wallProperties);
        }
    }
}
