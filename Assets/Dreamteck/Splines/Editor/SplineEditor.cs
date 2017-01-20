#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Dreamteck.Splines;

namespace Dreamteck.Splines
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SplineComputer))]
    public class SplineEditor : Editor 
    {
        public enum PointTool { None, Create, Delete, Move, Rotate, Scale, NormalEdit };
        public PointTool tool = PointTool.None;
        public int createPointMode = 0;
        public float createPointOffset = 0f;
        public int createNormalMode = 0;
        private bool showThickness = false;

        private List<int> selectedPoints = new List<int>();

        public int[] pointSelection
        {
            get
            {
                return selectedPoints.ToArray();
            }
        }
        public bool mouseLeft = false;
        public bool mouseright = false;
        public bool mouseLeftDown = false;
        public bool mouserightDown = false;
        public bool mouseLeftUp = false;
        public bool mouserightUp = false;
        public bool controlDown = false;
        public bool control = false;
        public bool controlUp = false;

        private Tool lastTool = Tool.None;

        private Camera editorCamera = null;

        private bool holdSelection = false;
        private bool interpolationFoldout = false;

        private Vector2 rectDragStart = Vector2.zero;
        private Vector2 rectDragEnd = Vector2.zero;

        private SplineComputer computer;

        private SplineEditorToolbar toolbar = null;

        public static float addSize = 1f;
        public static Color addColor = Color.white;

        private PointTransformer transformer = null;

        public bool scaleSize = true;
        public bool scaleTangents = true;
        public bool rotateNormal = true;
        public bool rotateTangents = true;

        private SplinePoint[] points = new SplinePoint[0];

        private bool dragSelect = false;
        private bool listenSelect = false;

        private bool emptyClick = false;

        private bool thicknessAutoRotate = false;
        private static Color orange = new Color(1f, 0.564f, 0f);
        private TS_Transform tsTransform;


        private static List<SplineComputer> drawComputers = new List<SplineComputer>();

        MorphWindow morphWindow = null;


        public int selectedPointsCount
        {
            get { return selectedPoints.Count; }
            set { }
        }

        [MenuItem("GameObject/Dreamteck/Spline/Computer")]
        private static void NewEmptySpline()
        {
            int count = GameObject.FindObjectsOfType<SplineComputer>().Length;
            string objName = "Spline";
            if (count > 0) objName += " " + count;
            GameObject obj = new GameObject(objName);
            obj.AddComponent<SplineComputer>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Dreamteck/Spline/Node")]
        private static void NewSplineNode()
        {
            int count = GameObject.FindObjectsOfType<Node>().Length;
            string objName = "Node";
            if (count > 0) objName += " " + count;
            GameObject obj = new GameObject(objName);
            obj.AddComponent<Node>();
            Selection.activeGameObject = obj;
        }


        void OnEnable()
        {
            computer = (SplineComputer)target;
            tsTransform = new TS_Transform(computer.transform);
            tool = PointTool.None;
            lastTool = Tools.current;
            Tools.current = Tool.None;
            toolbar = new SplineEditorToolbar(this, computer);
            ClearSelection();
            SceneView.onSceneGUIDelegate += HandleInput;
            if (EditorPrefs.HasKey("SplineEditor_scaleSize")) scaleSize = EditorPrefs.GetBool("SplineEditor_scaleSize");
            if (EditorPrefs.HasKey("SplineEditor_scaleTangents")) scaleTangents = EditorPrefs.GetBool("SplineEditor_scaleTangents");
            if (EditorPrefs.HasKey("SplineEditor_roateNormal")) rotateNormal = EditorPrefs.GetBool("SplineEditor_roateNormal");
            if (EditorPrefs.HasKey("SplineEditor_rotateTangents")) rotateTangents = EditorPrefs.GetBool("SplineEditor_rotateTangents");

            if (EditorPrefs.HasKey("SplineEditor_createPointMode")) createPointMode = EditorPrefs.GetInt("SplineEditor_createPointMode");
            if (EditorPrefs.HasKey("SplineEditor_createNormalMode")) createNormalMode = EditorPrefs.GetInt("SplineEditor_createNormalMode");
            if (EditorPrefs.HasKey("SplineEditor_setNormalMode")) toolbar.setNormalMode = EditorPrefs.GetInt("SplineEditor_setNormalMode");
            if (EditorPrefs.HasKey("SplineEditor_createPointOffset")) createPointOffset = EditorPrefs.GetFloat("SplineEditor_createPointOffset");
            if (EditorPrefs.HasKey("SplineEditor_showThickness")) showThickness = EditorPrefs.GetBool("SplineEditor_showThickness");
            if (EditorPrefs.HasKey("SplineEditor_thicknessAutoRotate")) thicknessAutoRotate = EditorPrefs.GetBool("SplineEditor_thicknessAutoRotate");
        }

        void OnDisable()
        {
            if(Tools.current == Tool.None) Tools.current = lastTool;
            SceneView.onSceneGUIDelegate -= HandleInput;
            EditorPrefs.SetBool("SplineEditor_scaleSize", scaleSize);
            EditorPrefs.SetBool("SplineEditor_scaleTangents", scaleTangents);
            EditorPrefs.SetBool("SplineEditor_roateNormal", rotateNormal);
            EditorPrefs.SetBool("SplineEditor_rotateTangents", rotateTangents);
            EditorPrefs.SetInt("SplineEditor_createPointMode", createPointMode);
            EditorPrefs.SetInt("SplineEditor_createNormalMode", createNormalMode);
            EditorPrefs.SetInt("SplineEditor_setNormalMode", toolbar.setNormalMode);
            EditorPrefs.SetFloat("SplineEditor_createPointOffset", createPointOffset);
            EditorPrefs.SetBool("SplineEditor_showThickness", showThickness);
            EditorPrefs.SetBool("SplineEditor_thicknessAutoRotate", thicknessAutoRotate);
            if (morphWindow != null) morphWindow.Close();
            toolbar.Close();
        }

        void HandleInput(SceneView current) {
            GetInput();
            editorCamera = current.camera;
            if (!editorCamera.pixelRect.Contains(Event.current.mousePosition))
            {
                SplineEditorGUI.Update();
                control = false;
                controlDown = false;
                controlUp = false;
                FinalizeSelect();
            }
        }

        private static bool IsInAlwaysDraw(SplineComputer comp)
        {
            for (int i = 0; i < drawComputers.Count; i++)
            {
                if (drawComputers[i] == comp)
                {
                    return true;
                }
            }
            return false;
        }

        private static void AddToAlwaysDraw(SplineComputer comp)
        {
            bool found = false;
            for(int i = 0; i < drawComputers.Count; i++)
            {
                if(drawComputers[i] == comp)
                {
                    found = true;
                    break;
                }
            }
            if (!found) drawComputers.Add(comp);
        }

        private static void RemoveFromAlwaysDraw(SplineComputer comp)
        {
            for (int i = 0; i < drawComputers.Count; i++)
            {
                if (drawComputers[i] == comp)
                {
                    drawComputers.RemoveAt(i);
                    break;
                }
            }
        }

        private static void AlwaysDrawPaths(SceneView current)
        {
            for (int i = 0; i < drawComputers.Count; i++)
            {
                if (drawComputers[i] == null)
                {
                    drawComputers.RemoveAt(i);
                    if(i == 0) SceneView.onSceneGUIDelegate -= AlwaysDrawPaths;
                    break;
                }
                DrawPath(drawComputers[i], SceneView.currentDrawingSceneView.camera, false, false);
            }
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(computer, "Edit Points");
            GetInput();
            computer = (SplineComputer)target;
            if (computer.hasMorph && morphWindow == null && HasSelection()) ClearSelection();
            points = computer.GetPoints();
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (selectedPoints[i] >= points.Length)
                {
                    ClearSelection();
                    break;
                }
            }

            SplineComputer.Space lastSpace = computer.space;
            computer.space = (SplineComputer.Space)EditorGUILayout.EnumPopup("Space", computer.space);
            if (lastSpace != computer.space) InitializeTransformer();
            
        
            computer.type = (Spline.Type)EditorGUILayout.EnumPopup("Spline type", computer.type);
            if (points.Length > 1)
            {
                if (computer.type != Spline.Type.Linear) computer.precision = EditorGUILayout.Slider("Precision", (float)computer.precision, 0f, 0.9999f);
                interpolationFoldout = EditorGUILayout.Foldout(interpolationFoldout, "Custom interpolation");
                if (interpolationFoldout)
                {
                    if (computer.customValueInterpolation == null || computer.customValueInterpolation.keys.Length == 0)
                    {
                        if (GUILayout.Button("Add Value Interpolation"))
                        {
                            AnimationCurve curve = new AnimationCurve();
                            curve.AddKey(new Keyframe(0, 0, 0, 0));
                            curve.AddKey(new Keyframe(1, 1, 0, 0));
                            computer.customValueInterpolation = curve;
                        }
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        computer.customValueInterpolation = EditorGUILayout.CurveField("Value interpolation", computer.customValueInterpolation);
                        if (GUILayout.Button("x", GUILayout.MaxWidth(25))) computer.customValueInterpolation = null;
                        EditorGUILayout.EndHorizontal();
                    }
                    if (computer.customNormalInterpolation == null || computer.customNormalInterpolation.keys.Length == 0)
                    {
                        if (GUILayout.Button("Add Normal Interpolation"))
                        {
                            AnimationCurve curve = new AnimationCurve();
                            curve.AddKey(new Keyframe(0, 0, 0, 0));
                            curve.AddKey(new Keyframe(1, 1, 0, 0));
                            computer.customNormalInterpolation = curve;
                        }
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        computer.customNormalInterpolation = EditorGUILayout.CurveField("Normal interpolation", computer.customNormalInterpolation);
                        if (GUILayout.Button("x", GUILayout.MaxWidth(25))) computer.customNormalInterpolation = null;
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
            }
            EditorGUILayout.BeginHorizontal();
            string buttonText = "Close Path";
            if (computer.isClosed)
            {
                buttonText = "Break Path";
                if (selectedPointsCount == 1) GUI.color = SplineEditorGUI.selectionColor;
                if (computer.pointCount < 4) computer.Break();
            }
            if (computer.pointCount < 4 || computer.hasMorph) GUI.color = new Color(1f, 1f, 1f, 0.5f);
            if (GUILayout.Button(buttonText) && !computer.hasMorph)
            {
                if (computer.isClosed)
                {
                    if (selectedPointsCount == 1) BreakSelected();
                    else BreakPath();
                } else if(computer.pointCount >= 4) ClosePath();
            }
            GUI.color = Color.white;

            if (computer.hasMorph) GUI.color = orange;
            if (GUILayout.Button("Morph states"))
            {
                if (morphWindow == null)
                {
                    morphWindow = EditorWindow.GetWindow<MorphWindow>();
                    morphWindow.init(this, "Morph States", new Vector2(150, 300));
                }
            }
            if(computer.hasMorph) GUI.color = new Color(1f, 1f, 1f, 0.5f);
            else GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Drawing", EditorStyles.boldLabel);

            bool alwaysShowPath = IsInAlwaysDraw(computer);
            bool lastAlwaysShow = alwaysShowPath;
            alwaysShowPath = GUILayout.Toggle(alwaysShowPath, "Always Draw Spline");
            if (lastAlwaysShow != alwaysShowPath)
            {
                if (alwaysShowPath)
                {
                    if(drawComputers.Count == 0) SceneView.onSceneGUIDelegate += AlwaysDrawPaths;
                    AddToAlwaysDraw(computer);
                   
                }
                else
                {
                    RemoveFromAlwaysDraw(computer);
                    if (drawComputers.Count == 0) SceneView.onSceneGUIDelegate -= AlwaysDrawPaths;
                }

            }
            showThickness = GUILayout.Toggle(showThickness, "Show thickness");
            if (showThickness) thicknessAutoRotate = GUILayout.Toggle(thicknessAutoRotate, "Always face camera");

            PointMenu();

            if (GUI.changed)
            {
               if (computer.isClosed) points[points.Length - 1] = points[0];
               computer.SetPoints(points);
                EditorUtility.SetDirty(computer);
                SceneView.RepaintAll();
            }
        }


        void GetInput()
        {
            mouseLeftDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            mouserightDown = Event.current.type == EventType.MouseDown && Event.current.button == 1;
            mouseLeftUp = Event.current.type == EventType.MouseUp && Event.current.button == 0;
            mouserightUp = Event.current.type == EventType.MouseUp && Event.current.button == 1;

            controlDown = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl || Event.current.keyCode == KeyCode.LeftCommand || Event.current.keyCode == KeyCode.RightCommand);
            controlUp = Event.current.type == EventType.KeyUp && (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl || Event.current.keyCode == KeyCode.LeftCommand || Event.current.keyCode == KeyCode.RightCommand);

            if (controlDown) control = true;
            if (controlUp) control = false;
            if (mouseLeftDown) mouseLeft = true;
            if (mouserightDown) mouseright = true;
            if (mouseLeftUp) mouseLeft = false;
            if (mouserightUp) mouseright = false;
        }

        public bool IsPointSelected(int index)
        {
            return selectedPoints.Contains(index);
        }

        public bool HasSelection()
        {
            return selectedPoints.Count > 0;
        }

        public void ClearSelection()
        {
            selectedPoints.Clear();
            Repaint();
        }

        public void SelectPoint(int index)
        {
            if (computer.isClosed && index == computer.pointCount - 1) return;
            selectedPoints.Clear();
            selectedPoints.Add(index);
            InitializeTransformer();
            Repaint();
        }

        public void SelectPoints(List<int> indices)
        {
            selectedPoints.Clear();
            for (int i = 0; i < indices.Count; i++)
            {
                if (computer.isClosed && i == computer.pointCount - 1) continue;
                selectedPoints.Add(indices[i]);
            }
            InitializeTransformer();
            Repaint();
        }

        public void AddPointSelection(int index)
        {
            if (computer.isClosed && index == computer.pointCount - 1) return;
            if (selectedPoints.Contains(index)) return;
            selectedPoints.Add(index);
            InitializeTransformer();
            Repaint();
        }

        void OnSceneGUI()
        {
            GetInput();
            SceneView view = SceneView.currentDrawingSceneView;
            editorCamera = view.camera;
            computer = (SplineComputer)target;
            if (computer.hasMorph && morphWindow == null && HasSelection()) ClearSelection();
            points = computer.GetPoints();
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (selectedPoints[i] >= points.Length)
                {
                    ClearSelection();
                    break;
                }
            }
            if (mouseLeftDown && !toolbar.mouseHovers && !mouseright) emptyClick = true;
            

            if (!mouseright && !mouseLeft)
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
                {
                    ToggleScaleTool();
                    Event.current.Use();
                }
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.E)
                {
                    ToggleRotateTool();
                    Event.current.Use();
                }
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W)
                {
                    ToggleMoveTool();
                    Event.current.Use();
                }
            }

            List<SplineComputer> computers =  computer.GetConnectedComputers();
            DrawPath(computer);
            for (int i = 0; i < computers.Count; i++)
            {
                DrawPath(computers[i], SceneView.currentDrawingSceneView.camera, showThickness, thicknessAutoRotate, 0.5f);
            }


            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && HasSelection())
            {
                for (int i = selectedPoints.Count-1; i >= 0 ; i--)
                {
                    DeletePoint(selectedPoints[i]);
                }
                ClearSelection();
                points = computer.GetPoints();
                Event.current.Use();
            }

            if (tool == PointTool.Delete || tool == PointTool.Create) selectedPoints.Clear();

            if (tool == PointTool.Create)
            {
                if (mouseLeftDown && mouseright) tool = PointTool.None;
                if (!toolbar.mouseHovers) CreatePointMode();
                SceneView.RepaintAll(); 
            }
            
            if (Tools.current == Tool.None)
            {
                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlID);
                if (emptyClick && mouseLeftUp && !dragSelect && !HasSelection() && tool != PointTool.Delete && tool != PointTool.Create)
                {
                    if (morphWindow == null)
                    {
                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity)) Selection.activeGameObject = hit.transform.gameObject;
                        else Selection.activeGameObject = null;
                    }
                }
            }

            if (mouseLeftUp)
            {
                if (emptyClick && !dragSelect) ClearSelection();
                emptyClick = false;
            }

            if (Tools.current == Tool.None && (!computer.hasMorph || MorphWindow.editShapeMode)) HandlePoints();

            
            SplineEditorGUI.Reset();
            Handles.BeginGUI();
            if (!dragSelect && Tools.current == Tool.None) toolbar.Draw();
            else if (Tools.current != Tool.None)
            {
                Rect rect = new Rect(5*SplineEditorGUI.scale, 5 * SplineEditorGUI.scale, 150 * SplineEditorGUI.scale, 30 * SplineEditorGUI.scale);
                if (rect.Contains(Event.current.mousePosition)) toolbar.mouseHovers = true;
                if (SplineEditorGUI.Button(rect, "Enter Edit Mode")){
                    lastTool = Tools.current;
                    Tools.current = Tool.None;
                }
                SplineEditorGUI.Label(new Rect(Screen.width / 2f - 80 * SplineEditorGUI.scale, 5 * SplineEditorGUI.scale, 160 * SplineEditorGUI.scale, 40 * SplineEditorGUI.scale), "Editing the Transform");
            }
            if (tool != PointTool.Create && tool != PointTool.Delete && Tools.current == Tool.None) DragSelect();
            else rectDragEnd = rectDragStart = Vector2.zero;
            Handles.EndGUI();


            bool rebuild = false;
            if (!Application.isPlaying)
            {
                if (tsTransform.transform == null)
                {
                    rebuild = true;
                    tsTransform = new TS_Transform(computer.transform);
                }
                rebuild = tsTransform.HasChange();
                if (rebuild)
                {
                    //Update the linked nodes when the computer's transform moves
                    for (int i = 0; i < computer.nodeLinks.Length; i++)
                    {
                        computer.nodeLinks[i].node.UpdatePoint(computer, computer.nodeLinks[i].pointIndex, points[computer.nodeLinks[i].pointIndex]);
                        computer.nodeLinks[i].node.UpdateConnectedComputers(computer);
                    }
                }
                tsTransform.Update();
            }
            
            
            if (GUI.changed || rebuild)
            {
                if (rebuild)
                {
                    List<SplineComputer> computerList = computer.GetConnectedComputers();
                    for (int i = 0; i < computerList.Count; i++)
                    {
                        computerList[i].RebuildImmediate();
                    }
                }
                if (computer.isClosed) points[points.Length - 1] = points[0];
                computer.SetPoints(points);
                EditorUtility.SetDirty(computer);
            }
        }

        void CreatePointMode()
        {
            Vector3 createPoint = Vector3.zero;
            Vector3 normal = Vector3.up;
            bool canCreate = false;
            if (createPointMode == 0)
            {
                GetCreatePointOnPlane(-editorCamera.transform.forward, editorCamera.transform.position + editorCamera.transform.forward * createPointOffset, out createPoint);
                Handles.color = new Color(1f, 0.78f, 0.12f);
                DrawGrid(createPoint, editorCamera.transform.forward, Vector2.one * 10, 2.5f);
                Handles.color = Color.white;
                canCreate = true;
                normal = -editorCamera.transform.forward;
            }

            if (createPointMode == 2)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    canCreate = true;
                    createPoint = hit.point + hit.normal * createPointOffset;
                    Handles.color = Color.blue;
                    Handles.DrawLine(hit.point, createPoint);
                    Handles.RectangleCap(0, createPoint, Quaternion.LookRotation(-editorCamera.transform.forward, editorCamera.transform.up), HandleUtility.GetHandleSize(createPoint) * 0.1f);
                    Handles.color = Color.white;
                    normal = hit.normal;
                }
            }

            if (createPointMode == 3)
            {
                canCreate = AxisGrid(Vector3.right, new Color(0.85f, 0.24f, 0.11f, 0.92f), out createPoint);
                normal = Vector3.right;
            }

            if (createPointMode == 4)
            {
                canCreate = AxisGrid(Vector3.up, new Color(0.6f, 0.95f, 0.28f, 0.92f), out createPoint);
                normal = Vector3.up;
            }

            if (createPointMode == 5)
            {
                canCreate = AxisGrid(Vector3.forward, new Color(0.22f, 0.47f, 0.97f, 0.92f), out createPoint);
                normal = Vector3.forward;
            }
            if (createPointMode == 1)
            {
                canCreate = true;
                if (points.Length < 2) createPointMode = 0;
                else InsertSplinePoint(Event.current.mousePosition);
            }
            else if (mouseLeftDown && !toolbar.mouseHovers && canCreate && !mouseright) CreateSplinePoint(createPoint, normal);
            if (!canCreate) DrawMouseCross();
        }

        bool AxisGrid(Vector3 axis, Color color, out Vector3 origin)
        {
            float dot = Vector3.Dot(editorCamera.transform.position.normalized, axis);
            if (dot < 0f) axis = -axis;
            Plane plane = new Plane(axis, Vector3.zero);
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
            {
                origin = ray.GetPoint(rayDistance) + axis * createPointOffset;
                Handles.color = color;
                float distance = 1f;
                ray = new Ray(editorCamera.transform.position, -axis);
                if (plane.Raycast(ray, out rayDistance)) distance = Vector3.Distance(editorCamera.transform.position + axis * createPointOffset, origin);
                DrawGrid(origin, axis, Vector2.one * distance * 0.3f, distance*2.5f * 0.03f);
                Handles.DrawLine(origin, origin - axis * createPointOffset);
                Handles.color = Color.white;
                return true;
            }
            else
            {
                origin = Vector3.zero;
                return false;
            }
        }

        private void DrawMouseCross()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 origin = ray.GetPoint(1f);
            float size = 0.4f * HandleUtility.GetHandleSize(origin);
            Vector3 a = origin + editorCamera.transform.up * size - editorCamera.transform.right * size;
            Vector3 b = origin - editorCamera.transform.up * size + editorCamera.transform.right * size;
            Handles.color = Color.red;
            Handles.DrawLine(a, b);
            a = origin - editorCamera.transform.up * size - editorCamera.transform.right * size;
            b = origin + editorCamera.transform.up * size + editorCamera.transform.right * size;
            Handles.DrawLine(a, b);
            Handles.color = Color.white;
        }

        private double ProjectScreenSpace(Vector2 screenPoint)
        {
            float closestDistance = (screenPoint - HandleUtility.WorldToGUIPoint(points[0].position)).sqrMagnitude;
            double closestPercent = 0.0;
            double add = computer.moveStep;
            if (computer.type == Spline.Type.Linear) add /= 2.0;
            int count = 0;
            for (double i = add; i < 1.0; i += add)
            {
                SplineResult result = computer.Evaluate(i);
                Vector2 point = HandleUtility.WorldToGUIPoint(result.position);
                float dist = (point - screenPoint).sqrMagnitude;
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPercent = i;
                }
                count++;
            }
            return closestPercent;
        }

        private void InsertSplinePoint(Vector3 screenCoordinates)
        {
            double percent = ProjectScreenSpace(screenCoordinates);
            SplineResult result = computer.Evaluate(percent);
            if (mouseright)
            {
                Handles.CircleCap(0, result.position, Quaternion.LookRotation(editorCamera.transform.position - result.position), HandleUtility.GetHandleSize(result.position) * 0.2f);
                return;
            }
            if (Handles.Button(result.position, Quaternion.LookRotation(editorCamera.transform.position - result.position), HandleUtility.GetHandleSize(result.position) * 0.2f, HandleUtility.GetHandleSize(result.position) * 0.3f, Handles.CircleCap))
            {
                
                Undo.RecordObject(computer, "Create Point");
                EditorUtility.SetDirty(computer);
                SplinePoint newPoint = new SplinePoint(result.position, result.position);
                newPoint.size = result.size;
                newPoint.color = result.color;
                newPoint.normal = result.normal;
                SplinePoint[] newPoints = new SplinePoint[points.Length + 1];
                for (int i = 0; i < newPoints.Length; i++)
                {
                    double floatIndex = (points.Length - 1) * percent;
                    int pointIndex = Mathf.Clamp(DMath.FloorInt(floatIndex), 0, points.Length - 2);
                    if (i <= pointIndex) newPoints[i] = points[i];
                    else if (i == pointIndex + 1) newPoints[i] = newPoint;
                    else newPoints[i] = points[i - 1];
                }
                points = newPoints;
                computer.SetPoints(points);
                
            }
        }


        bool GetCreatePointOnPlane(Vector3 normal, Vector3 origin, out Vector3 result)
        {
            Plane plane = new Plane(normal, origin);
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
            {
                result = ray.GetPoint(rayDistance);
                return true;
            } else if(normal == Vector3.zero)
            {
                result = origin;
                return true;
            }
            else
            {
                result = ray.GetPoint(0f);
                return true;
            }
        }


        private void CreateSplinePoint(Vector3 position, Vector3 normal)
        {
            Undo.RecordObject(computer, "Create Point");
            EditorUtility.SetDirty(computer);
            computer = (SplineComputer)target;
            SplinePoint newPoint = new SplinePoint(position, position);
            newPoint.size = addSize;
            newPoint.color = addColor;
            SplinePoint[] newPoints = new SplinePoint[points.Length + 1];
            for (int i = 0; i < newPoints.Length; i++)
            {
                if (i < points.Length) newPoints[i] = points[i];
                else newPoints[i] = newPoint;
            }
            if (computer.isClosed) newPoints[0] = newPoint;
            points = newPoints;
            bool closeSpline = false;
            if (!computer.isClosed && points.Length > 2)
            {
                Vector2 first = HandleUtility.WorldToGUIPoint(points[0].position);
                Vector2 last = HandleUtility.WorldToGUIPoint(points[points.Length - 1].position);
                if (Vector2.Distance(first, last) <= 20f) if (EditorUtility.DisplayDialog("Close spline?", "Do you want to make the spline path closed ?", "Yes", "No")) closeSpline = true;
            }
            if (createNormalMode == 0) points[points.Length - 1].normal = normal;
            else SetNormal(ref points[points.Length-1], createNormalMode);
            computer.SetPoints(points);
            if (closeSpline) computer.Close();
            EditorUtility.SetDirty(computer);
        }

        public static void DrawPath(SplineComputer comp, Camera editorCamera, bool showThickness, bool thicknessAutoRotate, float alpha = 1f, double fromPercent = 0.0, double toPercent = 1.0, bool autoColor = true)
        {
            Color prevColor = Handles.color;
            Color handleColor = prevColor;
            if(autoColor) handleColor = comp.hasMorph && !MorphWindow.editShapeMode ? orange : comp.editorPathColor;
            handleColor.a = alpha;
            Handles.color = handleColor;
            SplinePoint[] drawPoints = comp.GetPoints();
            if (drawPoints.Length < 2) return;
            double add = comp.moveStep;
            if (add < 0.0025) add = 0.0025;
            
            if(comp.type == Spline.Type.BSpline && comp.pointCount > 1)
            {
                SplinePoint[] compPoints = comp.GetPoints();
                Handles.color = new Color(handleColor.r, handleColor.g, handleColor.b, 0.5f*alpha);
                for (int i = 0; i < compPoints.Length - 1; i++)
                {
                    Handles.DrawLine(compPoints[i].position, compPoints[i + 1].position);
                }
                Handles.color = handleColor;
            }

            if (!showThickness)
            {
                double percent = fromPercent;
                while (true)
                {
                    Handles.DrawLine(comp.EvaluatePosition(percent), comp.EvaluatePosition(percent + add));
                    if (percent == toPercent) break;
                    percent = DMath.Move(percent, toPercent, add);
                }
                return;
            } else
            {
                double percent = fromPercent;
                while (true)
                {
                    SplineResult from = comp.Evaluate(percent);
                    SplineResult to = comp.Evaluate(percent + add);
                    Vector3 toNormal = to.normal;
                    Vector3 fromNormal = from.normal;

                    if (thicknessAutoRotate)
                    {
                        toNormal = (editorCamera.transform.position - to.position).normalized;
                        fromNormal = (editorCamera.transform.position - from.position).normalized;
                    }

                    Vector3 toRight = Vector3.Cross(to.direction, toNormal).normalized*to.size*0.5f;

                    Vector3 fromRight = Vector3.Cross(from.direction, fromNormal).normalized*from.size*0.5f;

                    Handles.DrawLine(from.position+fromRight, to.position+toRight);
                    Handles.DrawLine(from.position - fromRight, to.position - toRight);
                    Handles.DrawLine(from.position + fromRight, from.position - fromRight);
                    if (percent == toPercent) break;
                    percent = DMath.Move(percent, toPercent, add);
                }
            }

            Handles.color = prevColor;
        }

        void DrawPath(SplineComputer comp)
        {
            DrawPath(comp, editorCamera, showThickness, thicknessAutoRotate);
        }

        void PointMenu()
        {
            int select = -1;
            if (selectedPoints == null || selectedPoints.Count == 0 || points.Length == 0)
            {
                select = SplineEditorGUI.PointSelectionMenu(computer);
                if (select >= 0) SelectPoint(select);
                return;
            }
            Vector3 avgPos = Vector3.zero;
            Vector3 avgTan = Vector3.zero;
            Vector3 avgTan2 = Vector3.zero;
            Vector3 avgNormal = Vector3.zero;
            float avgSize = 0f;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avgPos += points[selectedPoints[i]].position;
                avgNormal += points[selectedPoints[i]].normal;
                avgSize += points[selectedPoints[i]].size;
                avgTan += points[selectedPoints[i]].tangent;
                avgTan2 += points[selectedPoints[i]].tangent2;
            }


            avgPos /= selectedPoints.Count;
            avgTan /= selectedPoints.Count;
            avgTan2 /= selectedPoints.Count;
            avgSize /= selectedPoints.Count;
            avgNormal.Normalize();
            SplinePoint avgPoint = new SplinePoint(avgPos, avgPos);
            avgPoint.tangent = avgTan;
            avgPoint.tangent2 = avgTan2;
            avgPoint.size = avgSize;
            avgPoint.color = points[selectedPoints[0]].color;
            avgPoint.type = points[selectedPoints[0]].type;
            SplinePoint.Type lastType = avgPoint.type;
            Color lastColor = avgPoint.color;
            avgPoint.normal = avgNormal;
            string title = "Point";
            if (selectedPoints.Count == 1) title += " " + selectedPoints[0];
            else title = "Multiple Points (Average values)";
            if (computer.isClosed)
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    if (selectedPoints[i] == points.Length - 1)
                    {
                        if (selectedPoints.Count - 1 == 1) title = "Point " + selectedPoints[0];
                        break;
                    }
                }
            }
            float boxHeight = 150f;
            if (computer.type == Spline.Type.Bezier) boxHeight = 212f;
            GUILayout.Box(title, GUILayout.Width(Screen.width - 45), GUILayout.Height(boxHeight));
            GUI.BeginGroup(GUILayoutUtility.GetLastRect());
            if (computer.space == SplineComputer.Space.Local) avgPoint.position = computer.transform.InverseTransformPoint(avgPoint.position);
            float yPosition = 25;
            avgPoint.position = EditorGUI.Vector3Field(new Rect(10, yPosition, Screen.width - 65, 25), "Position", avgPoint.position);
            yPosition += 22;
            if (computer.type == Spline.Type.Bezier)
            {
                avgPoint.tangent = EditorGUI.Vector3Field(new Rect(10, yPosition, Screen.width - 65, 25), "Tangent 1", avgPoint.tangent);
                yPosition += 22;
                avgPoint.tangent2 = EditorGUI.Vector3Field(new Rect(10, yPosition, Screen.width - 65, 25), "Tangent 2", avgPoint.tangent2);
                yPosition += 22;
            }
            if (computer.space == SplineComputer.Space.Local) avgPoint.position = computer.transform.TransformPoint(avgPoint.position);
            EditorGUI.LabelField(new Rect(10, yPosition, Screen.width - 65, 30), "Normal");
            if (computer.space == SplineComputer.Space.Local) avgPoint.normal = computer.transform.InverseTransformDirection(avgPoint.normal);
                float last = avgPoint.normal.x;
                avgPoint.normal.x = EditorGUI.Slider(new Rect(10 + Screen.width / 3f, yPosition, Screen.width / 1.5f - 65, 15), avgPoint.normal.x, -1f, 1f);
                yPosition += 17;
            if (!Mathf.Approximately(last, avgPoint.normal.x))
            {
                avgPoint.normal.y = Mathf.MoveTowards(avgPoint.normal.y, 0f, Mathf.Abs(last - avgPoint.normal.x) * Mathf.Abs(avgPoint.normal.y));
                avgPoint.normal.z = Mathf.MoveTowards(avgPoint.normal.z, 0f, Mathf.Abs(last - avgPoint.normal.x) * Mathf.Abs(avgPoint.normal.z));
            }
            last = avgPoint.normal.y;
            avgPoint.normal.y = EditorGUI.Slider(new Rect(10 + Screen.width / 3f, yPosition, Screen.width / 1.5f - 65, 15), avgPoint.normal.y, -1f, 1f);
            yPosition += 17;
            if (!Mathf.Approximately(last, avgPoint.normal.y))
            {
                avgPoint.normal.x = Mathf.MoveTowards(avgPoint.normal.x, 0f, Mathf.Abs(last - avgPoint.normal.y) * Mathf.Abs(avgPoint.normal.x));
                avgPoint.normal.z = Mathf.MoveTowards(avgPoint.normal.z, 0f, Mathf.Abs(last - avgPoint.normal.y) * Mathf.Abs(avgPoint.normal.z));
            }
            last = avgPoint.normal.z;
            avgPoint.normal.z = EditorGUI.Slider(new Rect(10 + Screen.width / 3f, yPosition, Screen.width / 1.5f - 65, 15), avgPoint.normal.z, -1f, 1f);
            yPosition += 22;
            if (!Mathf.Approximately(last, avgPoint.normal.z))
            {
                avgPoint.normal.x = Mathf.MoveTowards(avgPoint.normal.x, 0f, Mathf.Abs(last - avgPoint.normal.y) * Mathf.Abs(avgPoint.normal.x));
                avgPoint.normal.y = Mathf.MoveTowards(avgPoint.normal.y, 0f, Mathf.Abs(last - avgPoint.normal.z) * Mathf.Abs(avgPoint.normal.y));
            }
            avgPoint.normal.Normalize();
            if (avgPoint.normal == Vector3.zero) avgPoint.normal = avgNormal;
            if (computer.space == SplineComputer.Space.Local) avgPoint.normal = computer.transform.TransformDirection(avgPoint.normal);
            avgPoint.size = EditorGUI.FloatField(new Rect(10, yPosition, Screen.width - 65, 15), "Size", avgPoint.size);
            yPosition += 22;
            avgPoint.color = EditorGUI.ColorField(new Rect(10, yPosition, Screen.width - 65, 15), "Color", avgPoint.color);
            yPosition += 22;
            if (computer.type == Spline.Type.Bezier) avgPoint.type = (SplinePoint.Type)EditorGUI.EnumPopup(new Rect(10, yPosition, Screen.width - 65, 15), "Point Type", avgPoint.type);
            

            GUI.EndGroup();
            GUILayout.Space(0);
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (!Mathf.Approximately(avgPos.x, avgPoint.position.x))
                {
                    Vector3 newPos = points[selectedPoints[i]].position;
                    newPos.x = avgPoint.position.x;
                    points[selectedPoints[i]].SetPosition(newPos);
                }
                if (!Mathf.Approximately(avgPos.y, avgPoint.position.y))
                {
                    Vector3 newPos = points[selectedPoints[i]].position;
                    newPos.y = avgPoint.position.y;
                    points[selectedPoints[i]].SetPosition(newPos);
                }
                if (!Mathf.Approximately(avgPos.z, avgPoint.position.z))
                {
                    Vector3 newPos = points[selectedPoints[i]].position;
                    newPos.z = avgPoint.position.z;
                    points[selectedPoints[i]].SetPosition(newPos);
                }
                if (avgPoint.normal != avgNormal)  points[selectedPoints[i]].normal = avgPoint.normal;
                if (avgPoint.size != avgSize) points[selectedPoints[i]].size = avgPoint.size;
                if (lastColor != avgPoint.color) points[selectedPoints[i]].color = avgPoint.color;
                if (lastType != avgPoint.type) points[selectedPoints[i]].type = avgPoint.type;
                if (computer.type != Spline.Type.Bezier) continue;
                if (!Mathf.Approximately(avgTan.x, avgPoint.tangent.x))
                {
                    Vector3 newPos = points[selectedPoints[i]].tangent;
                    newPos.x = avgPoint.tangent.x;
                    points[selectedPoints[i]].SetTangentPosition(newPos);
                }
                if (!Mathf.Approximately(avgTan.y, avgPoint.tangent.y))
                {
                    Vector3 newPos = points[selectedPoints[i]].tangent;
                    newPos.y = avgPoint.tangent.y;
                    points[selectedPoints[i]].SetTangentPosition(newPos);
                }
                if (!Mathf.Approximately(avgTan.z, avgPoint.tangent.z))
                {
                    Vector3 newPos = points[selectedPoints[i]].tangent;
                    newPos.z = avgPoint.tangent.z;
                    points[selectedPoints[i]].SetTangentPosition(newPos);
                }

                if (!Mathf.Approximately(avgTan2.x, avgPoint.tangent2.x))
                {
                    Vector3 newPos = points[selectedPoints[i]].tangent2;
                    newPos.x = avgPoint.tangent2.x;
                    points[selectedPoints[i]].SetTangent2Position(newPos);
                }
                if (!Mathf.Approximately(avgTan2.y, avgPoint.tangent2.y))
                {
                    Vector3 newPos = points[selectedPoints[i]].tangent2;
                    newPos.y = avgPoint.tangent2.y;
                    points[selectedPoints[i]].SetTangent2Position(newPos);
                }
                if (!Mathf.Approximately(avgTan2.z, avgPoint.tangent2.z))
                {
                    Vector3 newPos = points[selectedPoints[i]].tangent2;
                    newPos.z = avgPoint.tangent2.z;
                    points[selectedPoints[i]].SetTangent2Position(newPos);
                }
            }
            select = SplineEditorGUI.PointSelectionMenu(computer);
            if (select >= 0) SelectPoint(select);
        }

        public void BreakSelected()
        {
            Undo.RecordObject(computer, "Break path");
            EditorUtility.SetDirty(computer);
            computer.Break(selectedPoints[0]);
            points = computer.GetPoints();
            ClearSelection();
        }

        public void BreakPath()
        {
            Undo.RecordObject(computer, "Break path");
            EditorUtility.SetDirty(computer);
            computer.Break();
            points = computer.GetPoints();
            ClearSelection();
        }

        public void ClosePath()
        {
            Undo.RecordObject(computer, "Close path");
            EditorUtility.SetDirty(computer);
            computer.Close();
            ClearSelection();
        }

        public void CenterSelection()
        {
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avg += points[selectedPoints[i]].position;
            }
            avg /= selectedPoints.Count;
            Vector3 delta = computer.transform.position - avg;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
               points[selectedPoints[i]].SetPosition(points[selectedPoints[i]].position + delta);
            }
        }

        public void FlatSelection(int axis)
        {
            Undo.RecordObject(computer, "Align points");
            EditorUtility.SetDirty(computer);
            Vector3 avg = Vector3.zero;
            bool alignTangents = false;
            if (computer.type == Spline.Type.Bezier)
            {
                if (EditorUtility.DisplayDialog("Align tangents", "Do you want to align the tangents too ?", "Yes", "No")) alignTangents = true;
            }
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avg += points[selectedPoints[i]].position;
            }
            avg /= selectedPoints.Count;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                Vector3 pos = points[selectedPoints[i]].position;
                switch (axis)
                {
                    case 0: pos.x = avg.x; break;
                    case 1: pos.y = avg.y; break;
                    case 2: pos.z = avg.z; break;
                }
                points[selectedPoints[i]].SetPosition(pos);
                if (alignTangents)
                {
                    Vector3 tan = points[selectedPoints[i]].tangent;
                    Vector3 tan2 = points[selectedPoints[i]].tangent2;
                    switch (axis)
                    {
                        case 0: tan.x = avg.x; tan2.x = avg.x;  break;
                        case 1: tan.y = avg.y; tan2.y = avg.y;  break;
                        case 2: tan.z = avg.z; tan2.z = avg.z;  break;
                    }
                    points[selectedPoints[i]].SetTangentPosition(tan);
                    points[selectedPoints[i]].SetTangent2Position(tan2);
                }
            }
        }

        public void MirrorSelection(int axis)
        {
            bool mirrorTangents = false;
            if (computer.type == Spline.Type.Bezier)
            {
                if (EditorUtility.DisplayDialog("Mirror tangents", "Do you want to mirror the tangents too ?", "Yes", "No")) mirrorTangents = true;
            }
            float min = 0f, max = 0f;
            switch (axis)
            {
                case 0: min = max = points[selectedPoints[0]].position.x; break;
                case 1: min = max = points[selectedPoints[0]].position.y; break;
                case 2: min = max = points[selectedPoints[0]].position.z; break;
            }
            if (mirrorTangents)
            {
                float value = 0f;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[0]].tangent.x; break;
                    case 1: value = points[selectedPoints[0]].tangent.y; break;
                    case 2: value = points[selectedPoints[0]].tangent.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[0]].tangent2.x; break;
                    case 1: value = points[selectedPoints[0]].tangent2.y; break;
                    case 2: value = points[selectedPoints[0]].tangent2.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
            }
            for (int i = 1; i < selectedPoints.Count; i++)
            {
                float value = 0f;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[i]].position.x; break;
                    case 1: value = points[selectedPoints[i]].position.y; break;
                    case 2: value = points[selectedPoints[i]].position.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
                if (mirrorTangents)
                {
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent.x; break;
                        case 1: value = points[selectedPoints[i]].tangent.y; break;
                        case 2: value = points[selectedPoints[i]].tangent.z; break;
                    }
                    if (value < min) min = value;
                    if (value > max) max = value;
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent2.x; break;
                        case 1: value = points[selectedPoints[i]].tangent2.y; break;
                        case 2: value = points[selectedPoints[i]].tangent2.z; break;
                    }
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            for (int i = 0; i < selectedPoints.Count; i++)
            {
                float value = 0f;
                if (mirrorTangents)
                {
                    //Point position
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].position.x; break;
                        case 1: value = points[selectedPoints[i]].position.y; break;
                        case 2: value = points[selectedPoints[i]].position.z; break;
                    }
                    float percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: points[selectedPoints[i]].position.x = value; break;
                        case 1: points[selectedPoints[i]].position.y = value; break;
                        case 2: points[selectedPoints[i]].position.z = value; break;
                    }
                    //Tangent 1
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent.x; break;
                        case 1: value = points[selectedPoints[i]].tangent.y; break;
                        case 2: value = points[selectedPoints[i]].tangent.z; break;
                    }
                    percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: points[selectedPoints[i]].tangent.x = value; break;
                        case 1: points[selectedPoints[i]].tangent.y = value; break;
                        case 2: points[selectedPoints[i]].tangent.z = value; break;
                    }
                    //Tangent 2
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent2.x; break;
                        case 1: value = points[selectedPoints[i]].tangent2.y; break;
                        case 2: value = points[selectedPoints[i]].tangent2.z; break;
                    }
                    percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: points[selectedPoints[i]].tangent2.x = value; break;
                        case 1: points[selectedPoints[i]].tangent2.y = value; break;
                        case 2: points[selectedPoints[i]].tangent2.z = value; break;
                    }
                }
                else
                {
                    Vector3 pos = points[selectedPoints[i]].position;
                    switch (axis)
                    {
                        case 0: value = pos.x; break;
                        case 1: value = pos.y; break;
                        case 2: value = pos.z; break;
                    }
                    float percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: pos.x = value; break;
                        case 1: pos.y = value; break;
                        case 2: pos.z = value; break;
                    }
                    points[selectedPoints[i]].SetPosition(pos);
                }
            }
        }

        public void DistributeEvenly()
        {
            if (selectedPoints.Count < 3) return;
            Undo.RecordObject(computer, "Space points evenly");
            EditorUtility.SetDirty(computer);
            float avgDistance = 0f;
            List<int> tempSelected = new List<int>(selectedPoints.ToArray());
            if (computer.isClosed && IsPointSelected(0)) tempSelected.Add(points.Length - 1);
            Vector3[] directions = new Vector3[tempSelected.Count - 1];
            for (int i = 1; i < tempSelected.Count; i++)
            {
                Vector3 direction = points[tempSelected[i]].position - points[tempSelected[i - 1]].position;
                avgDistance += direction.magnitude;
                directions[i - 1] = direction.normalized;
            }
            avgDistance /= directions.Length;
            for (int i = 1; i < tempSelected.Count-1; i++)
            {
                points[tempSelected[i]].SetPosition(points[tempSelected[i - 1]].position + directions[i - 1] * avgDistance);
            }
            if (computer.type != Spline.Type.Bezier) return;
            if (!EditorUtility.DisplayDialog("Distribute tangents", "Do you want to distribute the tangents too ?", "Yes", "No")) return;
            avgDistance = 0f;
            directions = new Vector3[tempSelected.Count*2];
            int dirIndex = 0;
            for (int i = 0; i < tempSelected.Count; i++)
            {
                avgDistance += Vector3.Distance(points[tempSelected[i]].tangent, points[tempSelected[i]].position);
                avgDistance += Vector3.Distance(points[tempSelected[i]].tangent2, points[tempSelected[i]].position);
                directions[dirIndex] = Vector3.Normalize(points[tempSelected[i]].tangent - points[tempSelected[i]].position);
                dirIndex++;
                directions[dirIndex] = Vector3.Normalize(points[tempSelected[i]].tangent2 - points[tempSelected[i]].position);
                dirIndex++;
            }
            avgDistance /= directions.Length;
            for (int i = 0; i < tempSelected.Count; i++)
            {
                points[tempSelected[i]].SetTangentPosition(points[tempSelected[i]].position + directions[i * 2]*avgDistance);
                points[tempSelected[i]].SetTangent2Position(points[tempSelected[i]].position + directions[i * 2+1]*avgDistance);
            }
        }

        void PointClick(int index)
        {
            emptyClick = false;
            if (tool == PointTool.Delete)
            {
                DeletePoint(index);
                selectedPoints.Clear();
                Repaint();
            }
            else if (!IsPointSelected(index) && tool != PointTool.Create)
            {
                if (control) AddPointSelection(index);
                else SelectPoint(index);
                Repaint();
            }
        }

        private void HandlePoints()
        {
            if (computer.pointCount == 0) return;
            Undo.RecordObject(computer, "Edit Points");
            //Handle closed splines
            int change = 0;
            int clickPoint = -1;
            bool canFreeMove = tool == PointTool.None;
            //Draw points
            int pointLength = points.Length;
            if (computer.isClosed) pointLength = points.Length - 1;
            bool overlapping = false;
            for (int i = 0; i < pointLength; i++)
            {
                if (computer.isClosed && i == points.Length - 1) break;
                if (IsPointSelected(i)) Handles.color = SplineEditorGUI.selectionColor;
                else
                {
                    if(i == 0 && computer.pointCount > 1 && !computer.isClosed)
                    {
                        Vector2 screenPos1 = HandleUtility.WorldToGUIPoint(points[0].position);
                        Vector2 screenPos2 = HandleUtility.WorldToGUIPoint(points[points.Length-1].position);
                        if (Vector2.Distance(screenPos1, screenPos2) <= 8)
                        {
                            overlapping = true;
                            Handles.color = new Color(1f, 0.78f, 0.12f);
                        }
                    } else if(i == computer.pointCount-1 && overlapping) Handles.color = new Color(1f, 0.78f, 0.12f);
                }
                Color buttonColor = IsPointSelected(i) ? SplineEditorGUI.selectionColor : computer.editorPathColor;
                if (tool == PointTool.Delete) buttonColor = Color.red;
                if (SplineEditorHandles.Button(points[i].position, !canFreeMove, buttonColor, 0.12f)) clickPoint = i;

                if (canFreeMove)
                {
                    if (FreeMovePoint(i))
                    {
                        change++;
                        clickPoint = i;
                    }
                }
                Handles.color = Color.white;
            }

            if (clickPoint >= 0)
            {
                holdSelection = true;
                PointClick(clickPoint);
                SceneView.RepaintAll();
                //return;
            }
            else holdSelection = false;

            //SCALE ---------
            if (tool == PointTool.Scale)
            {
                if (mouseLeftUp) InitializeTransformer();
                Vector3 lastScale = transformer.scale;
                transformer.scale = Handles.ScaleHandle(transformer.scale, transformer.center, computer.space == SplineComputer.Space.Local ? computer.transform.rotation : Quaternion.identity, HandleUtility.GetHandleSize(transformer.center));
                if (lastScale != transformer.scale)
                {
                    change++;
                    points = transformer.GetScaled(scaleSize, scaleTangents);
                }
            }
            else if (tool == PointTool.Rotate)
            {
                //ROTATE-------------
                if (mouseLeftUp) InitializeTransformer();
                Quaternion lastRotation = transformer.rotation;
                Handles.color = Color.white;
                transformer.rotation = Handles.RotationHandle(lastRotation, transformer.center);
                Handles.color = Color.yellow;
                for(int i = 0; i < selectedPoints.Count; i ++) Handles.DrawLine(points[selectedPoints[i]].position, points[selectedPoints[i]].position + HandleUtility.GetHandleSize(points[selectedPoints[i]].position) * points[selectedPoints[i]].normal);
                Handles.color = Color.yellow;
                if (lastRotation != transformer.rotation)
                {
                    change++;
                    points = transformer.GetRotated(rotateNormal, rotateTangents);
                }
            }
            else
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    if (computer.isClosed && selectedPoints[i] == points.Length - 1) continue;
                    else if (tool == PointTool.Move) change += MovePoint(selectedPoints[i]) ? 1 : 0;
                    else if (tool == PointTool.NormalEdit) change += SetNormal(ref points[selectedPoints[i]]) ? 1 : 0;
                    if (computer.type == Spline.Type.Bezier)
                    {
                        if (canFreeMove || tool == PointTool.Move) change += HandleTangents(selectedPoints[i]) ? 1 : 0;
                    }
                }
            }
            if (mouseLeftUp) holdSelection = false;
            if (change > 0)
            {
                emptyClick = false;
                holdSelection = true;
            }
        }


        bool MovePoint(int index)
        {
            Vector3 lastPos = points[index].position;
            points[index].SetPosition(Handles.PositionHandle(points[index].position, computer.space == SplineComputer.Space.Local ? computer.transform.rotation : Quaternion.identity));
            bool changed = lastPos != points[index].position;
            if (selectedPoints.Count > 1 && changed)
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    if (selectedPoints[i] != index)
                    {
                        points[selectedPoints[i]].SetPosition(points[selectedPoints[i]].position + (points[index].position - lastPos));
                    }
                }
            }
            return changed;
        }

        bool FreeMovePoint(int index)
        {
            Color previousColor = Handles.color;
            if(Handles.color != SplineEditorGUI.selectionColor) Handles.color = computer.editorPathColor;
            Vector3 lastPos = points[index].position;
            points[index].SetPosition(Handles.FreeMoveHandle(points[index].position, Quaternion.identity, HandleUtility.GetHandleSize(points[index].position) * 0.1f, Vector3.zero, Handles.RectangleCap));
            bool changed = lastPos != points[index].position;
            if (selectedPoints.Count > 1 && changed)
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    if (selectedPoints[i] != index)
                    {
                        points[selectedPoints[i]].SetPosition(points[selectedPoints[i]].position + (points[index].position - lastPos));
                    }
                }
            }
            Handles.color = previousColor;
            return changed;
        }


        public void SetSelectedNormals(int normalMode)
        {
            for(int i = 0; i < selectedPoints.Count; i++)
            {
                SetNormal(ref points[selectedPoints[i]], normalMode);
            }
        }

        bool SetNormal(ref SplinePoint point, int setMode = 0)
        {
            Handles.color = SplineEditorGUI.selectionColor;
            Handles.DrawWireDisc(point.position, point.normal, HandleUtility.GetHandleSize(point.position) * 0.35f);
            Handles.DrawWireDisc(point.position, point.normal, HandleUtility.GetHandleSize(point.position) * 0.7f);
            Handles.color = Color.yellow;
            Handles.DrawLine(point.position, point.position + HandleUtility.GetHandleSize(point.position) * point.normal);
            bool changed = false;
            int pointIndex = -1;
            if(setMode == 3)
            {
                for(int i = 0; i < points.Length; i++)
                {
                    if(points[i].Equals(point))
                    {
                        pointIndex = i;
                        break;
                    }
                }
            }
            if (setMode != 0)
            {
                switch (setMode)
                {
                    case 1: point.normal = Vector3.Normalize(editorCamera.transform.position - point.position); break;
                    case 2: point.normal = editorCamera.transform.forward; break;
                    case 3: point.normal = CalculatePointNormal(pointIndex); break;
                    case 4: point.normal = Vector3.left; break;
                    case 5: point.normal = Vector3.right; break;
                    case 6: point.normal = Vector3.up; break;
                    case 7: point.normal = Vector3.down; break;
                    case 8: point.normal = Vector3.forward; break;
                    case 9: point.normal = Vector3.back; break;
                    case 10: point.normal *= -1; break;
                }
                changed = true;
            }
            else
            {
                Vector3 normalPos = point.position + point.normal * HandleUtility.GetHandleSize(point.position);
                Vector3 lastPos = normalPos;
                normalPos = Handles.FreeMoveHandle(normalPos, Quaternion.identity, HandleUtility.GetHandleSize(normalPos) * 0.15f, Vector3.zero, Handles.CircleCap);
                changed = lastPos != normalPos;
                normalPos -= point.position;
                normalPos.Normalize();
                point.normal = normalPos;
            }
            Handles.color = Color.white;
            return changed;
        }

        bool HandleTangents(int index)
        {
            Vector3 lastPos = points[index].tangent;
            Vector3 lastPos2 = points[index].tangent2;

            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawDottedLine(points[index].position, points[index].tangent, 6f);
            Handles.DrawDottedLine(points[index].position, points[index].tangent2, 6f);
            Handles.color = Color.white;

            if (tool == PointTool.Move && selectedPoints.Count == 1) points[index].SetTangentPosition(Handles.PositionHandle(points[index].tangent, computer.space == SplineComputer.Space.Local ? computer.transform.rotation : Quaternion.identity));
            else if (tool == PointTool.None || selectedPoints.Count > 1) points[index].SetTangentPosition(Handles.FreeMoveHandle(points[index].tangent, Quaternion.identity, HandleUtility.GetHandleSize(points[index].tangent) * 0.1f, Vector3.zero, Handles.CircleCap));
            if (tool == PointTool.Move && selectedPoints.Count == 1) points[index].SetTangent2Position(Handles.PositionHandle(points[index].tangent2, computer.space == SplineComputer.Space.Local ? computer.transform.rotation : Quaternion.identity));
            else if (tool == PointTool.None || selectedPoints.Count > 1) points[index].SetTangent2Position(Handles.FreeMoveHandle(points[index].tangent2, Quaternion.identity, HandleUtility.GetHandleSize(points[index].tangent2) * 0.1f, Vector3.zero, Handles.CircleCap));
            bool changed = points[index].tangent != lastPos || points[index].tangent2 != lastPos2;
            return changed;
        }



        private void DrawWireSphere(Vector3 position, float radius)
        {
            Handles.DrawWireDisc(position, Vector3.up, radius / 2f);
            Handles.DrawWireDisc(position, editorCamera.transform.position - position, radius / 2f);
            Handles.DrawWireDisc(position, Vector3.forward, radius / 2f);
        }

        private void DeletePoint(int index)
        {
            if (computer.hasMorph)
            {
                Debug.Log("Cannot delete points when there are morphs");
                return;
            }
            Undo.RecordObject(computer, "Delete Point");
            EditorUtility.SetDirty(computer);
            SplinePoint[] newPoints = new SplinePoint[points.Length - 1];
            for (int i = 0; i < newPoints.Length; i++)
            {
                if (i < index) newPoints[i] = points[i];
                else newPoints[i] = points[i + 1];
            }
            points = newPoints;
            computer.SetPoints(points);
            points = computer.GetPoints();
            EditorUtility.SetDirty(computer);
        }


        void DragSelect()
        {
            Rect selectRect;
            if (holdSelection || toolbar.mouseHovers)
            {
                dragSelect = listenSelect = false;
                return;
            }

            if (mouseLeftDown)
            {
                listenSelect = true;
                rectDragStart = rectDragEnd = Event.current.mousePosition;
                SceneView.RepaintAll();
            }

            if (listenSelect && mouseLeft)
            {
                rectDragEnd = Event.current.mousePosition;
                if (rectDragEnd == rectDragStart) return;
               
                Color col = SplineEditorGUI.selectionColor;
                col.a = 0.4f;
                GUI.color = col;
                selectRect = new Rect(Mathf.Min(rectDragStart.x, rectDragEnd.x), Mathf.Min(rectDragStart.y, rectDragEnd.y), Mathf.Abs(rectDragEnd.x - rectDragStart.x), Mathf.Abs(rectDragEnd.y - rectDragStart.y));
                if (selectRect.width >= 5 && selectRect.height >= 5)
                {
                    dragSelect = true;
                    GUI.Box(selectRect, "", SplineEditorGUI.whiteBox);
                }
                GUI.color = Color.white;
                SceneView.RepaintAll();
            }

            if (dragSelect && mouseLeftUp) FinalizeSelect();
        }

        void FinalizeSelect() {
           Rect selectRect = new Rect(Mathf.Min(rectDragStart.x, rectDragEnd.x), Mathf.Min(rectDragStart.y, rectDragEnd.y), Mathf.Abs(rectDragEnd.x - rectDragStart.x), Mathf.Abs(rectDragEnd.y - rectDragStart.y));
            dragSelect = listenSelect = false;
            if (selectRect.width > 0f && selectRect.height > 0f)
            {
                if (!control) ClearSelection();
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 guiPoint = HandleUtility.WorldToGUIPoint(points[i].position);
                    if (selectRect.Contains(guiPoint))
                    {
                        AddPointSelection(i);
                    }
                }
            }
            rectDragEnd = rectDragStart;
            SceneView.RepaintAll();
        }


        void DrawGrid(Vector3 center, Vector3 normal, Vector2 size, float scale)
        {
            Vector3 right = Vector3.Cross(Vector3.up, normal).normalized;
            if (Mathf.Abs(Vector3.Dot(Vector3.up, normal)) >= 0.9999f) right = Vector3.Cross(Vector3.forward, normal).normalized;
            Vector3 up = Vector3.Cross(normal, right).normalized;
            Vector3 startPoint = center - right * size.x * 0.5f + up * size.y * 0.5f;
            float i = 0f;
            float add = scale;
            while (i <= size.x)
            {
                Vector3 point = startPoint + right * i;
                Handles.DrawLine(point, point - up * size.y);
                i += add;
            }

            i = 0f;
            add = scale;
            while (i <= size.x)
            {
                Vector3 point = startPoint - up * i;
                Handles.DrawLine(point, point + right * size.x);
                i += add;
            }
        }

        public void ToggleMoveTool()
        {
            if (tool != PointTool.Move && Tools.current == Tool.None && HasSelection())
            {
                tool = PointTool.Move;
                Tools.current = Tool.None;
            }
            else if (tool == PointTool.Move || (Tools.current != Tool.None && Tools.current != Tool.Move) || (!HasSelection() && Tools.current != Tool.Move))
            {
                tool = PointTool.None;
                lastTool = Tools.current = Tool.Move;
            }
            else
            {
                tool = PointTool.None;
                Tools.current = Tool.None;
            }
        }

       public void ToggleRotateTool()
        {
            if (tool != PointTool.Rotate && Tools.current == Tool.None && HasSelection())
            {
                tool = PointTool.Rotate;
                InitializeTransformer();
                Tools.current = Tool.None;
            }
            else if (tool == PointTool.Rotate || (Tools.current != Tool.None && Tools.current != Tool.Rotate) || (!HasSelection() && Tools.current != Tool.Rotate))
            {
                tool = PointTool.None;
                lastTool = Tools.current = Tool.Rotate;
            }
            else
            {
                tool = PointTool.None;
                Tools.current = Tool.None;
            }
        }

        public void ToggleScaleTool()
        {
            if (tool != PointTool.Scale && Tools.current == Tool.None && HasSelection())
            {
                tool = PointTool.Scale;
                InitializeTransformer();
                Tools.current = Tool.None;
            }
            else if (tool == PointTool.Scale || (Tools.current != Tool.None && Tools.current != Tool.Scale) || (!HasSelection() && Tools.current != Tool.Scale))
            {
                tool = PointTool.None;
                lastTool = Tools.current = Tool.Scale;
            }
            else
            {
                tool = PointTool.None;
                Tools.current = Tool.None;
            }
        }

        void ToggleNormalTool()
        {

        }

        void InitializeTransformer()
        {
            if(computer.space == SplineComputer.Space.Local) transformer = new PointTransformer(points, selectedPoints, computer.transform);
            else transformer = new PointTransformer(points, selectedPoints);
        }

        Vector3 CalculatePointNormal(int index)
        {
            if (points.Length < 3)
            {
                Debug.Log("Spline needs to have at least 3 control points in order to calculate normals");
                return Vector3.zero;
            }
            Vector3 side1 = Vector3.zero;
            Vector3 side2 = Vector3.zero;
            if (index == 0)
            {
                if (computer.isClosed)
                {
                    side1 = points[index].position - points[index + 1].position;
                    side2 = points[index].position - points[points.Length - 2].position;
                }
                else
                {
                    side1 = points[0].position - points[1].position;
                    side2 = points[0].position - points[2].position;
                }
            }
            else if (index == points.Length - 1)
            {
                side1 = points[points.Length - 1].position - points[points.Length - 3].position;
                side2 = points[points.Length - 1].position - points[points.Length - 2].position;
            }
            else
            {
                side1 = points[index].position - points[index + 1].position;
                side2 = points[index].position - points[index - 1].position;
            }
            return Vector3.Cross(side1.normalized, side2.normalized).normalized;
        }

    }
}
#endif