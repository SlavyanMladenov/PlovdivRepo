using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    public class ExtrudeMeshTool : MeshGenTool, ISplineTool
    {
        public string GetName()
        {
            return "Extrude Mesh";
        }

        private void Init()
        {
            ExtrudeMesh extruder = (ExtrudeMesh)generator;
            LoadValues("ExtrudeMeshTool");
            if (EditorPrefs.HasKey("ExtrudeMeshTool_axis")) extruder.axis = (ExtrudeMesh.Axis)EditorPrefs.GetInt("ExtrudeMeshTool_axis");
            if (EditorPrefs.HasKey("ExtrudeMeshTool_removeInnerFaces")) extruder.removeInnerFaces = EditorPrefs.GetInt("ExtrudeMeshTool_removeInnerFaces") != 0;
            if (EditorPrefs.HasKey("ExtrudeMeshTool_repeat")) extruder.repeat = EditorPrefs.GetInt("ExtrudeMeshTool_repeat");
            if (EditorPrefs.HasKey("ExtrudeMeshTool_spacing")) extruder.spacing = EditorPrefs.GetFloat("ExtrudeMeshTool_spacing");
            Vector2 scale = extruder.scale;
            if (EditorPrefs.HasKey("ExtrudeMeshTool_scaleX")) scale.x = EditorPrefs.GetFloat("ExtrudeMeshTool_scaleX");
            if (EditorPrefs.HasKey("ExtrudeMeshTool_scaleY")) scale.y = EditorPrefs.GetFloat("ExtrudeMeshTool_scaleY");
            extruder.scale = scale;
        }

        public void Close()
        {
            if (obj == null) return;
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Save changes?", "You are about to close the Extrude Mesh Tool, do you want to save the generated object?", "Yes", "No")) Save();
                else Cancel();
            } else Cancel();
            promptSave = false;
            isOpen = false;
        }

        protected override void DrawGUI()
        {
            ExtrudeMesh extruder = (ExtrudeMesh)generator;
            base.DrawGUI();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);
            Object obj = extruder.sourceMesh;
            obj = EditorGUILayout.ObjectField("Source Mesh", obj, typeof(Object), true);
            if (extruder.sourceMesh == null)
            {
                EditorGUILayout.HelpBox("No mesh selected. Select a mesh from the field above.", MessageType.Warning);
            }
            if (obj != null)
            {
                if (obj is Mesh) extruder.sourceMesh = (Mesh)obj;
                else if (obj is GameObject)
                {
                    GameObject gameObj = (GameObject)obj;
                    MeshFilter filter = gameObj.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null) extruder.sourceMesh = filter.sharedMesh;
                    MeshRenderer meshRend = gameObj.GetComponent<MeshRenderer>();
                    if (meshRend != null)
                    {
                        MeshRenderer extrudeRend = extruder.GetComponent<MeshRenderer>();
                        if (meshRend.sharedMaterials != null) extrudeRend.sharedMaterials = meshRend.sharedMaterials;
                        else if (meshRend.materials != null) extrudeRend.materials = meshRend.materials;
                    }
                }
            }

            extruder.axis = (ExtrudeMesh.Axis)EditorGUILayout.EnumPopup("Axis", extruder.axis);
            extruder.removeInnerFaces = EditorGUILayout.Toggle("Remove Inner Faces", extruder.removeInnerFaces);
            extruder.repeat = EditorGUILayout.IntField("Repeat", extruder.repeat);
            if (extruder.repeat < 1) extruder.repeat = 1;
            extruder.spacing = EditorGUILayout.Slider("Spacing", (float)extruder.spacing, 0f, 1f);
            extruder.scale = EditorGUILayout.Vector2Field("Scale", extruder.scale);
            if (EditorGUI.EndChangeCheck())
            {
                BuildMesh();
                EditorUtility.SetDirty(extruder.gameObject);
                promptSave = true;
                BuildMesh();
            }
            if (GUI.changed) SaveValues("ExtrudeMeshTool");
        }

        protected override void SaveValues(string prefix)
        {
            ExtrudeMesh extruder = (ExtrudeMesh)generator;
            base.SaveValues(prefix);
            EditorPrefs.SetInt("ExtrudeMeshTool_axis", (int)extruder.axis);
            EditorPrefs.SetInt("ExtrudeMeshTool_removeInnerFaces", extruder.removeInnerFaces ? 1 : 0);
            EditorPrefs.SetInt("ExtrudeMeshTool_repeat", extruder.repeat);
            EditorPrefs.SetFloat("ExtrudeMeshTool_spacing", (float)extruder.spacing);
            EditorPrefs.SetFloat("ExtrudeMeshTool_scaleX", extruder.scale.x);
            EditorPrefs.SetFloat("ExtrudeMeshTool_scaleY", extruder.scale.y);
        }

        public void Draw(Rect windowRect)
        {
            showSize = false;
            showColor = false;
            showDoubleSided = false;
            showFlipFaces = false;
            GetSpline();
            if (computer == null)
            {
                EditorGUILayout.HelpBox("No spline selected! Select an object with a SplineComputer component.", MessageType.Warning);
                return;
            }
            else if (obj == null && !isOpen)
            {
                BuildObject(computer, computer.name + "_extrude");
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
                    BuildObject(computer, computer.name + "_extrude");
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
            generator = obj.AddComponent<ExtrudeMesh>();
            generator.computer = computer;
        }
    }
}
