using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

namespace Dreamteck.Splines
{
    public class ObjectSpawnTool : SplineTool, ISplineTool
    {
        protected GameObject obj;
        protected ObjectController spawner;

        public string GetName()
        {
            return "Spawn Objects";
        }

        public void Close()
        {
            if (obj == null) return;
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Save changes?", "You are about to close the Object Spawn Tool, do you want to save the generated objects?", "Yes", "No")) Save();
                else Cancel();
            }
            else Cancel();
            promptSave = false;
            isOpen = false;
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
                BuildObject(computer, computer.name + "_objects");
                Rebuild();
            }
            if (!isOpen) Init();
            isOpen = true;
            EditorGUI.BeginChangeCheck();
            if(obj != null) DrawGUI();
            if (EditorGUI.EndChangeCheck()) promptSave = true;
            if (GUI.changed)
            {
                if (spawner != null)
                {
                    spawner.enabled = false;
                    Rebuild();
                    EditorUtility.SetDirty(spawner.gameObject);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (obj == null)
            {
                if (GUILayout.Button("New"))
                {
                    BuildObject(computer, computer.name + "_objects");
                    Rebuild();
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

        protected override void DrawGUI()
        {
            base.DrawGUI();
            SerializedObject serializedObject = new SerializedObject(spawner);
            spawner.objectMethod = (ObjectController.ObjectMethod)EditorGUILayout.EnumPopup("Object Method", spawner.objectMethod);
            if (spawner.objectMethod == ObjectController.ObjectMethod.Instantiate)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Instantiate Objects", EditorStyles.boldLabel);
                serializedObject.Update();
                float labelWidth = EditorGUIUtility.labelWidth;
                float fieldWidth = EditorGUIUtility.fieldWidth;
                EditorGUIUtility.labelWidth = 0;
                EditorGUIUtility.fieldWidth = 0;
                SerializedProperty tps = serializedObject.FindProperty("objects");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(tps, true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUIUtility.fieldWidth = fieldWidth;
                bool hasObj = false;
                for (int i = 0; i < spawner.objects.Length; i++)
                {
                    if (spawner.objects[i] != null)
                    {
                        hasObj = true;
                        break;
                    }
                }

                if (hasObj) spawner.spawnCount = EditorGUILayout.IntField("Spawn count", spawner.spawnCount);
                else spawner.spawnCount = 0;
                spawner.delayedSpawn = EditorGUILayout.Toggle("Delayed spawn", spawner.delayedSpawn);
                if (spawner.delayedSpawn)
                {
                    spawner.spawnDelay = EditorGUILayout.FloatField("Spawn Delay", spawner.spawnDelay);
                }
                spawner.iteration = (ObjectController.Iteration)EditorGUILayout.EnumPopup("Iteration", spawner.iteration);

            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
            spawner.applyRotation = EditorGUILayout.Toggle("Apply Rotation", spawner.applyRotation);
            spawner.applyScale = EditorGUILayout.Toggle("Apply Scale", spawner.applyScale);

            spawner.objectPositioning = (ObjectController.Positioning)EditorGUILayout.EnumPopup("Object Positioning", spawner.objectPositioning);
            spawner.positionOffset = EditorGUILayout.Slider("Evaluate Offset", spawner.positionOffset, -1f, 1f);

            spawner.offset = EditorGUILayout.Vector2Field("Offset", spawner.offset);
            spawner.randomizeOffset = EditorGUILayout.Toggle("Randomize Offset", spawner.randomizeOffset);
            if (spawner.randomizeOffset)
            {
                spawner.randomSize = EditorGUILayout.Vector2Field("Size", spawner.randomSize);
                spawner.randomSeed = EditorGUILayout.IntField("Random Seed", spawner.randomSeed);
                //user.randomOffsetSize = EditorGUILayout.FloatField("Size", user.randomOffsetSize);
                spawner.shellOffset = EditorGUILayout.Toggle("Shell", spawner.shellOffset);
                spawner.useRandomOffsetRotation = EditorGUILayout.Toggle("Apply offset rotation", spawner.useRandomOffsetRotation);
            }
        }

        private void Init()
        {
            LoadValues("ObjectSpawnTool");
            
        }

        protected override void LoadValues(string prefix)
        {
            base.LoadValues(prefix);
            //Load other values
        }

        protected override void SaveValues(string prefix)
        {
            base.SaveValues(prefix);
        }

        protected void Save()
        {
            GameObject.DestroyImmediate(spawner); 
            obj.transform.parent = computer.transform;
            obj = null;
        }

        protected void Cancel()
        {
            SplineUser user = obj.GetComponent<SplineUser>();
            if (user != null) user.computer.Unsubscribe(user);
            GameObject.DestroyImmediate(obj);
        }

        protected virtual void BuildObject(SplineComputer computer, string name)
        {
            if (obj != null) GameObject.DestroyImmediate(obj);
            obj = new GameObject(name);
            obj.transform.position = computer.transform.position;
            obj.transform.rotation = computer.transform.rotation;
            obj.transform.localScale = computer.transform.localScale;
            obj.transform.parent = computer.transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            spawner = obj.AddComponent<ObjectController>();
            spawner.computer = computer;
        }

        protected override void Rebuild()
        {
            base.Rebuild();
            spawner.resolution = resolution;
            spawner.clipFrom = clipFrom;
            spawner.clipTo = clipTo;
            spawner.Rebuild(true);
        }
    }
}
