#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(SplineFollower), true)]
    public class SplineFollowerEditor : SplineUserEditor
    {
        private int trigger = -1;
        private bool triggerFoldout = false;
        public override void OnInspectorGUI()
        {
            BaseGUI();
            SplineFollower user = (SplineFollower)target;
            user.wrapMode = (SplineFollower.Wrap)EditorGUILayout.EnumPopup("Wrap mode", user.wrapMode);
            GUI.color = Color.white;

            user.findStartPoint = EditorGUILayout.Toggle("Find Start Point", user.findStartPoint);
            if (!user.findStartPoint) user.startPercent = EditorGUILayout.Slider("Start percent", (float)user.startPercent, (float)user.clipFrom, (float)user.clipTo);
            
            user.autoFollow = EditorGUILayout.Toggle("Auto follow", user.autoFollow);
            if (user.autoFollow)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(20));
                user.followSpeed = EditorGUILayout.FloatField("Follow speed", user.followSpeed);
                EditorGUILayout.EndHorizontal();
            }
            user.direction = (Spline.Direction)EditorGUILayout.EnumPopup("Direction", user.direction);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(20));
            user.applyPosition = EditorGUILayout.Toggle("Apply position", user.applyPosition);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(20));
            user.applyRotation = EditorGUILayout.Toggle("Apply rotation", user.applyRotation);
            EditorGUILayout.EndHorizontal();

            if (user.applyRotation)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(40));
                user.applyDirectionRotation = EditorGUILayout.Toggle("Apply follow direction", user.applyDirectionRotation);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(20));
            user.applyScale = EditorGUILayout.Toggle("Apply scale", user.applyScale);
            EditorGUILayout.EndHorizontal();

            if (user.applyScale)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(40));
                user.baseScale = EditorGUILayout.Vector3Field("Base scale", user.baseScale);
                EditorGUILayout.EndHorizontal();
            }


            user.offset = EditorGUILayout.Vector2Field("Offset", user.offset);

            if (GUI.changed && !Application.isPlaying && user.computer != null)
            {
                if (!user.findStartPoint)
                {
                    SplineResult result = GetFollowResult(user, user.startPercent);
                    if (user.autoFollow)
                    {
                        if (user.applyPosition) user.transform.position = result.position;
                        if (user.applyRotation)
                        {
                            float dir = 1;
                            if (user.applyDirectionRotation) dir = user.followSpeed > 0 ? 1f : -1f;
                            user.transform.rotation = Quaternion.LookRotation(result.direction * dir, result.normal);
                        }
                        if (user.applyScale) user.transform.localScale = user.baseScale * result.size;
                    }
                    else SceneView.RepaintAll();
                }  
            }





            triggerFoldout = EditorGUILayout.Foldout(triggerFoldout, "Triggers");
            if (triggerFoldout)
            {
                int lastTrigger = trigger;
                SplineEditorGUI.TriggerArray(ref user.triggers, ref trigger);
                if (lastTrigger != trigger) Repaint();
            }
        }

        public SplineResult GetFollowResult(SplineFollower follower, double percent)
        {
            SplineResult result = follower.Evaluate(percent);
            Vector3 right = Vector3.Cross(result.direction, result.normal);
            result.position += -right * follower.offset.x * result.size + result.normal * follower.offset.y * result.size;
            return result;
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            SplineFollower user = (SplineFollower)target;

            for (int i = 0; i < user.triggers.Length; i++)
            {
               if(SplineEditorHandles.DrawTrigger(user.triggers[i], user))
                {
                    trigger = i;
                    Repaint();
                }
            }

            if (Application.isPlaying)
            {
                if (!user.autoFollow)
                {
                    Handles.color = SplineEditorGUI.selectionColor;
                    Handles.DrawLine(user.transform.position, user.followResult.position);
                    Handles.SphereCap(0, user.followResult.position, Quaternion.LookRotation(user.followResult.direction), HandleUtility.GetHandleSize(user.followResult.position) * 0.2f);
                    Handles.color = Color.white;
                }
                return;
            }
            if (user.computer == null) return;
            if (user.findStartPoint)
            {
                SplineResult result = GetFollowResult(user, user.computer.Project(user.transform.position, user.address, 4, user.clipFrom, user.clipTo));
                Handles.DrawLine(user.transform.position, result.position);
                Handles.SphereCap(0, result.position, Quaternion.LookRotation(result.direction), HandleUtility.GetHandleSize(result.position) * 0.2f);
            } else if(!user.autoFollow)
            {
                SplineResult result = GetFollowResult(user, user.startPercent);
                Handles.DrawLine(user.transform.position, result.position);
                Handles.SphereCap(0, result.position, Quaternion.LookRotation(result.direction), HandleUtility.GetHandleSize(result.position) * 0.2f);
            }
        }
    }
}
#endif
