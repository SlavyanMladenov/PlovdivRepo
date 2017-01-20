using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine.Events;
using System.Reflection;


namespace Dreamteck.Splines
{

    public static class SplineEditorGUI
    {
        public static GUIStyle defaultField;
        public static GUIStyle smallField;
        public static GUIStyle defaultButton;
        public static GUIStyle dropdownItem;
        public static GUIStyle bigButton;
        public static GUIStyle bigButtonSelected;
        public static GUIStyle labelText;
        public static GUIStyle whiteBox;
        public static Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);
        public static Color textColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        public static Color activeColor = new Color(1f, 1f, 1f, 1f);
        public static Color selectionColor = new Color(0f, 0.564f, 1f);
        public static Color blackColor = new Color(0, 0, 0, 0.7f);
        private static bool[] controlStates = new bool[0];
        private static int controlIndex = 0;
        public static float scale = 1f;
        public static Color buttonContentColor = Color.black;
        public static Color selectedButtonContentColor = new Color(1f, 1f, 1f, 0.95f);
        private static SplineTrigger.Type addTriggerType = SplineTrigger.Type.Double;
        private static bool initialized = false;
        private static Texture2D white = null;

        public static void Update()
        {
            controlStates = new bool[0];
            if (Application.isPlaying) initialized = false;
        }

        public static void Reset()
        {
            controlIndex = 0;
        }

        public static void Initialize()
        {
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            defaultButton = new GUIStyle(GUI.skin.GetStyle("button"));
            buttonContentColor = defaultButton.normal.textColor;

            //If the button text color is too dark, generate a brightened version
            float avg = (buttonContentColor.r + buttonContentColor.g + buttonContentColor.b) / 3f;
            if (avg <= 0.2f) buttonContentColor = new Color(0.2f, 0.2f, 0.2f);
            whiteBox = new GUIStyle(GUI.skin.GetStyle("box"));
            whiteBox.normal.background = white;
            defaultField = new GUIStyle(GUI.skin.GetStyle("textfield"));
            defaultField.normal.background = white;
            defaultField.normal.textColor = Color.white;
            defaultField.padding = new RectOffset(Mathf.RoundToInt(5 * scale), Mathf.RoundToInt(5 * scale), Mathf.RoundToInt(5 * scale), Mathf.RoundToInt(5 * scale));
            defaultField.alignment = TextAnchor.MiddleLeft;
            smallField = new GUIStyle(GUI.skin.GetStyle("textfield"));
            smallField.normal.background = white;
            smallField.normal.textColor = Color.white;
            smallField.padding = new RectOffset(Mathf.RoundToInt(2 * scale), Mathf.RoundToInt(2 * scale), Mathf.RoundToInt(2 * scale), Mathf.RoundToInt(2 * scale));
            smallField.alignment = TextAnchor.MiddleLeft;
            smallField.clipping = TextClipping.Clip;
            labelText = new GUIStyle(GUI.skin.GetStyle("label"));
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleRight;
            labelText.normal.textColor = Color.white;
            dropdownItem = new GUIStyle(GUI.skin.GetStyle("button"));
            dropdownItem.normal.background = white;
            dropdownItem.normal.textColor = Color.white;
            dropdownItem.padding = new RectOffset(Mathf.RoundToInt(10 * scale), 0, 0, 0);
            dropdownItem.alignment = TextAnchor.MiddleLeft;
            

            bigButton = new GUIStyle(GUI.skin.GetStyle("button"));
            bigButton.fontStyle = FontStyle.Bold;
            bigButton.normal.textColor = buttonContentColor;
            bigButton.padding = new RectOffset(3, 3, 3, 3);

            bigButtonSelected = new GUIStyle(GUI.skin.GetStyle("button"));
            bigButtonSelected.fontStyle = FontStyle.Bold;
            bigButtonSelected.normal.textColor = new Color(0.95f, 0.95f, 0.95f);
            bigButton.padding = new RectOffset(4, 4, 4, 4);


            bigButton.fontSize = Mathf.RoundToInt(30 * scale);
            bigButtonSelected.fontSize = Mathf.RoundToInt(30 * scale);
            bigButton.normal.textColor = Color.white;

            defaultButton.fontSize = Mathf.RoundToInt(14 * scale);
            dropdownItem.fontSize = Mathf.RoundToInt(12 * scale);
            labelText.fontSize = Mathf.RoundToInt(12 * scale);
            defaultField.fontSize = Mathf.RoundToInt(14 * scale);
            smallField.fontSize = Mathf.RoundToInt(11 * scale);
            if (!Application.isPlaying) initialized = true;
        }

