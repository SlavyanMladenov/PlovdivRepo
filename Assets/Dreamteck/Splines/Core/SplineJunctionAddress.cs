using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Dreamteck.Splines {
    [System.Serializable]
    public class SplineJunctionAddress : ScriptableObject {
        [System.Serializable]
        public struct Element
        {
            public int junctionIndex;
            public int connectionIndex;
        }
        public SplineComputer root = null;
        public Element[] elements
        {
            get { return _elements; }
        }
        [SerializeField]
        private Element[] _elements = new Element[0];
        public int depth
        {
            get {
                if (_elements == null) return 0;
                return _elements.Length; }
        }

        public void Init(SplineComputer comp)
        {
            root = comp;
            _elements = new Element[0];
        }

        public void Enter(int junctionIndex, int connectionIndex)
        {
            SplineComputer computer = GetLastComputer();
            if (computer == null) return;
            if (junctionIndex < 0 || junctionIndex >= computer.nodeLinks.Length) return;
            if (connectionIndex < 0 || connectionIndex >= computer.nodeLinks[junctionIndex].node.GetConnections().Length) return;
            Element newNode = new Element();
            newNode.junctionIndex = junctionIndex;
            newNode.connectionIndex = connectionIndex;
            Element[] newAddress = new Element[_elements.Length + 1];
            _elements.CopyTo(newAddress, 0);
            newAddress[newAddress.Length - 1] = newNode;
            _elements = newAddress;
        }

        public void Clear()
        {
            Exit(_elements.Length);
        }

        public void Exit(int depth)
        {
            if (depth > _elements.Length) depth = _elements.Length;
            Element[] newAddress = new Element[_elements.Length - depth];
            for(int i = 0; i < newAddress.Length; i++)
            {
                newAddress[i] = _elements[i];
            }
            _elements = newAddress;
        }

        public SplineComputer GetLastComputer()
        {
            return GetComputerAt(_elements.Length - 1);
        }
        
        public SplineComputer GetComputerAt(int addressIndex)
        {
            if (root == null) return null;
            SplineComputer current = root;
            int index = 0;
            while (index <= addressIndex)
            {
                current = current.nodeLinks[_elements[index].junctionIndex].node.GetConnections()[_elements[index].connectionIndex].computer;
                index++;
            }
            return current;
        }

        public SplineComputer[] GetComputers()
        {
            SplineComputer[] compList = new SplineComputer[_elements.Length+1];
            if (root == null) return compList;
            SplineComputer current = root;
            compList[0] = current;
            int index = 0;
            while (index < _elements.Length)
            {
                if (current.nodeLinks.Length <= _elements[index].junctionIndex)
                {
                    Clear();
                    current.Rebuild();
                    Debug.LogError("Address is not valid. Clearing.");
                    break;
                }
                if (current.nodeLinks[_elements[index].junctionIndex].node == null)
                {
                    Clear();
                    current.Rebuild();
                    Debug.LogError("Address is not valid. Clearing.");
                    break;
                }
                if (current.nodeLinks[_elements[index].junctionIndex].node.GetConnections().Length <= _elements[index].connectionIndex)
                {
                    Clear();
                    current.Rebuild();
                    Debug.LogError("Address is not valid. Clearing.");
                    break;
                }
                current = current.nodeLinks[_elements[index].junctionIndex].node.GetConnections()[_elements[index].connectionIndex].computer;
                compList[index + 1] = current;
                index++;
            }
            return compList;
        }

        public int GetPointCount(int capIndex = -1) //Will return the number of spline points up until the given computer connection (two connected points are considered as one point)
        {
            if (root == null) return 0;
            if (_elements.Length == 0) return 0;
            int points = root.nodeLinks[elements[0].junctionIndex].pointIndex + 1;
            SplineComputer[] computers = GetComputers();
            points = 0;
            if (capIndex == 0) return 1;
            for(int i = 0; i <= _elements.Length; i++)
            {
                int fromPoint = 0;
                if (i > 0) fromPoint = computers[i - 1].nodeLinks[_elements[i - 1].junctionIndex].node.GetConnections()[_elements[i - 1].connectionIndex].pointIndex;
                int toPoint = computers[i].pointCount - 1;
                if (i == _elements.Length) toPoint = computers[i].pointCount - 1;
                else toPoint = computers[i].nodeLinks[_elements[i].junctionIndex].pointIndex;
                points += toPoint - fromPoint;
                if (capIndex > 0)
                {
                    if (i == capIndex-1) break;
                }
            }
            return points+1;
        }

        public void GetEvaluationValues(double percent, out int computerIndex, out SplineComputer[] computers, out double evaluatePercent)
        {
            computers = GetComputers();
            if (depth == 0)
            {
                computerIndex = 0;
                evaluatePercent = percent;
                return;
            }
            int allPoints = GetPointCount();
            double pointValue = percent * (allPoints - 1);
            int pointIndex = DMath.FloorInt(pointValue);
            computerIndex = 0;
            evaluatePercent = 0f;
            for (int i = 0; i < computers.Length; i++)
            {
                int start, end;
                GetConnectionRange(computers, i, out start, out end);
                int pathStart = LocalToPathPoint(start, computers, i);
                int pathEnd = LocalToPathPoint(end, computers, i);
                //Debug.Log("Checking computer " + i + " " + computers[i].name + "|  Starts at " + start + " ends at " + end + " path Start: " + pathStart + " path end " + pathEnd);
                if (pathStart <= pointIndex && (pointIndex < pathEnd || i == computers.Length-1))
                {
                    double lerpValue = DMath.InverseLerp(pathStart, pathEnd, pointValue);
                    computerIndex = i;
                    double startPercent = (double)start / (computers[i].pointCount - 1);
                    double endPercent = (double)end / (computers[i].pointCount - 1);
                    evaluatePercent = DMath.Lerp(startPercent, endPercent, lerpValue);
                    break;
                }
            }
            //Debug.Log("----- Getting evaluation Values at " + percent + " comp index " + computerIndex + " percent " + evaluatePercent + " all points " + allPoints + " point index: " + pointIndex);
        }

        public void GetEvaluationPercent(int computerIndex, double evaluatePercent, out double percent)
        {
            SplineComputer[] computers = GetComputers();
            if (depth == 0)
            {
                percent = evaluatePercent;
                return;
            }
            if (computerIndex >= computers.Length)
            {
                percent = 1f;
                return;
            }
            int fromPoint, toPoint;
            GetConnectionRange(computers, computerIndex, out fromPoint, out toPoint);
            double fromPercent = (double)fromPoint / (computers[computerIndex].pointCount - 1);
            double toPercent = (double)toPoint / (computers[computerIndex].pointCount - 1);
            double lerpPercent = DMath.InverseLerp(fromPercent, toPercent, evaluatePercent);
            int startPointNumber = GetPointCount(computerIndex);
            int endPointNumber = GetPointCount(computerIndex + 1);
            int allPoints = GetPointCount();
            double startPercent = (double)(startPointNumber - 1) / (allPoints - 1);
            double endPercent = (double)(endPointNumber - 1) / (allPoints - 1);
            percent = DMath.Lerp(startPercent, endPercent, lerpPercent);
        }

       public void GetConnectionRange(SplineComputer[] computers, int currentComputer, out int fromPoint, out int toPoint)
        {
            fromPoint = 0;
            toPoint = computers[computers.Length - 1].pointCount - 1;
            if (currentComputer == 0) toPoint = computers[0].nodeLinks[_elements[0].junctionIndex].pointIndex;
            else if (currentComputer == computers.Length - 1) fromPoint = computers[computers.Length - 2].nodeLinks[_elements[depth - 1].junctionIndex].node.GetConnections()[_elements[depth - 1].connectionIndex].pointIndex;
            else
            {
                fromPoint = computers[currentComputer - 1].nodeLinks[_elements[currentComputer - 1].junctionIndex].node.GetConnections()[_elements[currentComputer - 1].connectionIndex].pointIndex;
                toPoint = computers[currentComputer].nodeLinks[_elements[currentComputer].junctionIndex].pointIndex;
            }
        }

        public int PathToLocalPoint(int pointIndex, SplineComputer[] computers, int computerIndex)
        {
            if (computerIndex == 0)
            {
                if (pointIndex >= computers[computerIndex].pointCount) return computers[computerIndex].pointCount - 1;
                else return pointIndex;
            }
            else
            {
                int passed = 0;
                for (int i = 0; i <= computerIndex; i++)
                {
                    if (i > 0) passed--;
                    int start, end;
                    GetConnectionRange(computers, i, out start, out end);
                    
                    if (i == computerIndex)
                    {
                        int result = pointIndex - passed + start;
                        return result;
                    }
                    else passed += end - start;
                }
                return computers[computerIndex].pointCount - 1;
            }
        }

        public int LocalToPathPoint(int pointIndex, SplineComputer[] computers, int computerIndex)
        {
            if (computerIndex == 0) return pointIndex;
            int passed = 0;
            for (int i = 0; i <= computerIndex; i++)
            {
                int start, end;
                GetConnectionRange(computers, i, out start, out end);
               // Debug.Log("LOCALTOPATHPOINT " + computers[computerIndex].name + ": comp " + i + " passed " + passed);
                if (i == computerIndex)
                {
                   // Debug.Log("PREVIOUSLY PASSED " + passed + " POINT INDEX " + pointIndex);
                    int result = passed + (pointIndex-start);
                    return result;
                }
                else passed += end - start;
            }
            return 0;
        }
    }
}