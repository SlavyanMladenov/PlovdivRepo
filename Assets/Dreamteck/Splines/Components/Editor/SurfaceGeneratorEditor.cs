#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(SurfaceGenerator))]
    public class SurfaceGeneratorEditor : MeshGenEditor
    {
        public override void BaseGUI()
        {
            SurfaceGenerator user = (SurfaceGenerator)target;
            base.BaseGUI();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            user.expand = EditorGUILayout.FloatField("Expand", user.expand);
            if (user.extrudeComputer == null)
            {
                user.extrude = EditorGUILayout.FloatField("Extrude", user.extrude);
            }
            user.extrudeComputer = (SplineComputer)EditorGUILayout.ObjectField("Extrude Path", user.extrudeComputer, typeof(SplineComputer), true);
            if(user.extrudeComputer != null)
            {
                float clipFrom = (float)user.extrudeClipFrom;
                float clipTo = (float)user.extrudeClipTo;
                EditorGUILayout.MinMaxSlider(new GUIContent("Extrude Clip Range:"), ref clipFrom, ref clipTo, 0f, 1f);
                user.extrudeClipFrom = clipFrom;
                user.extrudeClipTo = clipTo;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            user.uvOffset = EditorGUILayout.Vector2Field("UV Offset", user.uvOffset);
            user.uvScale = EditorGUILayout.Vector2Field("UV Scale", user.uvScale);

            if(user.extrude != 0f || user.extrudeComputer != null)
            {
                user.sideUvOffset = EditorGUILayout.Vector2Field("Side UV Offset", user.sideUvOffset);
                user.sideUvScale = EditorGUILayout.Vector2Field("Side UV Scale", user.sideUvScale);
                user.uniformUvs = EditorGUILayout.Toggle("Unform UVs", user.uniformUvs);
            }
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(user);
            
        }

        protected override void OnSceneGUI()
        {
            SurfaceGenerator user = (SurfaceGenerator)target;
            if(user.extrudeComputer != null)
            SplineEditor.DrawPath(user.extrudeComputer, SceneView.currentDrawingSceneView.camera, false, false, 0.5f);
        }
        
        public override void OnInspectorGUI()
        {
            showSize = false;
            showRotation = false;
            BaseGUI();
            ShowInfo();
        }  
    }
}
#endif