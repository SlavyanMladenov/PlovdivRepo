#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(SplineProjector), true)]
    public class SplineProjectorEditor : SplineUserEditor
    {
        private Vector3 lastPos = Vector3.zero;
        private int trigger = -1;
        private bool triggerFoldout = false;
        public override void OnInspectorGUI()
        {
            SplineProjector user = (SplineProjector)target;
            if (user.mode == SplineProjector.Mode.Accurate)
            {
                showResolution = false;
                showAveraging = false;
               
            }
            else
            {
                showResolution = true;
                showAveraging = true;
            }
            BaseGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Projector", EditorStyles.boldLabel);
            user.mode = (SplineProjector.Mode)EditorGUILayout.EnumPopup("Mode", user.mode);
            if(user.mode == SplineProjector.Mode.Accurate) user.subdivide = EditorGUILayout.IntSlider("Subdivisions", user.subdivide, 1, 8);
            user.projectTarget = (Transform)EditorGUILayout.ObjectField("Project Target", user.projectTarget, typeof(Transform), true);
            user.target = (Transform)EditorGUILayout.ObjectField("Apply Target", user.target, typeof(Transform), true);
            GUI.color = Color.white;

            if(user.target != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(20));
                user.applyPosition = EditorGUILayout.Toggle("Apply position", user.applyPosition);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(20));
                user.applyRotation = EditorGUILayout.Toggle("Apply rotation", user.applyRotation);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(20));
                user.applyScale = EditorGUILayout.Toggle("Apply scale", user.applyScale);
                EditorGUILayout.EndHorizontal();
            }


            //user.smooth = EditorGUILayout.Toggle("Smooth", user.smooth);
            user.autoProject = EditorGUILayout.Toggle("Auto Project", user.autoProject);

            triggerFoldout = EditorGUILayout.Foldout(triggerFoldout, "Triggers");
            if (triggerFoldout)
            {
                int lastTrigger = trigger;
                serializedObject.Update();
                SplineEditorGUI.TriggerArray(ref user.triggers, ref trigger);
                serializedObject.ApplyModifiedProperties();
                if (lastTrigger != trigger) Repaint();
            }

            if (GUI.changed && !Application.isPlaying && user.computer != null)
            {
                if (user.autoProject) {
                    user.CalculateProjection();
                    if (user.target == null) SceneView.RepaintAll();
                }
            }
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            SplineProjector user = (SplineProjector)target;
            for (int i = 0; i < user.triggers.Length; i++)
            {
                if (SplineEditorHandles.DrawTrigger(user.triggers[i], user))
                {
                    trigger = i;
                    Repaint();
                }
            }
            if (user.computer == null) return;
            if (Application.isPlaying) return;
            Vector3 projectPos = user.projectTarget.position;
            if (user.autoProject && lastPos != projectPos)
            {
                lastPos = projectPos;
                user.CalculateProjection();
            }
            if (!user.autoProject) return;
            if (user.projectResult == null) return;
            if (user.target == null)
            {
                Handles.color = Color.white;
                Handles.DrawLine(user.transform.position, user.projectResult.position);
                Handles.SphereCap(0, user.projectResult.position, Quaternion.LookRotation(user.projectResult.direction), HandleUtility.GetHandleSize(user.projectResult.position) * 0.2f);
            }

        }
    }
}
#endif
