using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    public class Node : MonoBehaviour
    {
        [System.Serializable]
        public class Connection
        {
            public SplineComputer computer
            {
                get { return _computer; }
            }

            public int pointIndex
            {
                get { return _pointIndex; }
            }

            [SerializeField]
            private int _pointIndex = 0;
            [SerializeField]
            private SplineComputer _computer = null;
            public SplinePoint point;

            internal bool isValid
            {
                get
                {
                    if (_computer == null) return false;
                    if (_pointIndex >= _computer.pointCount) return false;
                    return true;
                }
            }

            internal Connection(SplineComputer comp, int index, SplinePoint inputPoint)
            {
                _pointIndex = index;
                _computer = comp;
                point = inputPoint;
            }
        }
        public enum Type { Smooth, Free }
        public Type type = Type.Smooth;

        public bool transformNormals
        {
            get { return _transformNormals; }
            set
            {
                if (value != _transformNormals)
                {
                    _transformNormals = value;
                    UpdatePoints();
                }
            }
        }

        public bool transformSize
        {
            get { return _transformSize; }
            set
            {
                if (value != _transformSize)
                {
                    _transformSize = value;
                    UpdatePoints();
                }
            }
        }

        public bool transformTangents
        {
            get { return _transformTangents; }
            set
            {
                if (value != _transformTangents)
                {
                    _transformTangents = value;
                    UpdatePoints();
                }
            }
        }

        [SerializeField]
        private Connection[] connections = new Connection[0];
        [SerializeField]
        private bool _transformSize = true;
        [SerializeField]
        private bool _transformNormals = true;
        [SerializeField]
        private bool _transformTangents = true;

        private TS_Transform tsTransform;

        void Awake()
        {
            tsTransform = new TS_Transform(this.transform);
        }

        void LateUpdate()
        {
            Run();
        }

        void Update()
        {
            Run();
        }

        private void Run()
        {
            if (tsTransform.HasChange())
            {
                UpdateConnectedComputers();
                tsTransform.Update();
            }
        }

        private SplinePoint GetPoint(int connectionIndex)
        {
            SplinePoint result = connections[connectionIndex].point;
            result.position = this.transform.position;
            if (_transformNormals) result.normal = transform.TransformDirection(result.normal);
            if (_transformTangents)
            {
                result.tangent = transform.TransformPoint(result.tangent);
                result.tangent2 = transform.TransformPoint(result.tangent2);
            }
            else
            {
                result.tangent += result.position;
                result.tangent2 += result.position;
            }
            if (_transformSize) result.size *= (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3f;
            return result;
        }

        private void SetPoint(int connectionIndex, SplinePoint worldPoint)
        {
            Connection connection = connections[connectionIndex];
            connection.point = worldPoint;
            connection.point.position = Vector3.zero;
            if (_transformTangents)
            {
                connection.point.tangent = transform.InverseTransformPoint(connection.point.tangent);
                connection.point.tangent2 = transform.InverseTransformPoint(connection.point.tangent2);
            }
            else
            {
                connection.point.tangent -= this.transform.position;
                connection.point.tangent2 -= this.transform.position;
            }
            if (_transformNormals) connection.point.normal = transform.InverseTransformDirection(connection.point.normal);
            if (_transformSize) connection.point.size /= (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3f;
        }

        void OnDestroy()
        {
            ClearConnections();
        }

        public void ClearConnections()
        {
            connections = new Connection[0];
            for (int i = 0; i < connections.Length; i++)
            {
                if(connections[i].computer != null) connections[i].computer.RemoveNodeLink(connections[i].pointIndex);
            }
        }

        public void UpdateConnectedComputers(SplineComputer excludeComputer = null)
        {
            for (int i =  connections.Length-1; i >= 0; i--)
            {
                if (!connections[i].isValid)
                {
                    RemoveConnection(i);
                    continue;
                }
                if (connections[i].computer == excludeComputer) continue;
                if (type == Type.Smooth && i != 0) SetPoint(i, GetPoint(0));
                connections[i].computer.SetPoint(connections[i].pointIndex, GetPoint(i), false);
            }
        }

        public void UpdatePoint(SplineComputer computer, int pointIndex, SplinePoint point)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) transform.position = point.position;
            else
            {
                tsTransform.position = point.position;
                tsTransform.Update();
            }
#else
            tsTransform.position = point.position;
            tsTransform.Update();
#endif
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].computer == computer && connections[i].pointIndex == pointIndex)
                {
                    SetPoint(i, point);
                    break;
                }
            }
        }

        private void UpdatePoints()
        {
            for (int i = connections.Length-1; i >= 0; i--)
            {
                if (!connections[i].isValid)
                {
                    RemoveConnection(i);
                    continue;
                }
                SplinePoint point = connections[i].computer.GetPoint(connections[i].pointIndex);
                point.SetPosition(this.transform.position);
                SetPoint(i, point);
            }
        }

#if UNITY_EDITOR
        //Use this to maintain the connections between computers in the editor
        public void EditorMaintainConnections()
        {
            MaintainConnections();
            for(int i = 0; i < connections.Length; i++)
            {
                bool found = false;
                for(int n = 0; n < connections[i].computer.nodeLinks.Length; n++)
                {
                    if(connections[i].computer.nodeLinks[n].node == this && connections[i].computer.nodeLinks[n].pointIndex == connections[i].pointIndex)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) connections[i].computer.AddNodeLink(this, connections[i].pointIndex);
            }
        }
#endif
        //Remove invalid connections
        private void MaintainConnections()
        {
            for (int i = connections.Length-1; i >= 0; i--)
            {
                if (!connections[i].isValid) RemoveConnection(i);
            }
        }

        public void AddConnection(SplineComputer computer, int pointIndex)
        {
            for (int i = 0; i < computer.nodeLinks.Length; i++)
            {
                if (computer.nodeLinks[i].pointIndex == pointIndex)
                {
                    Debug.LogError("Connection has been already added in " + computer.nodeLinks[i].node);
                    return;
                }
            }
            SplinePoint point = computer.GetPoint(pointIndex);
            point.SetPosition(this.transform.position);
            Connection newConnection = new Connection(computer, pointIndex, point);
            Connection[] newConnections = new Connection[connections.Length + 1];
            connections.CopyTo(newConnections, 0);
            newConnections[connections.Length] = newConnection;
            connections = newConnections;
            computer.AddNodeLink(this, pointIndex);
            UpdateConnectedComputers();
        }

        public void RemoveConnection(SplineComputer computer, int pointIndex)
        {
            int index = -1;
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].computer == computer && connections[i].pointIndex == pointIndex)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                Debug.LogError("Connection not found in " + name);
                return;
            }
            RemoveConnection(index);
        }

        private void RemoveConnection(int index)
        {
            Connection[] newConnections = new Connection[connections.Length - 1];
            SplineComputer computer = connections[index].computer;
            int pointIndex = connections[index].pointIndex;
            for (int i = 0; i < connections.Length; i++)
            {
                if (i < index) newConnections[i] = connections[i];
                else if (i == index) continue;
                else newConnections[i - 1] = connections[i];
            }
            connections = newConnections;
            if (computer != null) computer.RemoveNodeLink(pointIndex);
        }

        public bool HasConnection(SplineComputer computer, int pointIndex)
        {
            for (int i = connections.Length-1; i >= 0; i--)
            {
                if (!connections[i].isValid){
                    RemoveConnection(i);
                    continue;
                }
                if (connections[i].computer == computer && connections[i].pointIndex == pointIndex) return true;
            }
            return false;
        }

        public Connection[] GetConnections()
        {
            return connections;
        }

    }
}
