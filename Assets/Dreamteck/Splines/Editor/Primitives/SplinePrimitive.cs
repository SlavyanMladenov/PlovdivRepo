using UnityEngine;
using System.Collections;
using Dreamteck.Splines;

namespace Dreamteck.Splines
{
    [System.Serializable]
    public class SplinePrimitive
    {
        [System.NonSerialized]
        protected SplineComputer computer;
        [System.NonSerialized]
        protected bool lastClosed = false;
        [System.NonSerialized]
        protected SplinePoint[] lastPoints = new SplinePoint[0];
        [System.NonSerialized]
        protected Spline.Type lastType = Spline.Type.Bezier;
        [System.NonSerialized]
        protected Vector3 origin = Vector3.zero;

        public virtual void Init(SplineComputer comp)
        {
            computer = comp;
            lastClosed = comp.isClosed;
            lastType = comp.type;
            lastPoints = comp.GetPoints(SplineComputer.Space.Local);
        }

        protected void Revert()
        {
            if (lastClosed) computer.Close();
            else computer.Break();
            computer.SetPoints(lastPoints, SplineComputer.Space.Local);
            computer.type = lastType;
        }

        protected void UpdateUsers()
        {
            if (computer == null) return;
            SplineUser[] users = computer.GetComponents<SplineUser>();
            foreach (SplineUser user in users) user.Rebuild(true);
            computer.Rebuild();
        }

        protected static SplinePoint[] CreatePoints(int count, float size, Vector3 normal, Color color)
        {
            SplinePoint[] points = new SplinePoint[count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new SplinePoint();
                points[i].type = SplinePoint.Type.Smooth;
                points[i].normal = normal;
                points[i].color = color;
                points[i].size = size;
            }
            return points;
        }

        protected static void OffsetPoints(SplinePoint[] points, Vector3 origin)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i].SetPosition(points[i].position + origin);
            }
        }

        protected static void RotatePoints(SplinePoint[] points, Quaternion rotation)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i].position = rotation * points[i].position;
                points[i].tangent = rotation * points[i].tangent;
                points[i].tangent2 = rotation * points[i].tangent2;
            }
        }
    }
}
