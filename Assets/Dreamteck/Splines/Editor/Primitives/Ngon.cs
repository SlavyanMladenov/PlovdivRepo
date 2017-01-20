using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines;
namespace Dreamteck.Splines
{
    public class NgonPrimitive : SplinePrimitive, ISplinePrimitive
    {
        
        private float radius = 1f;
        private int axis = 1;
        private int sides = 3;
        private float rotation = 0f;
        private string[] axisText = new string[] {"X", "Y", "Z"};

        public string GetName()
        {
            return "N-gon";
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
            sides = EditorGUILayout.IntField("Sides", sides);
            if (sides < 3) sides = 3;
            radius = EditorGUILayout.FloatField("Radius", radius);
            rotation = EditorGUILayout.FloatField("Rotation", rotation);
            SplinePoint[] generated = GetPoints(axis, radius, sides, rotation);
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

        public static SplinePoint[] GetPoints(int axis, float radius, int sides, float rotation) 
        {
            Vector3 look = Vector3.right;
            if (axis == 1) look = Vector3.up;
            if (axis == 2) look = Vector3.forward;
            SplinePoint[] points = CreatePoints(sides+1, 1f, look, Color.white);
            for(int i = 0; i < sides; i++)
            {
                float percent = (float)i / (float)sides;
                Vector3 pos = Quaternion.AngleAxis(360f * percent, Vector3.forward) * Vector3.right * radius;
                points[i].SetPosition(pos);
            }
            points[points.Length-1] = points[0];


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
