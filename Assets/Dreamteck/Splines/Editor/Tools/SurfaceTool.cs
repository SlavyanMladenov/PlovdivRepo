using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    public class SurfaceTool : MeshGenTool, ISplineTool
    {
        public string GetName()
        {
            return "Surface";
        }

        private void Init()
        {
            SurfaceGenerator surface = (SurfaceGenerator)generator;
            LoadValues("SurfaceTool");
            surface.uvScale = uvScale;
            surface.uvOffset = uvOffset;
            if (EditorPrefs.HasKey("SurfaceTool_expand")) surface.expand = EditorPrefs.GetFloat("SurfaceTool_expand");
            if (EditorPrefs.HasKey("SurfaceTool_extrude")) surface.extrude = EditorPrefs.GetFloat("SurfaceTool_extrude");
            Vector2 uv = surface.sideUvScale;
            if (EditorPrefs.HasKey("SurfaceTool_sideUVScaleX")) uv.x = EditorPrefs.GetFloat("SurfaceTool_sideUVScaleX");
            if (EditorPrefs.HasKey("SurfaceTool_sideUVScaleY")) uv.y = EditorPrefs.GetFloat("SurfaceTool_sideUVScaleY");
            surface.sideUvScale = uv;
            uv = surface.sideUvOffset;
            if (EditorPrefs.HasKey("SurfaceTool_sideUVOffsetX")) uv.x = EditorPrefs.GetFloat("SurfaceTool_sideUVOffsetX");
            if (EditorPrefs.HasKey("SurfaceTool_sideUVOffsetY")) uv.y = EditorPrefs.GetFloat("SurfaceTool_sideUVOffsetY");
            surface.sideUvOffset = uv;
        }

        public void Close()
        {
            if (obj == null) return;
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Save changes?", "You are about to close the Surface Tool, do you want to save the generated object?", "Yes", "No")) Save();
                else Cancel();
            } else Cancel();
            promptSave = false;
            isOpen = false;
        }

        protected override void DrawGUI()
        {
            SurfaceGenerator surface = (SurfaceGenerator)generator;
            base.DrawGUI();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            surface.expand = EditorGUILayout.FloatField("Expand", surface.expand);
            surface.extrude = EditorGUILayout.FloatField("Extrude", surface.extrude);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            surface.uvOffset = EditorGUILayout.Vector2Field("UV Offset", surface.uvOffset);
            surface.uvScale = EditorGUILayout.Vector2Field("UV Scale", surface.uvScale);

            if (surface.extrude != 0f)
            {
                surface.sideUvOffset = EditorGUILayout.Vector2Field("Side UV Offset", surface.sideUvOffset);
                surface.sideUvScale = EditorGUILayout.Vector2Field("Side UV Scale", surface.sideUvScale);
            }
            if (EditorGUI.EndChangeCheck())
            {
                BuildMesh();
                EditorUtility.SetDirty(surface.gameObject);
                promptSave = true;
                BuildMesh();
            }
            if (GUI.changed) SaveValues("SurfaceTool");
        }

        protected override void SaveValues(string prefix)
        {
            SurfaceGenerator surface = (SurfaceGenerator)generator;
            uvScale = surface.uvScale;
            uvOffset = surface.uvOffset;
            base.SaveValues(prefix);
            EditorPrefs.SetFloat("SurfaceTool_expand", surface.expand);
            EditorPrefs.SetFloat("SurfaceTool_extrude", surface.extrude);
            EditorPrefs.SetFloat("SurfaceTool_sideUVScaleX", surface.sideUvScale.x);
            EditorPrefs.SetFloat("SurfaceTool_sideUVScaleY", surface.sideUvScale.y);
            EditorPrefs.SetFloat("SurfaceTool_sideUVOffsetX", surface.sideUvOffset.x);
            EditorPrefs.SetFloat("SurfaceTool_sideUVOffsetY", surface.sideUvOffset.y);
        }

        public void Draw(Rect windowRect)
        {
            showSize = false;
            showRotation = false;
            GetSpline();
            if (computer == null)
            {
                EditorGUILayout.HelpBox("No spline selected! Select an object with a SplineComputer component.", MessageType.Warning);
                return;
            }
            else if (obj == null && !isOpen)
            {
                BuildObject(computer, computer.name + "_surface");
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
            generator = obj.AddComponent<SurfaceGenerator>();
            generator.computer = computer;
        }
    }
}
