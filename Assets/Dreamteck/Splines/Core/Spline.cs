using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Dreamteck;

namespace Dreamteck.Splines {
    //The Spline class defines a spline with world coordinates. It comes with various sampling methods
    [System.Serializable]
    public class Spline {
        public enum Direction { Forward = 1, Backward = -1 }
        public enum Type { Hermite, BSpline, Bezier, Linear};
        public SplinePoint[] points = new SplinePoint[0];
        [SerializeField]
        private bool closed = false;
        public Type type = Type.Bezier;
        public AnimationCurve customValueInterpolation = null;
        public AnimationCurve customNormalInterpolation = null;
        [Range(0f, 0.9999f)]
        public double precision = 0.9f;
        /// <summary>
        /// Returns true if the spline is closed
        /// </summary>
        public bool isClosed
        {
            get
            {
                return closed;
            }
            set { }
        }
        /// <summary>
        /// The step size of the percent incrementation when evaluating a spline (based on percision)
        /// </summary>
        public double moveStep
        {
            get {
                if (type == Type.Linear) return 1f / (points.Length-1);
                return 1f / (iterations-1);
            }
            set { }
        }
       /// <summary>
        /// The total count of samples for the spline (based on the precision)
       /// </summary>
       public int iterations
        {
            get {
                if (type == Type.Linear) return points.Length;
                return DMath.CeilInt(1.0 / ((1.0 - precision) / (points.Length - 1)))+1;
            }
            set { }
        }


		public Spline(Type t){
			type = t;
			points = new SplinePoint[0];
		}

        public Spline(Type t, double p)
        {
            type = t;
            precision = p;
            points = new SplinePoint[0];
        }

        /// <summary>
        /// Calculate the length of the spline
        /// </summary>
        /// <param name="from">Calculate from [0-1] default: 0f</param>
        /// <param name="to">Calculate to [0-1] default: 1f</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <returns></returns>
        public float CalculateLength(double from = 0.0, double to = 1.0, double resolution = 1.0)
        {
            resolution = DMath.Clamp01(resolution);
            if (resolution == 0.0) return 0f;
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            if (to < from) to = from;
            double percent = from;
            Vector3 lastPos = EvaluatePosition(percent);
            float sum = 0f;
            while (true)
            {
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 pos = EvaluatePosition(percent);
                sum += (pos - lastPos).magnitude;
                lastPos = pos;
                if (percent == to) break;
            }
            return sum;
        }

        /// <summary>
        /// Project point on the spline. Returns evaluation percent.
        /// </summary>
        /// <param name="point">3D Point</param>
        /// <param name="subdivide">Subdivisions default: 4</param>
        /// <param name="from">Sample from [0-1] default: 0f</param>
        /// <param name="to">Sample to [0-1] default: 1f</param>
        /// <returns></returns>
        public double Project(Vector3 point, int subdivide = 4, double from = 0.0, double to = 1.0)
        {
            double t = GetClosestPoint(point, from, to, points.Length*6);
            if(t == 0f)
            {
                float endDist = (point - EvaluatePosition(to - moveStep)).sqrMagnitude;
                float beginDist = (point - EvaluatePosition(from + moveStep)).sqrMagnitude;
                if(endDist < beginDist) t = GetClosestPoint(point, to-moveStep, to, points.Length * 6);
            }

            double delta = moveStep;
            int stepsInSegment = Mathf.RoundToInt(Mathf.Max(iterations/points.Length, 10));
            for (int i = 0; i < subdivide; i++)
            {
                t = GetClosestPoint(point, t - delta, t + delta, stepsInSegment*10);
                delta /= 10;
            }
            return t;
        }

