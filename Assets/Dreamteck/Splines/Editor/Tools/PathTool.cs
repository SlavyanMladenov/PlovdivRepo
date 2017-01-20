using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    public class PathTool : MeshGenTool, ISplineTool
    {
        public string GetName()
        {
            return "Path";
        }

        private void Init()
        {
            PathGenerator path = (PathGenerator)generator;
            LoadValues("PathTool");
            path.uvScale = uvScale;
            path.uvOffset = uvOffset;
            if (EditorPrefs.HasKey("PathTool_slices")) path.slices = EditorPrefs.GetInt("PathTool_slices");
            if (EditorPrefs.HasKey("PathTool_useCurveShape")) path.useShapeCurve = EditorPrefs.GetInt("PathTool_useCurveShape") != 0;
        }

        public void Close()
        {
            if (obj == null) return;
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Save changes?", "You are about to close the Path Tool, do you want to save the generated object?", "Yes", "No")) Save();
                else Cancel();
            } else Cancel();
            promptSave = false;
            isOpen = false;
        }

        protected override void DrawGUI()
        {
            PathGenerator path = (PathGenerator)generator;
            base.DrawGUI();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            path.slices = EditorGUILayout.IntField("Slices", path.slices);
            path.useShapeCurve = EditorGUILayout.Toggle("Use Curve Shape", path.useShapeCurve);
            if (path.useShapeCurve)
            {
                if (path.slices == 1) EditorGUILayout.HelpBox("Slices are set to 1. The curve shape may not be approximated correctly. You can increase the slices in order to fix that.", MessageType.Warning);
                path.shape = EditorGUILayout.CurveField("Shape Curve", path.shape);
                path.shapeExposure = EditorGUILayout.FloatField("Shape Exposure", path.shapeExposure);
            }
            if (path.slices < 1) path.slices = 1;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            path.uvOffset = EditorGUILayout.Vector2Field("UV Offset", path.uvOffset);
            path.uvScale = EditorGUILayout.Vector2Field("UV Scale", path.uvScale);

            if (EditorGUI.EndChangeCheck())
            {
                BuildMesh();
                EditorUtility.SetDirty(path.gameObject);
                promptSave = true;
                BuildMesh();
            }
            if (GUI.changed) SaveValues("PathTool");
        }

        protected override void SaveValues(string prefix)
        {
            PathGenerator path = (PathGenerator)generator;
            uvScale = path.uvScale;
            uvOffset = path.uvOffset;
            base.SaveValues(prefix);
            EditorPrefs.SetInt("PathTool_slices", path.slices);
            EditorPrefs.SetInt("PathTool_useCurveShape", path.useShapeCurve ? 1 : 0);
        }

        public void Draw(Rect windowRect)
        {
            GetSpline();
            if (computer == null)
            {
                EditorGUILayout.HelpBox("No spline selected! Select an object with a SplineComputer component.", MessageType.Warning);
                return;
            }
            else if (obj == null && !isOpen)
            {
                BuildObject(computer, computer.name + "_path");
                BuildMesh();
            }
            if (!isOpen) Init();
            isOpen = true;

            if (obj != null) DrawGUI();

            EditorGUILayout.BeginHorizontal();
            if (obj == null)
            {
                if (GUILayout.Button("New"))
                {
                    BuildObject(computer, computer.name + "_path");
                    BuildMesh();
                }
            }
            else
            {
                if (GUILayout.Button("Save"))
                {
                    Save();
                }
                if (GUILayout.Button("Cancel"))
                {
                    Cancel();
                }
            }
            EditorGUILayout.EndHorizontal();

        }

        protected override void BuildObject(SplineComputer computer, string name)
        {
            base.BuildObject(computer, name);
            generator = obj.AddComponent<PathGenerator>();
            generator.computer = computer;
        }
    }
}
