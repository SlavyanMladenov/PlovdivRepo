using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Dreamteck.Splines {
    public class PresetsWindow : SplineEditorWindow
    {
        private ISplinePrimitive[] primitives;
        private SplinePreset[] presets;
        private Vector2 scroll = Vector2.zero;
        private int primitiveIndex = -1;
        private int presetIndex = -1;
        private bool showPrimitives = true, showPresets = false;
        private SplinePreset newPreset = null;
        private SplineComputer computer;

        public override void init(Editor input, string name, Vector2 minSize)
        {
            base.init(input, name, minSize);
            computer = (SplineComputer)editor.target;
            GetPrimitives();
            GetPresets();
        }
        
        void GetPrimitives()
        {
            List<Type> types = FindDerivedClasses.GetAllDerivedClasses(typeof(ISplinePrimitive));
            primitives = new ISplinePrimitive[types.Count];
            int count = 0;
            foreach (Type t in types)
            {
                primitives[count] = (ISplinePrimitive)Activator.CreateInstance(t);
                primitives[count].Init(computer);
                count++;
            }
        }

        Vector3 GetOrigin(SplineComputer comp)
        {
            Vector3 avg = Vector3.zero;
            SplinePoint[] points = comp.GetPoints(SplineComputer.Space.Local);
            for(int i = 0; i < comp.pointCount; i++)
            {
                avg += points[i].position;
            }
            if (points.Length > 0) avg /= points.Length;
            return avg;
        }

        void GetPresets()
        {
            presets = SplinePreset.LoadAll();
            for(int i = 0; i < presets.Length; i++)
            {
                presets[i].Init(computer);
            }
        }
        

        void OnGUI()
        {
            if (computer == null) return;

            showPrimitives = EditorGUILayout.Foldout(showPrimitives, "Primitives");
            if (showPrimitives)
            {
                Primitives();
            }           

            showPresets = EditorGUILayout.Foldout(showPresets, "Saved Presets");
            if (showPresets)
            {
                Presets();
            }
        }

        void OnDestroy()
        {
            if (primitiveIndex >= 0)
            {
                if(!EditorUtility.DisplayDialog("Keep changes?", "Do you want to apply the primitive to the spline ?", "Apply", "Revert")) primitives[primitiveIndex].Cancel();
            }
            if (presetIndex >= 0)
            {
                if (!EditorUtility.DisplayDialog("Keep changes?", "Do you want to apply the preset to the spline ?", "Apply", "Revert")) presets[presetIndex].Cancel();
            }
            presetIndex = -1;
            primitiveIndex = -1;
            SplineUser[] users = computer.GetComponents<SplineUser>();
            foreach (SplineUser user in users) user.Rebuild(true);
            computer.Rebuild();
            SceneView.RepaintAll();
            primitives = null;
            presets = null;
            newPreset = null;
        }

        void SavePresetDialog()
        {
            newPreset.name = EditorGUILayout.TextField("Preset name", newPreset.name);
            EditorGUILayout.LabelField("Description");
            newPreset.description = EditorGUILayout.TextArea(newPreset.description); 
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                string lower = newPreset.name.ToLower();
                string noSlashes = lower.Replace('/', '_');
                noSlashes = noSlashes.Replace('\\', '_');
                string noSpaces = noSlashes.Replace(' ', '_');
                newPreset.Save(noSpaces);
                newPreset = null;
                GetPresets();
            }
            if (GUILayout.Button("Cancel")) newPreset = null;
            EditorGUILayout.EndHorizontal();
        }

        void Primitives()
        {
            if (primitives == null) return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginScrollView(scroll, GUILayout.Width(position.width * 0.35f), GUILayout.MaxHeight(5*22));
            for (int i = 0; i < primitives.Length; i++)
            {
                if (primitiveIndex == i) GUILayout.Label(primitives[i].GetName());
                else if (GUILayout.Button(primitives[i].GetName()))
                {
                    Undo.RecordObject(this, "PresetsWindow state");
                    Undo.RecordObject(computer, "Spline state");
                    if (primitiveIndex >= 0 && primitiveIndex < primitives.Length) primitives[primitiveIndex].Cancel();
                    primitiveIndex = i;
                    primitives[i].SetOrigin(GetOrigin((SplineComputer)editor.target));
                    presetIndex = -1;
                }
            }
            EditorGUILayout.EndScrollView();
            


            if (primitiveIndex >= 0 && primitiveIndex < primitives.Length)
            {
                EditorGUILayout.BeginVertical();
                primitives[primitiveIndex].Draw();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save"))
                {
                    Undo.RecordObject(this, "PresetsWindow state");
                    Undo.RecordObject(computer, "Spline state");
                    primitiveIndex = -1;
                    this.Close();
                }
                
                if (GUILayout.Button("Cancel"))
                {
                    Undo.RecordObject(this, "PresetsWindow state");
                    Undo.RecordObject(computer, "Spline state");
                    primitives[primitiveIndex].Cancel();
                    primitiveIndex = -1;
                    SplineUser[] users = computer.GetComponents<SplineUser>();
                    foreach (SplineUser user in users) user.Rebuild(true);
                    computer.Rebuild();
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        void Presets()
        {
            if (presets == null) return;
            EditorGUILayout.BeginHorizontal();
           
            EditorGUILayout.BeginScrollView(scroll, GUILayout.Width(position.width * 0.35f), GUILayout.MaxHeight(5 * 22));
            if (presets.Length == 0)
            {
                EditorGUILayout.HelpBox("No saved presets available", MessageType.Info);
            }
            for (int i = 0; i < presets.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if (presetIndex == i) GUILayout.Label(presets[i].name);
                else if (GUILayout.Button(presets[i].name))
                {
                    Undo.RecordObject(this, "PresetsWindow state");
                    Undo.RecordObject(computer, "Spline state");
                    presetIndex = i;
                    primitiveIndex = -1;
                    computer.SetPoints(presets[i].points);
                    computer.type = presets[i].type;
                    if (presets[i].isClosed) computer.Close();
                    else computer.Break();
                    SplineUser[] users = computer.GetComponents<SplineUser>();
                    foreach (SplineUser user in users) user.Rebuild(true);
                    computer.Rebuild();
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("X", GUILayout.MaxWidth(30)))
                {
                   if(EditorUtility.DisplayDialog("Delete preset ?", "Do you want to delete this preset ? This action cannot be undone.", "Yes", "No"))
                    {
                        SplinePreset.Delete(presets[i].filename);
                        GetPresets();
                        if (presetIndex >= presets.Length) presetIndex = -1;
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Create Preset"))
            {
                presetIndex = -1;
                newPreset = new SplinePreset(computer.GetPoints(), computer.isClosed, computer.type);
                GetPresets();
            }
            EditorGUILayout.EndScrollView();

            if (presetIndex >= 0 && presetIndex < presets.Length)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(presets[presetIndex].name);
                EditorGUILayout.LabelField(presets[presetIndex].description);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save"))
                {
                    computer.Rebuild();
                    SceneView.RepaintAll();
                    presetIndex = -1;
                    this.Close();
                }
                if (GUILayout.Button("Cancel"))
                {
                    presets[presetIndex].Cancel();
                    presetIndex = -1;
                    SplineUser[] users = computer.GetComponents<SplineUser>();
                    foreach (SplineUser user in users) user.Rebuild(true);
                    computer.Rebuild();
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            else if (computer.pointCount > 0)
            {
                EditorGUILayout.BeginVertical();
                if (newPreset != null) SavePresetDialog();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
        
    }
}