        /// <summary>
        /// Casts rays along the spline against all colliders in the scene
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percent of evaluation where the hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <returns></returns>
        public bool Raycast(out RaycastHit hit, out double hitPercent, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
        , QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal
#endif
        )
        {
            resolution = DMath.Clamp01(resolution);
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            hitPercent = 0f;
            if (resolution == 0f)
            {
                hit = new RaycastHit();
                hitPercent = 0f;
                return false;
            }
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent);
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
                if (Physics.Linecast(fromPos, toPos, out hit, layerMask, hitTriggers))
#else 
                if (Physics.Linecast(fromPos, toPos, out hit, layerMask))
#endif
                {
                    double segmentPercent = (hit.point - fromPos).sqrMagnitude / (toPos - fromPos).sqrMagnitude;
                    hitPercent = DMath.Lerp(prevPercent, percent, segmentPercent);
                    return true;
                }
                fromPos = toPos;
                if (percent == to) break;
            }
            return false;
        }


        /// <summary>
        /// Casts rays along the spline against all colliders in the scene and returns all hits. Order is not guaranteed.
        /// </summary>
        /// <param name="hits">Hit information</param>
        /// <param name="hitPercents">The percents of evaluation where each hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <returns></returns>
        public bool RaycastAll(out RaycastHit[] hits, out double[] hitPercents, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
            , QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal
#endif
            )
        {
            resolution = DMath.Clamp01(resolution);
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            List<RaycastHit> hitList = new List<RaycastHit>();
            List<double> percentList = new List<double>();
            if (resolution == 0f)
            {
                hits = new RaycastHit[0];
                hitPercents = new double[0];
                return false;
            }
            bool hasHit = false;
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent);
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
                RaycastHit[] h = Physics.RaycastAll(fromPos, toPos - fromPos, Vector3.Distance(fromPos, toPos), layerMask, hitTriggers);
#else 
                RaycastHit[] h = Physics.RaycastAll(fromPos, toPos - fromPos, Vector3.Distance(fromPos, toPos), layerMask);
