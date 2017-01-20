using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines;
namespace Dreamteck.Splines
{
    public class CirclePrimitive : SplinePrimitive, ISplinePrimitive
    {
        
        private float radius = 1f;
        private int axis = 1;
        private string[] axisText = new string[] {"X", "Y", "Z"};

        public string GetName()
        {
            return "Circle";
        }

        public void SetOrigin(Vector3 o)
        {
            origin = o;
        }

        public override void Init(SplineComputer comp)
        {
            base.Init(comp);
        }

        public void Draw()
        {
            axis = EditorGUILayout.Popup("Axis", axis, axisText);
            radius = EditorGUILayout.FloatField("Radius", radius);
            SplinePoint[] generated = GetPoints(axis, radius);
            OffsetPoints(generated, origin);
            computer.type = Spline.Type.Bezier;
            computer.SetPoints(generated, SplineComputer.Space.Local);
            computer.Close();
            if (GUI.changed)
            {
                UpdateUsers();
                SceneView.RepaintAll();
            }
        }

        public static SplinePoint[] GetPoints(int axis, float radius) 
        {
            Vector3 look = Vector3.right;
            if (axis == 1) look = Vector3.up;
            if (axis == 2) look = Vector3.forward;
            SplinePoint[] points = CreatePoints(5, 1f, look, Color.white);
            points[0].position = Vector3.up / 2f * radius;
            points[0].tangent = points[0].position + Vector3.right * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;
            points[0].tangent2 = points[0].position - Vector3.right * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;

            points[1].position = Vector3.left / 2f * radius;
            points[1].tangent = points[1].position + Vector3.up * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;
            points[1].tangent2 = points[1].position - Vector3.up * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;

           
            points[2].position = Vector3.down / 2f * radius;
            points[2].tangent = points[2].position + Vector3.left * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;
            points[2].tangent2 = points[2].position - Vector3.left * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;
            points[3].position = Vector3.right / 2f * radius;
            points[3].tangent = points[3].position + Vector3.down * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;
            points[3].tangent2 = points[3].position - Vector3.down * 2 * (Mathf.Sqrt(2f) - 1f) / 3f * radius;
            points[4] = points[0];

            if (look != Vector3.forward)
            {
                Quaternion lookRot = Quaternion.LookRotation(look);
                RotatePoints(points, lookRot);
            }
            return points;
        }

        public void Cancel()
        {
            Revert();
        }

    }
}
