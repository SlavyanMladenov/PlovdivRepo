using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines;
namespace Dreamteck.Splines
{
    public class SpiralPrimitive : SplinePrimitive, ISplinePrimitive
    {
        
        private float radius = 1f;
        private int axis = 1;
        private float offset = 1f;
        private int iterations = 3;
        private string[] axisText = new string[] {"X", "Y", "Z"};

        public string GetName()
        {
            return "Spiral";
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
            radius = EditorGUILayout.FloatField("Radius", radius);
            offset = EditorGUILayout.FloatField("Offset", offset);
            iterations = EditorGUILayout.IntField("Iterations", iterations);
            if (iterations < 1) iterations = 1;
            SplinePoint[] generated = GetPoints(axis, radius, offset, iterations);
            OffsetPoints(generated, origin);
            computer.Break();
            computer.type = Spline.Type.Bezier;
            computer.SetPoints(generated, SplineComputer.Space.Local);
            if (GUI.changed)
            {
                UpdateUsers();
                SceneView.RepaintAll();
            }
        }

        public static SplinePoint[] GetPoints(int axis, float radius, float offset, int iterations) 
        {
            Vector3 look = Vector3.right;
            if (axis == 1) look = Vector3.up;
            if (axis == 2) look = Vector3.forward;
            SplinePoint[] points = CreatePoints(iterations*4+1, 1f, look, Color.white);
            Vector3 _offset = Vector3.zero;
            Vector3 offsetAdd = Vector3.forward * offset / 4f;
            for (int i = 0; i < iterations; i++)
            {
                points[0+i*4].position = Vector3.up / 2f * radius + _offset;
                points[0 + i * 4].tangent = points[0 + i * 4].position - (Vector3.right * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                points[0 + i * 4].tangent2 = points[0 + i * 4].position + (Vector3.right * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                _offset += offsetAdd;


                points[1 + i * 4].position = Vector3.right / 2f * radius + _offset;
                points[1 + i * 4].tangent = points[1 + i * 4].position - (Vector3.down * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                points[1 + i * 4].tangent2 = points[1 + i * 4].position + (Vector3.down * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                _offset += offsetAdd;

                points[2 + i * 4].position = Vector3.down / 2f * radius + _offset;
                points[2 + i * 4].tangent = points[2 + i * 4].position - (Vector3.left * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                points[2 + i * 4].tangent2 = points[2 + i * 4].position + (Vector3.left * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                _offset += offsetAdd;

                points[3 + i * 4].position = Vector3.left / 2f * radius + _offset;
                points[3 + i * 4].tangent = points[3 + i * 4].position - (Vector3.up * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                points[3 + i * 4].tangent2 = points[3 + i * 4].position + (Vector3.up * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
                _offset += offsetAdd;
            }

            points[points.Length-1].position = Vector3.up / 2f * radius + _offset;
            points[points.Length - 1].tangent = points[points.Length - 1].position - (Vector3.right * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
            points[points.Length - 1].tangent2 = points[points.Length - 1].position + (Vector3.right * radius + offsetAdd) * 2 * (Mathf.Sqrt(2f) - 1f) / 3f;
            _offset += Vector3.forward * offset / 4f;


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
