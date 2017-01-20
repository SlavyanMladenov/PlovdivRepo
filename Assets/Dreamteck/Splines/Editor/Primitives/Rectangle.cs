using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines;
namespace Dreamteck.Splines
{
    public class RectanglePrimitive : SplinePrimitive, ISplinePrimitive
    {

        private Vector2 size = Vector2.one;
        private int axis = 1;
        private float rotation = 0f;
        private string[] axisText = new string[] {"X", "Y", "Z"};

        public string GetName()
        {
            return "Rectangle";
        }

        public override void Init(SplineComputer comp)
        {
            base.Init(comp);
        }

        public void SetOrigin(Vector3 o)
        {
            origin = o;
        }

        public void Draw()
        {
            axis = EditorGUILayout.Popup("Axis", axis, axisText);
            size = EditorGUILayout.Vector2Field("Size", size);
            rotation = EditorGUILayout.FloatField("Rotation", rotation);
            SplinePoint[] generated = GetPoints(axis, size, rotation);
            OffsetPoints(generated, origin);
            computer.type = Spline.Type.Linear;
            computer.SetPoints(generated, SplineComputer.Space.Local);
            computer.Close();
            if (GUI.changed)
            {
                UpdateUsers();
                SceneView.RepaintAll();
            }
        }

        public static SplinePoint[] GetPoints(int axis, Vector2 size, float rotation) 
        {
            Vector3 look = Vector3.right;
            if (axis == 1) look = Vector3.up;
            if (axis == 2) look = Vector3.forward;
            SplinePoint[] points = CreatePoints(5, 1f, look, Color.white);
            points[0].position = points[0].tangent = Vector3.up / 2f * size.y + Vector3.left / 2f * size.x;
            points[1].position = points[1].tangent = Vector3.up / 2f * size.y + Vector3.right / 2f * size.x;
            points[2].position = points[2].tangent = Vector3.down / 2f * size.y + Vector3.right / 2f * size.x;
            points[3].position = points[3].tangent = Vector3.down / 2f * size.y + Vector3.left / 2f * size.x;
            points[4] = points[0];

            if (look != Vector3.forward)
            {
                Quaternion lookRot = Quaternion.LookRotation(look);
                RotatePoints(points, lookRot);
            }
            RotatePoints(points, Quaternion.AngleAxis(rotation, look));
            return points;
        }

        public void Cancel()
        {
            Revert();
        }

    }
}