        public static void SetScale(float s)
        {
            if(s != scale)
            {
                scale = s;
                Initialize();
            } scale = s;
        }


        public static bool BigButton(Rect position, GUIContent content, bool active = true, bool selected = false)
        {
            if (!initialized) Initialize();
            bool result = false;
            if (position.width < 30*scale) position.width = 30 * scale;
            if (position.height < 30 * scale) position.height = 30 * scale;
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = buttonContentColor;
            if (!active) GUI.color = inactiveColor;
            else
            {
                GUI.color = activeColor;
                if (selected)
                {
                    GUI.backgroundColor = selectionColor;
                    GUI.contentColor = selectedButtonContentColor;
                }
            }
            if (GUI.Button(position, content, selected ? bigButtonSelected : bigButton))
            {
                Event.current.Use();
                if (active) result = true;
            }
            GUI.backgroundColor = Color.white;
            GUI.contentColor = previousContentColor;
            return result;
        }

        public static bool Button(Rect position, string text, bool active = true, bool selected = false)
        {
            if (!initialized) Initialize();
            bool result = false;
            if (!active) GUI.color = inactiveColor;
            else
            {
                if (selected) GUI.backgroundColor = selectionColor;
                GUI.color = activeColor;
            }
            if (GUI.Button(position, text, defaultButton))
            {
                Event.current.Use();
                if (active) result = true;
            }
            GUI.backgroundColor = Color.white;
            return result;
        }

