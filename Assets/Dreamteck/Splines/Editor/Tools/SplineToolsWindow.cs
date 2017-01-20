#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;
namespace Dreamteck.Splines
{
    public class SplineToolsWindow : EditorWindow
    {
        private static ISplineTool[] tools;
        private int toolIndex = -1;
        private Vector2 scroll = Vector2.zero;
        [MenuItem("Window/Dreamteck/Spline Tools")]
        static void Init()
        {
            SplineToolsWindow window = (SplineToolsWindow)EditorWindow.GetWindow(typeof(SplineToolsWindow));
            window.name = "Spline tools";
            List<Type> types = FindDerivedClasses.GetAllDerivedClasses(typeof(ISplineTool));
            tools = new ISplineTool[types.Count];
            int count = 0;
            foreach(Type t in types)
            {
                tools[count] = (ISplineTool)Activator.CreateInstance(t);
                count++;
            }
            window.Show();
        }

        void OnDestroy()
        {
            if (toolIndex >= 0 && toolIndex < tools.Length) tools[toolIndex].Close();
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginScrollView(scroll, GUILayout.Width(position.width * 0.35f), GUILayout.Height(position.height-10));
            if (tools == null) Init();
            for(int i = 0; i < tools.Length; i ++)
            {
                if (toolIndex == i) GUILayout.Label(tools[i].GetName());
                else if (GUILayout.Button(tools[i].GetName()))
                {
                    if (toolIndex >= 0 && toolIndex < tools.Length) tools[toolIndex].Close();
                    toolIndex = i;
                }
            }
            GUILayout.EndScrollView();

            if(toolIndex >= 0 && toolIndex < tools.Length)
            {
                GUILayout.BeginVertical();
                tools[toolIndex].Draw(new Rect(position.width * 0.35f, 0, position.width * 0.65f-5, position.height - 10));
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
        
    }
}
#endif