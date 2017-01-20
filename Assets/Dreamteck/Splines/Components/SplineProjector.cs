using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    [AddComponentMenu("Dreamteck/Splines/Spline Projector")]
    public class SplineProjector : SplineUser
    {
        public enum Mode {Accurate, Cached}
        public Mode mode
        {
            get { return _mode; }
            set
            {
                if(value != _mode)
                {
                    _mode = value;
                    Rebuild(false);
                }
            }
        }

        public bool autoProject
        {
            get { return _autoProject; }
            set
            {
                if(value != _autoProject)
                {
                    _autoProject = value;
                    if (_autoProject) Rebuild(false);
                }
            }
        }

        public int subdivide
        {
            get { return _subdivide; }
            set
            {
                if (value != _subdivide)
                {
                    _subdivide = value;
                    if (_mode == Mode.Accurate) Rebuild(false);
                }
            }
        }

        public Transform projectTarget
        {
            get {
                if (_projectTarget == null) return this.transform;
                return _projectTarget; 
            }
            set
            {
                if (value != _projectTarget)
                {
                    _projectTarget = value;
                    finalTarget = new TS_Transform(_projectTarget);
                    Rebuild(false);
                }
            }
        }

        public Transform target
        {
            get { return applyTarget; }
            set
            {
                if (value != applyTarget)
                {
                    applyTarget = value;
                    Rebuild(false);
                }
            }
        }

        public bool applyPosition
        {
            get { return _applyPosition; }
            set
            {
                if (value != _applyPosition)
                {
                    _applyPosition = value;
                    if (_autoProject && _applyPosition) Rebuild(false);
                }
            }
        }

        public bool applyRotation
        {
            get { return _applyRotation; }
            set
            {
                if (value != _applyRotation)
                {
                    _applyRotation = value;
                    if (_autoProject && _applyRotation) Rebuild(false);
                }
            }
        }

        public bool applyScale
        {
            get { return _applyScale; }
            set
            {
                if (value != _applyScale)
                {
                    _applyScale = value;
                    if (_autoProject && _applyScale) Rebuild(false);
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private Mode _mode = Mode.Accurate;
        [SerializeField]
        [HideInInspector]
        private bool _autoProject = true;
        [SerializeField]
        [HideInInspector]
        private int _subdivide = 4;
        [SerializeField]
        [HideInInspector]
        private Transform _projectTarget;

        [SerializeField]
        [HideInInspector]
        private bool _applyPosition = true;
        [SerializeField]
        [HideInInspector]
        private bool _applyRotation = true;
        [SerializeField]
        [HideInInspector]
        private bool _applyScale = false;

        [SerializeField]
        [HideInInspector]
        private Transform applyTarget = null;

        private Vector3 baseScale = Vector3.one;
        [HideInInspector]
        public SplineTrigger[] triggers = new SplineTrigger[0];

        [HideInInspector]
        public SplineResult projectResult
        {
            get { return result; }
            set { }
        }
        [SerializeField]
        [HideInInspector]
        private SplineResult result = null;
        [SerializeField]
        [HideInInspector]
        private TS_Transform finalTarget;
        double traceFromA = -1.0, traceToA = -1.0, traceFromB = -1.0;

        // Use this for initialization
        protected override void Awake()
        {
            base.Awake();
            GetProjectTarget();
        }

#if UNITY_EDITOR
        public override void EditorAwake()
        {
            base.EditorAwake();
            GetProjectTarget();
        }
#endif

        private void GetProjectTarget()
        {
            if (_projectTarget != null) finalTarget = new TS_Transform(_projectTarget);
            else finalTarget = new TS_Transform(this.transform);
        }

        // Update is called once per frame
        protected override void Run()
        {
            base.Run();
            if (autoProject)
            {
                if (finalTarget == null) GetProjectTarget();
                else if (finalTarget.transform == null) GetProjectTarget();
                if (finalTarget.HasPositionChange())
                {
                    finalTarget.Update();
                    RebuildImmediate(false);
                }
            }
         }

        protected override void Build()
        {
            base.Build();
            InternalCalculateProjection();
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            CheckTriggers();
            ApplyProjection();
        }

        private void CheckTriggers()
        {
            if (traceFromA >= 0f)
            {
                if (clipTo - traceFromA > traceFromB)
                {
                    traceToA = clipTo;
                    traceFromB = clipFrom;
                }
                else
                {
                    traceToA = clipFrom;
                    traceFromB = clipTo;
                }
                if (System.Math.Abs(traceToA - traceFromA) + System.Math.Abs(result.percent - traceFromB) < System.Math.Abs(result.percent - traceFromA))
                {
                    CheckTriggers(traceFromA, traceToA);
                    CheckTriggers(traceFromB, result.percent);
                }
                else CheckTriggers(traceFromA, result.percent);
            }
        }

        private void CheckTriggers(double prevPercent, double curPercent)
        {
            for (int i = 0; i < triggers.Length; i++)
            {
                if (clipFrom <= triggers[i].position && clipTo >= triggers[i].position)
                {
                    triggers[i].Check(prevPercent, curPercent);
                }
            }
        }

        public void CalculateProjection()
        {
            finalTarget.Update();
            Rebuild(false);
        }

        private void InternalCalculateProjection()
        {
            if (mode == Mode.Accurate && computer == null)
            {
                result = new SplineResult();
                return;
            }
            traceFromA = -1.0;
            traceToA = -1.0;
            traceFromB = -1.0;
            if (_mode == Mode.Accurate)
            {
                double percent = computer.Project(finalTarget.position, _address, subdivide, clipFrom, clipTo);
                if (result != null) traceFromA = result.percent;
                result = computer.Evaluate(percent, _address);
            } else result = Project(finalTarget.position);
        }

        private void ApplyProjection()
        {
            if (applyTarget == null) return;
            if (_applyPosition)
            {
                applyTarget.position = result.position;
                if (applyTarget == finalTarget.transform) finalTarget.Update();
            }
            if (_applyRotation) applyTarget.rotation = result.rotation;
            if (_applyScale) applyTarget.localScale = baseScale * result.size;
        }
    }
}
