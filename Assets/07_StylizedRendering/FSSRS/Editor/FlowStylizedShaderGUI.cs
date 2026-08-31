using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowState.Rendering.Editor
{
    public sealed class FlowStylizedShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUILayout.LabelField("FLOW STATE - Incomplete Print", EditorStyles.boldLabel);
            EditorGUILayout.Space(3f);
            base.OnGUI(materialEditor, properties);

            foreach (Object target in materialEditor.targets)
            {
                if (target is not Material material)
                    continue;

                bool hasNormalMap = material.GetTexture("_BumpMap") != null;
                CoreUtils.SetKeyword(material, "_NORMALMAP", hasNormalMap);
                bool alphaClip = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f;
                CoreUtils.SetKeyword(material, "_ALPHATEST_ON", alphaClip);
            }
        }
    }
}
