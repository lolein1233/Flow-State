using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowState.Rendering
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PlayerComicBorder : MonoBehaviour
    {
        private const string ShaderName = "FLOWSTATE/FSSRS/Player Comic Plate";
        private const string GeneratedRootName = "__FSSRS_PlayerComicBorder";

        [SerializeField] private Transform sourceRoot;
        [SerializeField, Min(0.001f)] private float echoWidth = 0.078f;
        [SerializeField, Min(0.001f)] private float paperWidth = 0.058f;
        [SerializeField, Min(0.001f)] private float colorWidth = 0.041f;
        [SerializeField, Min(0.001f)] private float inkWidth = 0.019f;
        [SerializeField] private Vector2 echoRegistration = new(1.8f, -1.1f);
        [SerializeField] private Vector2 paperRegistration = new(-0.55f, 0.25f);
        [SerializeField] private Vector2 colorRegistration = new(0.85f, -0.45f);
        [SerializeField] private Vector2 inkRegistration = new(-0.12f, 0.08f);
        [SerializeField, Range(0f, 0.5f)] private float edgeJitter = 0.16f;
        [SerializeField, Range(0f, 0.5f)] private float colorBreakup = 0.08f;

        private readonly List<ShellBinding> bindings = new();
        private Transform generatedRoot;
        private Material echoMaterial;
        private Material paperMaterial;
        private Material colorMaterial;
        private Material inkMaterial;

        private sealed class ShellBinding
        {
            public Renderer source;
            public Renderer[] plates;
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnValidate()
        {
            echoWidth = Mathf.Max(echoWidth, paperWidth + 0.001f);
            paperWidth = Mathf.Max(paperWidth, colorWidth + 0.001f);
            colorWidth = Mathf.Max(colorWidth, inkWidth + 0.001f);
            inkWidth = Mathf.Max(0.001f, inkWidth);
        }

        private void LateUpdate()
        {
            foreach (ShellBinding binding in bindings)
            {
                if (binding.source == null)
                    continue;

                bool visible = binding.source.enabled && binding.source.gameObject.activeInHierarchy;
                foreach (Renderer plate in binding.plates)
                {
                    if (plate != null)
                        plate.enabled = visible;
                }

                if (binding.source is not SkinnedMeshRenderer sourceSkinned)
                    continue;

                foreach (Renderer plate in binding.plates)
                {
                    if (plate is not SkinnedMeshRenderer plateSkinned)
                        continue;

                    int count = Mathf.Min(sourceSkinned.sharedMesh.blendShapeCount, plateSkinned.sharedMesh.blendShapeCount);
                    for (int index = 0; index < count; index++)
                        plateSkinned.SetBlendShapeWeight(index, sourceSkinned.GetBlendShapeWeight(index));
                }
            }
        }

        public void Configure(Transform newSourceRoot)
        {
            sourceRoot = newSourceRoot;
            ApplyExpressiveDefaults();
            Rebuild();
        }

        [ContextMenu("Apply Expressive Border Defaults")]
        public void ApplyExpressiveDefaults()
        {
            echoWidth = 0.078f;
            paperWidth = 0.058f;
            colorWidth = 0.041f;
            inkWidth = 0.019f;
            echoRegistration = new Vector2(1.8f, -1.1f);
            paperRegistration = new Vector2(-0.55f, 0.25f);
            colorRegistration = new Vector2(0.85f, -0.45f);
            inkRegistration = new Vector2(-0.12f, 0.08f);
            edgeJitter = 0.16f;
            colorBreakup = 0.08f;
        }

        [ContextMenu("Rebuild Comic Border")]
        public void Rebuild()
        {
            Cleanup();

            Transform targetRoot = sourceRoot != null ? sourceRoot : transform;
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Player comic border shader '{ShaderName}' was not found.", this);
                return;
            }

            echoMaterial = CreatePlateMaterial(shader, "Chromatic Echo", 3f, echoWidth, echoRegistration, edgeJitter * 1.05f, colorBreakup, 0.7f, 2.2f, 0.15f, 1.15f, 0.65f, 3004);
            paperMaterial = CreatePlateMaterial(shader, "Paper Cut", 0f, paperWidth, paperRegistration, edgeJitter * 0.25f, 0f, 1f, 0.4f, 0.035f, 0.55f, 0.12f, 3005);
            colorMaterial = CreatePlateMaterial(shader, "Emotion Registration", 1f, colorWidth, colorRegistration, edgeJitter, colorBreakup, 1f, 1.1f, 0.09f, 1f, 0.34f, 3006);
            inkMaterial = CreatePlateMaterial(shader, "Broken Ink", 2f, inkWidth, inkRegistration, edgeJitter * 0.5f, colorBreakup * 0.25f, 1f, 1.8f, 0.025f, 0.7f, 0.18f, 3007);

            GameObject rootObject = new(GeneratedRootName)
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = gameObject.layer
            };
            generatedRoot = rootObject.transform;
            generatedRoot.SetParent(targetRoot, false);

            Renderer[] sources = targetRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer source in sources)
            {
                if (!CanOutline(source))
                    continue;

                Renderer[] plates =
                {
                    CreatePlateRenderer(source, echoMaterial, "Echo"),
                    CreatePlateRenderer(source, paperMaterial, "Paper"),
                    CreatePlateRenderer(source, colorMaterial, "Emotion"),
                    CreatePlateRenderer(source, inkMaterial, "Ink")
                };
                bindings.Add(new ShellBinding { source = source, plates = plates });
            }
        }

        private Renderer CreatePlateRenderer(Renderer source, Material material, string plateName)
        {
            GameObject plateObject = new($"{plateName} Plate - {source.name}")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = source.gameObject.layer
            };
            plateObject.transform.SetParent(source.transform, false);

            Renderer plate;
            Mesh mesh;
            if (source is SkinnedMeshRenderer sourceSkinned)
            {
                mesh = sourceSkinned.sharedMesh;
                SkinnedMeshRenderer skinned = plateObject.AddComponent<SkinnedMeshRenderer>();
                skinned.sharedMesh = mesh;
                skinned.bones = sourceSkinned.bones;
                skinned.rootBone = sourceSkinned.rootBone;
                skinned.localBounds = sourceSkinned.localBounds;
                skinned.quality = sourceSkinned.quality;
                skinned.updateWhenOffscreen = true;
                plate = skinned;
            }
            else
            {
                MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                mesh = sourceFilter.sharedMesh;
                MeshFilter filter = plateObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                plate = plateObject.AddComponent<MeshRenderer>();
            }

            int materialCount = Mathf.Max(1, source.sharedMaterials.Length, mesh.subMeshCount);
            Material[] materials = new Material[materialCount];
            for (int index = 0; index < materials.Length; index++)
                materials[index] = material;

            plate.sharedMaterials = materials;
            plate.shadowCastingMode = ShadowCastingMode.Off;
            plate.receiveShadows = false;
            plate.allowOcclusionWhenDynamic = false;
            plate.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            plate.renderingLayerMask = source.renderingLayerMask;
            return plate;
        }

        private static Material CreatePlateMaterial(
            Shader shader,
            string plateName,
            float role,
            float width,
            Vector2 registration,
            float jitter,
            float breakup,
            float alpha,
            float animationPhase,
            float pulseAmount,
            float flowSpeed,
            float panelMotion,
            int renderQueue)
        {
            Material material = new(shader)
            {
                name = "Runtime Player Comic " + plateName,
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = renderQueue
            };
            material.SetFloat("_PlateRole", role);
            material.SetFloat("_ShellWidth", width);
            material.SetVector("_RegistrationOffset", new Vector4(registration.x, registration.y, 0f, 0f));
            material.SetFloat("_JitterAmount", jitter);
            material.SetFloat("_Breakup", breakup);
            material.SetFloat("_Alpha", alpha);
            material.SetFloat("_AnimationPhase", animationPhase);
            material.SetFloat("_PulseAmount", pulseAmount);
            material.SetFloat("_FlowSpeed", flowSpeed);
            material.SetFloat("_PanelMotion", panelMotion);
            return material;
        }

        private bool CanOutline(Renderer source)
        {
            if (source == null || source.transform.IsChildOf(generatedRoot))
                return false;
            if (source.sharedMaterial != null && source.sharedMaterial.shader != null &&
                source.sharedMaterial.shader.name == ShaderName)
                return false;
            if (source is LineRenderer or TrailRenderer or ParticleSystemRenderer or SpriteRenderer)
                return false;
            if (source is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh != null;

            return source is MeshRenderer && source.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null;
        }

        private void Cleanup()
        {
            foreach (ShellBinding binding in bindings)
            {
                if (binding.plates == null)
                    continue;

                foreach (Renderer plate in binding.plates)
                {
                    if (plate != null)
                        DestroyRuntimeObject(plate.gameObject);
                }
            }

            bindings.Clear();
            DestroyRuntimeObject(generatedRoot != null ? generatedRoot.gameObject : null);
            generatedRoot = null;
            DestroyRuntimeObject(echoMaterial);
            DestroyRuntimeObject(paperMaterial);
            DestroyRuntimeObject(colorMaterial);
            DestroyRuntimeObject(inkMaterial);
            echoMaterial = null;
            paperMaterial = null;
            colorMaterial = null;
            inkMaterial = null;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
