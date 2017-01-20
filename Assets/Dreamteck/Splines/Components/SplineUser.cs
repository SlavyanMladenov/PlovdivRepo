using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dreamteck.Splines {
    //SplineUser samples SplineComputer and supports multithreading.
    public class SplineUser : MonoBehaviour {
        public enum UpdateMethod { Update, FixedUpdate, LateUpdate }
        [HideInInspector]
        public UpdateMethod updateMethod = UpdateMethod.Update;

        public SplineComputer computer
        {
            get {
                return _computer;
            }
            set
            {
                if (value != _computer)
                {
                    if (_computer != null) _computer.Unsubscribe(this);
                    if (value != null) value.Subscribe(this);
                    _address = ScriptableObject.CreateInstance<SplineJunctionAddress>();
                    _address.Init(value);
                    _computer = value;
                    if(_computer != null) RebuildImmediate(true);
                }
            }
        }

        public double resolution
        {
            get
            {
                return _resolution;
            }
            set
            {
                if (value != _resolution)
                {
                    animResolution = (float)_resolution;
                    _resolution = value;
                    Rebuild(true);
                }
            }
        }

        public double clipFrom
        {
            get
            {
               return _clipFrom; 
            }
            set
            {
                if (value != _clipFrom)
                {
                    animClipFrom = (float)_clipFrom;
                    _clipFrom = DMath.Clamp01(value);
                    if (_clipFrom > _clipTo) _clipTo = _clipFrom;
                    getClippedSamples = true;
                    Rebuild(false);
                }
                else _clipFrom = value;
            }
        }

        public double clipTo
        {
            get
            {
                return _clipTo;
            }
            set
            {
                if (value != _clipTo)
                {
                    animClipTo = (float)_clipTo;
                    _clipTo = DMath.Clamp01(value);
                    if (_clipTo < _clipFrom) _clipFrom = _clipTo;
                    getClippedSamples = true;
                    Rebuild(false);
                } else _clipTo = value;
            }
        }


        public bool averageResultVectors
        {
            get
            {
                return _averageResultVectors; 
            }
            set
            {
                if (value != _averageResultVectors)
                {
                    _averageResultVectors = value;
                    Rebuild(true);
                }
            }
        }

        //The percent of the spline that we're traversing
        public double span
        {
            get
            {
                return _clipTo - _clipFrom;
            }
        }

        public SplineJunctionAddress address
        {
            get
            {
                if (_address == null)
                {
                    _address = ScriptableObject.CreateInstance<SplineJunctionAddress>();
                    _address.Init(_computer);
                }
                else _address.root = _computer;
                return _address;
            }
        }

        //Serialized values
        [SerializeField]
        [HideInInspector]
        protected SplineJunctionAddress _address = null;
        [SerializeField]
        [HideInInspector]
        private SplineComputer _computer;
        [SerializeField]
        [HideInInspector]
        private double _resolution = 1f;
        [SerializeField]
        [HideInInspector]
        private double _clipTo = 1f;
        [SerializeField]
        [HideInInspector]
        private double _clipFrom = 0f;
        [SerializeField]
        [HideInInspector]
        private bool _averageResultVectors = true;
        [SerializeField]
        [HideInInspector]
        protected SplineResult[] samples = new SplineResult[0];
        [SerializeField]
        [HideInInspector]
        protected SplineResult[] clippedSamples = new SplineResult[0];

        //float values used for making animations
        [SerializeField]
        [HideInInspector]
        private float animClipFrom = 0f;
        [SerializeField]
        [HideInInspector]
        private float animClipTo = 1f;
        [SerializeField]
        [HideInInspector]
        private double animResolution = 1.0;


        private bool rebuild = false;
        private bool sample = false;
        private volatile bool getClippedSamples = false;

        //Threading values
        [HideInInspector]
        public volatile bool multithreaded = false;
        private Thread buildThread = null;
        private volatile bool postThread = false;
        private volatile bool threadRebuild = false;
        private volatile bool threadSample = false;
        private volatile bool threadWork = false;
        protected object locker = new object();

#if UNITY_EDITOR
        /// <summary>
        /// USE THIS ONLY IN A COMPILER DIRECTIVE REQUIRING UNITY_EDITOR!!!
        /// </summary>
        protected bool isPlaying = false;
#endif


#if UNITY_EDITOR
        /// <summary>
        /// Used by the custom editor. DO NO CALL THIS METHOD IN YOUR GAME CODE
        /// </summary>
        public virtual void EditorAwake()
        {
            //Create a new instance of the address. Otherwise it would be a reference
            SplineJunctionAddress previous = _address;
            if (previous != null)
            {
                 _address = ScriptableObject.CreateInstance<SplineJunctionAddress>();
                 _address.Init(_computer);
                for (int i = 0; i < previous.elements.Length; i++)
                {
                    _address.Enter(previous.elements[i].junctionIndex, previous.elements[i].connectionIndex);
                }
            }
            if (computer != null) RebuildImmediate(true);
            else RebuildImmediate(false);
        }
#endif

        protected virtual void Awake() {
#if UNITY_EDITOR
            isPlaying = true;
#endif
            if (computer == null) computer = GetComponent<SplineComputer>();
        }

        protected virtual void Reset()
        {
            Awake();
        }

        protected virtual void OnEnable()
        {
            if (computer != null)  _computer.Subscribe(this);
        }

        protected virtual void OnDisable()
        {
            if (computer != null) _computer.Unsubscribe(this);
            threadWork = false;
        }

        protected virtual void OnDestroy()
        {
            if (computer != null) _computer.Unsubscribe(this);
            threadWork = false;
        }

        protected virtual void OnApplicationQuit()
        {
            threadWork = false;
        }

        void OnDidApplyAnimationProperties()
        {
            bool clip = false;
            if (_clipFrom != animClipFrom || _clipTo != animClipTo) clip = true;
            bool resample = false;
            if (_resolution != animResolution) resample = true;
            _clipFrom = animClipFrom;
            _clipTo = animClipTo;
            _resolution = animResolution;
            Rebuild(resample);
            if (!resample && clip) GetClippedSamples();
        }

        /// <summary>
        /// Rebuild the SplineUser. This will cause Build and Build_MT to be called.
        /// </summary>
        /// <param name="sampleComputer">Should the SplineUser sample the SplineComputer</param>
        public void Rebuild(bool sampleComputer)
        {
#if UNITY_EDITOR
            //If it's the editor and it's not playing, then rebuild immediate
            if (Application.isPlaying)
            {
                rebuild = true;
                if (sampleComputer) sample = true;
            } else RebuildImmediate(sampleComputer);
#else
            rebuild = true;
             if (sampleComputer) sample = true;
#endif
        }

        /// <summary>
        /// Rebuild the SplineUser immediate. This method will call sample samples and call Build as soon as it's called even if the component is disabled.
        /// </summary>
        /// <param name="sampleComputer">Should the SplineUser sample the SplineComputer</param>
        public virtual void RebuildImmediate(bool sampleComputer)
        {
#if UNITY_EDITOR
            if (PrefabUtility.GetPrefabType(this.gameObject) == PrefabType.Prefab) return;
#endif
            if (threadWork)
            {
                if(sampleComputer) threadSample = true;
                threadRebuild = true;
            }
            else
            {
                if (sampleComputer) SampleComputer();
                else if (getClippedSamples) GetClippedSamples();
                Build();
                PostBuild();
            }
            rebuild = false;
            sampleComputer = false;
            getClippedSamples = false;
        }

        /// <summary>
        /// Enter a junction address.
        /// </summary>
        /// <param name="element">The address element to add to the address</param>
        public virtual void EnterAddress(SplineJunctionAddress.Element element)
        {
            int lastDepth = _address.depth;
            _address.Enter(element.junctionIndex, element.connectionIndex);
            if (_address.depth != lastDepth) Rebuild(true);
        }

        /// <summary>
        /// Clear the junction address.
        /// </summary>
        public virtual void ClearAddress()
        {
            int lastDepth = _address.depth;
            _address.Clear();
            if (_address.depth != lastDepth) Rebuild(true);
        }

        /// <summary>
        /// Exit junction address.
        /// </summary>
        /// <param name="depth">How many address elements to exit</param>
        public virtual void ExitAddress(int depth)
        {
            int lastDepth = _address.depth;
            _address.Exit(depth);
            if (_address.depth != lastDepth) Rebuild(true);
        }

        private void Update()
        {
            if (updateMethod == UpdateMethod.Update) RunMain();
        }

        private void LateUpdate()
        {
            if (updateMethod == UpdateMethod.LateUpdate) RunMain();
        }

        private void FixedUpdate()
        {
            if (updateMethod == UpdateMethod.FixedUpdate) RunMain();
        }

        //Update logic for handling threads and rebuilding
        private void RunMain()
        {
            //Handle threading
#if UNITY_EDITOR
            if (multithreaded) threadWork = Application.isPlaying && System.Environment.ProcessorCount > 1;
            else threadWork = threadRebuild = postThread = false;
#else
            if (multithreaded) threadWork = System.Environment.ProcessorCount > 1; //Don't check Application.isplaying if it's not the UnityEditor
            else threadWork = threadRebuild = postThread = false;
#endif
            if (threadWork)
            {
                if (postThread)
                {
                    lock (locker)
                    {
                        PostBuild();
                    }
                    postThread = false;
                }
                if (buildThread == null)
                {
                    buildThread = new Thread(RunThread);
                    buildThread.Start();
                }
                else if (!buildThread.IsAlive)
                {
                    Debug.Log("thread died");
                    buildThread = new Thread(RunThread);
                    buildThread.Start();
                }
            }
            else if (buildThread != null)
            {
                buildThread.Abort();
                buildThread = null;
            }
            if (computer == null) return;
            if (rebuild && computer != null && this.enabled)
            {
                if (threadWork)
                {
                    threadSample = sample;
                    threadRebuild = true;
                }
                else
                {
                    if (sample)
                    {
                        SampleComputer();
                        sample = false;
                    } else if (getClippedSamples) GetClippedSamples();
                    Build();
                    PostBuild();
                }
                rebuild = false;
            }
            Run();
        }

        //Update logic for threads.
        private void RunThread()
        {
            while (threadWork)
            {
                if (threadRebuild)
                {
                    lock (locker)
                    {
                        if (threadSample)
                        {
                            SampleComputer();
                            threadSample = false;
                        } else if (getClippedSamples) GetClippedSamples();
                        Build();
                        threadRebuild = false;
                        postThread = true;
                    }
                }
            }
        }

        /// Code to run every Update/FixedUpdate/LateUpdate
        protected virtual void Run()
        {

        }

        //Used for calculations. Called on the main or the worker thread.
        protected virtual void Build()
        {
        }

        //Called on the Main thread only - used for applying the results from Build
        protected virtual void PostBuild()
        {

        }

        //Sample the computer
        private void SampleComputer()
        {
            if (computer == null)
            {
                Debug.LogError(this.name + " does not have a reference to a SplineComputer. Rebuild failed");
                return;
            }
            double moveStep = computer.moveStep / _resolution;
            int fullIterations = DMath.CeilInt(1.0 / moveStep) + 1;
            double _span = span;
            if (_span != span) fullIterations = DMath.CeilInt(_span / moveStep) + 1;
            samples = new SplineResult[fullIterations];
            int ix = 0;
            double percent = 0.0;
            //Get all samples
            while (true)
            {
                double eval = percent;
                if (computer.isClosed && percent == 1.0) eval = 0.0;
                SplineResult result = computer.Evaluate(eval, address);
                result.percent = eval;
                samples[ix] = result;
                ix++;
                if (ix >= fullIterations || percent == 1.0) break;
                percent = DMath.Move(percent, 1.0, moveStep);
            }
            clippedSamples = new SplineResult[0];
            if (samples.Length == 0) return;
            if (samples.Length > 1)
            {
                if (_averageResultVectors)
                {
                    //Average directions
                    Vector3 lastDir = samples[1].position - samples[0].position;
                    for (int i = 0; i < samples.Length - 1; i++)
                    {
                        Vector3 dir = (samples[i + 1].position - samples[i].position).normalized;
                        samples[i].direction = (lastDir + dir).normalized;
                        samples[i].normal = (samples[i].normal + samples[i + 1].normal).normalized;
                        lastDir = dir;
                    }

                    if (computer.isClosed) samples[samples.Length - 1].direction = samples[0].direction = Vector3.Slerp(samples[0].direction, lastDir, 0.5f);
                    else samples[samples.Length - 1].direction = lastDir;
                }
            }
            if (computer.isClosed && _span == 1f)
            {
                //Handle closed splines
                samples[samples.Length - 1] = new SplineResult(samples[0]);
                samples[samples.Length - 1].percent = clipTo;
            }
            GetClippedSamples();
        }

        /// <summary>
        /// Gets the clipped samples defined by clipFrom and clipTo
        /// </summary>
        private void GetClippedSamples()
        {
            double clipFromValue = _clipFrom * (samples.Length - 1);
            double clipToValue = _clipTo * (samples.Length - 1);
            int clippedIterations = DMath.CeilInt(clipToValue) - DMath.FloorInt(clipFromValue) + 1;
            if (span == 1.0)
            {
                clippedSamples = samples;
                return;
            }
            else clippedSamples = new SplineResult[clippedIterations];
            int clipFromIndex = DMath.FloorInt(clipFromValue);
            int clipToIndex = DMath.CeilInt(clipToValue);
            if (clipFromIndex + 1 < samples.Length) clippedSamples[0] = SplineResult.Lerp(samples[clipFromIndex], samples[clipFromIndex + 1], clipFromValue - clipFromIndex);
            for (int i = 1; i < clippedSamples.Length - 1; i++)
            {
                clippedSamples[i] = samples[clipFromIndex + i];
            }
            if (clipToIndex - 1 >= 0) clippedSamples[clippedSamples.Length - 1] = SplineResult.Lerp(samples[clipToIndex], samples[clipToIndex - 1], clipToIndex - clipToValue);
            getClippedSamples = false;
        }

        /// <summary>
        /// Evaluate the sampled samples
        /// </summary>
        /// <param name="percent">Percent [0-1] of evaulation</param>
        /// <returns></returns>
        public SplineResult Evaluate(double percent)
        {
            if (samples.Length == 0) return null;
            percent = DMath.Clamp01(percent);
            int index = GetSampleIndex(percent);
            double percentExcess = (samples.Length - 1) * percent - index;
            if (percentExcess > 0.0 && index < samples.Length - 1) return SplineResult.Lerp(samples[index], samples[index + 1], percentExcess);
            else return new SplineResult(samples[index]);
        }

        /// <summary>
        /// Evaluate the sampled samples and pass the result to an existing result reference
        /// </summary>
        /// <param name="percent">Percent [0-1] of evaulation</param>
        /// <returns></returns>
        public void Evaluate(double percent, ref SplineResult result)
        {
            if (samples.Length == 0) return;
            percent = DMath.Clamp01(percent);
            int index = GetSampleIndex(percent);
            double percentExcess = (samples.Length - 1) * percent - index;
            if (result == null) result = new SplineResult(samples[index]);
            else  result.Absorb(samples[index]);
            if (percentExcess > 0.0 && index < samples.Length - 1) result.Lerp(samples[index + 1], percentExcess);
        }

        /// <summary>
        /// Get the index of the sampled result at percent
        /// </summary>
        /// <param name="percent">Percent [0-1] of evaulation</param>
        /// <returns></returns>
        public int GetSampleIndex(double percent)
        {
            return DMath.FloorInt(percent * (samples.Length - 1));
        }

        /// <summary>
        /// Project a point onto the sampled SplineComputer
        /// </summary>
        /// <param name="point">Point in space</param>
        /// <param name="from">Start check from</param>
        /// <param name="to">End check at</param>
        /// <returns></returns>
        public SplineResult Project(Vector3 point, double from = 0.0, double to = 1.0)
        {
            if (samples.Length == 0) return new SplineResult();
            if (computer == null) return new SplineResult();
            //First make a very rough sample of the from-to region 
            int steps = (computer.pointCount - 1) * 6; //Sampling six points per segment is enough to find the closest point range
            int step = samples.Length / steps;
            if (step < 1) step = 1;
            float minDist = (point - samples[0].position).sqrMagnitude;
            int fromIndex = 0;
            int toIndex = samples.Length - 1;
            if (from != 0.0) fromIndex = GetSampleIndex(from);
            if (to != 1.0) toIndex = Mathf.CeilToInt((float)to * (samples.Length - 1));
            int checkFrom = fromIndex;
            int checkTo = toIndex;

            //Find the closest point range which will be checked in detail later
            for (int i = fromIndex; i <= toIndex; i += step)
            {
                if (i > toIndex) i = toIndex;
                float dist = (point - samples[i].position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    checkFrom = Mathf.Max(i - step, 0);
                    checkTo = Mathf.Min(i + step, samples.Length - 1);
                }
                if (i == toIndex) break;
            }
            minDist = (point - samples[checkFrom].position).sqrMagnitude;

            int index = checkFrom;
            //Find the closest result within the range
            for (int i = checkFrom + 1; i <= checkTo; i++)
            {
                float dist = (point - samples[i].position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    index = i;
                }
            }
            //Project the point on the line between the two closest samples
            int backIndex = index - 1;
            if (backIndex < 0) backIndex = 0;
            int frontIndex = index + 1;
            if (frontIndex > samples.Length - 1) frontIndex = samples.Length - 1;
            Vector3 back = Dreamteck.Utils.ProjectOnLine(samples[backIndex].position, samples[index].position, point);
            Vector3 front = Dreamteck.Utils.ProjectOnLine(samples[index].position, samples[frontIndex].position, point);
            float backLength = (samples[index].position - samples[backIndex].position).magnitude;
            float frontLength = (samples[index].position - samples[frontIndex].position).magnitude;
            float backProjectDist = (back - samples[backIndex].position).magnitude;
            float frontProjectDist = (front - samples[frontIndex].position).magnitude;
            if (backIndex < index && index < frontIndex)
            {
                if ((point - back).sqrMagnitude < (point - front).sqrMagnitude)  return SplineResult.Lerp(samples[backIndex], samples[index], backProjectDist / backLength);
                else return SplineResult.Lerp(samples[frontIndex], samples[index], frontProjectDist / frontLength);
            } else if (backIndex < index)  return SplineResult.Lerp(samples[backIndex], samples[index], backProjectDist / backLength);
            else return SplineResult.Lerp(samples[frontIndex], samples[index], frontProjectDist / frontLength);
        }
    }
}
