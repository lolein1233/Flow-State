using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace FlowState.Playground.Editor
{
    public static class PlaygroundScenePolish
    {
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != PlaygroundSceneBuilder.ScenePath) throw new InvalidOperationException("Only the experiment can be edited.");
            var bridge = UnityEngine.Object.FindFirstObjectByType<PlaygroundPlayerBridge>();
            var door = UnityEngine.Object.FindObjectsByType<SlidingMechanism>(FindObjectsSortMode.None).First(x => x.manual);
            var south = GameObject.Find("Pulsador de acceso");
            var handle = south.GetComponent<MechanismHandle>() ?? south.AddComponent<MechanismHandle>();
            handle.mechanism = door; handle.interactionPoint = door.interactionPoint; handle.focusSignal = door.focusSignal;
            var north = GameObject.Find("Pulsador interior");
            if (north == null)
            {
                north = UnityEngine.Object.Instantiate(south, south.transform.parent); north.name = "Pulsador interior";
                north.transform.position = new Vector3(2.9f, 1.9f, -6.5f); north.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                var northHandle = north.GetComponent<MechanismHandle>(); northHandle.interactionPoint = north.transform; northHandle.focusSignal = handle.focusSignal;
            }
            bridge.interactables = new PlaygroundInteractable[] { handle, north.GetComponent<MechanismHandle>(), UnityEngine.Object.FindFirstObjectByType<MechanicalButton>() };
            // The camera copy contains the authored spray prop. Hide this optional prop in this mechanics-only experiment.
            foreach (var renderer in bridge.view.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var renderer in bridge.GetComponentsInChildren<Renderer>(true))
                if (renderer.name.Contains("Spray") || renderer.name.Contains("LATA")) renderer.enabled = false;

            string materialPath = PlaygroundSceneBuilder.Folder + "/Materials/WorldStencil.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var labels = UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (material == null)
            {
                material = new Material(Shader.Find("FLOWSTATE/Playground/World Stencil"));
                material.mainTexture = labels.First().font.material.mainTexture;
                AssetDatabase.CreateAsset(material, materialPath);
            }
            foreach (var label in labels) label.GetComponent<Renderer>().sharedMaterial = material;
            var brick = AssetDatabase.LoadAssetAtPath<Material>(PlaygroundSceneBuilder.Folder + "/Materials/Brick.mat");
            brick.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04_Materiales e Imagenes/textures/red_brick_diff_1k.jpg"));
            brick.SetTextureScale("_BaseMap", new Vector2(9f, 4f));
            brick.SetColor("_BaseColor", new Color(0.82f, 0.72f, 0.65f));
            brick.SetFloat("_PaletteInfluence", 0.45f);
            EditorUtility.SetDirty(brick); AssetDatabase.SaveAssetIfDirty(brick);
            // The surface and all stencil geometry are visible only from their physical side.
            door.focusSignal = null;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            NormalizeTemplates(material);
        }

        static void NormalizeTemplates(Material stencil)
        {
            foreach (string name in new[] { "MechanicalButton", "SlidingGate", "PushableCrate" })
            {
                string path = PlaygroundSceneBuilder.Folder + "/Prefabs/" + name + ".prefab";
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var label in prefab.GetComponentsInChildren<TextMesh>(true)) label.GetComponent<Renderer>().sharedMaterial = stencil;
                    var button = prefab.GetComponent<MechanicalButton>();
                    var gate = prefab.GetComponent<SlidingMechanism>();
                    if (button != null)
                    {
                        button.activated = new UnityEvent();
                        if (button.signal == null) button.signal = TemplateSignal(prefab.transform, new Vector3(0f, 0.6f, -0.15f));
                        button.focusSignal = button.signal;
                    }
                    if (gate != null)
                    {
                        gate.opened = new UnityEvent(); gate.closed = new UnityEvent(); gate.obstructed = new UnityEvent();
                        if (gate.stateSignal == null) gate.stateSignal = TemplateSignal(prefab.transform, new Vector3(0f, 3f, -0.25f));
                    }
                    var crate = prefab.GetComponent<PushableBody>(); if (crate != null) crate.acceleration = 24f;
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
        }
        static DiegeticSignal TemplateSignal(Transform parent, Vector3 localPosition)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = "Local state indicator";
            obj.transform.SetParent(parent, false); obj.transform.localPosition = localPosition; obj.transform.localScale = new Vector3(0.42f, 0.11f, 0.08f);
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlaygroundSceneBuilder.Folder + "/Materials/SafetyOchre.mat");
            var signal = obj.AddComponent<DiegeticSignal>(); signal.surfaces = new[] { obj.GetComponent<Renderer>() }; return signal;
        }
    }
}
