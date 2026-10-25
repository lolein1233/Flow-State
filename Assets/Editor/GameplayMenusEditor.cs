using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(PauseMenuController))]
public sealed class GameplayMenusEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (Application.isPlaying) return;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Edita los textos, botones y fondos en los hijos PauseMenu y ControlsMenu. Los cambios se conservan al entrar en Play.", MessageType.Info);
        if (GUILayout.Button("Ver pausa")) Preview((PauseMenuController)target, 0);
        if (GUILayout.Button("Ver ayuda · movimiento / graffiti")) Preview((PauseMenuController)target, 1);
        if (GUILayout.Button("Ver ayuda · música / presentación")) Preview((PauseMenuController)target, 2);
        if (GUILayout.Button("Ocultar menús")) Preview((PauseMenuController)target, -1);
    }

    [MenuItem("FLOWSTATE/UI/Crear menús editables en Gameplay")]
    public static void CreateMenus()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/02_Escenas/Gameplay.unity")
        {
            Debug.LogWarning("Abre Gameplay fuera de Play Mode para crear los menús editables.");
            return;
        }

        const string materialPath = "Assets/Resources/UI/PauseHolographicFoil.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/UI/PauseHolographicFoil.shader");
            var material = new Material(shader) { name = "PauseHolographicFoil" };
            material.SetFloat("_Opacity", 0.22f);
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        var controller = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            var root = new GameObject("MENÚS - PAUSA Y CONTROLES");
            Undo.RegisterCreatedObjectUndo(root, "Crear menús editables");
            controller = Undo.AddComponent<PauseMenuController>(root);
        }
        controller.CreateEditableMenus();
        foreach (Component component in controller.GetComponentsInChildren<Component>(true))
            if (component != null) EditorUtility.SetDirty(component);
        Preview(controller, 0);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = controller.gameObject;
    }

    private static void Preview(PauseMenuController controller, int view)
    {
        var serialized = new SerializedObject(controller);
        var pause = serialized.FindProperty("menuRoot").objectReferenceValue as GameObject;
        var controls = serialized.FindProperty("controlsRoot").objectReferenceValue as GameObject;
        if (pause == null || controls == null) return;
        Undo.RecordObjects(new Object[] { pause, controls }, "Vista de menús");
        pause.SetActive(view == 0);
        controls.SetActive(view > 0);
        pause.GetComponent<CanvasGroup>().alpha = 1f;
        controls.GetComponent<CanvasGroup>().alpha = 1f;
        if (view > 0) controller.SetControlsPage(view - 1);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        SceneView.RepaintAll();
    }
}
