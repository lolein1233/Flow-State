using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlowState.Rendering.Editor
{
    [CustomEditor(typeof(FlowStatePaletteController))]
    public sealed class FlowStatePaletteControllerEditor : UnityEditor.Editor
    {
        private static readonly GUIContent[] PreviewLabels =
        {
            new("Normal", "Look vivo principal"),
            new("B/N", "Estado neutro o momento bajo"),
            new("Ira", "Rojo de alta intensidad"),
            new("Flow max", "Paleta multicolor al máximo")
        };

        private static readonly FlowEmotion[] PreviewEmotions =
        {
            FlowEmotion.Clarity,
            FlowEmotion.Neutral,
            FlowEmotion.Anger,
            FlowEmotion.CreativeFlow
        };

        private SerializedProperty initialEmotion;

        private void OnEnable()
        {
            initialEmotion = serializedObject.FindProperty("initialEmotion");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "initialEmotion");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Vista emocional", EditorStyles.boldLabel);

            int selected = FindPreviewIndex((FlowEmotion)initialEmotion.intValue);
            int next = GUILayout.Toolbar(selected, PreviewLabels, GUILayout.Height(32f));
            if (next >= 0 && next != selected)
                ApplyPreview(PreviewEmotions[next]);

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyPreview(FlowEmotion emotion)
        {
            FlowStatePaletteController controller = (FlowStatePaletteController)target;
            Undo.RecordObject(controller, "Change FSSRS emotion preview");
            initialEmotion.intValue = (int)emotion;
            serializedObject.ApplyModifiedProperties();
            controller.SetEmotion(emotion, 0f);
            EditorUtility.SetDirty(controller);

            if (!Application.isPlaying && controller.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        private static int FindPreviewIndex(FlowEmotion emotion)
        {
            for (int index = 0; index < PreviewEmotions.Length; index++)
            {
                if (PreviewEmotions[index] == emotion)
                    return index;
            }

            return -1;
        }
    }
}
