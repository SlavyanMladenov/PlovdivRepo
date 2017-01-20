using UnityEngine;
using Dreamteck;

namespace Dreamteck.Splines{
    [System.Serializable]
	public class SplineResult{
        public Vector3 position = Vector3.zero;
        public Vector3 normal = Vector3.up;
        public Vector3 direction = Vector3.forward;
        public Color color = Color.white;
        public float size = 1f;
        public double percent = 0.0;

        public Quaternion rotation
        {
            get { return Quaternion.LookRotation(direction, normal); }
        }

        public Vector3 right
        {
            get { return Vector3.Cross(normal, direction); }
        }


        public static SplineResult Lerp(SplineResult a, SplineResult b, float t)
        {
            SplineResult result = new SplineResult();
            result.position = Vector3.Lerp(a.position, b.position, t);
            result.direction = Vector3.Slerp(a.direction, b.direction, t);
            result.normal = Vector3.Slerp(a.normal, b.normal, t);
            result.color = Color.Lerp(a.color, b.color, t);
            result.size = Mathf.Lerp(a.size, b.size, t);
            result.percent = DMath.Lerp(a.percent, b.percent, t);
            return result;
        }

        public static SplineResult Lerp(SplineResult a, SplineResult b, double t)
        {
            SplineResult result = new SplineResult();
            float ft = (float)t;
            result.position = DMath.LerpVector3(a.position, b.position, t);
            result.direction = Vector3.Slerp(a.direction, b.direction, ft);
            result.normal = Vector3.Slerp(a.normal, b.normal, ft);
            result.color = Color.Lerp(a.color, b.color, ft);
            result.size = Mathf.Lerp(a.size, b.size, ft);
            result.percent = DMath.Lerp(a.percent, b.percent, t);
            return result;
        }

        public void Lerp(SplineResult b, double t)
        {
            float ft = (float)t;
            this.position = DMath.LerpVector3(this.position, b.position, t);
            this.direction = Vector3.Slerp(this.direction, b.direction, ft);
            this.normal = Vector3.Slerp(this.normal, b.normal, ft);
            this.color = Color.Lerp(this.color, b.color, ft);
            this.size = Mathf.Lerp(this.size, b.size, ft);
            this.percent = DMath.Lerp(this.percent, b.percent, t);
        }

        public void Lerp(SplineResult b, float t)
        {
            this.position = Vector3.Lerp(this.position, b.position, t);
            this.direction = Vector3.Slerp(this.direction, b.direction, t);
            this.normal = Vector3.Slerp(this.normal, b.normal, t);
            this.color = Color.Lerp(this.color, b.color, t);
            this.size = Mathf.Lerp(this.size, b.size, t);
            this.percent = DMath.Lerp(this.percent, b.percent, t);
        }

        public void Absorb(SplineResult input)
        {
            position = input.position;
            direction = input.direction;
            normal = input.normal;
            color = input.color;
            size = input.size;
            percent = input.percent;
        }

        public SplineResult()
        {
        }
		
        public SplineResult(Vector3 p, Vector3 n, Vector3 d, Color c, float s, double t)
        {
            position = p;
            normal = n;
            direction = d;
            color = c;
            size = s;
            percent = t;
        }

        public SplineResult(SplineResult input)
        {
            position = input.position;
            normal = input.normal;
            direction = input.direction;
            color = input.color;
            size = input.size;
            percent = input.percent;
        }
	}
}