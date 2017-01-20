using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    public class SplineTool
    {
        protected SplineComputer computer;
        protected float resolution = 1f;
        protected float clipTo = 1f;
        protected float clipFrom = 0f;
        protected bool isOpen = false;
        protected bool promptSave = false;
        protected bool showResolution = true;
        protected bool showClip = true;

        protected virtual void DrawGUI()
        {
            EditorGUILayout.LabelField("Spline User", EditorStyles.boldLabel);
            SplineComputer lastComputer = computer;
            computer = (SplineComputer)EditorGUILayout.ObjectField("Computer", computer, typeof(SplineComputer), true);
            if (computer != lastComputer) Selection.activeGameObject = computer.gameObject;
            if(computer == null) EditorGUILayout.HelpBox("No SplineComputer is selected. Reference a spline computer!", MessageType.Error);
            if (showResolution) resolution = EditorGUILayout.Slider("Resolution", resolution, 0f, 1f);
            if (showClip)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.MinMaxSlider(new GUIContent("Clip range:"), ref clipFrom, ref clipTo, 0f, 1f);
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(30));
                clipFrom = EditorGUILayout.FloatField(clipFrom);
                clipTo = EditorGUILayout.FloatField(clipTo);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();
            }
        }

        protected virtual void LoadValues(string prefix)
        {
            if (EditorPrefs.HasKey(prefix + "_resolution")) resolution = EditorPrefs.GetFloat(prefix + "_resolution");
            if (EditorPrefs.HasKey(prefix + "_clipTo")) clipTo = EditorPrefs.GetFloat(prefix + "_clipTo");
            if (EditorPrefs.HasKey(prefix + "_clipFrom")) clipFrom = EditorPrefs.GetFloat(prefix + "_clipFrom");
        }

        protected virtual void SaveValues(string prefix)
        {
            EditorPrefs.SetFloat(prefix + "_resolution", resolution);
            EditorPrefs.SetFloat(prefix + "_clipTo", clipTo);
            EditorPrefs.SetFloat(prefix + "_clipFrom", clipFrom);
        }

        protected virtual void Rebuild()
        {
            
        }


        protected void GetSpline()
        {
            computer = null;
            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                computer = Selection.gameObjects[i].GetComponent<SplineComputer>();
                if (computer != null)  break;
            }
        }
    }

}