using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(SplineUser), true)]
    public class SplineUserEditor : Editor
    {
        protected bool showResolution = true;
        protected bool showClip = true;
        protected bool showAveraging = true;
        protected bool showUpdateMethod = true;
        protected bool showMultithreading = true;
        private PathWindow pathWindow = null;

        public virtual void BaseGUI() {
            base.OnInspectorGUI();
            SplineUser user = (SplineUser)target;
            if (user.computer != null && !user.computer.IsSubscribed(user)) user.computer.Subscribe(user);
            Undo.RecordObject(user, "Inspector Change");

            EditorGUILayout.LabelField("Spline User", EditorStyles.boldLabel);
            user.computer = (SplineComputer)EditorGUILayout.ObjectField("Computer", user.computer, typeof(SplineComputer), true);
            if (showUpdateMethod) user.updateMethod = (SplineUser.UpdateMethod)EditorGUILayout.EnumPopup("Update Method", user.updateMethod);
            if(user.computer == null)
            {
                EditorGUILayout.HelpBox("No SplineComputer is selected. Reference a spline computer!", MessageType.Error);
            }
            if(showResolution)  user.resolution = (double)EditorGUILayout.Slider("Resolution", (float)user.resolution, 0f, 1f);
            if (showClip)
            {
                EditorGUILayout.BeginHorizontal();
                float clipFrom = (float)user.clipFrom;
                float clipTo = (float)user.clipTo;
                EditorGUILayout.MinMaxSlider(new GUIContent("Clip Range:"), ref clipFrom, ref clipTo, 0f, 1f);
                user.clipFrom = clipFrom;
                user.clipTo = clipTo;
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(30));
                user.clipFrom = EditorGUILayout.FloatField((float)user.clipFrom);
                user.clipTo = EditorGUILayout.FloatField((float)user.clipTo);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();
            }
            if(showAveraging) user.averageResultVectors = EditorGUILayout.Toggle("Average Result Vectors", user.averageResultVectors);
            if(showMultithreading) user.multithreaded = EditorGUILayout.Toggle("Multithreading", user.multithreaded);
            if(user.computer != null && user.computer.nodeLinks.Length > 0)
            {
                if(GUILayout.Button("Edit junction path"))
                {
                    pathWindow = EditorWindow.GetWindow<PathWindow>();
                    pathWindow.init(this, "Junction Path", new Vector2(300, 150));
                }
            }
        }

        protected virtual void OnSceneGUI()
        {
            SplineUser user = (SplineUser)target;
            if (user.address.depth == 0)
            {
                if (user.computer != null && user.computer.transform != user.transform) SplineEditor.DrawPath(user.computer, SceneView.currentDrawingSceneView.camera, false, false);
            } else
            {
                if (user.computer == null) return;
                List<SplineComputer> allComputers =  user.computer.GetConnectedComputers();
                for(int i = 0; i < allComputers.Count; i++)
                {
                    SplineEditor.DrawPath(allComputers[i], SceneView.currentDrawingSceneView.camera, false, false, 0.4f);
                }
                SplineComputer[] computers = user.address.GetComputers();
                for(int i = 0; i < computers.Length; i++)
                {
                    int start, end;
                    user.address.GetConnectionRange(computers, i, out start, out end);
                    double startPercent = (double)start / (computers[i].pointCount - 1);
                    double endPercent = (double)end / (computers[i].pointCount - 1);
                    endPercent -= user.computer.moveStep;
                    SplineEditor.DrawPath(computers[i], SceneView.currentDrawingSceneView.camera, false, false, 1f, startPercent, endPercent, false);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            BaseGUI();
        }

        protected virtual void OnDestroy()
        {
            if (pathWindow != null) pathWindow.Close();
            SplineUser user = (SplineUser)target;
            if (Application.isEditor && !Application.isPlaying)
            {
                if (user == null) OnDelete(); //The object or the component is being deleted
                else if (user.computer != null) user.Rebuild(true);
            }
        }

        protected virtual void OnDelete()
        {

        }

        protected virtual void Awake()
        {
            SplineUser user = (SplineUser)target;
            user.EditorAwake();
        }
    }
}