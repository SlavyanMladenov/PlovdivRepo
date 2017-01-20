using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{

    public static class SplineEditorHandles
    {
        public static bool DrawTrigger(SplineTrigger trigger, SplineUser user)
        {
            if (trigger == null) return false;
            Camera cam = SceneView.currentDrawingSceneView.camera;
            SplineResult result = user.Evaluate(trigger.position);
            Handles.color = trigger.color;
            float size = HandleUtility.GetHandleSize(result.position);
            Vector3 center = result.position;
            Vector2 screenPosition = HandleUtility.WorldToGUIPoint(center);
            screenPosition.y += 20f;
            Vector3 localPos = cam.transform.InverseTransformPoint(center);
            if (localPos.z > 0f)
            {
                Handles.BeginGUI();
                SplineEditorGUI.Label(new Rect(screenPosition.x - 120 + trigger.name.Length * 4, screenPosition.y, 120, 25), trigger.name);
                Handles.EndGUI();
            }
            bool buttonClick = Button(center, false, Color.white, 0.3f);

            Vector3 right = Vector3.Cross(cam.transform.position - result.position, result.direction).normalized * size * 0.1f;
            if (trigger.type == SplineTrigger.Type.Backward)
            {
                center += result.direction * size * 0.06f;
                Vector3 front = center - result.direction * size * 0.2f;
                Handles.DrawLine(center + right, front);
                Handles.DrawLine(front, center - right);
                Handles.DrawLine(center - right, center + right);
            }
            else if (trigger.type == SplineTrigger.Type.Forward)
            {
                center -= result.direction * size * 0.06f;
                Vector3 front = center + result.direction * size * 0.2f;
                Handles.DrawLine(center + right, front);
                Handles.DrawLine(front, center - right);
                Handles.DrawLine(center - right, center + right);
            }
            else
            {
                center += result.direction * size * 0.025f;
                Vector3 front = center + result.direction * size * 0.17f;
                Handles.DrawLine(center + right, front);
                Handles.DrawLine(front, center - right);
                Handles.DrawLine(center - right, center + right);
                center -= result.direction * size * 0.05f;
                front = center - result.direction * size * 0.17f;
                Handles.DrawLine(center + right, front);
                Handles.DrawLine(front, center - right);
                Handles.DrawLine(center - right, center + right);
            }
            right = Vector3.Cross(cam.transform.position - result.position, result.direction).normalized * size * 0.25f;
            Handles.DrawLine(result.position + right / 2f, result.position + right);
            Handles.DrawLine(result.position - right / 2f, result.position - right);

            Vector3 lastPos = result.position;
            Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.5f);
            result.position = Handles.FreeMoveHandle(result.position, Quaternion.LookRotation(cam.transform.position - result.position), size * 0.2f, Vector3.zero, Handles.CircleCap);
            if (result.position != lastPos)
            {
                double projected = user.computer.Project(result.position, user.address);
                trigger.position = projected;
            }
            Handles.color = Color.white;
            return buttonClick;
        }

        public static bool Button(Vector3 position, bool drawHandle, Color color, float size)
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            Vector3 localPos = cam.transform.InverseTransformPoint(position);
            if (localPos.z < 0f) return false;

            size *= HandleUtility.GetHandleSize(position);
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(position);
            Vector2 screenRectBase = HandleUtility.WorldToGUIPoint(position - cam.transform.right * size + cam.transform.up * size);
            Rect rect = new Rect(screenRectBase.x, screenRectBase.y, (screenPos.x - screenRectBase.x) * 2f, (screenPos.y - screenRectBase.y) * 2f);
            if (drawHandle)
            {
                Color previousColor = Handles.color;
                Handles.color = color;
                Handles.RectangleCap(0, position, Quaternion.LookRotation(-cam.transform.forward), HandleUtility.GetHandleSize(position) * 0.1f);
                Handles.color = previousColor;
            }
            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static void DrawCross()
        {

        }
    }
}

