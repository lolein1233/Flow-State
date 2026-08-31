using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FlowState.Rendering.Editor
{
    public static class FSSRSLookDevBuilder
    {
        private const string Root = "Assets/07_StylizedRendering/FSSRS";
        private const string SourceScene = "Assets/02_Escenas/Game.unity";
        private const string LookDevScene = "Assets/02_Escenas/Game_LookDev_FSSRS.unity";
        private const string SourceRenderer = "Assets/Settings/PC_Renderer.asset";
        private const string LookDevRenderer = Root + "/Renderer/FSSRS_PC_Renderer.asset";
        private const string PipelineAssetPath = "Assets/Settings/PC_RPAsset.asset";
        private const string LookDevProfilePath = Root + "/Profiles/FSSRS_LookDev_Profile.asset";

        [MenuItem("FLOW STATE/FSSRS/Build LookDev V1")]
        public static void BuildLookDev()
        {
            try
            {
                Scene currentScene = SceneManager.GetActiveScene();
                if (currentScene.isDirty)
                    throw new InvalidOperationException("Save or discard the current scene changes before building LookDev.");

                EnsureFolders();
                CreatePrintTextures();
                Dictionary<string, FlowPaletteProfile> palettes = CreatePalettes();
                Dictionary<string, FSSRSStylePreset> presets = CreatePresets();
                VolumeProfile profile = CreateLookDevProfile(presets["Street"]);
                UniversalRendererData rendererData = CreateRenderer();
                int rendererIndex = RegisterRenderer(rendererData);
                Scene lookDevScene = CreateOrOpenLookDevScene();

                ConfigureScene(lookDevScene, rendererIndex, profile, palettes["CreativeFlow"], presets["Street"]);
                ConvertLookDevMaterials();

                EditorSceneManager.MarkSceneDirty(lookDevScene);
                EditorSceneManager.SaveScene(lookDevScene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(LookDevScene);
                Debug.Log("FSSRS LookDev V1 built successfully. Original scene and materials were not modified.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                Root + "/Renderer",
                Root + "/Profiles",
                Root + "/Palettes",
                Root + "/Presets",
                Root + "/Textures/Ink",
                Root + "/Textures/Halftone",
                Root + "/Textures/Hatching",
                Root + "/Textures/Grunge",
                Root + "/Materials/LookDev/Characters",
                Root + "/Materials/LookDev/Environment"
            };

            foreach (string folder in folders)
                EnsureFolder(folder);
        }

        private static void EnsureFolder(string assetPath)
        {
            string current = "Assets";
            foreach (string part in assetPath.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static void CreatePrintTextures()
        {
            CreateTexture(Root + "/Textures/Ink/FS_InkBreakup_01.png", (x, y, size) =>
            {
                float coarse = Hash(x / 4, y / 4, 17);
                float fine = Hash(x, y, 41);
                float value = Mathf.Lerp(coarse, fine, 0.32f);
                return Color.white * Mathf.Lerp(0.58f, 1f, value);
            });

            CreateTexture(Root + "/Textures/Hatching/FS_Hatching_01.png", (x, y, size) =>
            {
                float stripe = Mathf.Repeat(x + y * 2f, 15f);
                return stripe < 2f ? new Color(0.18f, 0.18f, 0.18f, 1f) : Color.white;
            });

            CreateTexture(Root + "/Textures/Hatching/FS_Hatching_02.png", (x, y, size) =>
            {
                bool first = Mathf.Repeat(x + y, 18f) < 2f;
                bool second = Mathf.Repeat(-x + y, 23f) < 1.5f;
                return first || second ? new Color(0.12f, 0.12f, 0.12f, 1f) : Color.white;
            });

            CreateTexture(Root + "/Textures/Halftone/FS_HalftoneDots_01.png", (x, y, size) =>
            {
                Vector2 cell = new(Mathf.Repeat(x, 16f) - 8f, Mathf.Repeat(y, 16f) - 8f);
                return cell.magnitude < 3.5f ? Color.black : Color.white;
            });

            CreateTexture(Root + "/Textures/Grunge/FS_PrintGrain_01.png", (x, y, size) =>
            {
                float value = Hash(x, y, 89);
                value = value > 0.82f ? 0.35f : Mathf.Lerp(0.82f, 1f, value);
                return Color.white * value;
            });
        }

        private static void CreateTexture(string assetPath, Func<int, int, int, Color> pixelFactory)
        {
            if (File.Exists(assetPath))
                return;

            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = pixelFactory(x, y, size);

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.sRGBTexture = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static float Hash(int x, int y, int seed)
        {
            uint value = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            value = (value ^ (value >> 13)) * 1274126177u;
            return (value ^ (value >> 16)) / (float)uint.MaxValue;
        }

        private static Dictionary<string, FlowPaletteProfile> CreatePalettes()
        {
            return new Dictionary<string, FlowPaletteProfile>
            {
                ["Neutral"] = CreatePalette("FP_Neutral_Unfinished", "E8E1D2", "111015", "253247", "76AFC0", "E7E1CE", "E7AF24"),
                ["Doubt"] = CreatePalette("FP_Doubt", "D8D6D1", "15131A", "28304B", "687787", "C9C6BA", "B54863"),
                ["Anger"] = CreatePalette("FP_Anger", "E2D9CF", "100C0C", "3A1518", "C1392F", "F4C663", "FF3B1F"),
                ["CreativeFlow"] = CreatePalette("FP_CreativeFlow", "E7DFC4", "0E1017", "2F1851", "C02FA8", "F7E33B", "16C9D5"),
                ["Clarity"] = CreatePalette("FP_Clarity", "F0E9D7", "11100F", "2C5862", "6CC5C0", "F5E36B", "E84A32")
            };
        }

        private static FlowPaletteProfile CreatePalette(
            string name,
            string paper,
            string ink,
            string shadow,
            string mid,
            string highlight,
            string accent)
        {
            string path = Root + "/Palettes/" + name + ".asset";
            FlowPaletteProfile palette = AssetDatabase.LoadAssetAtPath<FlowPaletteProfile>(path);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<FlowPaletteProfile>();
                palette.name = name;
                AssetDatabase.CreateAsset(palette, path);
            }

            FlowPaletteState state = new()
            {
                paper = ParseColor(paper),
                ink = ParseColor(ink),
                shadow = ParseColor(shadow),
                mid = ParseColor(mid),
                highlight = ParseColor(highlight),
                accent = ParseColor(accent)
            };
            palette.Configure(state);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }

        private static Dictionary<string, FSSRSStylePreset> CreatePresets()
        {
            return new Dictionary<string, FSSRSStylePreset>
            {
                ["Clean"] = CreatePreset("FSSRS_Clean", 0.55f, 1f, 0, 0.04f, 0f, 0f, 0.12f),
                ["Comic"] = CreatePreset("FSSRS_Comic", 0.88f, 1.25f, 6, 0.08f, 0.1f, 0.14f, 0.24f),
                ["Street"] = CreatePreset("FSSRS_Street", 0.88f, 1.4f, 6, 0.055f, 0.06f, 0.1f, 0.24f),
                ["Punk"] = CreatePreset("FSSRS_Punk", 1f, 1.9f, 4, 0.16f, 0.16f, 0.25f, 0.4f),
                ["Identity"] = CreatePreset("FSSRS_Identity", 0.96f, 1.55f, 5, 0.1f, 0.18f, 0.2f, 0.42f)
            };
        }

        private static FSSRSStylePreset CreatePreset(
            string name,
            float outline,
            float thickness,
            int posterize,
            float grain,
            float halftone,
            float hatch,
            float paletteInfluence)
        {
            string path = Root + "/Presets/" + name + ".asset";
            FSSRSStylePreset preset = AssetDatabase.LoadAssetAtPath<FSSRSStylePreset>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<FSSRSStylePreset>();
                preset.name = name;
                AssetDatabase.CreateAsset(preset, path);
            }

            preset.outlineIntensity = outline;
            preset.outlineThickness = thickness;
            preset.depthThreshold = 0.012f;
            preset.normalThreshold = 0.22f;
            preset.lumaThreshold = 0.17f;
            preset.posterizeSteps = posterize;
            preset.grainIntensity = grain;
            preset.halftoneIntensity = halftone;
            preset.halftoneScale = 4f;
            preset.hatchIntensity = hatch;
            preset.paletteInfluence = paletteInfluence;
            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static VolumeProfile CreateLookDevProfile(FSSRSStylePreset streetPreset)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LookDevProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "FSSRS LookDev Profile";
                AssetDatabase.CreateAsset(profile, LookDevProfilePath);
            }

            profile.components.RemoveAll(component => component == null);

            FSSRSVolumeComponent fssrs = GetOrAdd<FSSRSVolumeComponent>(profile);
            fssrs.enabledEffect.Override(true);
            fssrs.outlineColor.Override(ParseColor("111015"));
            streetPreset.ApplyTo(fssrs);

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(0.8f);

            MotionBlur motionBlur = GetOrAdd<MotionBlur>(profile);
            motionBlur.intensity.Override(0f);

            ChromaticAberration chromaticAberration = GetOrAdd<ChromaticAberration>(profile);
            chromaticAberration.intensity.Override(0f);

            FilmGrain filmGrain = GetOrAdd<FilmGrain>(profile);
            filmGrain.intensity.Override(0f);

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.16f);
            vignette.smoothness.Override(0.3f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
                return component;

            component = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static UniversalRendererData CreateRenderer()
        {
            if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(LookDevRenderer) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceRenderer, LookDevRenderer))
                    throw new InvalidOperationException("Could not duplicate the PC renderer.");
                AssetDatabase.ImportAsset(LookDevRenderer, ImportAssetOptions.ForceSynchronousImport);
            }

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(LookDevRenderer);
            if (!rendererData.TryGetRendererFeature(out FSSRSRendererFeature _))
                AddRendererFeature(rendererData);

            rendererData.SetDirty();
            EditorUtility.SetDirty(rendererData);
            return rendererData;
        }

        private static void AddRendererFeature(UniversalRendererData rendererData)
        {
            FSSRSRendererFeature feature = ScriptableObject.CreateInstance<FSSRSRendererFeature>();
            feature.name = "FSSRS Print Composite";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            SerializedObject serializedRenderer = new(rendererData);
            SerializedProperty features = serializedRenderer.FindProperty("m_RendererFeatures");
            SerializedProperty featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            int index = features.arraySize;
            features.InsertArrayElementAtIndex(index);
            features.GetArrayElementAtIndex(index).objectReferenceValue = feature;
            featureMap.InsertArrayElementAtIndex(index);
            featureMap.GetArrayElementAtIndex(index).longValue = localId;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int RegisterRenderer(UniversalRendererData rendererData)
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            SerializedObject serializedPipeline = new(pipeline);
            SerializedProperty rendererList = serializedPipeline.FindProperty("m_RendererDataList");

            for (int index = 0; index < rendererList.arraySize; index++)
            {
                if (rendererList.GetArrayElementAtIndex(index).objectReferenceValue == rendererData)
                    return index;
            }

            int newIndex = rendererList.arraySize;
            rendererList.InsertArrayElementAtIndex(newIndex);
            rendererList.GetArrayElementAtIndex(newIndex).objectReferenceValue = rendererData;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return newIndex;
        }

        private static Scene CreateOrOpenLookDevScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LookDevScene) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScene, LookDevScene))
                    throw new InvalidOperationException("Could not duplicate the Game scene.");
                AssetDatabase.ImportAsset(LookDevScene, ImportAssetOptions.ForceSynchronousImport);
            }

            return EditorSceneManager.OpenScene(LookDevScene, OpenSceneMode.Single);
        }

        private static void ConfigureScene(
            Scene scene,
            int rendererIndex,
            VolumeProfile profile,
            FlowPaletteProfile palette,
            FSSRSStylePreset preset)
        {
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (camera == null)
                throw new InvalidOperationException("The LookDev scene has no camera.");

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.SetRenderer(rendererIndex);
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;

            GameObject root = GameObject.Find("FSSRS LookDev");
            if (root == null)
                root = new GameObject("FSSRS LookDev");

            Volume volume = root.GetComponent<Volume>();
            if (volume == null)
                volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            FlowStatePaletteController controller = root.GetComponent<FlowStatePaletteController>();
            if (controller == null)
                controller = root.AddComponent<FlowStatePaletteController>();
            controller.Configure(palette, preset, volume);

            SceneManager.MoveGameObjectToScene(root, scene);
            EditorUtility.SetDirty(cameraData);
            EditorUtility.SetDirty(volume);
            EditorUtility.SetDirty(controller);
        }

        private static void ConvertLookDevMaterials()
        {
            Shader shader = Shader.Find("FLOWSTATE/FSSRS/Stylized Lit");
            if (shader == null)
                throw new InvalidOperationException("FSSRS stylized shader was not found.");

            Texture ink = AssetDatabase.LoadAssetAtPath<Texture>(Root + "/Textures/Ink/FS_InkBreakup_01.png");
            Texture hatch = AssetDatabase.LoadAssetAtPath<Texture>(Root + "/Textures/Hatching/FS_Hatching_02.png");
            ConvertHierarchy("Player", true, shader, ink, hatch);
            ConvertHierarchy("Blockin_FS", false, shader, ink, hatch);
            ConvertHierarchy("Plane", false, shader, ink, hatch);
        }

        private static void ConvertHierarchy(string rootName, bool character, Shader shader, Texture ink, Texture hatch)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
                return;

            Dictionary<Material, Material> converted = new();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material source = materials[index];
                    if (source == null || source.shader == null || source.shader.name != "Universal Render Pipeline/Lit")
                        continue;

                    if (!converted.TryGetValue(source, out Material replacement))
                    {
                        replacement = CreateLookDevMaterial(source, character, shader, ink, hatch);
                        converted.Add(source, replacement);
                    }

                    materials[index] = replacement;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Material CreateLookDevMaterial(Material source, bool character, Shader shader, Texture ink, Texture hatch)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string guid = AssetDatabase.AssetPathToGUID(sourcePath);
            string safeName = SanitizeFileName(source.name);
            string folder = Root + "/Materials/LookDev/" + (character ? "Characters" : "Environment");
            string destination = $"{folder}/{safeName}_{guid[..Math.Min(8, guid.Length)]}_FSSRS.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(destination);
            if (material == null)
            {
                Texture baseMap = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
                Color baseColor = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
                Texture normalMap = source.HasProperty("_BumpMap") ? source.GetTexture("_BumpMap") : null;
                float normalScale = source.HasProperty("_BumpScale") ? source.GetFloat("_BumpScale") : 1f;

                material = new Material(shader) { name = source.name + " FSSRS" };
                material.SetTexture("_BaseMap", baseMap);
                material.SetColor("_BaseColor", baseColor);
                material.SetTexture("_BumpMap", normalMap);
                material.SetFloat("_BumpScale", normalScale);
                AssetDatabase.CreateAsset(material, destination);
            }

            material.SetTexture("_InkTexture", ink);
            material.SetTexture("_HatchTexture", hatch);
            material.SetFloat("_BandCount", character ? 3f : 4f);
            material.SetFloat("_PaletteInfluence", character ? 0.82f : 0.58f);
            material.SetFloat("_HatchStrength", character ? 0.24f : 0.18f);
            material.SetFloat("_HalftoneStrength", character ? 0.12f : 0.06f);
            material.SetFloat("_InkBreakup", character ? 0.1f : 0.18f);
            material.SetFloat("_RimStrength", character ? 0.55f : 0.08f);
            material.SetColor("_RimColor", character ? ParseColor("16C9D5") : ParseColor("E7AF24"));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") != null);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace('/', '_').Replace('\\', '_');
        }
    }
}
