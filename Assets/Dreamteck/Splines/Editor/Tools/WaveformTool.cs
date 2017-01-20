using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    public class WaveformTool : MeshGenTool, ISplineTool
    {
        public string GetName()
        {
            return "Waveform";
        }

        private void Init()
        {
            WaveformGenerator wave = (WaveformGenerator)generator;
            LoadValues("WaveformTool");
            wave.uvScale = uvScale;
            wave.uvOffset = uvOffset;
            if (EditorPrefs.HasKey("WaveformTool_axis")) wave.axis = (WaveformGenerator.Axis)EditorPrefs.GetInt("WaveformTool_axis");
            if (EditorPrefs.HasKey("WaveformTool_slices")) wave.slices = EditorPrefs.GetInt("WaveformTool_slices");
            if (EditorPrefs.HasKey("WaveformTool_symmetry")) wave.symmetry = EditorPrefs.GetInt("WaveformTool_symmetry") != 0;
        }

        public void Close()
        {
            if (obj == null) return;
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Save changes?", "You are about to close the Waveform Tool, do you want to save the generated object?", "Yes", "No")) Save();
                else Cancel();
            } else Cancel();
            promptSave = false;
            isOpen = false;
        }

        protected override void DrawGUI()
        {
            WaveformGenerator wave = (WaveformGenerator)generator;
            base.DrawGUI();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Axis", EditorStyles.boldLabel);
            wave.axis = (WaveformGenerator.Axis)EditorGUILayout.EnumPopup("Axis", wave.axis);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            wave.slices = EditorGUILayout.IntField("Slices", wave.slices);
            if (wave.slices < 1) wave.slices = 1;
            wave.symmetry = EditorGUILayout.Toggle("Use Symmetry", wave.symmetry);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            wave.uvWrapMode = (WaveformGenerator.UVWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", wave.uvWrapMode);
            wave.uvOffset = EditorGUILayout.Vector2Field("UV Offset", wave.uvOffset);
            wave.uvScale = EditorGUILayout.Vector2Field("UV Scale", wave.uvScale);
            if (EditorGUI.EndChangeCheck())
            {
                BuildMesh();
                EditorUtility.SetDirty(wave.gameObject);
                promptSave = true;
                BuildMesh();
            }
            if (GUI.changed) SaveValues("WaveformTool");
        }

        protected override void SaveValues(string prefix)
        {
            WaveformGenerator wave = (WaveformGenerator)generator;
            uvScale = wave.uvScale;
            uvOffset = wave.uvOffset;
            base.SaveValues(prefix);
            EditorPrefs.SetInt("WaveformTool_axis", (int)wave.axis);
            EditorPrefs.SetInt("WaveformTool_slices", wave.slices);
            EditorPrefs.SetInt("WaveformTool_symmetry", wave.symmetry ? 1 : 0);
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
                BuildObject(computer, computer.name + "_wave");
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
                    BuildObject(computer, computer.name + "_wave");
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
            generator = obj.AddComponent<WaveformGenerator>();
            generator.computer = computer;
        }
    }
}
