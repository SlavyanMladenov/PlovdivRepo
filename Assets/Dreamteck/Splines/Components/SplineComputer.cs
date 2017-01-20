using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Dreamteck.Splines
{
    //MonoBehaviour wrapper for the spline class. It transforms the spline using the object's transform and provides thread-safe methods for sampling
    [AddComponentMenu("Dreamteck/Splines/Spline Computer")]
    public partial class SplineComputer : MonoBehaviour
    {
        [System.Serializable]
        public class NodeLink
        {
            public Node node = null;
            public int pointIndex = 0;
        }
#if UNITY_EDITOR
        public Color editorPathColor = Color.white;
#endif
        public enum Space { World, Local };
        public SplineComputer.Space space
        {
            get { return _space; }
            set
            {
                if (value != _space) Rebuild();
                _space = value;
            }
        }
        public Spline.Type type
        {
            get
            {
                return spline.type;
            }

            set
            {
                if (value != spline.type)
                {
                    spline.type = value;
                    Rebuild();
                }
                else spline.type = value;
            }
        }

        public double precision
        {
            get { return spline.precision; }
            set
            {
                if (value != spline.precision)
                {
                    if (value >= 1f) value = 0.99999;
                    spline.precision = value;
                    Rebuild();
                }
                else
                {
                    spline.precision = value;
                }
            }
        }

        public AnimationCurve customValueInterpolation
        {
            get { return spline.customValueInterpolation; }
            set
            {
                spline.customValueInterpolation = value;
                Rebuild();
            }
        }

        public AnimationCurve customNormalInterpolation
        {
            get { return spline.customNormalInterpolation; }
            set
            {
                spline.customNormalInterpolation = value;
                Rebuild();
            }
        }

        public int iterations
        {
            get
            {
                return spline.iterations;
            }
        }

        public double moveStep
        {
            get
            {
                return spline.moveStep;
            }
        }

        public bool isClosed
        {
            get
            {
                return spline.isClosed;
            }
        }

        public int pointCount
        {
            get
            {
                return spline.points.Length;
            }
        }

        public Morph morph
        {
            get
            {
                if (_morph == null) _morph = new Morph(this);
                else if (!_morph.initialized) _morph = new Morph(this);
                return _morph;
            }
        }

        public NodeLink[] nodeLinks
        {
            get { return _nodeLinks; }
        }

        public bool hasMorph
        {
            get
            {
                if (_morph == null) return false;
                return _morph.GetChannelCount() > 0;
            }
        }

        /// <summary>
        /// Thread-safe transform's position
        /// </summary>
        public Vector3 position
        {
            get {
                if (tsTransform == null) return this.transform.position;
                return tsTransform.position; }
               }
        /// <summary>
        /// Thread-safe transform's rotation
        /// </summary>
        public Quaternion rotation
        {
            get {
                if (tsTransform == null) return this.transform.rotation;
                return tsTransform.rotation; 
            }
        }
        /// <summary>
        /// Thread-safe transform's scale
        /// </summary>
        public Vector3 scale
        {
            get {
                if (tsTransform == null) return this.transform.localScale;
                return tsTransform.scale; 
            }
        }

        [SerializeField]
        private Spline spline = new Spline(Spline.Type.Hermite);
        [SerializeField]
        private Morph _morph = null;
        [SerializeField]
        private SplineComputer.Space _space = SplineComputer.Space.Local;
        [SerializeField]
        private SplineUser[] subscribers = new SplineUser[0];
        [SerializeField]
        private NodeLink[] _nodeLinks = new NodeLink[0];
        private bool rebuildPending = false;

        private TS_Transform tsTransform = null;

        private bool updateRebuild = false;
        private bool lateUpdateRebuild = false;
        private SplineUser.UpdateMethod method = SplineUser.UpdateMethod.Update;


        void Awake()
        {
            tsTransform = new TS_Transform(this.transform);
        }

        void LateUpdate()
        {
            method = SplineUser.UpdateMethod.LateUpdate;
            Run();
            if (lateUpdateRebuild) RebuildOnUpdate();
            lateUpdateRebuild = false;
        }

        void Update()
        {
            method = SplineUser.UpdateMethod.Update;
            Run();
            if (updateRebuild) RebuildOnUpdate();
            updateRebuild = false;
        }

        private void Run()
        {
            if (tsTransform.HasChange())
            {
                updateRebuild = true;
                lateUpdateRebuild = true;
                if (_nodeLinks.Length > 0) UpdateConnectedNodes(GetPoints());
                tsTransform.Update();
            }
        }

        void OnEnable()
        {
            if (rebuildPending)
            {
                rebuildPending = false;
                Rebuild();
            }
        }


        /// <summary>
        /// Subscribe a SplineUser to this computer. This will rebuild the user automatically when there are changes.
        /// </summary>
        /// <param name="input">The SplineUser to subscribe</param>
        public void Subscribe(SplineUser input)
        {
            int emptySlot = -1;
            for (int i = 0; i < subscribers.Length; i++)
            {
                if (subscribers[i] == input) return;
                else if (subscribers[i] == null && emptySlot < 0) emptySlot = i;
            }
            if (emptySlot >= 0) subscribers[emptySlot] = input;
            else
            {
                SplineUser[] newSubscribers = new SplineUser[subscribers.Length + 1];
                subscribers.CopyTo(newSubscribers, 0);
                newSubscribers[subscribers.Length] = input;
                subscribers = newSubscribers;
            }
        }

        /// <summary>
        /// Unsubscribe a SplineUser from this computer's updates
        /// </summary>
        /// <param name="input">The SplineUser to unsubscribe</param>
        public void Unsubscribe(SplineUser input)
        {
            int removeSlot = -1;
            for (int i = 0; i < subscribers.Length; i++)
            {
                if (subscribers[i] == input)
                {
                    removeSlot = i;
                    break;
                }
            }
            if (removeSlot < 0) return;
            SplineUser[] newSubscribers = new SplineUser[subscribers.Length - 1];
            int index = subscribers.Length - 1;
            for (int i = 0; i < subscribers.Length; i++)
            {
                if (index == removeSlot) continue;
                else if (i < index) newSubscribers[i] = subscribers[i];
                else newSubscribers[i - 1] = subscribers[i - 1];
            }
            subscribers = newSubscribers;
        }

        /// <summary>
        /// Checks if a user is subscribed to that computer
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsSubscribed(SplineUser user)
        {
            for (int i = 0; i < subscribers.Length; i++)
            {
                if (subscribers[i] == user)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add a link to a Node for a point in this computer. This is called by the Node component.
        /// </summary>
        /// <param name="node">Node input</param>
        /// <param name="pointIndex">Point index to link</param>
        public void AddNodeLink(Node node, int pointIndex)
        {
            if (node == null) return;
            if (pointIndex < 0 || pointIndex >= spline.points.Length) return;
            for (int i = 0; i < nodeLinks.Length; i++)
            {
                if (nodeLinks[i].node == node && nodeLinks[i].pointIndex == pointIndex)
                {
                    Debug.LogError("Junction already exists, cannot add junction " + node.name + " at point " + pointIndex);
                    return;
                }
            }
            if (!node.HasConnection(this, pointIndex))
            {
                Debug.LogError("Junction " + node.name + " does not have a connection for point " + pointIndex + " Call AddConnection on the junction.");
                return;
            }
            NodeLink newLink = new NodeLink();
            newLink.node = node;
            newLink.pointIndex = pointIndex;
            NodeLink[] newLinks = new NodeLink[_nodeLinks.Length + 1];
            _nodeLinks.CopyTo(newLinks, 0);
            newLinks[_nodeLinks.Length] = newLink;
            _nodeLinks = newLinks;
        }

        /// <summary>
        /// Remove a link to a node from a point in this computer. This is called by the Node component.
        /// </summary>
        /// <param name="pointIndex">Point index to remove link from.</param>
        public void RemoveNodeLink(int pointIndex)
        {
            int index = -1;
            for (int i = 0; i < _nodeLinks.Length; i++)
            {
                if (_nodeLinks[i].pointIndex == pointIndex)
                {
                    if (_nodeLinks[i].node != null && _nodeLinks[i].node.HasConnection(this, pointIndex))
                    {
                        Debug.LogError("Unable to remove junction because connection still exists in " + _nodeLinks[i].node.name + ". Call RemoveConnection on the junction.");
                        return;
                    }
                    else
                    {
                        index = i;
                        break;
                    }
                }
            }
            if (index < 0) return;
            RemoveNodeLinkAt(index);
        }

        /// <summary>
        /// Get the points from this computer's spline. All points are transformed in world coordinates.
        /// </summary>
        /// <returns></returns>
        public SplinePoint[] GetPoints(SplineComputer.Space getSpace = SplineComputer.Space.World)
        {
            SplinePoint[] points = new SplinePoint[spline.points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = spline.points[i];
                if (_space == SplineComputer.Space.Local && getSpace == SplineComputer.Space.World)
                {
                    points[i].position =TransformPoint(points[i].position);
                    points[i].tangent = TransformPoint(points[i].tangent);
                    points[i].tangent2 = TransformPoint(points[i].tangent2);
                    points[i].normal = TransformDirection(points[i].normal);
                }
            }
            return points;
        }

        /// <summary>
        /// Get a point from this computer's spline. The point is transformed in world coordinates.
        /// </summary>
        /// <param name="index">Point index</param>
        /// <returns></returns>
        public SplinePoint GetPoint(int index, SplineComputer.Space getSpace = SplineComputer.Space.World)
        {
            if (index < 0 || index >= spline.points.Length) return new SplinePoint();
            if (_space == SplineComputer.Space.Local && getSpace == SplineComputer.Space.World)
            {
                SplinePoint point = spline.points[index];
                point.position = TransformPoint(point.position);
                point.tangent = TransformPoint(point.tangent);
                point.tangent2 = TransformPoint(point.tangent2);
                point.normal = TransformDirection(point.normal);
                return point;
            } else return spline.points[index];
        }

        /// <summary>
        /// Set the points of this computer's spline. All points must be in world coordinates.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="updateNodes">Should it also update any linked nodes?</param>
        public void SetPoints(SplinePoint[] points, SplineComputer.Space setSpace = SplineComputer.Space.World)
        {
            bool rebuild = false;
            if (points.Length != spline.points.Length)
            {
                rebuild = true;
                spline.points = new SplinePoint[points.Length];
            }
            SplinePoint newPoint;
            for (int i = 0; i < points.Length; i++)
            {
                newPoint = points[i];
                if (_space == SplineComputer.Space.Local && setSpace == SplineComputer.Space.World)
                {
                    newPoint.position = InverseTransformPoint(points[i].position);
                    newPoint.tangent = InverseTransformPoint(points[i].tangent);
                    newPoint.tangent2 = InverseTransformPoint(points[i].tangent2);
                    newPoint.normal = InverseTransformDirection(points[i].normal);
                }
                if (!rebuild)
                {
                    if (newPoint.position != spline.points[i].position) rebuild = true;
                    else if (newPoint.tangent != spline.points[i].tangent) rebuild = true;
                    else if (newPoint.tangent2 != spline.points[i].tangent2) rebuild = true;
                    else if (newPoint.size != spline.points[i].size) rebuild = true;
                    else if (newPoint.type != spline.points[i].type) rebuild = true;
                    else if (newPoint.color != spline.points[i].color) rebuild = true;
                    else if (newPoint.normal != spline.points[i].normal) rebuild = true;
                }
                spline.points[i] = newPoint;
            }
            if (rebuild)
            {
                Rebuild();
                UpdateConnectedNodes(points);
            }
            
        }


        /// <summary>
        /// Set a point of this computer's spline. The point must be in world coordinates.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="point"></param>
        public void SetPoint(int index, SplinePoint point, bool updateNode = true)
        {
            if (index < 0 || index >= spline.points.Length) return;
            bool rebuild = false;
            SplinePoint newPoint = point;
            if (_space == SplineComputer.Space.Local)
            {
                newPoint.position = InverseTransformPoint(point.position);
                newPoint.tangent = InverseTransformPoint(point.tangent);
                newPoint.tangent2 = InverseTransformPoint(point.tangent2);
                newPoint.normal = InverseTransformDirection(point.normal);
            }
            if (newPoint.position != spline.points[index].position) rebuild = true;
            else if (newPoint.tangent != spline.points[index].tangent) rebuild = true;
            else if (newPoint.tangent2 != spline.points[index].tangent2) rebuild = true;
            else if (newPoint.size != spline.points[index].size) rebuild = true;
            else if (newPoint.type != spline.points[index].type) rebuild = true;
            else if (newPoint.color != spline.points[index].color) rebuild = true;
            else if (newPoint.normal != spline.points[index].normal) rebuild = true;
            spline.points[index] = newPoint;
            if (rebuild)
            {
                Rebuild();
                if(updateNode) SetNodeForPoint(index, point);
            }
        }
        
        /// <summary>
        /// Same as Spline.EvaluatePosition but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="address">Optional address if junctions should be used</param>
        /// <returns></returns>
        public Vector3 EvaluatePosition(double percent, SplineJunctionAddress address = null)
        {
            if (IsLegitAddress(address))
            {
                int computerIndex;
                double evaluatePercent;
                SplineComputer[] computers;
                address.GetEvaluationValues(percent, out computerIndex, out computers, out evaluatePercent);
                return computers[computerIndex].EvaluatePosition(evaluatePercent);
            }
            Vector3 result = spline.EvaluatePosition(percent);
            if (_space == SplineComputer.Space.Local) result = TransformPoint(result);
            return result;
        }

        /// <summary>
        /// Same as Spline.Evaluate but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="address">Optional address if junctions should be used</param>
        /// <returns></returns>
        public SplineResult Evaluate(double percent, SplineJunctionAddress address = null)
        {
            if (IsLegitAddress(address))
            {
                int computerIndex;
                double evaluatePercent;
                SplineComputer[] computers;
                address.GetEvaluationValues(percent, out computerIndex, out computers, out evaluatePercent);
                return computers[computerIndex].Evaluate(evaluatePercent);
            }
            SplineResult result = spline.Evaluate(percent);
            if (_space == SplineComputer.Space.Local) TransformResult(result);
            return result;
        }

        /// <summary>
        /// Same as Spline.Evaluate but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void Evaluate(ref SplineResult[] samples, double from = 0.0, double to = 1.0)
        {
            spline.Evaluate(ref samples, from, to);
            if (_space == SplineComputer.Space.Local)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    TransformResult(samples[i]);
                }
            }
        }

        /// <summary>
        /// Same as Spline.EvaluatePositions but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void EvaluatePositions(Vector3[] positions, double from = 0.0, double to = 1.0)
        {
            spline.EvaluatePositions(positions, from, to);
            if (_space == SplineComputer.Space.Local)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = TransformPoint(positions[i]);
                }
            }
        }


        private void TransformResult(SplineResult result)
        {
            result.position = TransformPoint(result.position);
            result.direction = TransformDirection(result.direction);
            result.normal = TransformDirection(result.normal);
        }

        public void Rebuild()
        {
#if UNITY_EDITOR
            //If it's the editor and it's not playing, then rebuild immediate
            if (Application.isPlaying)
            {
                updateRebuild = true;
                lateUpdateRebuild = true;
            }
            else RebuildImmediate();
#else
            updateRebuild = true;
            lateUpdateRebuild = true;
#endif
        }

        public void RebuildImmediate()
        {
            if(Application.isPlaying) tsTransform.Update();
            for (int i = 0; i < subscribers.Length; i++)
            {
                if (subscribers[i] != null)
                {
                    subscribers[i].Rebuild(true);
                }
            }
        }

        private void RebuildOnUpdate()
        {
            for (int i = 0; i < subscribers.Length; i++)
            {
                RebuildUser(i);
            }
        }


        /// <summary>
        /// Rebuilds the users of connected via nodes computers
        /// </summary>
        public void RebuildConnectedUsers()
        {

        }

        private void RebuildUser(int index)
        {
            if (index >= subscribers.Length) return;
            if (subscribers[index] == null) return;
            if (method == subscribers[index].updateMethod) subscribers[index].Rebuild(true);
            else if (method == SplineUser.UpdateMethod.Update && subscribers[index].updateMethod == SplineUser.UpdateMethod.FixedUpdate) subscribers[index].Rebuild(true);
        }

        /// <summary>
        /// Same as Spline.Project but the point is transformed by the computer's transform.
        /// </summary>
        /// <param name="point">Point in space</param>
        /// <param name="subdivide">Subdivisions default: 4</param>
        /// <param name="from">Sample from [0-1] default: 0f</param>
        /// <param name="to">Sample to [0-1] default: 1f</param>
        /// <returns></returns>
        public double Project(Vector3 point, int subdivide = 4, double from = 0.0, double to = 1.0)
        {
            if (_space == SplineComputer.Space.Local) point = InverseTransformPoint(point);
            return spline.Project(point, subdivide, from, to);
        }

        /// <summary>
        /// Project a point along the path defined by address
        /// </summary>
        /// <param name="point">Point in space</param>
        /// <param name="address">Node address of junctions</param>
        /// <param name="subdivide">Subdivisions default: 4</param>
        /// <param name="from">Sample from [0-1] default: 0f</param>
        /// <param name="to">Sample to [0-1] default: 1f</param>
        /// <returns></returns>
        public double Project(Vector3 point, SplineJunctionAddress address, int subdivide = 4, double from = 0.0, double to = 1.0)
        {
            if (address == null) return 0.0;
            if (address.depth == 0) return Project(point, subdivide, from, to);
            SplineComputer[] computers = address.GetComputers();
            int minIndex = 0;
            double minDistance = 0.0;
            double minPercent = 0.0;
            int minStart = 0;
            int minEnd = 0;
            for(int i = 0; i < computers.Length; i++)
            {
                int start, end;
                address.GetConnectionRange(computers, i, out start, out end);
                double startPercent = (double)start / (computers[i].pointCount - 1);
                double endPercent = (double)end / (computers[i].pointCount - 1);
                double percent = computers[i].Project(point, subdivide, startPercent, endPercent);
                float distance = (computers[i].EvaluatePosition(percent)-point).sqrMagnitude;
                if(i == 0 || minDistance > distance)
                {
                    minDistance = distance;
                    minIndex = i;
                    minStart = start;
                    minEnd = end;
                    minPercent = DMath.InverseLerp(startPercent, endPercent, percent);
                }
            }
            minStart = address.LocalToPathPoint(minStart, computers, minIndex);
            minEnd = address.LocalToPathPoint(minEnd, computers, minIndex);
            int allPoints = address.GetPointCount();
            double minStartPercent = (double)minStart / (allPoints - 1);
            double minEndPercent = (double)minEnd / (allPoints - 1);
            return DMath.Lerp(minStartPercent, minEndPercent, minPercent);
        }

        /// <summary>
        /// Same as Spline.Break() but it will update all subscribed users
        /// </summary>
        public void Break()
        {
            Break(0);
        }

        /// <summary>
        /// Same as Spline.Break(at) but it will update all subscribed users
        /// </summary>
        /// <param name="at"></param>
        public void Break(int at)
        {
            if (spline.isClosed)
            {
                spline.Break(at);
                Rebuild();
            }
        }

        /// <summary>
        /// Same as Spline.Close() but it will update all subscribed users
        /// </summary>
        public void Close()
        {
            if (!spline.isClosed)
            {
                spline.Close();
                Rebuild();
            }
        }

        /// <summary>
        /// Same as Spline.CalculateLength but this takes the computer's transform into account when calculating the length.
        /// </summary>
        /// <param name="from">Calculate from [0-1] default: 0f</param>
        /// <param name="to">Calculate to [0-1] default: 1f</param>
        /// <param name="resolution">Resolution [0-1] default: 1f</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public float CalculateLength(double from = 0.0, double to = 1.0, double resolution = 1.0, SplineJunctionAddress address = null)
        {
            resolution = DMath.Clamp01(resolution);
            if (resolution == 0.0) return 0f;
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            if (to < from) to = from;
            double percent = from;
            Vector3 lastPos = EvaluatePosition(percent, address);
            float sum = 0f;
            double moveStep = GetMoveStepFromAddress(address);
            while (true)
            {
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 pos = EvaluatePosition(percent, address);
                sum += (pos - lastPos).magnitude;
                lastPos = pos;
                if (percent == to) break;
            }
            return sum;
        }

        /// <summary>
        /// Casts a ray along the transformed spline against all scene colliders.
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percent of evaluation where the hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public bool Raycast(out RaycastHit hit, out double hitPercent, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal
#endif
        , SplineJunctionAddress address = null)
        {
            resolution = DMath.Clamp01(resolution);
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent, address);
            hitPercent = 0f;
            double moveStep = GetMoveStepFromAddress(address);
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent, address);
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
        /// Casts a ray along the transformed spline against all scene colliders and returns all hits. Order is not guaranteed.
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percents of evaluation where each hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public bool RaycastAll(out RaycastHit[] hits, out double[] hitPercents, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal
#endif
            , SplineJunctionAddress address = null)
        {
            resolution = DMath.Clamp01(resolution);
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent, address);
            List<RaycastHit> hitList = new List<RaycastHit>();
            List<double> percentList = new List<double>();
            bool hasHit = false;
            double moveStep = GetMoveStepFromAddress(address);
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent, address);
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
        /// Returns an array of junctions available from the current position towards the end of the spline.
        /// </summary>
        /// <param name="percent">Current position</param>
        /// <param name="direction">Direction (forward or backward)</param>
        /// <param name="limitToOneJunction">Should it only return one junction's address element?</param>
        /// <returns></returns>
        public SplineJunctionAddress.Element[] GetAvailableJunctionsAtPosition(double percent, Spline.Direction direction, bool limitToOneJunction = false)
        {
            List<SplineJunctionAddress.Element> nodes = new List<SplineJunctionAddress.Element>();
            int pointIndex = DMath.FloorInt(percent * (spline.points.Length - 1));
            for (int i = 0; i < _nodeLinks.Length; i++)
            {
                if ((_nodeLinks[i].pointIndex > pointIndex && direction == Spline.Direction.Forward) || (_nodeLinks[i].pointIndex < pointIndex && direction == Spline.Direction.Forward))
                {
                    Node.Connection[] connections = _nodeLinks[i].node.GetConnections();
                    for (int n = 0; n < connections.Length; n++)
                    {
                        if (connections[n].computer == this) continue;
                        if (direction == Spline.Direction.Forward)
                        {
                            if (connections[n].pointIndex == connections[n].computer.pointCount - 1) continue;
                        }
                        else if (connections[n].pointIndex == 0) continue;

                        SplineJunctionAddress.Element node = new SplineJunctionAddress.Element();
                        node.junctionIndex = i;
                        node.connectionIndex = n;
                        nodes.Add(node);
                    }
                    if (limitToOneJunction && nodes.Count > 0) break;
                }
            }
            return nodes.ToArray();
        }

        /// <summary>
        /// Set the morph's weight to 1 and all others to 0
        /// </summary>
        /// <param name="index">The index of the morph which should be set to 1</param>
        public void SetMorphState(int index)
        {
            if (!hasMorph) return;
            for (int i = 0; i < _morph.GetChannelCount(); i++)
            {
                if (i != index) _morph.SetWeight(i, 0f);
                else _morph.SetWeight(i, 1f);
            }
        }

        /// <summary>
        /// Set the morph's weight to 1 and all others to 0
        /// </summary>
        /// <param name="morphName">The name of the morph which should be set to 1</param>
        public void SetMorphState(string morphName)
        {
            if (!hasMorph) return;
            string[] morphNames = _morph.GetChannelNames();
            for (int i = 0; i < morphNames.Length; i++)
            {
                if (morphNames[i] == morphName)
                {
                    SetMorphState(i);
                    return;
                }
            }
            Debug.LogError("Morph state " + morphName + " not found");
        }

        /// <summary>
        /// Set the morph's weight and reduce the weights of all other morphs automatically
        /// </summary>
        /// <param name="index">The index of the morph</param>
        /// <param name="percent">percent (0-1) to set the morph's weight to</param>
        public void SetMorphState(int index, float percent)
        {
            if (!hasMorph) return;
            percent = Mathf.Clamp01(percent);
            float inversePercent = 1f - percent;
            for (int i = 0; i < _morph.GetChannelCount(); i++)
            {
                if (i != index)
                {
                    float weight = _morph.GetWeight(i);
                    weight = Mathf.Clamp(weight, 0f, inversePercent);
                    _morph.SetWeight(i, weight);
                }
                else _morph.SetWeight(i, percent);
            }
        }

        /// <summary>
        /// Set the morph's weight and reduce the weights of all other morphs automatically
        /// </summary>
        /// <param name="morphName">The name of the morph which should be set to 1</param>
        /// <param name="percent">percent (0-1) to set the morph's weight to</param>
        public void SetMorphState(string morphName, float percent)
        {
            if (!hasMorph) return;
            string[] morphNames = _morph.GetChannelNames();
            for (int i = 0; i < morphNames.Length; i++)
            {
                if (morphNames[i] == morphName)
                {
                    SetMorphState(i, percent);
                    return;
                }
            }
            Debug.LogError("Morph state " + morphName + " not found");
        }

        /// <summary>
        /// Automatically blends through all morphs based on the given percent
        /// </summary>
        /// <param name="percent">Percent for blending</param>
        public void SetMorphState(float percent)
        {
            if (!hasMorph) return;
            int morphCount = _morph.GetChannelCount();
            float morphValue = percent * (morphCount - 1);
            for (int i = 0; i < morphCount; i++)
            {
                float delta = Mathf.Abs(i - morphValue);
                if (delta > 1f) _morph.SetWeight(i, 0f);
                else
                {
                    if (morphValue <= i) _morph.SetWeight(i, 1f - (i - morphValue));
                    else _morph.SetWeight(i, 1f - (morphValue - i));
                }
            }
        }

        /// <summary>
        /// Returns a list of all connected computers. This includes the base computer too.
        /// </summary>
        /// <returns></returns>
        public List<SplineComputer> GetConnectedComputers()
        {
            List<SplineComputer> computers = new List<SplineComputer>();
            computers.Add(this);
            GetConnectedComputers(ref computers);
            return computers;
        }

        private void GetConnectedComputers(ref List<SplineComputer> computers)
        {
            SplineComputer comp = computers[computers.Count - 1];
            for (int i = 0; i < comp._nodeLinks.Length; i++)
            {
                if (comp._nodeLinks[i].node == null) continue;
                Node.Connection[] connections = comp._nodeLinks[i].node.GetConnections();
                for (int n = 0; n < connections.Length; n++)
                {
                    bool found = false;
                    if (connections[n].computer == this) continue;
                    for (int x = 0; x < computers.Count; x++)
                    {
                        if (computers[x] == connections[n].computer)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        computers.Add(connections[n].computer);
                        GetConnectedComputers(ref computers);
                    }
                }
            }
        }

        private void RemoveNodeLinkAt(int index)
        {
            NodeLink[] newLinks = new NodeLink[_nodeLinks.Length - 1];
            for (int i = 0; i < _nodeLinks.Length; i++)
            {
                if (i == index) continue;
                else if (i < index) newLinks[i] = _nodeLinks[i];
                else newLinks[i - 1] = _nodeLinks[i];
            }
            _nodeLinks = newLinks;
            //Remove the address from the subscribers
            for (int i = 0; i < subscribers.Length; i++)
            {
                for (int n = 0; n < subscribers[i].address.depth; n++)
                {
                    if (subscribers[i].address.elements[n].junctionIndex == index)
                    {
                        subscribers[i].ExitAddress(subscribers[i].address.depth - n);
                        break;
                    }
                }
            }
        }

        private double GetMoveStepFromAddress(SplineJunctionAddress address)
        {
            if (!IsLegitAddress(address)) return spline.moveStep;
            int allPoints = address.GetPointCount();
            int iterations = 0;
            if (spline.type == Spline.Type.Linear) iterations = allPoints;
            else iterations = DMath.CeilInt(1.0 / ((1.0 - precision) / (allPoints - 1))) + 1;
            return 1f / (iterations - 1);
        }

        private bool IsLegitAddress(SplineJunctionAddress address)
        {
            return address != null && address.depth > 0 && address.root == this;
        }

        private void HandleNodeLinks()
        {
            for(int i = _nodeLinks.Length-1; i >= 0; i--)
            {
                if(_nodeLinks[i].node == null)
                {
                    RemoveNodeLinkAt(i);
                }
            }
        }

        //This magically updates the Node's position and all other points, connected to it when a point, linked to a Node is edited.
        private void SetNodeForPoint(int index, SplinePoint worldPoint)
        {
            for (int i = 0; i < _nodeLinks.Length; i++)
            {
                if (_nodeLinks[i].pointIndex == index)
                {
                    _nodeLinks[i].node.UpdatePoint(this, _nodeLinks[i].pointIndex, worldPoint);
                    break;
                }
            }
        }

        private void UpdateConnectedNodes(SplinePoint[] worldPoints)
        {
            for (int i = 0; i < _nodeLinks.Length; i++)
            {
                if (_nodeLinks[i].node == null)
                {
                    HandleNodeLinks();
                    Rebuild();
                    break;
                }
                _nodeLinks[i].node.UpdatePoint(this, _nodeLinks[i].pointIndex, worldPoints[_nodeLinks[i].pointIndex]);
                _nodeLinks[i].node.UpdateConnectedComputers(this);
            }
        }

        private Vector3 TransformPoint(Vector3 point)
        {
            if (tsTransform != null && tsTransform.transform != null) return tsTransform.TransformPoint(point);
            else return this.transform.TransformPoint(point);
        }

        private Vector3 InverseTransformPoint(Vector3 point)
        {
            if (tsTransform != null && tsTransform.transform != null) return tsTransform.InverseTransformPoint(point);
            else return this.transform.InverseTransformPoint(point);
        }

        private Vector3 TransformDirection(Vector3 direction)
        {
            if (tsTransform != null && tsTransform.transform != null) return tsTransform.TransformDirection(direction);
            else return this.transform.TransformDirection(direction);
        }

        private Vector3 InverseTransformDirection(Vector3 direction)
        {
            if (tsTransform != null && tsTransform.transform != null) return tsTransform.InverseTransformDirection(direction);
            else return this.transform.InverseTransformDirection(direction);
        }
    }
}