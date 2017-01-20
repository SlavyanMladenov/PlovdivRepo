using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Dreamteck.Splines{
    [CustomEditor(typeof(Node))]
    public class NodeEditor : Editor {
        private SplineComputer addComp = null;
        private int addPoint = 0;
        private Node lastnode = null;
        private Vector2 scroll;
        private Vector3 position, scale;
        private Quaternion rotation;
        private int[] availablePoints;

        public override void OnInspectorGUI()
        {
            Node node = (Node)target;
            SplineComputer lastComp = addComp;
            addComp = (SplineComputer)EditorGUILayout.ObjectField("Add Computer", addComp, typeof(SplineComputer), true);
            if (lastComp != addComp)
            {
                SceneView.RepaintAll();
                if (addComp != null) availablePoints = GetAvailablePoints(addComp);
            }
            if (addComp != null)
            {
                string[] pointNames = new string[availablePoints.Length];
                for (int i = 0; i < pointNames.Length; i++)
                {
                    pointNames[i] = "Point " + availablePoints[i];
                }
                if (availablePoints.Length > 0) addPoint = EditorGUILayout.Popup("Link point", addPoint, pointNames);
                else EditorGUILayout.LabelField("No Points Available");

                if (GUILayout.Button("Cancel"))
                {
                    addComp = null;
                    addPoint = 0;
                }
                if (addPoint >= 0 && availablePoints.Length > addPoint)
                {
                    if (node.HasConnection(addComp, availablePoints[addPoint])) EditorGUILayout.HelpBox("Connection already exists (" + addComp.name + "," + availablePoints[addPoint], MessageType.Error);
                    else if (GUILayout.Button("Link"))
                    {
                        AddConnection(addComp, availablePoints[addPoint]);
                    }
                }
            }
            else RenderConnections();
        }

        void RenderConnections()
        {
            Node node = (Node)target;
            Node.Connection[] connections = node.GetConnections();
            Rect viewRect = new Rect(0, 0, Screen.width - 60, connections.Length * 25);
            GUILayout.Box("Connections", GUILayout.Width(Screen.width - 30), GUILayout.Height(Mathf.Min(Mathf.Max(viewRect.height, 30), 110) + 30));
            Rect rect = GUILayoutUtility.GetLastRect();
            SplineComputer[] addComps;
            SplineComputer lastComp = addComp;
            bool dragged = SplineEditorGUI.DropArea<SplineComputer>(rect, out addComps);
            if (dragged && addComps.Length > 0) SelectComputer(addComps[0]);
            if (lastComp != addComp) SceneView.RepaintAll();
            rect.x += 5;
            rect.width -= 10;
            rect.height -= 30;
            rect.y += 20;
            if (connections.Length > 0)
            {
                scroll = GUI.BeginScrollView(rect, scroll, viewRect);
                for (int i = 0; i < connections.Length; i++)
                {
                    GUI.Label(new Rect(0, i * 25, viewRect.width * 0.75f, 20), connections[i].computer.name + " at point " + connections[i].pointIndex);
                    if (GUI.Button(new Rect(viewRect.width - 20, i * 25, 20, 20), "x"))
                    {
                        Undo.RecordObject(node, "Remove connection");
                        Undo.RecordObject(connections[i].computer, "Remove node");
                        node.RemoveConnection(connections[i].computer, connections[i].pointIndex);
                    }
                }
                GUI.EndScrollView();
            }
            else EditorGUI.HelpBox(rect, "Drag & Drop SplineComputers here to link their points.", MessageType.Info);

            node.transformNormals = EditorGUILayout.Toggle("Transform Normals", node.transformNormals);
            node.transformSize = EditorGUILayout.Toggle("Transform Size", node.transformSize);
            node.transformTangents = EditorGUILayout.Toggle("Transform Tangents", node.transformTangents);

            if (connections.Length > 1) node.type = (Node.Type)EditorGUILayout.EnumPopup("node type", node.type);
            
        }

        void OnEnable()
        {
            lastnode = ((Node)target);
            lastnode.EditorMaintainConnections();
        }

        void OnDestroy()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                if (((Node)target) == null)
                {
                    Node.Connection[] connections = lastnode.GetConnections();
                    for(int i = 0; i < connections.Length; i++)
                    {
                        Undo.RecordObject(connections[i].computer, "Delete node connections");
                    }
                    lastnode.ClearConnections();
                }
            }
        }

        void SelectComputer(SplineComputer comp)
        {
            addComp = comp;
            if (addComp != null) availablePoints = GetAvailablePoints(addComp);
            SceneView.RepaintAll();
            Repaint();
        }

        void AddConnection(SplineComputer computer, int pointIndex)
        {
            Node node = (Node)target;
            Node.Connection[] connections = node.GetConnections();
            if (EditorUtility.DisplayDialog("Link point?", "Add point " + pointIndex + " to connections?", "Yes", "No"))
            {
                Undo.RecordObject(addComp, "Add connection");
                Undo.RecordObject(node, "Add Connection");
                if (connections.Length == 0)
                {
                    if (EditorUtility.DisplayDialog("Align node to point?", "This is the first connection for the node, would you like to match the node's Transform to Point " + pointIndex + "'s coordinates? Otherwise, the point's position will snap to the node's Transform.", "Yes", "No"))
                    {
                        SplinePoint point = addComp.GetPoint(pointIndex);
                        node.transform.position = point.position;
                    }
                }
                node.AddConnection(computer, pointIndex);
                addComp = null;
                addPoint = 0;
                SceneView.RepaintAll();
                Repaint();
            }
        }

        int[] GetAvailablePoints(SplineComputer computer)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < computer.pointCount; i++)
            {
                bool found = false;
                for (int n = 0; n < computer.nodeLinks.Length; n++)
                {
                    if (computer.nodeLinks[n].pointIndex == i)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) indices.Add(i);
            }
            return indices.ToArray();
        }

        void OnSceneGUI()
        {
            Node node = (Node)target;
            Node.Connection[] connections = node.GetConnections();
            for (int i = 0; i < connections.Length; i++)
            {
                SplineEditor.DrawPath(connections[i].computer, SceneView.currentDrawingSceneView.camera, false, false, 0.5f);
            }
            bool update = false;
            if (position != node.transform.position)
            {
                position = node.transform.position;
                update = true;
            }
            if(scale != node.transform.localScale){
                scale = node.transform.localScale;
                update = true;
            }
            if (rotation != node.transform.rotation)
            {
                rotation = node.transform.rotation;
                update = true;
            }
            if(update) node.UpdateConnectedComputers();

            if (addComp == null) return;
            SplinePoint[] points = addComp.GetPoints();
            Transform camTransform = SceneView.currentDrawingSceneView.camera.transform;
            SplineEditor.DrawPath(addComp, SceneView.currentDrawingSceneView.camera, false, false, 0.5f);
            for (int i = 0; i < availablePoints.Length; i++)
            {
                if (addComp.isClosed && i == points.Length - 1) break;
                Handles.color = addComp.editorPathColor;
                if (Handles.Button(points[availablePoints[i]].position, Quaternion.LookRotation(-camTransform.forward, camTransform.up), HandleUtility.GetHandleSize(points[availablePoints[i]].position) * 0.1f, HandleUtility.GetHandleSize(points[availablePoints[i]].position) * 0.2f, Handles.CircleCap))
                {
                  AddConnection(addComp, availablePoints[i]);
                    break;
                }
            }

        }
    }
}
