using UnityEngine;
using System.Collections;
namespace Dreamteck.Splines
{
    [AddComponentMenu("Dreamteck/Splines/Spline Follower")]
    public class SplineFollower : SplineUser
    {
        public enum Wrap { Default, Loop, PingPong}
        [HideInInspector]
        public Wrap wrapMode = Wrap.Default;

        [Range(0f, 1f)] //Make the range to be in clipFrom-clipTo
        [HideInInspector]
        public double startPercent = 0.0;
        [HideInInspector]
        public bool findStartPoint = true;
        [HideInInspector]
        public bool applyPosition = true;
        [HideInInspector]
        public bool applyRotation = true;
        [HideInInspector]
        public bool applyDirectionRotation = true;
        [HideInInspector]
        public bool applyScale = false;
        [HideInInspector]
        public SplineTrigger[] triggers = new SplineTrigger[0];
        [HideInInspector]
        public Vector3 baseScale = Vector3.one;

        [HideInInspector]
        public bool autoFollow = true;

        public float followSpeed
        {
            get { return _followSpeed; }
            set
            {
                if (_followSpeed != value)
                {
                    if (value < 0f) value = 0f;
                    _followSpeed = value;
                }
            }
        }
        [SerializeField]
        [HideInInspector]
        private float _followSpeed = 1f;

        //Remove that, use the percent in followresult
        private SplineResult _followResult = new SplineResult();
        [HideInInspector]
        public Spline.Direction direction = Spline.Direction.Forward;
        [HideInInspector]
        public Vector2 offset;

        private bool white = false;

        public SplineResult followResult
        {
            get { return _followResult; }
        }
        

        // Use this for initialization
        void Start()
        {
            if (computer != null)
            {
                Restart();
                if (autoFollow) ApplyTransformation(this.transform);
            }
        }

        protected override void Run()
        {
            base.Run();
            if (computer == null) return;
            if (autoFollow) AutoFollow();
        }

        void AutoFollow()
        {
            Move(Time.deltaTime * _followSpeed);
        }

        private void ApplyTransformation(Transform input)
        {
            if (_followResult == null) return;
            if (applyPosition) input.position = _followResult.position;
            if (applyRotation) input.rotation = Quaternion.LookRotation(applyDirectionRotation ? _followResult.direction * (direction == Spline.Direction.Forward ? 1f : -1f) : _followResult.direction, _followResult.normal);
            if (applyScale) input.localScale = baseScale * _followResult.size;
        }

        public SplineJunctionAddress.Element[] GetAvailableJunctions()
        {
            return computer.GetAvailableJunctionsAtPosition(_followResult.percent, direction, true);
        }


        public override void EnterAddress(SplineJunctionAddress.Element node)
        {
            int computerIndex = 0;
            double evaluatePercent = 0f;
            SplineComputer[] computers;
            if (Application.isPlaying) _address.GetEvaluationValues(_followResult.percent, out computerIndex, out computers, out evaluatePercent);
            else _address.GetEvaluationValues(startPercent, out computerIndex, out computers, out evaluatePercent);
            base.EnterAddress(node);
            if (Application.isPlaying) _address.GetEvaluationPercent(computerIndex, evaluatePercent, out _followResult.percent);
            else _address.GetEvaluationPercent(computerIndex, evaluatePercent, out startPercent);
        }

        public override void ExitAddress(int depth)
        {
            int computerIndex = 0;
            double evaluatePercent = 0f;
            SplineComputer[] computers;
            if(Application.isPlaying) _address.GetEvaluationValues(_followResult.percent, out computerIndex, out computers, out evaluatePercent);
            else _address.GetEvaluationValues(startPercent, out computerIndex, out computers, out evaluatePercent);
            base.ExitAddress(depth);
            if (Application.isPlaying) _address.GetEvaluationPercent(computerIndex, evaluatePercent, out _followResult.percent);
            else _address.GetEvaluationPercent(computerIndex, evaluatePercent, out startPercent);
            
        }

        public void Restart()
        {
            if (computer == null) return;
            if (findStartPoint) SetPercent(computer.Project(this.transform.position, _address, 4, clipFrom, clipTo));
            else SetPercent(startPercent);
        }

