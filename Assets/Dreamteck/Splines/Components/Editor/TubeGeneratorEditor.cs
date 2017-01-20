#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(TubeGenerator))]
    public class TubeGeneratorEditor : MeshGenEditor
    {

        private TubeGenerator tubeGenerator = null;
        
        public override void BaseGUI()
        {
            tubeGenerator = (TubeGenerator)target;
            base.BaseGUI();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            tubeGenerator.sides = EditorGUILayout.IntField("Sides", tubeGenerator.sides);
            tubeGenerator.cap = EditorGUILayout.Toggle("Cap", tubeGenerator.cap);
            if (tubeGenerator.sides < 3) tubeGenerator.sides = 3;
            tubeGenerator.integrity = EditorGUILayout.Slider("Integrity", tubeGenerator.integrity, 0f, 360f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            tubeGenerator.uvOffset = EditorGUILayout.Vector2Field("UV Offset", tubeGenerator.uvOffset);
            tubeGenerator.uvScale = EditorGUILayout.Vector3Field("UV Scale", tubeGenerator.uvScale);
            tubeGenerator.uniformUVs = EditorGUILayout.Toggle("Uniform UVs", tubeGenerator.uniformUVs);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(tubeGenerator);
            }
        }

        
        public override void OnInspectorGUI()
        {
            BaseGUI();
            ShowInfo();
        } 
     
           
    }
}
#endif