         public static bool DropArea<T>(Rect rect, out T[] content)
        {
            if (!initialized) Initialize();
            content = new T[0];
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!rect.Contains(Event.current.mousePosition)) return false;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        List<T> contentList = new List<T>();
                        foreach (object dragged_object in DragAndDrop.objectReferences)
                        {
                            if (dragged_object is GameObject)
                            {
                                GameObject gameObject = (GameObject)dragged_object;
                                if (PrefabUtility.GetPrefabType(gameObject) == PrefabType.None)
                                {
                                    if(gameObject.GetComponent<T>() != null) contentList.Add(gameObject.GetComponent<T>());
                                }
                            }
                        }
                        content = contentList.ToArray();
                        return true;
                    } else return false;
            }
            return false;
        }

        public static bool DropDown(Rect position, GUIStyle style, string[] options, bool active, ref int currentOption)
        {
            if (!initialized) Initialize();
            if (!active) GUI.color = inactiveColor;
            else GUI.color = activeColor;
            bool mouseHovers = false;
            HandleControlsCount();
            if (GUI.Button(position, options[currentOption] + "     ▼", style)) if (active) controlStates[controlIndex] = !controlStates[controlIndex];
            if (controlStates[controlIndex] && active)
            {
                GUI.BeginGroup(new Rect(position.x, position.y + position.height, position.width, position.height * options.Length));
                //GUI.color = new Color(0f, 0f, 0f, 0.5f);
                GUI.backgroundColor = blackColor;
                GUI.Box(new Rect(0, 0, position.width, position.height * options.Length), "", whiteBox); 
                if (new Rect(0, 0, position.width, position.height * options.Length).Contains(Event.current.mousePosition)) mouseHovers = true;
                for (int i = 0; i < options.Length; i++)
                {
                    Rect current = new Rect(0, position.height * i, position.width, position.height);
                    if (current.Contains(Event.current.mousePosition)) GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
                    else GUI.backgroundColor = Color.clear;
                    if (GUI.Button(current, options[i], dropdownItem))
                    {
                        currentOption = i;
                        controlStates[controlIndex] = false;
                    }
                }
                if (controlStates[controlIndex])
                {
                    if (Event.current.type == EventType.MouseDown) controlStates[controlIndex] = false;
                }
                GUI.backgroundColor = Color.white;
                GUI.EndGroup();
            }
            controlIndex++;
            return mouseHovers;
        }

        public static void Label(Rect position, string text, bool active = true, GUIStyle style = null)
        {
            if (!initialized) Initialize();
            if (style == null)
            {
                style = labelText;
            }
            if (!active) GUI.color = inactiveColor;
            else GUI.color = activeColor;
            GUI.color = new Color(0f, 0f, 0f, GUI.color.a * 0.5f);
            GUI.Label(new Rect(position.x-1, position.y+1, position.width, position.height), text, style);
            if (!active) GUI.color = inactiveColor;
            else GUI.color = activeColor;
            GUI.Label(position, text, style);
        }

        public static float FloatField(Rect position, float value, bool active = true, GUIStyle style = null)
        {
            if (!initialized) Initialize();
            HandleControlsCount();
            if (style == null) style = smallField;
            if (!active) GUI.color = inactiveColor;
            else GUI.color = activeColor;
            GUI.backgroundColor = blackColor;
            double result = value;
            string str = GUI.TextField(position, value.ToString() + (controlStates[controlIndex] ? "." : ""), style);
            controlStates[controlIndex] = str.EndsWith(".");
            str = CleanStringForFloat(str);
            GUI.backgroundColor = Color.white;
            controlIndex++;
            if (str == "") return 0f;
            else if (double.TryParse(str, out result)) return (float)result;
            else return value;
        }

        private static string CleanStringForFloat(string input)
        {
            if (!initialized) Initialize();
            if (Regex.Match(input, @"^-?[0-9]*(?:\.[0-9]*)?$").Success)
                return input;
            else
            {
                return "0";
            }
        }

        public static float FloatDrag(Rect position, float value)
        {
            if (!initialized) Initialize();
            HandleControlsCount();
            if (position.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    controlStates[controlIndex] = true;
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    value = 0f;
                    Event.current.Use();
                }

            }
            if (Event.current.type == EventType.mouseUp) controlStates[controlIndex] = false;
            if (controlStates[controlIndex])
            {
                float delta = Event.current.delta.x;
                float moveStep = Mathf.Clamp(Mathf.Floor(Mathf.Log10(Mathf.Abs(value)) + 1), 1f, 5f);
                float movePerPixel = delta * moveStep * 0.1f;
                value += movePerPixel;
            }
            controlIndex++;
            return value;
        }

       private static void HandleControlsCount() {
            if (controlIndex >= controlStates.Length)
            {
                bool[] newStates = new bool[controlStates.Length + 1];
                controlStates.CopyTo(newStates, 0);
                controlStates = newStates;
            }
        }

       public static double ScreenPointToSplinePercent(SplineComputer computer, Vector2 screenPoint)
        {
            if (!initialized) Initialize();
            SplinePoint[] points = computer.GetPoints();
            float closestDistance = (screenPoint - HandleUtility.WorldToGUIPoint(points[0].position)).sqrMagnitude;
            double closestPercent = 0.0;
            double add = computer.moveStep;
            if (computer.type == Spline.Type.Linear) add /= 2f;
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

        public static int PointSelectionMenu(SplineComputer computer, int selected = 0, string title = "Select point")
        {
            if (!initialized) Initialize();
            GUILayout.Box(title, GUILayout.Width(Screen.width - 45), GUILayout.Height(50));
            GUI.BeginGroup(GUILayoutUtility.GetLastRect());
            string[] options = new string[(computer.isClosed ? computer.pointCount - 1 : computer.pointCount) + 1];
            for (int i = 0; i < options.Length - 1; i++)
            {
                options[i + 1] = "Point " + i;
                if (computer.type == Spline.Type.Bezier) options[i + 1] = "Point " + i + " Bezier " + (computer.GetPoint(i, SplineComputer.Space.Local).type == SplinePoint.Type.Smooth ? "(smooth)" : "(broken)");
            }
            options[0] = "- Select -";
            int selection = EditorGUI.Popup(new Rect(10, 25, Screen.width - 65, 30), selected, options) - 1;
            GUI.EndGroup();
            return selection;
        }

        private static List<MethodInfo> GetVoidMethods(Object behavior)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            List<MethodInfo> methods = new List<MethodInfo>();
            methods.AddRange(behavior.GetType().GetMethods(flags));
            for (int i = methods.Count - 1; i >= 0; i--)
            {
                if (methods[i].ReturnType != typeof(void))
                {
                    methods.RemoveAt(i);
                } else{
                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length == 0) continue;
                    if (parameters.Length > 1) methods.RemoveAt(i);
                    else
                    {
                        System.Type paramType = parameters[0].ParameterType;
                        if (paramType != typeof(int) && paramType != typeof(float) && paramType != typeof(double) && paramType != typeof(string) && paramType != typeof(bool) && paramType != typeof(MonoBehaviour) && paramType != typeof(GameObject) && paramType != typeof(Transform))
                        {
                            methods.RemoveAt(i);
                        }
                    }
                }
            }
            return methods;
        }

        public static void ActionField(SplineAction action)
        {
            if (!initialized) Initialize();
            EditorGUILayout.BeginHorizontal();
            action.target = (Object)EditorGUILayout.ObjectField(action.target, typeof(Object), true, GUILayout.MinWidth(120));
            if (action.target == null)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }
            GameObject gameObject = null;
            Transform transform = null;
            Component component = null;
            try
            {
                gameObject = (GameObject)action.target;
                transform = gameObject.transform;
            }
            catch
            {
                try
                {
                    transform = (Transform)action.target;
                    gameObject = transform.gameObject;
                }
                catch 
                {
                    try
                    {
                        component = (Component)action.target;
                        transform = component.transform;
                        gameObject = component.gameObject;
                    } catch(System.InvalidCastException ex3)
                    {
                        Debug.LogError(ex3.Message);
                        Debug.LogError("Supplied object is not a GameObject and is not a component");
                    }
                }
            }

            List<MethodInfo> methods = new List<MethodInfo>();
            List<string> names = new List<string>();
            int selected = 0;
            MethodInfo method = action.GetMethod();
            List<Object> targets = new List<Object>();
            if (gameObject != null)
            {
                List<MethodInfo> addRange = GetVoidMethods(gameObject);
                for (int i = 0; i < addRange.Count; i++)
                {
                    names.Add("GameObject/" + addRange[i].Name);
                    targets.Add(gameObject);
                    if (method == addRange[i]) selected = names.Count - 1;
                }
                methods.AddRange(addRange);
                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    string typeName = components[i].GetType().ToString();
                    addRange = GetVoidMethods(components[i]);
                    methods.AddRange(addRange);
                    for (int n = 0; n < addRange.Count; n++)
                    {
                        names.Add(typeName + "/" + addRange[n].Name);
                        targets.Add(components[i]);
                        if (method == addRange[n]) selected = names.Count - 1;
                    }
                }
            }

            selected = EditorGUILayout.Popup(selected, names.ToArray(), GUILayout.MinWidth(120));
            if (selected >= 0)
            {
                action.target = targets[selected];
                action.SetMethod(methods[selected]);
            }
            if (method != null)
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0)
                {
                    System.Type paramType = parameters[0].ParameterType;
                    if (paramType == typeof(int)) action.intValue = EditorGUILayout.IntField(action.intValue, GUILayout.MaxWidth(120));
                    else if (paramType == typeof(float)) action.floatValue = EditorGUILayout.FloatField(action.floatValue);
                    else if (paramType == typeof(double)) action.doubleValue = EditorGUILayout.FloatField((float)action.doubleValue);
                    else if (paramType == typeof(bool)) action.boolValue = EditorGUILayout.Toggle(action.boolValue);
                    else if (paramType == typeof(string)) action.stringValue = EditorGUILayout.TextField(action.stringValue);
                    else if (paramType == typeof(GameObject)) action.goValue = (GameObject)EditorGUILayout.ObjectField(action.goValue, typeof(GameObject), true);
                    else if (paramType == typeof(Transform)) action.transformValue = (Transform)EditorGUILayout.ObjectField(action.transformValue, typeof(Transform), true);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void TriggerArray(ref SplineTrigger[] triggers, ref int open) {
            if (!initialized) Initialize();
            EditorGUILayout.BeginVertical();
            for(int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] == null)
                {
                    GUILayout.Box("", GUILayout.Width(Screen.width-30), GUILayout.Height(20));
                    Rect rect = GUILayoutUtility.GetLastRect();
                    GUI.BeginGroup(rect);
                    GUI.Label(new Rect(25, 2, rect.width - 90, 16), "NULL");
                    if (GUI.Button(new Rect(rect.width - 62, 2, 45, 16), "x"))
                    {
                        SplineTrigger[] newTriggers = new SplineTrigger[triggers.Length - 1];
                        for (int n = 0; n < triggers.Length; n++)
                        {
                            if (n < i) newTriggers[n] = triggers[n];
                            else if (n == i) continue;
                            else newTriggers[n - 1] = triggers[n];
                        }
                        triggers = newTriggers;
                    }
                    GUI.EndGroup();
                    continue;
                }
                Color col = new Color(triggers[i].color.r, triggers[i].color.g, triggers[i].color.b);
                if (open == i) col.a = 1f;
                else col.a = 0.6f;
                GUI.color = col;
                GUILayout.Box("", GUILayout.Width(Screen.width-30), GUILayout.Height(20));
                GUI.color = Color.white;
                Rect boxRect = GUILayoutUtility.GetLastRect();
                GUI.BeginGroup(boxRect);
                Rect nameRect = new Rect(25, 2, boxRect.width - 90, 16);
                if (open == i) triggers[i].name = GUI.TextField(nameRect, triggers[i].name);
                else
                {
                    GUI.Label(nameRect, triggers[i].name);
                    if(nameRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
                    {
                        open = i;
                        GUI.EndGroup();
                        break;
                    }
                }
                triggers[i].enabled = GUI.Toggle(new Rect(2, 2, 21, 16), triggers[i].enabled, "");
                triggers[i].color = EditorGUI.ColorField(new Rect(boxRect.width - 62, 2, 45, 16), triggers[i].color);
                GUI.EndGroup();
                if (i != open) continue;
                EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                        triggers[i].position = EditorGUILayout.Slider("Position", (float)triggers[i].position, 0f, 1f);
                        triggers[i].type = (SplineTrigger.Type)EditorGUILayout.EnumPopup("Type", triggers[i].type);
                GUILayout.Label("Actions");
                for (int n = 0; n < triggers[i].actions.Length; n++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("x", GUILayout.Width(20)))
                    {
                        SplineAction[] newActions = new SplineAction[triggers[i].actions.Length - 1];
                        for (int x = 0; x < triggers[i].actions.Length; x++)
                        {
                            if (x < n) newActions[x] = triggers[i].actions[x];
                            else if (x == n) continue;
                            else newActions[x - 1] = triggers[i].actions[x];
                        }
                        triggers[i].actions = newActions;
                        break;
                    }
                    ActionField(triggers[i].actions[n]);
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("New Action"))
                { 
                    SplineAction[] newActions = new SplineAction[triggers[i].actions.Length + 1];
                    triggers[i].actions.CopyTo(newActions, 0);
                    newActions[newActions.Length - 1] = new SplineAction();
                    triggers[i].actions = newActions;
                }


                EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    open = -1;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    SplineTrigger[] newTriggers = new SplineTrigger[triggers.Length - 1];
                    for (int n = 0; n < triggers.Length; n++)
                    {
                        if (n < i) newTriggers[n] = triggers[n];
                        else if (n == i) continue;
                        else newTriggers[n - 1] = triggers[n];
                    }
                    triggers = newTriggers;
                }
                if (GUILayout.Button("d", GUILayout.Width(20)))
                {
                    SplineTrigger newTrigger = ScriptableObject.CreateInstance<SplineTrigger>();
                    newTrigger = (SplineTrigger)GameObject.Instantiate(triggers[i]);
                    newTrigger.name = "Trigger " + (triggers.Length + 1);
                    SplineTrigger[] newTriggers = new SplineTrigger[triggers.Length + 1];
                    triggers.CopyTo(newTriggers, 0);
                    newTriggers[newTriggers.Length - 1] = newTrigger;
                    triggers = newTriggers;
                    open = triggers.Length - 1;
                }
                if (i > 0)
                {
                    if (GUILayout.Button("▲", GUILayout.Width(20)))
                    {
                        SplineTrigger temp = triggers[i - 1];
                        triggers[i - 1] = triggers[i];
                        triggers[i] = temp;
                    }
                }
                if (i < triggers.Length - 1)
                {
                    if (GUILayout.Button("▼", GUILayout.Width(20)))
                    {
                        SplineTrigger temp = triggers[i + 1];
                        triggers[i + 1] = triggers[i];
                        triggers[i] = temp;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add new"))
            {
                SplineTrigger newTrigger = ScriptableObject.CreateInstance<SplineTrigger>();
                newTrigger.init(addTriggerType);
                newTrigger.name = "Trigger " + (triggers.Length+1);
                SplineTrigger[] newTriggers = new SplineTrigger[triggers.Length+1];
                triggers.CopyTo(newTriggers, 0);
                newTriggers[newTriggers.Length - 1] = newTrigger;
                triggers = newTriggers;
            }
            addTriggerType = (SplineTrigger.Type)EditorGUILayout.EnumPopup(addTriggerType);
            EditorGUILayout.EndHorizontal();
        }

    }
}
