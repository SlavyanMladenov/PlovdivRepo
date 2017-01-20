using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    public class TubeTool : MeshGenTool, ISplineTool
    {
        public string GetName()
        {
            return "Tube";
        }

        private void Init()
        {
            TubeGenerator tube = (TubeGenerator)generator;
            LoadValues("TubeTool");
            tube.uvScale = uvScale;
            tube.uvOffset = uvOffset;
            if (EditorPrefs.HasKey("TubeTool_sides")) tube.sides = EditorPrefs.GetInt("TubeTool_sides");
            if (EditorPrefs.HasKey("TubeTool_cap")) tube.cap = EditorPrefs.GetInt("TubeTool_cap") != 0;
            if (EditorPrefs.HasKey("TubeTool_roundness")) tube.integrity = EditorPrefs.GetFloat("TubeTool_roundness");
        }

        public void Close()
        {
            if (obj == null) return;
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Save changes?", "You are about to close the Tube Tool, do you want to save the generated object?", "Yes", "No")) Save();
                else Cancel();
            } else Cancel();
            promptSave = false;
            isOpen = false;
        }

        protected override void DrawGUI()
        {
            TubeGenerator tube = (TubeGenerator)generator;
            base.DrawGUI();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            tube.sides = EditorGUILayout.IntField("Sides", tube.sides);
            tube.cap = EditorGUILayout.Toggle("Cap", tube.cap);
            if (tube.sides < 3) tube.sides = 3;
            tube.integrity = EditorGUILayout.Slider("Integrity", tube.integrity, 0f, 360f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            tube.uvOffset = EditorGUILayout.Vector2Field("UV Offset", tube.uvOffset);
            tube.uvScale = EditorGUILayout.Vector3Field("UV Scale", tube.uvScale);
            if (EditorGUI.EndChangeCheck())
            {
                BuildMesh();
                EditorUtility.SetDirty(tube.gameObject);
                promptSave = true;
                BuildMesh();
            }
            if (GUI.changed) SaveValues("TubeTool");
        }

        protected override void SaveValues(string prefix)
        {
            TubeGenerator tube = (TubeGenerator)generator;
            uvScale = tube.uvScale;
            uvOffset = tube.uvOffset;
            base.SaveValues(prefix);
            EditorPrefs.SetInt("TubeTool_sides", tube.sides);
            EditorPrefs.SetInt("TubeTool_cap", tube.cap ? 1 : 0);
            EditorPrefs.SetFloat("TubeTool_roundness", tube.integrity);
            EditorPrefs.SetFloat("TubeTool_rotation", tube.rotation);
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
                BuildObject(computer, computer.name + "_tube");
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
                    BuildObject(computer, computer.name + "_tube");
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
            generator = obj.AddComponent<TubeGenerator>();
            generator.computer = computer;
        }
    }
}