        public void SetPercent(double percent)
        {
            _followResult = Evaluate(percent);
        }

        public void SetDistance(float distance)
        {
            _followResult = Evaluate(0.0);
            Move(distance);
        }

        private void CheckTriggers(double prevPercent, double curPercent)
        {
            for(int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] == null) continue;
                if(clipFrom <= triggers[i].position && clipTo >= triggers[i].position) triggers[i].Check(prevPercent, curPercent);
            }
        }

        public void Move(float distance)
        {
            if (distance < 0f) distance = 0f;
            _followResult = Evaluate(_followResult.percent);
            SplineResult lastResult = _followResult;
            double tracePercent = _followResult.percent;
            SplineResult traceResult = _followResult;
            double traceTriggersFrom = traceResult.percent;
            float moved = 0f;
            while (moved < distance)
            {
                int resultIndex = GetSampleIndex(traceResult.percent);
                if (direction == Spline.Direction.Forward)
                {
                    if (tracePercent == clipTo)
                    {
                        CheckTriggers((float)traceTriggersFrom, traceResult.percent);
                        if (wrapMode == Wrap.Default)
                        {
                            _followResult = traceResult;
                            break;
                        }
                        if (wrapMode == Wrap.Loop)
                        {
                            traceResult = Evaluate(clipFrom);
                            traceTriggersFrom = traceResult.percent;
                            resultIndex = GetSampleIndex(clipFrom);
                        }
                        if (wrapMode == Wrap.PingPong)
                        {
                            direction = Spline.Direction.Backward;
                            lastResult = traceResult;
                            traceTriggersFrom = traceResult.percent;
                            continue;
                        }
                    }
                    double nextPercent = (double)(resultIndex + 1) / (samples.Length-1);
                    if (nextPercent <= tracePercent) nextPercent = (double)(resultIndex + 2) / (samples.Length-1);
                    if (nextPercent > clipTo) nextPercent = clipTo;
                    tracePercent = nextPercent;
                }
                else
                {
                    if (tracePercent == clipFrom)
                    {
                        CheckTriggers((float)traceTriggersFrom, traceResult.percent);
                        if (wrapMode == Wrap.Default)
                        {
                            _followResult = traceResult;
                            break;
                        }
                        if (wrapMode == Wrap.Loop)
                        {
                            traceResult = Evaluate(clipTo);
                            traceTriggersFrom = traceResult.percent;
                            resultIndex = GetSampleIndex(clipTo);
                        }
                        if (wrapMode == Wrap.PingPong)
                        {
                            direction = Spline.Direction.Forward;
                            lastResult = traceResult;
                            traceTriggersFrom = traceResult.percent;
                            continue;
                        }
                    }
                    double nextPercent = (double)resultIndex / (samples.Length-1);
                    if (nextPercent >= tracePercent) nextPercent = (double)(resultIndex - 1) / (samples.Length-1);
                    if (nextPercent < clipFrom) nextPercent = clipFrom;
                    tracePercent = nextPercent;
                }
                lastResult = traceResult;
                traceResult = Evaluate(tracePercent);
                float traveled = (traceResult.position - lastResult.position).magnitude;
                moved += traveled;
                if (moved >= distance)
                {
                    float excess = moved - distance;
                    double lerpPercent = 1.0 - excess / traveled;
                    if (direction == Spline.Direction.Backward && !averageResultVectors)
                    {
                        traceResult.direction = samples[Mathf.Max(resultIndex - 1, 0)].direction;
                        float directionLerp = (float)lastResult.percent * (samples.Length-1)-resultIndex;
                        lastResult.direction = Vector3.Slerp(samples[resultIndex].direction, traceResult.direction, 1f - directionLerp);
                    }
                    _followResult = SplineResult.Lerp(lastResult, traceResult, lerpPercent);
                    CheckTriggers((float)traceTriggersFrom, _followResult.percent);
                    break;
                }
            }
            if (offset != Vector2.zero) _followResult.position += _followResult.right * offset.x + _followResult.normal * offset.y;
            
            ApplyTransformation(this.transform);
            white = !white;
        }

    }
}
