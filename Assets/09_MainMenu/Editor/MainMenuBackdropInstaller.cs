using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowState.Menu.Editor
{
    public static class MainMenuBackdropInstaller
    {
        const string ScenePath = "Assets/02_Escenas/MainMenu_FlowState.unity";
        const string RootName = "FLOW STATE · Living Graffiti Menu";
        const string StageName = "Living mural stage · mask + holo";
        const string LegacyStageName = "Living mural stage · Maikol + mask + holo";
        const string TextureRoot = "Assets/09_MainMenu/Textures/Backdrop";

        [MenuItem("FLOW STATE/Main Menu/Install Holographic Mask Backdrop")]
        public static void Install()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before installing the menu backdrop.");

            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save the currently open scene before installing the menu backdrop.");

            ImportBackdropTextures();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InstallIntoOpenScene(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("FLOW STATE holographic mask backdrop installed. Spray, can motion and menu input were preserved.");
        }

        public static void InstallIntoOpenScene(Scene scene)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("The FLOW STATE main-menu scene must be open.");

            GameObject root = scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == RootName);
            if (!root)
                throw new InvalidOperationException("Main-menu root was not found in " + ScenePath);

            MainMenuController menu = root.GetComponent<MainMenuController>();
            FlowStateMenuCameraController cameraController = root.GetComponentInChildren<FlowStateMenuCameraController>(true);
            if (!menu || !menu.painter || !menu.painter.wall || !menu.motion || !cameraController)
                throw new InvalidOperationException("The existing menu systems are incomplete; the backdrop was not installed.");

            RemoveGeneratedStage(root.transform, StageName);
            RemoveGeneratedStage(root.transform, LegacyStageName);

            ConfigureWallMaterial(menu.painter.wall.sharedMaterial);
            ConfigureMenuTypography(root);
            ConfigureCamera(cameraController.menuCamera);
            ConfigureOptionPaint(menu.options);

            GameObject stage = new GameObject(StageName);
            SceneManager.MoveGameObjectToScene(stage, scene);
            stage.transform.SetParent(root.transform, false);

            MenuBackdropMotion motion = stage.AddComponent<MenuBackdropMotion>();
            motion.wall = menu.painter.wall;
            motion.canMotion = menu.motion;
            motion.cameraController = cameraController;
            motion.accessibility = menu.cameraFeedback;
            motion.wallMotion = 0.82f;
            motion.hologramIntensity = 0.88f;
            motion.foilStrength = 0.74f;
            motion.foilDarkness = 0.28f;

            Selection.activeGameObject = stage;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(menu.painter.wall.sharedMaterial);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void ImportBackdropTextures()
        {
            foreach (string path in new[]
            {
                TextureRoot + "/FlowState_Chrome_Logo.png",
                TextureRoot + "/Maikol_Mask_Monument.png"
            })
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer)
                    throw new InvalidOperationException("Missing backdrop texture: " + path);
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        static void ConfigureWallMaterial(Material wall)
        {
            if (!wall || wall.shader.name != "FLOWSTATE/Menu/Living Wall")
                throw new InvalidOperationException("The menu wall is not using the expected Living Wall shader.");
            wall.SetTexture("_BackdropLogo", Required<Texture2D>(TextureRoot + "/FlowState_Chrome_Logo.png"));
            wall.SetTexture("_BackdropMask", Required<Texture2D>(TextureRoot + "/Maikol_Mask_Monument.png"));
            wall.SetColor("_BaseColor", new Color(0.035f, 0.038f, 0.045f, 1f));
            wall.SetColor("_BackdropAccent", new Color(1f, 0.015f, 0.31f, 1f));
            wall.SetFloat("_BackdropMotion", 0.82f);
            wall.SetFloat("_BackdropHoloIntensity", 0.88f);
            wall.SetFloat("_BackdropFoilStrength", 0.74f);
            wall.SetFloat("_BackdropFoilDarkness", 0.28f);
        }

        static void ConfigureMenuTypography(GameObject root)
        {
            Color paper = new Color(0.9f, 0.91f, 0.94f, 1f);
            foreach (string name in new[] { "Print edition", "Wall caption", "Input legend", "Last mark confirmation" })
            {
                TextMeshPro text = root.GetComponentsInChildren<TextMeshPro>(true).FirstOrDefault(candidate => candidate.name == name);
                if (text)
                    text.color = paper;
            }

            TextMeshPro preview = root.GetComponentsInChildren<TextMeshPro>(true).FirstOrDefault(candidate => candidate.name == "Editor preview · replaced by actual spray in Play");
            if (preview)
                preview.color = new Color(1f, 0.08f, 0.36f, 1f);

            Transform oldLogo = root.transform.Find("YakuzaStudio · official FLOWSTATE print");
            if (oldLogo)
                oldLogo.gameObject.SetActive(false);
        }

        static void ConfigureCamera(Camera camera)
        {
            if (!camera)
                return;
            camera.backgroundColor = new Color(0.012f, 0.014f, 0.019f, 1f);
        }

        static void ConfigureOptionPaint(MenuOptionData[] options)
        {
            Color[] pigments =
            {
                new Color(0.96f, 0.93f, 0.9f, 1f),
                new Color(1f, 0.84f, 0.86f, 1f),
                new Color(0.82f, 0.87f, 1f, 1f),
                new Color(0.88f, 0.9f, 0.94f, 1f)
            };
            for (int index = 0; options != null && index < options.Length && index < pigments.Length; index++)
                if (options[index])
                {
                    options[index].paintColor = pigments[index];
                    EditorUtility.SetDirty(options[index]);
                }
        }

        static void RemoveGeneratedStage(Transform root, string stageName)
        {
            Transform stage = root.Find(stageName);
            if (stage)
                UnityEngine.Object.DestroyImmediate(stage.gameObject);
        }

        static T Required<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset)
                throw new InvalidOperationException("Missing required menu asset: " + path);
            return asset;
        }
    }
}
