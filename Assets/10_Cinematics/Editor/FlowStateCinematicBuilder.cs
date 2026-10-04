#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowState.Cinematics;
using FlowState.Rendering;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace FlowState.Cinematics.Editor
{
    [InitializeOnLoad]
    public static class FlowStateCinematicBuilder
    {
        public const string ScenePath = "Assets/10_Cinematics/Scenes/FlowState_Trailer_Cinematic.unity";
        public const string TimelinePath = "Assets/10_Cinematics/Timelines/FlowState_Trailer_25s_98s.playable";
        public const string AudioPath = "Assets/10_Cinematics/Audio/FlowState_MadeYouLook_TrailerEdit_PREVIEW.mp3";
        public const string RootName = "CINEMATIC_ROOT";
        public const double Duration = 73.0;
        public const float SourceAudioIn = 25.0f;
        public const float FrameRate = 30.0f;

        private const string AnimationFolder = "Assets/10_Cinematics/Animations";
        private const string MaterialFolder = "Assets/10_Cinematics/Materials";
        private const string ProfilePath = "Assets/10_Cinematics/Profiles/FlowState_Cinematic_Profile.asset";
        private const string LogoPath = "Assets/04_Materiales e Imagenes/imagenes/YakuzaStudio_FLOWSTATE (1).png";
        private const string GraffitiMenuPath = "Assets/03_Prefabs/DrawPaintMenu.prefab";
        private const string SprayCanMaterialPath = "Assets/07_StylizedRendering/FSSRS/Materials/LookDev/Characters/LATA_f865e23c_FSSRS.mat";
        private const string FssrsRendererPath = "Assets/07_StylizedRendering/FSSRS/Renderer/FSSRS_PC_Renderer.asset";
        private const string PcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        private static readonly Vector3 StageOrigin = new Vector3(-123.84f, 203.4f, 271.8f);
        private const string RenderSessionKey = "FlowState.Cinematics.RenderPending";

        private static RecorderController recorderController;

        private sealed class ShotDefinition
        {
            public string Name;
            public double Start;
            public double End;
            public float FieldOfView;
            public float Dutch;
            public Vector3[] Positions;
            public Vector3[] Targets;
        }

        static FlowStateCinematicBuilder()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Flow State/Cinematics/01 Build Trailer Scene")]
        public static void BuildTrailerScene()
        {
            EnsureFolders();
            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            if (audioClip == null)
                throw new InvalidOperationException("Missing trailer audio at " + AudioPath);
            if (audioClip.length + 0.01f < SourceAudioIn + (float)Duration)
                throw new InvalidOperationException("The audio clip is too short for the requested 00:25-01:38 section.");

            PrepareSceneCopy();
            CleanupGeneratedContent();

            GameObject root = new GameObject(RootName);
            PlayableDirector director = root.AddComponent<PlayableDirector>();

            Camera mainCamera = PrepareMainCamera(root.transform);
            CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
            PrepareSceneForCinematic(mainCamera);

            TimelineAsset timeline = CreateTimelineAsset();
            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;

            Dictionary<string, Material> materials = CreateMaterials();
            CreateCinematicGrade(root.transform);
            CreateLighting(root.transform);
            CreateShowcaseContent(root.transform, director, mainCamera, materials);

            CreateCameraSequence(root.transform, timeline, director, brain);
            CreateAudioSequence(root.transform, timeline, director, audioClip);

            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(mainCamera);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            director.RebuildGraph();
            director.time = 0d;
            director.Evaluate();
            CinemachineBrain.UpdateMethods previousUpdateMethod = brain.UpdateMethod;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            brain.ManualUpdate();
            brain.UpdateMethod = previousUpdateMethod;
            Selection.activeGameObject = root;
            Debug.Log("[FlowState Cinematic] Trailer scene built: " + ScenePath);
        }

        [MenuItem("Flow State/Cinematics/02 Capture Preview Frames")]
        public static void CapturePreviewFrames()
        {
            EnsureCinematicSceneIsOpen();
            GameObject root = GameObject.Find(RootName);
            PlayableDirector director = root != null ? root.GetComponent<PlayableDirector>() : null;
            FlowStateCinematicShowcase showcase = root != null ? root.GetComponent<FlowStateCinematicShowcase>() : null;
            CinemachineBrain brain = UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>();
            Camera camera = brain != null ? brain.GetComponent<Camera>() : null;
            if (director == null || camera == null || brain == null)
                throw new InvalidOperationException("Build the cinematic scene before capturing previews.");

            string folder = Path.Combine(Application.dataPath, "Screenshots/CinematicPreview");
            Directory.CreateDirectory(folder);
            double[] times = { 2.5, 7.5, 14.5, 21.5, 30.5, 40.5, 50.5, 60.0, 68.5 };

            RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            CinemachineBrain.UpdateMethods previousUpdateMethod = brain.UpdateMethod;
            try
            {
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
                director.RebuildGraph();
                for (int i = 0; i < times.Length; i++)
                {
                    director.time = times[i];
                    director.Evaluate();
                    if (showcase != null)
                        showcase.EvaluateAt(times[i]);
                    brain.ManualUpdate();
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    image.Apply(false);
                    string fileName = string.Format("FlowState_Preview_{0:00}_{1:00.00}s.png", i + 1, times[i]);
                    File.WriteAllBytes(Path.Combine(folder, fileName), image.EncodeToPNG());
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                director.time = 0d;
                director.Evaluate();
                if (showcase != null)
                    showcase.EvaluateAt(0d);
                brain.ManualUpdate();
                brain.UpdateMethod = previousUpdateMethod;
            }

            AssetDatabase.Refresh();
            Debug.Log("[FlowState Cinematic] Preview frames saved to Assets/Screenshots/CinematicPreview.");
        }

        [MenuItem("Flow State/Cinematics/03 Render Trailer MP4")]
        public static void RenderTrailerMp4()
        {
            EnsureCinematicSceneIsOpen();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before starting the cinematic render.");

            SessionState.SetBool(RenderSessionKey, true);
            foreach (CinemachineBrain brain in UnityEngine.Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None))
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[FlowState Cinematic] Starting 1920x1080 / 30 fps MP4 render with audio.");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Flow State/Cinematics/Open Trailer Scene")]
        public static void OpenTrailerScene()
        {
            if (!File.Exists(Path.GetFullPath(ScenePath)))
                throw new FileNotFoundException("Build the cinematic scene first.", ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "10_Cinematics");
            EnsureFolder("Assets/10_Cinematics", "Audio");
            EnsureFolder("Assets/10_Cinematics", "Animations");
            EnsureFolder("Assets/10_Cinematics", "Editor");
            EnsureFolder("Assets/10_Cinematics", "Materials");
            EnsureFolder("Assets/10_Cinematics", "Profiles");
            EnsureFolder("Assets/10_Cinematics", "Scenes");
            EnsureFolder("Assets/10_Cinematics", "Timelines");
            EnsureFolder("Assets/10_Cinematics", "Meshes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void PrepareSceneCopy()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath)
                return;

            if (!activeScene.IsValid() || !activeScene.isLoaded)
                throw new InvalidOperationException("No loaded source scene is available for the cinematic copy.");

            if (!EditorSceneManager.SaveScene(activeScene, ScenePath, true))
                throw new InvalidOperationException("Unity could not save the cinematic scene copy.");

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureCinematicSceneIsOpen()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                OpenTrailerScene();
            if (GameObject.Find(RootName) == null)
                BuildTrailerScene();
        }

        private static void CleanupGeneratedContent()
        {
            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
                UnityEngine.Object.DestroyImmediate(oldRoot);

            DeleteGeneratedAsset(TimelinePath);
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationFolder }))
                DeleteGeneratedAsset(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void DeleteGeneratedAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static Camera PrepareMainCamera(Transform cinematicRoot)
        {
            Camera camera = Camera.main;
            if (camera == null)
                camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.gameObject.SetActive(true);
            camera.transform.SetParent(cinematicRoot, true);
            camera.gameObject.tag = "MainCamera";
            camera.enabled = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.012f, 0.04f, 1f);
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 1800f;
            camera.depth = 10f;
            ConfigureFssrsCamera(camera);

            CinemachineBrain brain = camera.GetComponent<CinemachineBrain>();
            if (brain == null)
                brain = camera.gameObject.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.8f);

            MonoBehaviour[] behaviours = camera.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour == brain)
                    continue;
                string typeName = behaviour.GetType().Name;
                if (typeName.Contains("CameraMouse") || typeName.Contains("CameraSwitcher") || typeName.Contains("CameraTransition"))
                    behaviour.enabled = false;
            }
            return camera;
        }

        private static void ConfigureFssrsCamera(Camera camera)
        {
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(FssrsRendererPath);
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PcPipelinePath);
            if (rendererData == null || pipeline == null)
                throw new InvalidOperationException("The FSSRS renderer or PC URP pipeline asset is missing.");
            if (!rendererData.TryGetRendererFeature(out FSSRSRendererFeature feature) || !feature.isActive)
                throw new InvalidOperationException("FSSRS Print Composite is not active on the cinematic renderer.");

            SerializedObject serializedPipeline = new SerializedObject(pipeline);
            SerializedProperty rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            int rendererIndex = -1;
            for (int index = 0; index < rendererList.arraySize; index++)
            {
                if (rendererList.GetArrayElementAtIndex(index).objectReferenceValue == rendererData)
                {
                    rendererIndex = index;
                    break;
                }
            }
            if (rendererIndex < 0)
            {
                rendererIndex = rendererList.arraySize;
                rendererList.InsertArrayElementAtIndex(rendererIndex);
                rendererList.GetArrayElementAtIndex(rendererIndex).objectReferenceValue = rendererData;
                serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }

            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.requiresDepthTexture = true;
            cameraData.requiresColorTexture = true;
            cameraData.volumeLayerMask = ~0;
            cameraData.SetRenderer(rendererIndex);
            EditorUtility.SetDirty(cameraData);
        }

        private static void PrepareSceneForCinematic(Camera mainCamera)
        {
            foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera != mainCamera)
                    camera.enabled = false;
            }
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                canvas.enabled = false;
            foreach (AudioSource source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                source.enabled = false;

            GameObject player = GameObject.Find("Player");
            if (player != null)
                player.SetActive(false);
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            Dictionary<string, Material> materials = new Dictionary<string, Material>();
            materials["Ink"] = CreateMaterial("M_Cinematic_Ink", new Color(0.003f, 0.004f, 0.009f), false);
            materials["Cyan"] = CreateMaterial("M_Cinematic_Cyan", new Color(0.03f, 0.95f, 1f), true);
            materials["Magenta"] = CreateMaterial("M_Cinematic_Magenta", new Color(1f, 0.02f, 0.42f), true);
            materials["Acid"] = CreateMaterial("M_Cinematic_Acid", new Color(0.38f, 1f, 0.02f), true);
            materials["Orange"] = CreateMaterial("M_Cinematic_Orange", new Color(1f, 0.26f, 0.02f), true);
            materials["White"] = CreateMaterial("M_Cinematic_White", new Color(0.92f, 0.96f, 1f), true);
            materials["Brick"] = CreateDetailedBrickMaterial();
            materials["Floor"] = AssetDatabase.LoadAssetAtPath<Material>("Assets/06_Modelos/Map/Blocking/Suelo.mat");
            if (materials["Floor"] == null)
                materials["Floor"] = materials["Ink"];
            materials["Graffiti"] = CreateGraffitiMaterial();
            materials["Mist"] = CreateMistMaterial();
            return materials;
        }

        private static Material CreateDetailedBrickMaterial()
        {
            string path = MaterialFolder + "/M_Cinematic_Detailed_Brick.mat";
            DeleteGeneratedAsset(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader is missing.");

            Material material = new Material(shader) { name = "M_Cinematic_Detailed_Brick" };
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04_Materiales e Imagenes/textures/red_brick_diff_1k.jpg");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04_Materiales e Imagenes/textures/red_brick_nor_gl_1k.exr");
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04_Materiales e Imagenes/textures/red_brick_arm_1k.jpg");
            material.SetTexture("_BaseMap", albedo);
            material.SetTextureScale("_BaseMap", new Vector2(5.5f, 3f));
            material.SetColor("_BaseColor", new Color(0.72f, 0.54f, 0.48f, 1f));
            material.SetTexture("_BumpMap", normal);
            material.SetTextureScale("_BumpMap", new Vector2(5.5f, 3f));
            material.SetFloat("_BumpScale", 0.8f);
            material.EnableKeyword("_NORMALMAP");
            if (mask != null)
            {
                material.SetTexture("_MetallicGlossMap", mask);
                material.SetTextureScale("_MetallicGlossMap", new Vector2(5.5f, 3f));
            }
            material.SetFloat("_Metallic", 0.05f);
            material.SetFloat("_Smoothness", 0.24f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateGraffitiMaterial()
        {
            string path = MaterialFolder + "/M_Cinematic_Layered_Graffiti.mat";
            DeleteGeneratedAsset(path);
            Shader shader = Shader.Find("FLOWSTATE/Graffiti/LayeredSprayStamp");
            if (shader == null)
                throw new InvalidOperationException("The layered graffiti spray shader is missing.");
            Material material = new Material(shader) { name = "M_Cinematic_Layered_Graffiti", renderQueue = 3010 };
            material.SetFloat("_GlobalOpacity", 1f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateMistMaterial()
        {
            string path = MaterialFolder + "/M_Cinematic_Spray_Mist.mat";
            DeleteGeneratedAsset(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material material = new Material(shader) { name = "M_Cinematic_Spray_Mist", renderQueue = 3050 };
            Color color = new Color(1f, 0.04f, 0.38f, 0.32f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateMaterial(string name, Color color, bool emissive)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            DeleteGeneratedAsset(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader) { name = name };
            Color finalColor = emissive ? color * 2.6f : color;
            finalColor.a = 1f;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", finalColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", finalColor);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateCinematicGrade(Transform parent)
        {
            DeleteGeneratedAsset(ProfilePath);
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.72f);
            bloom.intensity.Override(0.75f);
            bloom.scatter.Override(0.68f);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-0.15f);
            color.contrast.Override(18f);
            color.saturation.Override(12f);
            color.colorFilter.Override(new Color(0.88f, 0.93f, 1f, 1f));

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.72f);

            ChromaticAberration chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0.08f);

            FilmGrain grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.17f);
            grain.response.Override(0.72f);

            MotionBlur motionBlur = profile.Add<MotionBlur>(true);
            motionBlur.intensity.Override(0.16f);
            motionBlur.clamp.Override(0.12f);

            FSSRSVolumeComponent fssrs = profile.Add<FSSRSVolumeComponent>(true);
            fssrs.enabledEffect.Override(true);
            fssrs.outlineIntensity.Override(1f);
            fssrs.outlineThickness.Override(2.15f);
            fssrs.posterizeSteps.Override(5);
            fssrs.inkFleckIntensity.Override(0.18f);
            fssrs.halftoneIntensity.Override(0.36f);
            fssrs.halftoneScale.Override(3.1f);
            fssrs.hatchIntensity.Override(0.3f);
            fssrs.paletteInfluence.Override(0.82f);
            fssrs.paperLift.Override(0.42f);
            fssrs.colorSaturation.Override(0.72f);
            fssrs.accentBoost.Override(0.85f);

            GameObject volumeObject = new GameObject("Cinematic Color Grade");
            volumeObject.transform.SetParent(parent, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 250f;
            volume.sharedProfile = profile;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject keyObject = new GameObject("Cinematic Moon Key");
            keyObject.transform.SetParent(parent, false);
            keyObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.48f, 0.62f, 1f);
            key.intensity = 0.75f;
            key.shadows = LightShadows.Soft;

            CreatePointLight(parent, "Cyan City Glow", new Vector3(-190f, 80f, 245f), new Color(0.05f, 1f, 0.94f), 4200f, 165f);
            CreatePointLight(parent, "Magenta City Glow", new Vector3(170f, 95f, 430f), new Color(1f, 0.02f, 0.42f), 3600f, 155f);
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void CreateShowcaseContent(
            Transform parent,
            PlayableDirector director,
            Camera camera,
            Dictionary<string, Material> materials)
        {
            GameObject stage = new GameObject("GRAFFITI_SHOWCASE_STAGE");
            stage.transform.SetParent(parent, false);
            stage.transform.position = StageOrigin;

            GameObject wall = CreatePrimitive(
                stage.transform,
                "Real Brick Graffiti Wall",
                PrimitiveType.Cube,
                new Vector3(0f, 3.4f, 0.3f),
                new Vector3(14f, 7f, 0.5f),
                Vector3.zero,
                materials["Brick"]);
            wall.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;

            CreatePrimitive(stage.transform, "Brick Left Return", PrimitiveType.Cube, new Vector3(-7.1f, 3.4f, -2.15f), new Vector3(0.5f, 7f, 4.9f), Vector3.zero, materials["Brick"]);
            CreatePrimitive(stage.transform, "Brick Right Return", PrimitiveType.Cube, new Vector3(7.1f, 3.4f, -2.15f), new Vector3(0.5f, 7f, 4.9f), Vector3.zero, materials["Brick"]);
            CreatePrimitive(stage.transform, "Brick Top Beam", PrimitiveType.Cube, new Vector3(0f, 7.05f, 0.15f), new Vector3(14.6f, 0.45f, 0.8f), Vector3.zero, materials["Brick"]);
            CreatePrimitive(stage.transform, "Grounding Alley Floor", PrimitiveType.Cube, new Vector3(0f, -0.3f, -4.1f), new Vector3(17f, 0.55f, 10f), Vector3.zero, materials["Floor"]);

            CreateStageLight(stage.transform, "Stage Cyan Rim", new Vector3(-5.8f, 5.8f, -3.2f), new Color(0.02f, 0.85f, 1f), 18f, 9f);
            CreateStageLight(stage.transform, "Stage Magenta Rim", new Vector3(5.8f, 4.8f, -2.4f), new Color(1f, 0.02f, 0.28f), 14f, 8f);

            GameObject graffitiMenu = CreateGraffitiMenu(stage.transform);
            GameObject actor = CreateCinematicActor(stage.transform);
            Transform sprayCan = CreateCinematicSprayCan(stage.transform);
            ParticleSystem sprayMist = CreateSprayMist(sprayCan, materials["Mist"]);

            List<Vector3> paintPath = CreateGraffitiPaintPath();
            GameObject[] paintGroups = Array.Empty<GameObject>();
            Renderer graffitiLogo = CreateGraffitiLogoCanvas(stage.transform);
            FlowStatePaletteController palette = UnityEngine.Object.FindFirstObjectByType<FlowStatePaletteController>(FindObjectsInactive.Include);

            CreateEmotionHud(parent, camera, out CanvasGroup emotionGroup, out TMP_Text emotionLabel);
            CanvasGroup logoGroup = null;
            RectTransform logoRect = null;

            FlowStateCinematicShowcase showcase = parent.gameObject.AddComponent<FlowStateCinematicShowcase>();
            showcase.Configure(
                director,
                graffitiMenu,
                actor,
                sprayCan,
                sprayMist,
                paintGroups,
                paintPath.ToArray(),
                graffitiLogo,
                palette,
                emotionGroup,
                emotionLabel,
                logoGroup,
                logoRect);
        }

        private static GameObject CreateGraffitiMenu(Transform stage)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GraffitiMenuPath);
            if (prefab == null)
                throw new InvalidOperationException("Missing real graffiti menu prefab at " + GraffitiMenuPath);

            GameObject menu = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage);
            menu.name = "REAL_GRAFFITI_MENU_SPRAY_LAB";
            menu.transform.localPosition = new Vector3(0f, 3.42f, -0.16f);
            menu.transform.localRotation = Quaternion.identity;
            menu.transform.localScale = Vector3.one * 3.15f;
            foreach (TMP_Text text in menu.GetComponentsInChildren<TMP_Text>(true))
                text.ForceMeshUpdate(true, true);
            return menu;
        }

        private static GameObject CreateCinematicActor(Transform stage)
        {
            GameObject source = FindSceneObject("MAIKOL_Visual");
            if (source == null)
                return null;

            GameObject actor = UnityEngine.Object.Instantiate(source, stage);
            actor.name = "Cinematic Maikol - Emotion Shader Hero";
            actor.transform.localPosition = new Vector3(5.15f, 1.45f, -1.75f);
            actor.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
            actor.transform.localScale = Vector3.one * 1.5f;
            foreach (Animator animator in actor.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
            actor.AddComponent<FlowStateCinematicPainter>().CaptureRestPose();
            actor.SetActive(true);
            return actor;
        }

        private static Transform CreateCinematicSprayCan(Transform stage)
        {
            GameObject sprayCan = new GameObject();
            sprayCan.transform.SetParent(stage, false);
            sprayCan.name = "Cinematic Spray Can - Painting In Progress";
            Material metal = CreateMaterial("M_Spray_Metal", new Color(0.36f, 0.4f, 0.45f), false);
            Material label = CreateMaterial("M_Spray_Label", new Color(0.92f, 0.02f, 0.28f), false);
            Material ink = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/M_Cinematic_Ink.mat");
            CreatePrimitive(sprayCan.transform, "Metal aerosol can", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.23f, 0.25f, 0.23f), Vector3.zero, metal);
            CreatePrimitive(sprayCan.transform, "Magenta label", PrimitiveType.Cylinder, new Vector3(0f, -0.02f, 0f), new Vector3(0.235f, 0.19f, 0.235f), Vector3.zero, label);
            CreatePrimitive(sprayCan.transform, "Valve shoulder", PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f), new Vector3(0.22f, 0.085f, 0.22f), Vector3.zero, metal);
            CreatePrimitive(sprayCan.transform, "Spray nozzle", PrimitiveType.Cube, new Vector3(0f, 0.31f, 0.025f), new Vector3(0.08f, 0.09f, 0.12f), Vector3.zero, ink);
            CreatePrimitive(sprayCan.transform, "Nozzle outlet", PrimitiveType.Cylinder, new Vector3(0f, 0.32f, 0.089f), new Vector3(0.025f, 0.004f, 0.025f), new Vector3(90f, 0f, 0f), metal);
            sprayCan.SetActive(true);
            return sprayCan.transform;
        }

        private static void CreateStageLight(Transform stage, string name, Vector3 localPosition, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name, typeof(Light));
            lightObject.transform.SetParent(stage, false);
            lightObject.transform.localPosition = localPosition;
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        private static ParticleSystem CreateSprayMist(Transform sprayCan, Material material)
        {
            if (sprayCan == null)
                return null;

            GameObject mistObject = new GameObject("Visible Spray Mist", typeof(ParticleSystem));
            mistObject.transform.SetParent(sprayCan, false);
            mistObject.transform.localPosition = new Vector3(0f, 0.32f, 0.1f);
            mistObject.transform.localRotation = Quaternion.identity;
            ParticleSystem particles = mistObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.08f, 0.42f, 0.5f), new Color(0.12f, 1f, 0.94f, 0.32f));
            main.maxParticles = 260;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 120f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.025f;
            ParticleSystemRenderer renderer = mistObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        private static GameObject[] CreateGraffitiStrokeGroups(Transform stage, List<Vector3> paintPath, Material material)
        {
            GameObject root = new GameObject("REAL_LAYERED_GRAFFITI_STROKES");
            root.transform.SetParent(stage, false);
            List<GameObject> groups = new List<GameObject>();
            GraffitiSurfaceCanvas canvas = null;
            GameObject group = null;
            const int stampsPerGroup = 3;

            for (int index = 0; index < paintPath.Count; index++)
            {
                if (index % stampsPerGroup == 0)
                {
                    group = new GameObject(string.Format("Paint Reveal {0:000}", groups.Count + 1));
                    group.transform.SetParent(root.transform, false);
                    canvas = GraffitiSurfaceCanvas.GetOrCreate(group.transform, material);
                    groups.Add(group);
                }

                Vector3 localPoint = paintPath[index];
                Vector3 localTangent = index > 0 ? paintPath[index] - paintPath[index - 1] : Vector3.right;
                if (localTangent.sqrMagnitude < 0.0001f)
                    localTangent = Vector3.right;

                float colorPhase = index / (float)Mathf.Max(1, paintPath.Count - 1);
                Color color = colorPhase < 0.34f
                    ? new Color(1f, 0.035f, 0.34f, 0.92f)
                    : colorPhase < 0.72f
                        ? new Color(0.02f, 0.9f, 1f, 0.9f)
                        : new Color(0.72f, 1f, 0.02f, 0.9f);
                float size = 0.49f + Mathf.Sin(index * 1.91f) * 0.035f;
                float nozzleShape = index % 37 == 0 ? 4f : (index % 19 == 0 ? 3f : 0f);
                Vector4 profile = new Vector4(0.66f, 1f, 0.42f, 0.17f + index * 0.731f);
                Vector4 spray = new Vector4(nozzleShape, 27f, 0.68f, nozzleShape > 3.5f ? 0.46f : 0.16f);
                canvas.AddStamp(
                    stage.TransformPoint(localPoint),
                    stage.TransformDirection(Vector3.back),
                    stage.TransformDirection(localTangent.normalized),
                    nozzleShape == 3f ? size * 1.55f : size,
                    nozzleShape == 3f ? size * 0.55f : size,
                    color,
                    profile,
                    spray);
            }

            foreach (GameObject paintGroup in groups)
            {
                GraffitiSurfaceCanvas surface = paintGroup.GetComponentInChildren<GraffitiSurfaceCanvas>(true);
                if (surface != null)
                {
                    System.Reflection.MethodInfo flush = typeof(GraffitiSurfaceCanvas).GetMethod(
                        "LateUpdate",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    flush?.Invoke(surface, null);
                    Mesh baked = UnityEngine.Object.Instantiate(surface.GetComponent<MeshFilter>().sharedMesh);
                    string meshPath = "Assets/10_Cinematics/Meshes/" + paintGroup.name.Replace(" ", "_") + ".asset";
                    Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (saved == null)
                    {
                        AssetDatabase.CreateAsset(baked, meshPath);
                        saved = baked;
                    }
                    else
                    {
                        EditorUtility.CopySerialized(baked, saved);
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                    MeshFilter filter = surface.GetComponent<MeshFilter>();
                    Mesh transient = filter.sharedMesh;
                    typeof(GraffitiSurfaceCanvas).GetField("mesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(surface, null);
                    UnityEngine.Object.DestroyImmediate(surface);
                    UnityEngine.Object.DestroyImmediate(transient);
                    filter.sharedMesh = saved;
                    EditorUtility.SetDirty(saved);
                }
                paintGroup.SetActive(false);
            }
            return groups.ToArray();
        }

        private static List<Vector3> CreateGraffitiPaintPath()
        {
            List<Vector3> points = new List<Vector3>();
            const int count = 520;
            for (int index = 0; index <= count; index++)
            {
                float t = index / (float)count;
                float x = Mathf.Lerp(-5.25f, 5.25f, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float y = 3.28f + Mathf.Sin(t * Mathf.PI * 5.2f - 0.45f) * (1.05f * envelope)
                    + Mathf.Sin(t * Mathf.PI * 11f) * 0.18f;
                points.Add(new Vector3(x, y, -0.09f));
            }
            return points;
        }

        private static Renderer CreateGraffitiLogoCanvas(Transform stage)
        {
            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            Shader shader = Shader.Find("FLOWSTATE/Cinematics/Logo Graffiti Reveal");
            if (logo == null || shader == null)
                throw new InvalidOperationException("Official logo or graffiti reveal shader is missing.");
            string materialPath = MaterialFolder + "/M_Cinematic_Official_Logo_Graffiti.mat";
            DeleteGeneratedAsset(materialPath);
            Material material = new Material(shader) { name = "M_Cinematic_Official_Logo_Graffiti" };
            material.SetTexture("_BaseMap", logo);
            material.SetFloat("_Reveal", 0f);
            material.SetFloat("_Feather", 0.022f);
            material.SetFloat("_NoiseScale", 42f);
            material.SetFloat("_Emission", 0.22f);
            AssetDatabase.CreateAsset(material, materialPath);
            GameObject canvas = CreatePrimitive(stage, "OFFICIAL_FLOW_STATE_GRAFFITI_ON_WALL", PrimitiveType.Quad,
                new Vector3(0f, 3.42f, -0.04f), new Vector3(12.2f, 6.8f, 1f), Vector3.zero, material);
            Collider collider = canvas.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
            return canvas.GetComponent<Renderer>();
        }

        private static void AddLine(List<Vector3> points, Vector3 start, Vector3 end, float spacing = 0.075f)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(start, end) / spacing));
            for (int index = 0; index <= count; index++)
                points.Add(Vector3.Lerp(start, end, index / (float)count));
        }

        private static void CreateEmotionHud(Transform parent, Camera camera, out CanvasGroup group, out TMP_Text label)
        {
            Canvas canvas = CreateCameraCanvas(parent, camera, "FSSRS Emotion Shader HUD", 80);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            GameObject textObject = new GameObject("Emotion State Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(72f, 54f);
            rect.sizeDelta = new Vector2(760f, 135f);
            label = textObject.GetComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Tipografias/owned SDF.asset");
            label.fontSize = 34f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 2f;
            label.alignment = TextAlignmentOptions.BottomLeft;
        }

        private static void CreateOfficialLogo(Transform parent, Camera camera, out CanvasGroup group, out RectTransform logoRect)
        {
            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (logo == null)
                throw new InvalidOperationException("Missing official FLOW STATE logo at " + LogoPath);

            Canvas canvas = CreateCameraCanvas(parent, camera, "FINAL_TITLE_FLOW_STATE", 100);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            GameObject logoObject = new GameObject("Official FLOW STATE Logo PNG", typeof(RectTransform), typeof(RawImage));
            logoObject.transform.SetParent(canvas.transform, false);
            logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = new Vector2(0f, 20f);
            logoRect.sizeDelta = new Vector2(1740f, 870f);
            RawImage image = logoObject.GetComponent<RawImage>();
            image.texture = logo;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static Canvas CreateCameraCanvas(Transform parent, Camera camera, string name, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 0.5f;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject FindSceneObject(string name)
        {
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == name && candidate.scene.IsValid() && candidate.scene == SceneManager.GetActiveScene())
                    return candidate;
            }
            return null;
        }

        private static void CreateHeroMural(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Quaternion worldRotation,
            string headline,
            string strapline,
            Dictionary<string, Material> materials,
            Color primary,
            Color secondary)
        {
            GameObject mural = new GameObject(name);
            mural.transform.SetParent(parent, false);
            mural.transform.SetPositionAndRotation(worldPosition, worldRotation);

            CreatePrimitive(mural.transform, "Wall", PrimitiveType.Cube, Vector3.zero, new Vector3(126f, 58f, 2f), Vector3.zero, materials["Wall"]);
            CreatePrimitive(mural.transform, "Frame Top", PrimitiveType.Cube, new Vector3(0f, 30f, -0.2f), new Vector3(132f, 2f, 3f), Vector3.zero, materials["Ink"]);
            CreatePrimitive(mural.transform, "Frame Bottom", PrimitiveType.Cube, new Vector3(0f, -30f, -0.2f), new Vector3(132f, 2f, 3f), Vector3.zero, materials["Ink"]);
            CreatePrimitive(mural.transform, "Frame Left", PrimitiveType.Cube, new Vector3(-65f, 0f, -0.2f), new Vector3(2f, 62f, 3f), Vector3.zero, materials["Ink"]);
            CreatePrimitive(mural.transform, "Frame Right", PrimitiveType.Cube, new Vector3(65f, 0f, -0.2f), new Vector3(2f, 62f, 3f), Vector3.zero, materials["Ink"]);

            Material primaryMaterial = primary.g > 0.9f && primary.r < 0.5f ? materials["Cyan"] : materials["Magenta"];
            Material secondaryMaterial = secondary.g > 0.8f ? materials["Acid"] : materials["Magenta"];
            CreatePaintStrokes(mural.transform, primaryMaterial, secondaryMaterial, materials["Orange"]);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Tipografias/owned SDF.asset");
            if (font == null)
                font = TMP_Settings.defaultFontAsset;

            CreateWorldText(mural.transform, "Headline Shadow", headline, font, new Color(0.005f, 0.006f, 0.012f), 96f, new Vector3(3f, -2f, -1.38f), new Vector2(116f, 51f));
            CreateWorldText(mural.transform, "Headline", headline, font, primary, 96f, new Vector3(0f, 1f, -1.56f), new Vector2(116f, 51f));
            CreateWorldText(mural.transform, "Strapline", strapline, font, secondary, 20f, new Vector3(0f, -22f, -1.7f), new Vector2(110f, 10f));

            CreatePointLight(mural.transform, "Mural Rim A", new Vector3(-42f, 4f, -24f), primary, 2200f, 72f);
            CreatePointLight(mural.transform, "Mural Rim B", new Vector3(40f, -4f, -20f), secondary, 1900f, 68f);
        }

        private static void CreatePaintStrokes(Transform parent, Material primary, Material secondary, Material accent)
        {
            Vector3[] positions =
            {
                new Vector3(-38f, 14f, -1.2f), new Vector3(35f, 16f, -1.22f), new Vector3(-44f, -13f, -1.24f),
                new Vector3(43f, -10f, -1.25f), new Vector3(-12f, 25f, -1.27f), new Vector3(14f, -24f, -1.29f)
            };
            Vector3[] scales =
            {
                new Vector3(38f, 4f, 0.45f), new Vector3(34f, 3f, 0.4f), new Vector3(31f, 3.5f, 0.42f),
                new Vector3(28f, 4.5f, 0.45f), new Vector3(18f, 3f, 0.4f), new Vector3(22f, 3f, 0.4f)
            };
            float[] angles = { 15f, -12f, -22f, 20f, 34f, -30f };
            Material[] choices = { primary, secondary, accent, primary, secondary, accent };
            for (int i = 0; i < positions.Length; i++)
                CreatePrimitive(parent, "Paint Stroke " + (i + 1), PrimitiveType.Cube, positions[i], scales[i], new Vector3(0f, 0f, angles[i]), choices[i]);

            for (int i = 0; i < 18; i++)
            {
                float angle = i * 2.399f;
                float radius = 20f + (i % 5) * 7f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.62f, -1.38f - i * 0.002f);
                float size = 1.1f + (i % 4) * 0.9f;
                Material material = i % 3 == 0 ? accent : (i % 2 == 0 ? primary : secondary);
                CreatePrimitive(parent, "Paint Splatter " + (i + 1), PrimitiveType.Sphere, position, new Vector3(size, size, 0.35f), Vector3.zero, material);
            }
        }

        private static void CreateFinalTitle(Transform parent, Dictionary<string, Material> materials)
        {
            GameObject title = new GameObject("FINAL_TITLE_FLOW_STATE");
            title.transform.SetParent(parent, false);
            title.transform.position = new Vector3(-55f, 142f, 330f);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Tipografias/owned SDF.asset");
            if (font == null)
                font = TMP_Settings.defaultFontAsset;
            CreateWorldText(title.transform, "Final Shadow", "FLOW STATE", font, new Color(0.002f, 0.003f, 0.008f), 112f, new Vector3(4f, -4f, 0.6f), new Vector2(230f, 52f));
            CreateWorldText(title.transform, "Final Title", "FLOW STATE", font, new Color(0.08f, 1f, 0.94f), 112f, Vector3.zero, new Vector2(230f, 52f));
            CreateWorldText(title.transform, "Final Tag", "MAKE THE CITY YOUR CANVAS", font, new Color(1f, 0.05f, 0.48f), 24f, new Vector3(0f, -28f, -0.2f), new Vector2(190f, 18f));
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler,
            Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.Euler(localEuler);
            gameObject.transform.localScale = localScale;
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
            return gameObject;
        }

        private static TextMeshPro CreateWorldText(
            Transform parent,
            string name,
            string value,
            TMP_FontAsset font,
            Color color,
            float fontSize,
            Vector3 localPosition,
            Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.identity;
            TextMeshPro text = textObject.GetComponent<TextMeshPro>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.characterSpacing = -2f;
            text.lineSpacing = -12f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = size;
            return text;
        }

        private static TimelineAsset CreateTimelineAsset()
        {
            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "FlowState_Trailer_25s_98s";
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = Duration;
            timeline.editorSettings.fps = FrameRate;
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            return timeline;
        }

        private static void CreateCameraSequence(
            Transform parent,
            TimelineAsset timeline,
            PlayableDirector director,
            CinemachineBrain brain)
        {
            List<ShotDefinition> shots = CreateShotDefinitions();
            GameObject cameraRig = new GameObject("Cinematic Camera Rig");
            cameraRig.transform.SetParent(parent, false);
            Animator animator = cameraRig.AddComponent<Animator>();

            GameObject cameraObject = new GameObject("CM_Hero_Master");
            cameraObject.transform.SetParent(cameraRig.transform, false);
            cameraObject.transform.position = shots[0].Positions[0];
            cameraObject.transform.rotation = LookRotation(shots[0].Positions[0], shots[0].Targets[0], shots[0].Dutch);
            CinemachineCamera cinematicCamera = cameraObject.AddComponent<CinemachineCamera>();
            cinematicCamera.Lens.FieldOfView = 46f;
            cinematicCamera.Lens.NearClipPlane = 0.15f;
            cinematicCamera.Lens.FarClipPlane = 1800f;
            cinematicCamera.Lens.Dutch = 0f;

            AnimationClip animation = CreateMasterCameraAnimationClip(shots, cameraObject.name);
            AnimationTrack animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Master Cinematic Camera Motion");
            animationTrack.trackOffset = TrackOffset.ApplyTransformOffsets;
            TimelineClip animationTimelineClip = animationTrack.CreateClip(animation);
            animationTimelineClip.displayName = "Nine-shot cinematic camera choreography";
            animationTimelineClip.start = 0d;
            animationTimelineClip.duration = Duration;
            director.SetGenericBinding(animationTrack, animator);
        }

        private static AnimationClip CreateMasterCameraAnimationClip(List<ShotDefinition> shots, string targetPath)
        {
            AnimationClip clip = new AnimationClip
            {
                name = "AC_Master_Cinematic_Camera",
                frameRate = FrameRate,
                legacy = false
            };

            List<float> times = new List<float>();
            List<Vector3> positions = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            List<Vector3> paintingPath = CreateGraffitiPaintPath();
            float oneFrame = 1f / FrameRate;
            for (int shotIndex = 0; shotIndex < shots.Count; shotIndex++)
            {
                ShotDefinition shot = shots[shotIndex];
                float segmentStart = (float)shot.Start;
                float segmentEnd = shotIndex < shots.Count - 1
                    ? Mathf.Max(segmentStart + oneFrame, (float)shots[shotIndex + 1].Start - oneFrame)
                    : (float)Duration;
                int samples = segmentStart >= 18f && segmentStart < 56f ? Mathf.CeilToInt((segmentEnd - segmentStart) * 10f) : shot.Positions.Length - 1;
                for (int pointIndex = 0; pointIndex <= samples; pointIndex++)
                {
                    float normalized = pointIndex / (float)samples;
                    float time = Mathf.Lerp(segmentStart, segmentEnd, normalized);
                    Vector3 position;
                    Vector3 target;
                    if (segmentStart >= 18f && segmentStart < 56f)
                    {
                        float progress = Mathf.Clamp01((time - 18f) / 37f) * (paintingPath.Count - 1);
                        int from = Mathf.FloorToInt(progress);
                        Vector3 tip = Vector3.Lerp(paintingPath[from], paintingPath[Mathf.Min(from + 1, paintingPath.Count - 1)], progress - from);
                        bool macro = segmentStart >= 36f && segmentStart < 46f;
                        target = StageOrigin + (macro ? tip + new Vector3(0.1f, -0.15f, -0.15f) : new Vector3(tip.x + 0.1f, 1.9f, -0.3f));
                        position = target + (macro ? new Vector3(1.7f, 0.55f, -3.2f) : new Vector3(2.1f - normalized * 0.6f, 0.95f, -5.2f));
                    }
                    else if (segmentStart == 0f || segmentStart >= 56f)
                    {
                        target = StageOrigin + new Vector3(0f, 2.15f, 0f);
                        position = StageOrigin + new Vector3(Mathf.Lerp(-1.1f, 1.1f, normalized), 3.0f, segmentStart >= 64f ? -10.5f - normalized * 1.5f : -9.3f - normalized * 0.7f);
                    }
                    else
                    {
                        position = shot.Positions[pointIndex];
                        target = shot.Targets[pointIndex];
                    }
                    times.Add(time);
                    positions.Add(position);
                    Quaternion rotation = LookRotation(position, target, shot.Dutch);
                    if (rotations.Count > 0 && Quaternion.Dot(rotations[rotations.Count - 1], rotation) < 0f)
                        rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    rotations.Add(rotation);
                }
            }

            SetCurve(clip, targetPath, "m_LocalPosition.x", times.ToArray(), GetValues(positions.ToArray(), 0));
            SetCurve(clip, targetPath, "m_LocalPosition.y", times.ToArray(), GetValues(positions.ToArray(), 1));
            SetCurve(clip, targetPath, "m_LocalPosition.z", times.ToArray(), GetValues(positions.ToArray(), 2));
            SetCurve(clip, targetPath, "m_LocalRotation.x", times.ToArray(), GetValues(rotations.ToArray(), 0));
            SetCurve(clip, targetPath, "m_LocalRotation.y", times.ToArray(), GetValues(rotations.ToArray(), 1));
            SetCurve(clip, targetPath, "m_LocalRotation.z", times.ToArray(), GetValues(rotations.ToArray(), 2));
            SetCurve(clip, targetPath, "m_LocalRotation.w", times.ToArray(), GetValues(rotations.ToArray(), 3));
            clip.EnsureQuaternionContinuity();

            string path = AnimationFolder + "/" + clip.name + ".anim";
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static List<ShotDefinition> CreateShotDefinitions()
        {
            return new List<ShotDefinition>
            {
                Shot("Can and Emotion Shader", 0d, 4.5d, 40f, -1.1f,
                    new[] { StageOrigin + new Vector3(7.6f, 3.2f, -6.8f), StageOrigin + new Vector3(6.5f, 2.7f, -5.5f), StageOrigin + new Vector3(5.7f, 2.5f, -4.7f) },
                    new[] { StageOrigin + new Vector3(4.6f, 2.2f, -1.2f), StageOrigin + new Vector3(4.8f, 2.3f, -1.3f), StageOrigin + new Vector3(5f, 2.4f, -1.4f) }),
                Shot("Spray Lab Full Menu", 4.5d, 11.5d, 46f, 0.35f,
                    new[] { StageOrigin + new Vector3(-0.8f, 3.8f, -10.1f), StageOrigin + new Vector3(0f, 3.55f, -9.2f), StageOrigin + new Vector3(0.55f, 3.35f, -8.5f) },
                    new[] { StageOrigin + new Vector3(0f, 3.45f, -0.15f), StageOrigin + new Vector3(0f, 3.42f, -0.15f), StageOrigin + new Vector3(0f, 3.35f, -0.15f) }),
                Shot("Nozzle and Color Selection", 11.5d, 18d, 34f, -0.65f,
                    new[] { StageOrigin + new Vector3(3.7f, 2.8f, -6.2f), StageOrigin + new Vector3(2.6f, 2.45f, -5.1f), StageOrigin + new Vector3(1.7f, 2.25f, -4.45f) },
                    new[] { StageOrigin + new Vector3(2.6f, 2.15f, -0.12f), StageOrigin + new Vector3(2.25f, 2.0f, -0.12f), StageOrigin + new Vector3(1.8f, 1.9f, -0.12f) }),
                Shot("First Spray Stroke", 18d, 27d, 31f, 0.9f,
                    new[] { StageOrigin + new Vector3(-6.2f, 3.9f, -4.8f), StageOrigin + new Vector3(-5.5f, 3.5f, -3.9f), StageOrigin + new Vector3(-4.8f, 3.15f, -3.2f) },
                    new[] { StageOrigin + new Vector3(-4.5f, 3.6f, 0f), StageOrigin + new Vector3(-4.25f, 3.4f, 0f), StageOrigin + new Vector3(-4f, 3.15f, 0f) }),
                Shot("Anger to Creative Flow", 27d, 36.5d, 42f, -0.7f,
                    new[] { StageOrigin + new Vector3(6.4f, 3.1f, -6.4f), StageOrigin + new Vector3(5.1f, 2.8f, -5.2f), StageOrigin + new Vector3(4.1f, 2.65f, -4.5f) },
                    new[] { StageOrigin + new Vector3(3.8f, 2.8f, -0.5f), StageOrigin + new Vector3(3.25f, 3.05f, -0.15f), StageOrigin + new Vector3(2.7f, 3.2f, 0f) }),
                Shot("Layered Paint Macro", 36.5d, 46d, 28f, 0.4f,
                    new[] { StageOrigin + new Vector3(-0.9f, 5.2f, -3.7f), StageOrigin + new Vector3(0.15f, 4.55f, -3.0f), StageOrigin + new Vector3(1.15f, 3.95f, -2.65f) },
                    new[] { StageOrigin + new Vector3(-0.35f, 4.8f, 0f), StageOrigin + new Vector3(0.65f, 4.15f, 0f), StageOrigin + new Vector3(1.55f, 3.55f, 0f) }),
                Shot("Graffiti Finishing Pass", 46d, 56d, 32f, -0.9f,
                    new[] { StageOrigin + new Vector3(5.8f, 4.6f, -4.5f), StageOrigin + new Vector3(5.15f, 3.65f, -3.5f), StageOrigin + new Vector3(4.65f, 2.55f, -3.0f) },
                    new[] { StageOrigin + new Vector3(4.35f, 4.7f, 0f), StageOrigin + new Vector3(4.1f, 3.5f, 0f), StageOrigin + new Vector3(3.8f, 2.35f, 0f) }),
                Shot("FLOW Graffiti Hero Reveal", 56d, 64d, 48f, 0.55f,
                    new[] { StageOrigin + new Vector3(-2.2f, 4.0f, -9.6f), StageOrigin + new Vector3(-0.8f, 3.8f, -10.8f), StageOrigin + new Vector3(0.5f, 3.7f, -11.8f) },
                    new[] { StageOrigin + new Vector3(0f, 3.3f, 0f), StageOrigin + new Vector3(0f, 3.25f, 0f), StageOrigin + new Vector3(0f, 3.2f, 0f) }),
                Shot("Official FLOW STATE Signature", 64d, 73d, 50f, -0.25f,
                    new[] { StageOrigin + new Vector3(1.1f, 4.3f, -12.5f), StageOrigin + new Vector3(0.1f, 4.8f, -14.3f), StageOrigin + new Vector3(-1.1f, 5.3f, -16.2f) },
                    new[] { StageOrigin + new Vector3(0f, 3.2f, 0f), StageOrigin + new Vector3(0f, 3.3f, 0f), StageOrigin + new Vector3(0f, 3.5f, 0f) })
            };
        }

        private static ShotDefinition Shot(
            string name,
            double start,
            double end,
            float fieldOfView,
            float dutch,
            Vector3[] positions,
            Vector3[] targets)
        {
            return new ShotDefinition
            {
                Name = name,
                Start = start,
                End = end,
                FieldOfView = fieldOfView,
                Dutch = dutch,
                Positions = positions,
                Targets = targets
            };
        }

        private static AnimationClip CreateCameraAnimationClip(ShotDefinition shot, int index)
        {
            AnimationClip clip = new AnimationClip
            {
                name = string.Format("AC_{0:00}_{1}", index + 1, shot.Name.Replace(" ", "_")),
                frameRate = FrameRate,
                legacy = false
            };
            float length = (float)(shot.End - shot.Start);
            float[] times = new float[shot.Positions.Length];
            Quaternion[] rotations = new Quaternion[shot.Positions.Length];
            for (int i = 0; i < times.Length; i++)
            {
                times[i] = length * i / (times.Length - 1f);
                rotations[i] = LookRotation(shot.Positions[i], shot.Targets[i], shot.Dutch);
                if (i > 0 && Quaternion.Dot(rotations[i - 1], rotations[i]) < 0f)
                    rotations[i] = new Quaternion(-rotations[i].x, -rotations[i].y, -rotations[i].z, -rotations[i].w);
            }

            SetCurve(clip, "m_LocalPosition.x", times, GetValues(shot.Positions, 0));
            SetCurve(clip, "m_LocalPosition.y", times, GetValues(shot.Positions, 1));
            SetCurve(clip, "m_LocalPosition.z", times, GetValues(shot.Positions, 2));
            SetCurve(clip, "m_LocalRotation.x", times, GetValues(rotations, 0));
            SetCurve(clip, "m_LocalRotation.y", times, GetValues(rotations, 1));
            SetCurve(clip, "m_LocalRotation.z", times, GetValues(rotations, 2));
            SetCurve(clip, "m_LocalRotation.w", times, GetValues(rotations, 3));
            clip.EnsureQuaternionContinuity();

            string path = AnimationFolder + "/" + clip.name + ".anim";
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Quaternion LookRotation(Vector3 position, Vector3 target, float dutch)
        {
            Quaternion look = Quaternion.LookRotation((target - position).normalized, Vector3.up);
            return look * Quaternion.AngleAxis(dutch, Vector3.forward);
        }

        private static float[] GetValues(Vector3[] values, int axis)
        {
            float[] result = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = axis == 0 ? values[i].x : (axis == 1 ? values[i].y : values[i].z);
            return result;
        }

        private static float[] GetValues(Quaternion[] values, int axis)
        {
            float[] result = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                switch (axis)
                {
                    case 0: result[i] = values[i].x; break;
                    case 1: result[i] = values[i].y; break;
                    case 2: result[i] = values[i].z; break;
                    default: result[i] = values[i].w; break;
                }
            }
            return result;
        }

        private static void SetCurve(AnimationClip clip, string propertyName, float[] times, float[] values)
        {
            SetCurve(clip, string.Empty, propertyName, times, values);
        }

        private static void SetCurve(AnimationClip clip, string path, string propertyName, float[] times, float[] values)
        {
            Keyframe[] keys = new Keyframe[times.Length];
            for (int i = 0; i < times.Length; i++)
                keys[i] = new Keyframe(times[i], values[i]);
            AnimationCurve curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void CreateAudioSequence(
            Transform parent,
            TimelineAsset timeline,
            PlayableDirector director,
            AudioClip audioClip)
        {
            GameObject audioObject = new GameObject("Cinematic Audio");
            audioObject.transform.SetParent(parent, false);
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;

            AudioTrack audioTrack = timeline.CreateTrack<AudioTrack>(null, "Made You Look - 00:25 to 01:38");
            TimelineClip audioTimelineClip = audioTrack.CreateClip<AudioPlayableAsset>();
            AudioPlayableAsset playableAsset = (AudioPlayableAsset)audioTimelineClip.asset;
            playableAsset.clip = audioClip;
            playableAsset.loop = false;
            audioTimelineClip.displayName = "Made You Look [00:25-01:38]";
            audioTimelineClip.start = 0d;
            audioTimelineClip.clipIn = SourceAudioIn;
            audioTimelineClip.duration = Duration;
            audioTimelineClip.easeInDuration = 0.08d;
            audioTimelineClip.easeOutDuration = 0.32d;
            director.SetGenericBinding(audioTrack, audioSource);
        }

        private static void CreateFinalTitleActivation(TimelineAsset timeline, PlayableDirector director)
        {
            GameObject finalTitle = GameObject.Find("FINAL_TITLE_FLOW_STATE");
            if (finalTitle == null)
                return;
            ActivationTrack activationTrack = timeline.CreateTrack<ActivationTrack>(null, "Final Title Reveal");
            TimelineClip activationClip = activationTrack.CreateDefaultClip();
            activationClip.displayName = "Reveal FLOW STATE identity";
            activationClip.start = 61.75d;
            activationClip.duration = Duration - activationClip.start;
            director.SetGenericBinding(activationTrack, finalTitle);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RenderSessionKey, false))
                EditorApplication.delayCall += BeginRecordingInPlayMode;
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= MonitorRecording;
                if (recorderController != null && recorderController.IsRecording())
                    recorderController.StopRecording();
                recorderController = null;
                SessionState.SetBool(RenderSessionKey, false);
            }
        }

        private static void BeginRecordingInPlayMode()
        {
            GameObject root = GameObject.Find(RootName);
            PlayableDirector director = root != null ? root.GetComponent<PlayableDirector>() : null;
            if (director == null)
            {
                Debug.LogError("[FlowState Cinematic] PlayableDirector not found; render cancelled.");
                SessionState.SetBool(RenderSessionKey, false);
                EditorApplication.ExitPlaymode();
                return;
            }

            string outputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Captures");
            Directory.CreateDirectory(outputFolder);
            string outputPath = Path.Combine(outputFolder, "FlowState_Logo_Graffiti_Cinematic_v6_FSSRS");

            RecorderControllerSettings controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            MovieRecorderSettings movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.name = "FlowState Trailer MP4";
            movieSettings.Enabled = true;
            movieSettings.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
                EncodingProfile = CoreEncoderSettings.H264EncodingProfile.High,
                GopSize = 30
            };
            movieSettings.CaptureAlpha = false;
            movieSettings.CaptureAudio = true;
            movieSettings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };
            movieSettings.OutputFile = outputPath;

            controllerSettings.AddRecorderSettings(movieSettings);
            controllerSettings.SetRecordModeToFrameInterval(0, Mathf.RoundToInt((float)Duration * FrameRate) - 1);
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.FrameRate = FrameRate;
            controllerSettings.CapFrameRate = true;
            controllerSettings.ExitPlayMode = true;

            director.time = 0d;
            director.Evaluate();
            recorderController = new RecorderController(controllerSettings);
            recorderController.PrepareRecording();
            if (!recorderController.StartRecording())
            {
                Debug.LogError("[FlowState Cinematic] Unity Recorder could not start.");
                SessionState.SetBool(RenderSessionKey, false);
                EditorApplication.ExitPlaymode();
                return;
            }

            director.Play();
            EditorApplication.update -= MonitorRecording;
            EditorApplication.update += MonitorRecording;
            Debug.Log("[FlowState Cinematic] Recording to " + outputPath + ".mp4");
        }

        private static void MonitorRecording()
        {
            if (recorderController == null || recorderController.IsRecording())
                return;

            EditorApplication.update -= MonitorRecording;
            SessionState.SetBool(RenderSessionKey, false);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }
    }
}
#endif