#endif
                for (int i = 0; i < h.Length; i++)
                {
                    hasHit = true;
                    double segmentPercent = (h[i].point - fromPos).sqrMagnitude / (toPos - fromPos).sqrMagnitude;
                    percentList.Add(DMath.Lerp(prevPercent, percent, segmentPercent));
                    hitList.Add(h[i]);
                }
                fromPos = toPos;
                if (percent == to) break;
            }
            hits = hitList.ToArray();
            hitPercents = percentList.ToArray();
            return hasHit;
        }

        /// <summary>
        /// Evaluate the spline and return position. This is simpler and faster than Evaluate.
        /// </summary>
        /// <param name="percent">Percent of evaluation [0-1]</param>
        public Vector3 EvaluatePosition(double percent)
        {
            Vector3 point = new Vector3();
            EvaluatePosition(ref point, percent);
            return point;
        }

        /// <summary>
        /// Evaluate the spline at given time and return a SplineResult
        /// </summary>
        /// <param name="percent">Percent of evaluation [0-1]</param>
        public SplineResult Evaluate(double percent)
        {
            percent = DMath.Clamp01(percent);
			if(closed && points.Length <= 2) closed = false;
            percent = DMath.Clamp01(percent);
            SplineResult result = new SplineResult();
            if (points.Length == 1)
            {
                result.position = points[0].position;
                result.normal = points[0].normal;
                result.direction = Vector3.forward;
                result.size = points[0].size;
                result.color = points[0].color;
                result.percent = percent;
                return result;
            }

			Vector3 point = new Vector3();
            double doubleIndex = (points.Length - 1) * percent;
            int pointIndex = Mathf.Clamp(DMath.FloorInt(doubleIndex), 0, points.Length - 2);
            double getPercent = doubleIndex - pointIndex;
            EvaluatePosition(ref point, percent);
            result.position = point;
            result.percent = percent;
			if(pointIndex <= points.Length-2){
				SplinePoint nextPoint = points[pointIndex+1];
				if(pointIndex == points.Length-2 && closed) nextPoint = points[0];
                float valueInterpolation = (float)getPercent;
                if (customValueInterpolation != null)
                {
                    if (customValueInterpolation.keys.Length > 0f) valueInterpolation = customValueInterpolation.Evaluate(valueInterpolation);
                }
                float normalInterpolation = (float)getPercent;
                if (customNormalInterpolation != null)
                {
                    if (customNormalInterpolation.keys.Length > 0) normalInterpolation = customNormalInterpolation.Evaluate(normalInterpolation); 
                }
                result.size = Mathf.Lerp(points[pointIndex].size, nextPoint.size, valueInterpolation);
                result.color = Color.Lerp(points[pointIndex].color, nextPoint.color, valueInterpolation);
                result.normal = Vector3.Slerp(points[pointIndex].normal, nextPoint.normal, normalInterpolation);
			} else{ 
				if(closed){
					result.size = points[0].size;
					result.color = points[0].color;
					result.normal = points[0].normal;
				} else {
					result.size = points[pointIndex].size;
					result.color = points[pointIndex].color;
					result.normal = points[pointIndex].normal;
				}
			}
            double step = (1.0 - precision)/points.Length;
            if(percent < 1.0-step)  result.direction = EvaluatePosition(percent + step) - result.position;
            else
            {
                if (closed)
                {
                    result.direction = EvaluatePosition(percent + step) - result.position;
                    result.direction += EvaluatePosition(percent + step - 1f) - EvaluatePosition(0f);
                }
                else
                {
                    result.direction = EvaluatePosition(DMath.Clamp01(percent + step)) - result.position;
                    if (Mathf.Max(result.direction.x, result.direction.y, result.direction.z) <= 0.009f) //If the direction vector is too small, calculate the direction backwards
                    {
                        double backPercent = DMath.Clamp01(percent - step);
                        result.direction = result.position - EvaluatePosition(backPercent);
                    }
                }
            }
            result.direction.Normalize();
			return result;
		}

        /// <summary>
        /// Evaluates the spline segment based on the spline's precision. 
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void Evaluate(ref SplineResult[] samples, double from = 0.0, double to = 1.0)
        {
            from = DMath.Clamp01(from);
            to = DMath.Clamp(to, from, 1.0);
            double fromValue = from * (iterations - 1);
            double toValue = to * (iterations - 1);
            int clippedIterations = DMath.CeilInt(toValue) - DMath.FloorInt(fromValue) + 1;
            if (samples == null) samples = new SplineResult[clippedIterations];
            if (samples.Length != clippedIterations) samples = new SplineResult[clippedIterations];
            double percent = from;
            double ms = moveStep;
            int index = 0;
            while (true)
            {
                samples[index] = Evaluate(percent);
                index++;
                if (index >= samples.Length) break;
                percent = DMath.Move(percent, to, ms);
            }
        }

        public void EvaluatePositions(Vector3[] positions, double from = 0.0, double to = 1.0)
        {
            from = DMath.Clamp01(from);
            to = DMath.Clamp(to, from, 1.0);
            double fromValue = from * (iterations - 1);
            double toValue = to * (iterations - 1);
            int clippedIterations = DMath.CeilInt(toValue) - DMath.FloorInt(fromValue) + 1;
            if (positions.Length != clippedIterations) positions = new Vector3[clippedIterations];
            double percent = from;
            double ms = moveStep;
            int index = 0;
            while (true)
            {
                EvaluatePosition(ref positions[index], percent);
                index++;
                if (index >= positions.Length) break;
                percent = DMath.Move(percent, to, ms);
            }
        }

        //Get closest point in spline segment. Used for projection
        private double GetClosestPoint(Vector3 point, double from, double to, int steps)
        {
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            double step = (to - from) / steps;
            double Res = 0;
            double Ref = double.MaxValue;
            for (int i = 0; i <= steps; i++)
            {
                double t = from + step * i;
                float L = (EvaluatePosition(t) - point).sqrMagnitude;
                if (L < Ref)
                {
                    Ref = L;
                    Res = t;
                }
            }
            return Res;
        }

        /// <summary>
        /// Break the closed spline
        /// </summary>
        public void Break()
        {
            Break(0);
        }

        /// <summary>
        /// Break the closed spline at given point
        /// </summary>
        /// <param name="at"></param>
        public void Break(int at)
        {
            if (!closed) return;
            if (at >= points.Length) return;
            SplinePoint[] prev = new SplinePoint[at];
            for (int i = 0; i < prev.Length; i++) prev[i] = points[i];
            for (int i = at; i < points.Length - 1; i++) points[i - at] = points[i];
            for (int i = 0; i < prev.Length; i++) points[points.Length - at + i - 1] = prev[i];
            points[points.Length - 1] = points[0];
            closed = false;
        }

        /// <summary>
        /// Close the spline. This will cause the first and last points of the spline to merge
        /// </summary>
        public void Close()
        {
            if (points.Length < 4)
            {
                Debug.LogError("Points need to be at least 4 to close the spline");
                return;
            }
            closed = true;
        }

        private void EvaluatePosition(ref Vector3 point, double percent)
        {
            percent = DMath.Clamp01(percent);
            if (closed && points.Length <= 2) closed = false;
            double doubleIndex = (points.Length - 1) * percent;
            int pointIndex = Mathf.Clamp(DMath.FloorInt(doubleIndex), 0, points.Length - 2);
            //Replace this with GetPoint
            switch (type)
            {
                case Type.Hermite: point = HermiteGetPoint(doubleIndex - pointIndex, pointIndex); break;
                case Type.Bezier: point = BezierGetPoint(doubleIndex - pointIndex, pointIndex); break;
                case Type.BSpline: point = BSPGetPoint(doubleIndex - pointIndex, pointIndex); break;
                case Type.Linear: point = LinearGetPoint(doubleIndex - pointIndex, pointIndex); break;
            }
        }

        private Vector3 GetPoint(double percent, int pointIndex)
        {
            switch (type)
            {
                case Type.Hermite: return HermiteGetPoint(percent, pointIndex);
                case Type.Bezier: return BezierGetPoint(percent, pointIndex);
                case Type.Linear: return LinearGetPoint(percent, pointIndex);
                default: return Vector3.zero;
            }
        }

        private Vector3 LinearGetPoint(double t, int i)
        {
			Vector3 result = Vector3.zero;
			if(points.Length > 0) result = points[0].position;
			if(i < points.Length-1){
				t = DMath.Clamp01(t);
				i = Mathf.Clamp(i, 0, points.Length-2);
				
				if(closed && i == points.Length-2) points[points.Length-1] = points[0];
				
				Vector3 P0 = points[i].position;
				Vector3 P1 = points[i+1].position;
				result = Vector3.Lerp(P0, P1, (float)t);
			}	
			return result;
		}

        private Vector3 BSPGetPoint(double t, int i)
        {
            //Used for getting a point on a B-spline
            Vector3 result = Vector3.zero;
            if (points.Length > 0) result = points[0].position;
            if (points.Length  > 1)
            {
                t = DMath.Clamp01(t);
                Vector3[] P = GetBCFFPoints(i);
                result.x = (float)(((-3.0 * P[0].x + 3.0 * P[2].x) / 6.0 + t * ((3.0 * P[0].x - 6.0 * P[1].x + 3.0 * P[2].x) / 6.0 + t * (-P[0].x + 3.0 * P[1].x - 3.0 * P[2].x + P[3].x) / 6.0)) * t + (P[0].x + 4.0 * P[1].x + P[2].x) / 6.0);
                result.y = (float)(((-3.0 * P[0].y + 3.0 * P[2].y) / 6.0 + t * ((3.0 * P[0].y - 6.0 * P[1].y + 3.0 * P[2].y) / 6.0 + t * (-P[0].y + 3.0 * P[1].y - 3.0 * P[2].y + P[3].y) / 6.0)) * t + (P[0].y + 4.0 * P[1].y + P[2].y) / 6.0);
                result.z = (float)(((-3.0 * P[0].z + 3.0 * P[2].z) / 6.0 + t * ((3.0 * P[0].z - 6.0 * P[1].z + 3.0 * P[2].z) / 6.0 + t * (-P[0].z + 3.0 * P[1].z - 3.0 * P[2].z + P[3].z) / 6.0)) * t + (P[0].z + 4.0 * P[1].z + P[2].z) / 6.0);
            }
            return result;
        }

        private Vector3 BezierGetPoint(double t, int i)
        {
            //Used for getting a point on a Bezier spline
            Vector3 result = Vector3.zero;
            if (points.Length > 0)  result = points[0].position;
            if (i < points.Length - 1)
            {
                t = DMath.Clamp01(t);
                i = Mathf.Clamp(i, 0, points.Length - 2);

                if (closed && i == points.Length - 2) points[points.Length - 1] = points[0];

                Vector3 P0 = points[i].position;
                Vector3 P1 = points[i].type == SplinePoint.Type.Broken ? points[i].tangent2 : points[i].position + (points[i].position - points[i].tangent);
                Vector3 P2 = points[i + 1].tangent;
                Vector3 P3 = points[i + 1].position;
                float ft = (float)t;
                result = Mathf.Pow(1 - ft, 3) * P0 + 3 * Mathf.Pow((1 - ft), 2) * ft * P1 + 3 * (1 - ft) * Mathf.Pow(ft, 2) * P2 + Mathf.Pow(ft, 3) * P3;
            }	
			return result;
		}

        private Vector3 HermiteGetPoint(double t, int i)
        {
			//Used for getting a point on a catmull rom spline
            double t2 = t * t;
            double t3 = t2 * t;
			Vector3 result = new Vector3();
			if(points.Length > 0) result = points[0].position;
			if(closed && i == points.Length-2) points[points.Length-1] = points[0];
			if(i < points.Length){

                if (points.Length > 1)
                {
                    Vector3[] P = GetBCFFPoints(i);
                    result.x = (float)(0.5 * ((2.0 * P[1].x) + (-P[0].x + P[2].x) * t + (2.0 * P[0].x - 5.0 * P[1].x + 4 * P[2].x - P[3].x) * t2 + (-P[0].x + 3.0 * P[1].x - 3.0 * P[2].x + P[3].x) * t3));
                    result.y = (float)(0.5 * ((2.0 * P[1].y) + (-P[0].y + P[2].y) * t + (2.0 * P[0].y - 5.0 * P[1].y + 4 * P[2].y - P[3].y) * t2 + (-P[0].y + 3.0 * P[1].y - 3.0 * P[2].y + P[3].y) * t3));
                    result.z = (float)(0.5 * ((2.0 * P[1].z) + (-P[0].z + P[2].z) * t + (2.0 * P[0].z - 5.0 * P[1].z + 4 * P[2].z - P[3].z) * t2 + (-P[0].z + 3.0 * P[1].z - 3.0 * P[2].z + P[3].z) * t3));
                }
			}
			return result;
		}

        private Vector3[] GetBCFFPoints(int i)
        {
            //Gets an array with the current point, the previous one, the next one and the one after that. Used for Hermite and Bspline
            Vector3[] positions = new Vector3[4];
            if (i > 0) positions[0] = points[i - 1].position;
            else if (closed && points.Length - 2 > i) positions[0] = points[points.Length - 2].position;
            else if (i + 1 < points.Length) positions[0] = points[i].position + (points[i].position - points[i + 1].position); //Extrapolate
            else positions[0] = points[i].position;
            positions[1] = points[i].position;
            if (i + 1 < points.Length) positions[2] = points[i + 1].position;
            else if (closed && (i + 2) - points.Length != i) positions[2] = points[(i + 2) - points.Length].position;
            else positions[2] = positions[1] + (positions[1] - positions[0]); //Extrapolate
            if (i + 2 < points.Length) positions[3] = points[i + 2].position;
            else if (closed && (i + 3) - points.Length != i) positions[3] = points[(i + 3) - points.Length].position;
            else positions[3] = positions[2] + (positions[2] - positions[1]); //Extrapolate
            return positions;
        }
	}


}
