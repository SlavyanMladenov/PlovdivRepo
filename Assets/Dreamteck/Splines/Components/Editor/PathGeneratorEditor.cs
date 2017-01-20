#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(PathGenerator), true)]
    public class PathGeneratorEditor : MeshGenEditor
    {
        public override void BaseGUI()
        {
            PathGenerator pathGenerator = (PathGenerator)target;
            base.BaseGUI();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            pathGenerator.slices = EditorGUILayout.IntField("Slices", pathGenerator.slices);
            pathGenerator.useShapeCurve = EditorGUILayout.Toggle("Use Curve Shape", pathGenerator.useShapeCurve);
            if (pathGenerator.useShapeCurve)
            {
                if (pathGenerator.slices == 1) EditorGUILayout.HelpBox("Slices are set to 1. The curve shape may not be approximated correctly. You can increase the slices in order to fix that.", MessageType.Warning);
                pathGenerator.shape = EditorGUILayout.CurveField("Shape Curve", pathGenerator.shape);
                pathGenerator.shapeExposure = EditorGUILayout.FloatField("Shape Exposure", pathGenerator.shapeExposure);
            }
            if (pathGenerator.slices < 1) pathGenerator.slices = 1;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            pathGenerator.uvOffset = EditorGUILayout.Vector2Field("UV Offset", pathGenerator.uvOffset);
            pathGenerator.uvScale = EditorGUILayout.Vector2Field("UV Scale", pathGenerator.uvScale);
            pathGenerator.uniformUVs = EditorGUILayout.Toggle("Uniform UVs", pathGenerator.uniformUVs);
            if (EditorGUI.EndChangeCheck())
            {
                //EditorUtility.SetDirty(pathGenerator);
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
