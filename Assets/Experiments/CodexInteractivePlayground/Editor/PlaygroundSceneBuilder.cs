using System;
using System.IO;
using System.Linq;
using FlowState.Rendering;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FlowState.Playground.Editor
{
    public static class PlaygroundSceneBuilder
    {
        public const string Folder = "Assets/Experiments/CodexInteractivePlayground";
        public const string ScenePath = Folder + "/InteractivePlayground.unity";
        static Transform environment;
        static Material concrete, brick, steel, orange, dark, white, teal;

        // Deliberately refuses to overwrite any scene, material or prefab.
        [MenuItem("Flow State/Experiments/Create Codex Interactive Playground")]
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Playground already exists. Open it; the builder never overwrites assets.");
            var source = UnityEngine.Object.FindFirstObjectByType<FPSController>();
            if (source == null || source.gameObject.scene.path != "Assets/02_Escenas/Gameplay.unity")
                throw new InvalidOperationException("Open Gameplay in Edit Mode to copy the authored player.");
            if (Application.isPlaying || source.gameObject.scene.isDirty)
                throw new InvalidOperationException("Source scene must be in Edit Mode and clean. No source scene will be saved by this tool.");

            Directory.CreateDirectory(Folder + "/Materials");
            Directory.CreateDirectory(Folder + "/Prefabs");
            var originalScene = source.gameObject.scene;
            var paletteSource = UnityEngine.Object.FindFirstObjectByType<FlowStatePaletteController>();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            environment = new GameObject("Codex Interactive Playground | Patio 07").transform;
            MakeMaterials();
            MakeLighting(paletteSource);
            var player = CopyPlayer(source);
            MakeArchitecture();

            var door = Door("01 | Acceso de servicio", new Vector3(0f, 2.35f, -7f), new Vector3(4.2f, 4.7f, 0.35f), new Vector3(4.5f, 0f, 0f), true);
            var doorControl = Box("Pulsador de acceso", new Vector3(2.9f, 1.9f, -7.5f), new Vector3(0.42f, 0.62f, 0.2f), steel);
            doorControl.transform.SetParent(door.transform, true);
            // Fixed control point must remain at the jamb, independent of the sliding panel.
            doorControl.transform.SetParent(environment, true);
            var anchor = new GameObject("Access control anchor").transform;
            anchor.SetParent(doorControl.transform, false);
            anchor.position = new Vector3(2.9f, 1.9f, -7.65f);
            door.interactionPoint = anchor;
            var doorFocus = Signal("Access amber", new Vector3(2.9f, 2.08f, -7.62f));
            door.focusSignal = doorFocus;
            Label("E", new Vector3(2.9f, 1.79f, -7.62f), 0.22f, white, 0f);
            Label("ACCESO / 07", new Vector3(0f, 5.35f, -7.48f), 0.55f, white, 0f);
            Label("CODEX INTERACTIVE PLAYGROUND", new Vector3(0f, 6.2f, -7.49f), 0.27f, orange, 0f);

            var gate = Door("03 | Compuerta del patio", new Vector3(4.7f, 2.65f, 13f), new Vector3(4.6f, 5.3f, 0.38f), new Vector3(0f, 5.6f, 0f), false);
            gate.duration = 2f;
            var gateSignal = Signal("Gate status", new Vector3(1.95f, 3.6f, 12.7f));
            gate.stateSignal = gateSignal;
            Label("SALIDA", new Vector3(4.7f, 6.15f, 12.55f), 0.55f, white, 0f);
            Label("07", new Vector3(4.7f, 2.8f, 12.75f), 1.3f, orange, 0f, gate.transform);

            var crate = MakeCrate();
            var platform = Box("Plataforma de mantenimiento", new Vector3(-4f, 1.65f, 6.5f), new Vector3(5.5f, 3.3f, 4f), concrete);
            Box("Borde amarillo plataforma", new Vector3(-4f, 3.32f, 4.56f), new Vector3(5.5f, 0.08f, 0.16f), orange, false);
            for (int i = 0; i < 9; i++) Box("Chapa vertical plataforma", new Vector3(-6.45f + i * 0.6f, 1.65f, 4.47f), new Vector3(0.07f, 3.2f, 0.05f), steel, false);
            Box("Panel de control elevado", new Vector3(-4f, 4.6f, 8.1f), new Vector3(2f, 2.6f, 0.35f), steel);
            var buttonRoot = new GameObject("02 | Interruptor mecanico");
            buttonRoot.transform.SetParent(environment);
            buttonRoot.transform.position = new Vector3(-4f, 5.05f, 7.82f);
            var button = buttonRoot.AddComponent<MechanicalButton>();
            var housing = Box("Carcasa", buttonRoot.transform.position, new Vector3(0.65f, 0.78f, 0.18f), dark);
            housing.transform.SetParent(buttonRoot.transform, true);
            var cap = Box("Boton", buttonRoot.transform.position + Vector3.back * 0.14f, new Vector3(0.42f, 0.42f, 0.12f), orange);
            cap.transform.SetParent(buttonRoot.transform, true);
            button.cap = cap.transform;
            button.interactionPoint = cap.transform;
            button.signal = Signal("Button status", new Vector3(-4f, 5.68f, 7.85f));
            button.focusSignal = button.signal;
            Label("E", new Vector3(-4f, 4.49f, 7.78f), 0.28f, white, 0f);
            Label("CONTROL / 07", new Vector3(-4f, 5.97f, 7.78f), 0.22f, white, 0f);
            UnityEventTools.AddPersistentListener(button.activated, gate.RequestOpen);
            UnityEventTools.AddBoolPersistentListener(gate.opened, button.signal.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(gate.opened, doorFocus.SetActive, true);
            // Exposed cable visually connects the independent mechanism and its switch.
            Pipe(new Vector3(-3.05f, 5.7f, 8f), new Vector3(-3.05f, 6.8f, 8f), orange, 0.05f);
            Pipe(new Vector3(-3.05f, 6.8f, 8f), new Vector3(1.9f, 6.8f, 8f), orange, 0.05f);
            Pipe(new Vector3(1.9f, 6.8f, 8f), new Vector3(1.9f, 6.8f, 13f), orange, 0.05f);
            Pipe(new Vector3(1.9f, 6.8f, 13f), new Vector3(1.9f, 3.7f, 13f), orange, 0.05f);

            var goalObject = new GameObject("04 | Zona de finalizacion");
            goalObject.transform.SetParent(environment);
            goalObject.transform.position = new Vector3(4.7f, 2f, 19f);
            var goalVolume = goalObject.AddComponent<BoxCollider>(); goalVolume.isTrigger = true; goalVolume.size = new Vector3(4.8f, 4f, 3f);
            var goal = goalObject.AddComponent<PlaygroundGoal>(); goal.gate = gate; goal.button = button;
            goal.signal = Signal("Arrival beacon", new Vector3(4.7f, 4.7f, 22.48f));
            goal.signal.readyMarker = Box("Arrival illuminated crossbar", new Vector3(4.7f, 3.6f, 22.48f), new Vector3(3.4f, 0.2f, 0.07f), teal, false);
            goal.signal.readyMarker.SetActive(false);
            Label("RUTA COMPLETA", new Vector3(4.7f, 4f, 22.48f), 0.45f, white, 0f);

            var bridge = player.AddComponent<PlaygroundPlayerBridge>();
            bridge.view = player.GetComponentInChildren<Camera>();
            bridge.interactables = new PlaygroundInteractable[] { door, button };
            var recovery = new GameObject("Inicio / recuperacion").transform;
            recovery.SetParent(environment); recovery.position = player.transform.position;
            bridge.recovery = recovery;
            // Read the established selection key; no new binding is added to the original input system.
            var existingSelection = source.GetComponentInChildren<GraffitiMenuDrawer>(true);
            if (existingSelection != null) bridge.interactionKey = existingSelection.selectKey;

            SaveNewPrefab(crate, "PushableCrate");
            // Reusable mechanism prefabs retain geometry and self-contained state; scene-only targets stay in the scene.
            SaveNewPrefab(buttonRoot, "MechanicalButton");
            SaveNewPrefab(gate.gameObject, "SlidingGate");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Scene save failed.");
            // Close only the unchanged source scene, after the experiment has been saved.
            EditorSceneManager.CloseScene(originalScene, false);
            PlaygroundScenePolish.Apply();
            Selection.activeGameObject = environment.gameObject;
            Debug.Log("[Codex Interactive Playground] Independent scene created: " + ScenePath);
        }

        static GameObject CopyPlayer(FPSController source)
        {
            var copy = UnityEngine.Object.Instantiate(source.gameObject);
            copy.name = "Player | MAIKOL (copia aislada)";
            SceneManager.MoveGameObjectToScene(copy, SceneManager.GetActiveScene());
            // Strip only copied non-locomotion behaviour, so E cannot also open the graffiti book.
            foreach (var component in copy.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
            {
                if (component == null || component is FPSController || component is UniversalAdditionalCameraData || component is PlayerComicBorder) continue;
                UnityEngine.Object.DestroyImmediate(component);
            }
            foreach (var canvas in copy.GetComponentsInChildren<Canvas>(true)) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            var backup = copy.transform.Find("MAIKOL_Static_Backup"); if (backup != null) UnityEngine.Object.DestroyImmediate(backup.gameObject);
            var capsule = copy.GetComponent<CapsuleCollider>(); if (capsule != null) capsule.enabled = false;
            var mesh = copy.GetComponent<MeshRenderer>(); if (mesh != null) mesh.enabled = false;
            copy.transform.position = new Vector3(0f, 1.55f, -12f); copy.transform.rotation = Quaternion.identity;
            var fps = copy.GetComponent<FPSController>();
            fps.cameraOverride = copy.GetComponentInChildren<Camera>(); fps.cameraHolder = fps.cameraOverride.transform;
            fps.animator = copy.GetComponentInChildren<Animator>(); fps.visualRoot = fps.animator.transform;
            fps.graffitiRaycast = null; fps.graffitiCursorCanvas = null;
            var data = fps.cameraOverride.GetComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            return copy;
        }

        static void MakeMaterials()
        {
            concrete = CopyMaterial("Concrete", "Suelo_2acb1f17_FSSRS", new Color(0.42f, 0.43f, 0.42f));
            brick = CopyMaterial("Brick", "ladrillo_50325de2_FSSRS", new Color(0.54f, 0.35f, 0.26f));
            steel = CopyMaterial("Steel", "Cajas met_1e10ce3c_FSSRS", new Color(0.26f, 0.34f, 0.38f));
            orange = NewMaterial("SafetyOchre", new Color(0.92f, 0.48f, 0.15f));
            dark = NewMaterial("Ink", new Color(0.085f, 0.12f, 0.17f));
            white = NewMaterial("Paper", new Color(0.91f, 0.87f, 0.73f));
            teal = NewMaterial("StateTeal", new Color(0.23f, 0.73f, 0.65f));
        }
        static Material CopyMaterial(string name, string search, Color tint)
        {
            string guid = AssetDatabase.FindAssets(search + " t:Material").FirstOrDefault();
            var original = string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            var mat = original != null ? new Material(original) : new Material(Shader.Find("FLOWSTATE/FSSRS/Stylized Lit"));
            mat.name = name; mat.SetColor("_BaseColor", tint);
            return SaveMaterial(mat, name);
        }
        static Material NewMaterial(string name, Color color)
        {
            var mat = new Material(Shader.Find("FLOWSTATE/FSSRS/Stylized Lit")); mat.SetColor("_BaseColor", color);
            mat.SetFloat("_AmbientStrength", 0.58f); mat.SetFloat("_PaletteInfluence", 0.55f);
            mat.SetFloat("_HatchStrength", 0.1f); mat.SetFloat("_RimStrength", 0.12f);
            return SaveMaterial(mat, name);
        }
        static Material SaveMaterial(Material mat, string name)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            if (File.Exists(path)) throw new IOException("Refusing to overwrite " + path);
            AssetDatabase.CreateAsset(mat, path); return mat;
        }
        static void SaveNewPrefab(GameObject source, string name)
        {
            string path = Folder + "/Prefabs/" + name + ".prefab";
            if (File.Exists(path)) throw new IOException("Refusing to overwrite " + path);
            PrefabUtility.SaveAsPrefabAsset(source, path);
        }

        static void MakeLighting(FlowStatePaletteController paletteSource)
        {
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtDirection/IndustrialDusk/IndustrialDusk_Sky.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.43f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.26f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.14f, 0.17f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.23f, 0.26f, 0.34f); RenderSettings.fogDensity = 0.008f;
            var sunObject = new GameObject("Industrial Dusk / warm key"); sunObject.transform.SetParent(environment);
            sunObject.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
            var sun = sunObject.AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, 0.71f, 0.46f);
            sun.intensity = 1.5f; sun.shadows = LightShadows.Soft; RenderSettings.sun = sun;
            var volumeObject = new GameObject("Industrial Dusk / shared original profile"); volumeObject.transform.SetParent(environment);
            var volume = volumeObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 200f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/ArtDirection/IndustrialDusk/Gameplay_IndustrialDusk.asset");
            if (paletteSource != null)
            {
                var palette = new GameObject("FSSRS / authored palette"); palette.transform.SetParent(environment);
                var controller = palette.AddComponent<FlowStatePaletteController>();
                EditorUtility.CopySerialized(paletteSource, controller);
                // Avoid a cross-scene target; no shared profile is changed by this copy.
                var serialized = new SerializedObject(controller); serialized.FindProperty("targetVolume").objectReferenceValue = null; serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Lamp(new Vector3(-4f, 7f, 6f), new Color(1f, 0.63f, 0.33f), 7f, 10f);
            Lamp(new Vector3(4.7f, 6.6f, 12f), new Color(0.35f, 0.7f, 0.9f), 5f, 11f);
            Lamp(new Vector3(0f, 6f, -8f), new Color(1f, 0.67f, 0.4f), 5f, 9f);
        }
        static void MakeArchitecture()
        {
            Box("Patio / hormigon", new Vector3(0f, -0.3f, 3f), new Vector3(23f, 0.6f, 40f), concrete);
            Box("Muro oeste", new Vector3(-11.5f, 4.5f, 3f), new Vector3(0.6f, 9f, 40f), brick);
            Box("Muro este", new Vector3(11.5f, 4.5f, 3f), new Vector3(0.6f, 9f, 40f), brick);
            Box("Muro sur", new Vector3(0f, 4.5f, -17f), new Vector3(23.6f, 9f, 0.6f), brick);
            Box("Muro norte", new Vector3(0f, 5f, 23f), new Vector3(23.6f, 10f, 0.8f), brick);
            WallWithOpening(-7f, 0f, 4.2f, 4.7f);
            WallWithOpening(13f, 4.7f, 4.6f, 5.3f);
            for (int i = 0; i < 7; i++)
            {
                float z = -14f + i * 5.5f;
                Box("Contrafuerte oeste", new Vector3(-11.05f, 4f, z), new Vector3(0.6f, 8f, 0.5f), concrete);
                Box("Contrafuerte este", new Vector3(11.05f, 4f, z), new Vector3(0.6f, 8f, 0.5f), concrete);
            }
            for (int i = 0; i < 10; i++)
                Box("Junta del pavimento", new Vector3(0f, 0.008f, -14f + i * 4f), new Vector3(22f, 0.015f, 0.035f), dark, false);
            for (int i = 0; i < 3; i++)
                Box("Junta longitudinal", new Vector3(-6f + i * 6f, 0.009f, 3f), new Vector3(0.035f, 0.016f, 39f), dark, false);
            for (int i = 0; i < 10; i++)
                Box("Rejilla drenaje", new Vector3(8f, 0.02f, -3f + i * 0.22f), new Vector3(1.4f, 0.04f, 0.08f), dark, false);
            // Crate lane has a clear approach, wide recovery room and no trapping rails.
            for (int i = 0; i < 7; i++)
            {
                Box("Guia caja izquierda", new Vector3(-5.25f, 0.023f, -2f + i), new Vector3(0.08f, 0.025f, 0.55f), orange, false);
                Box("Guia caja derecha", new Vector3(-2.75f, 0.023f, -2f + i), new Vector3(0.08f, 0.025f, 0.55f), orange, false);
            }
            Box("Tope de alineacion visual", new Vector3(-4f, 0.025f, 3.3f), new Vector3(2.5f, 0.03f, 0.1f), orange, false);
            FloorArrow(new Vector3(-4f, 0.035f, 2.9f), 0f);
            FloorArrow(new Vector3(2f, 0.035f, 9.5f), 55f);
            FloorArrow(new Vector3(4.7f, 0.035f, 16f), 0f);
            Label("MANTENIMIENTO", new Vector3(-7.5f, 5.6f, 12.56f), 0.42f, white, 0f);
            Label("07", new Vector3(-9.5f, 3.4f, 12.56f), 1.4f, orange, 0f);
            for (int i = 0; i < 3; i++)
            {
                Pipe(new Vector3(10.8f, 1.5f + i * 0.38f, -4f), new Vector3(10.8f, 1.5f + i * 0.38f, 11f), steel, 0.13f);
                Box("Soporte tuberia", new Vector3(10.7f, 2f, -2f + i * 5f), new Vector3(0.4f, 1.6f, 0.18f), dark, false);
            }
            // Distant silhouettes are simple local geometry, with no downloaded assets.
            Box("Silueta fabrica oeste", new Vector3(-16f, 8f, 12f), new Vector3(7f, 16f, 18f), dark, false);
            Box("Silueta fabrica norte", new Vector3(2f, 10f, 28f), new Vector3(19f, 20f, 7f), steel, false);
            Box("Chimenea", new Vector3(-12.5f, 14f, 22f), new Vector3(2f, 28f, 2f), brick, false);
        }
        static void WallWithOpening(float z, float x, float width, float height)
        {
            float leftEdge = x - width * 0.5f, rightEdge = x + width * 0.5f;
            Box("Muro izquierdo " + z, new Vector3((-11.5f + leftEdge) * 0.5f, 4.5f, z), new Vector3(leftEdge + 11.5f, 9f, 0.6f), brick);
            Box("Muro derecho " + z, new Vector3((11.5f + rightEdge) * 0.5f, 4.5f, z), new Vector3(11.5f - rightEdge, 9f, 0.6f), brick);
            Box("Dintel " + z, new Vector3(x, (height + 9f) * 0.5f, z), new Vector3(width, 9f - height, 0.6f), concrete);
            foreach (float side in new[] { -1f, 1f }) Box("Jamba", new Vector3(x + side * (width * 0.5f + 0.1f), height * 0.5f, z - 0.38f), new Vector3(0.18f, height, 0.12f), orange, false);
        }
        static SlidingMechanism Door(string name, Vector3 position, Vector3 size, Vector3 travel, bool manual)
        {
            var leaf = new GameObject(name); leaf.transform.SetParent(environment); leaf.transform.position = position;
            var collider = leaf.AddComponent<BoxCollider>(); collider.size = size;
            var rb = leaf.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var door = leaf.AddComponent<SlidingMechanism>(); door.localTravel = travel; door.manual = manual;
            var face = Box("Chapa", position, size, steel, false); face.transform.SetParent(leaf.transform, true);
            for (int i = 0; i < 7; i++)
            {
                var rib = Box("Nervio metalico", position + new Vector3(0f, -size.y * 0.42f + i * size.y * 0.14f, -size.z * 0.6f), new Vector3(size.x * 0.98f, 0.1f, 0.1f), dark, false);
                rib.transform.SetParent(leaf.transform, true);
            }
            Box("Rail", position + new Vector3(travel.x * 0.5f, size.y * 0.5f + 0.1f, 0f), new Vector3(size.x + Mathf.Abs(travel.x) + 0.4f, 0.2f, 0.3f), dark, false);
            return door;
        }
        static GameObject MakeCrate()
        {
            var root = new GameObject("Caja movil / 65 kg"); root.transform.SetParent(environment); root.transform.position = new Vector3(-4f, 0.82f, -1.5f);
            var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(2f, 1.6f, 2f);
            var friction = new PhysicsMaterial("CrateContact") { staticFriction = 0.65f, dynamicFriction = 0.55f, bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
            string path = Folder + "/Materials/CrateContact.physicMaterial";
            if (File.Exists(path)) throw new IOException("Refusing to overwrite " + path);
            AssetDatabase.CreateAsset(friction, path); collider.sharedMaterial = friction;
            var body = root.AddComponent<Rigidbody>(); body.mass = 65f; body.linearDamping = 2.8f; body.angularDamping = 8f;
            body.constraints = RigidbodyConstraints.FreezeRotation; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.maxDepenetrationVelocity = 1.5f; body.solverIterations = 12; body.solverVelocityIterations = 6;
            root.AddComponent<PushableBody>();
            var shell = Box("Caja de chapa", root.transform.position, new Vector3(2f, 1.6f, 2f), steel, false); shell.transform.SetParent(root.transform, true);
            foreach (float x in new[] { -0.87f, 0.87f }) foreach (float z in new[] { -0.87f, 0.87f })
            {
                var edge = Box("Esquinero", root.transform.position + new Vector3(x, 0f, z), new Vector3(0.16f, 1.65f, 0.16f), orange, false); edge.transform.SetParent(root.transform, true);
            }
            foreach (float y in new[] { -0.65f, 0.65f })
            {
                var band = Box("Fleje", root.transform.position + new Vector3(0f, y, 0f), new Vector3(2.05f, 0.12f, 2.05f), dark, false); band.transform.SetParent(root.transform, true);
            }
            Label("EMPUJAR", root.transform.position + new Vector3(0f, 0.1f, -1.03f), 0.24f, white, 0f, root.transform);
            return root;
        }
        static GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool collision = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(environment);
            obj.transform.position = position; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        static void Pipe(Vector3 start, Vector3 end, Material material, float radius)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = "Conducto"; obj.transform.SetParent(environment);
            obj.transform.position = (start + end) * 0.5f; obj.transform.up = (end - start).normalized;
            obj.transform.localScale = new Vector3(radius * 2f, Vector3.Distance(start, end) * 0.5f, radius * 2f);
            obj.GetComponent<Renderer>().sharedMaterial = material; UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        }
        static void Label(string text, Vector3 position, float size, Material material, float yaw, Transform parent = null)
        {
            // Painted/stencilled lettering in the world, never a Canvas or screen overlay.
            var obj = new GameObject("Stencil / " + text); obj.transform.SetParent(parent != null ? parent : environment); obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var label = obj.AddComponent<TextMesh>(); label.text = text; label.fontSize = 64; label.characterSize = size / 6f; label.anchor = TextAnchor.MiddleCenter;
            label.color = material.GetColor("_BaseColor");
        }
        static void FloorArrow(Vector3 position, float yaw)
        {
            var root = new GameObject("Flecha pintada").transform; root.SetParent(environment); root.position = position; root.rotation = Quaternion.Euler(0f, yaw, 0f);
            var stem = Box("Trazo", position, new Vector3(0.12f, 0.015f, 0.8f), orange, false); stem.transform.SetParent(root, true);
            foreach (float side in new[] { -1f, 1f })
            {
                var arm = Box("Punta", position + new Vector3(side * 0.18f, 0f, 0.25f), new Vector3(0.1f, 0.015f, 0.5f), orange, false);
                arm.transform.SetParent(root, true); arm.transform.localRotation = Quaternion.Euler(0f, side * 45f, 0f);
            }
        }
        static DiegeticSignal Signal(string name, Vector3 position)
        {
            var obj = Box(name, position, new Vector3(0.42f, 0.11f, 0.08f), orange, false);
            var signal = obj.AddComponent<DiegeticSignal>(); signal.surfaces = new[] { obj.GetComponent<Renderer>() };
            return signal;
        }
        static void Lamp(Vector3 position, Color color, float intensity, float range)
        {
            var obj = new GameObject("Luminaria industrial"); obj.transform.SetParent(environment); obj.transform.position = position;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
            Box("Carcasa luminaria", position + Vector3.up * 0.1f, new Vector3(0.8f, 0.18f, 0.45f), steel, false);
        }
    }
}
