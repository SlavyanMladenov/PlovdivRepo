using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Dreamteck.Splines
{
    public class SplineEditorToolbar
    {
        private SplineEditor editor;
        private SplineComputer computer;
        public bool mouseHovers = false;
        public int setNormalMode = 0;
        private float scale = 1f;
        PresetsWindow presetWindow = null;

         GUIContent presetButtonContent = new GUIContent("P", "Open Primitives and Presets");
         GUIContent moveButtonContent = new GUIContent("M", "Move points");
         GUIContent rotateButtonContent = new GUIContent("R", "Rotate points");
         GUIContent scaleButtonContent = new GUIContent("S", "Scale points");
         GUIContent normalsButtonContent = new GUIContent("N", "Edit point normals");
        GUIContent addButtonContent = new GUIContent("+", "Enter point creation mode");
        GUIContent removeButtonContent = new GUIContent("-", "Enter point removal mode");

        public SplineEditorToolbar(SplineEditor edit, SplineComputer comp)
        {
            editor = edit;
            computer = comp;
            Texture2D tex = LoadTexture("presets.png");
            if (tex != null) { presetButtonContent.image = tex; presetButtonContent.text = ""; }

             tex = LoadTexture("move.png");
            if (tex != null) { moveButtonContent.image = tex; moveButtonContent.text = ""; }

             tex = LoadTexture("rotate.png");
            if (tex != null) { rotateButtonContent.image = tex; rotateButtonContent.text = ""; }

             tex = LoadTexture("scale.png");
            if (tex != null) { scaleButtonContent.image = tex; scaleButtonContent.text = ""; }

            tex = LoadTexture("normals.png");
            if (tex != null) { normalsButtonContent.image = tex; normalsButtonContent.text = ""; }
        }

        private Texture2D LoadTexture(string name)
        {
            string path = Application.dataPath + "/Dreamteck/Splines/Editor/Icons";
            if (!Directory.Exists(path)) { Debug.Log(path); return null; }
            if (!File.Exists(path + "/" + name)) return null;
            byte[] bytes = File.ReadAllBytes(path + "/" + name);
            Texture2D result = new Texture2D(1, 1);
            result.LoadImage(bytes);
            return result;
        }


        public void Close()
        {
            if (presetWindow != null) presetWindow.Close();
        }

        public void Draw()
        {
            if (Screen.width < 600) scale = 0.65f;
            else scale = 1f;
            SplineEditorGUI.SetScale(scale);
            SplineEditorGUI.scale = scale;
            mouseHovers = false;
            if (editor.tool == SplineEditor.PointTool.Create) Create(Mathf.RoundToInt(44*scale));
            if (editor.tool == SplineEditor.PointTool.NormalEdit) Normals(Mathf.RoundToInt(44 * scale));
            if(editor.tool == SplineEditor.PointTool.Scale) Scale(Mathf.RoundToInt(44 * scale));
            if (editor.tool == SplineEditor.PointTool.Rotate) Rotate(Mathf.RoundToInt(44 * scale));
            Main();
        }
         
        private void Main() {
            Rect barRect = new Rect(0f, 0f, Screen.width, 45*scale);
            if (barRect.Contains(Event.current.mousePosition)) mouseHovers = true;
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            GUI.Box(barRect, "", SplineEditorGUI.whiteBox);
            GUI.color = SplineEditorGUI.activeColor;
            if (computer.hasMorph && !MorphWindow.editShapeMode)
            {
                SplineEditorGUI.Label(new Rect(Screen.width / 2f - 200f, 10, 400, 30), "Editing unavailable outside of morph states.");
                return;
            }
            if (computer.hasMorph)
            {
                if (editor.tool == SplineEditor.PointTool.Create || editor.tool == SplineEditor.PointTool.Delete) editor.tool = SplineEditor.PointTool.None;
            }
            else
            {
                if (SplineEditorGUI.BigButton(new Rect(5 * scale, 5 * scale, 35 * scale, 35 * scale), addButtonContent, true, editor.tool == SplineEditor.PointTool.Create))
                {
                    if (editor.tool != SplineEditor.PointTool.Create) editor.tool = SplineEditor.PointTool.Create;
                    else editor.tool = SplineEditor.PointTool.None;
                }
                if (SplineEditorGUI.BigButton(new Rect(45 * scale, 5 * scale, 35 * scale, 35 * scale), removeButtonContent, computer.pointCount > 0, editor.tool == SplineEditor.PointTool.Delete))
                {
                    if (editor.tool != SplineEditor.PointTool.Delete) editor.tool = SplineEditor.PointTool.Delete;
                    else editor.tool = SplineEditor.PointTool.None;
                }
                if (SplineEditorGUI.BigButton(new Rect(85 * scale, 5 * scale, 35 * scale, 35 * scale), presetButtonContent, true, presetWindow != null))
                {
                    if (presetWindow == null)
                    {
                        presetWindow = EditorWindow.GetWindow<PresetsWindow>();
                        presetWindow.init(editor, "Primitives & Presets", new Vector3(200, 200));
                    }
                }
            }

            if (SplineEditorGUI.BigButton(new Rect(150 * scale, 5 * scale, 35 * scale, 35 * scale), moveButtonContent, editor.HasSelection(), editor.tool == SplineEditor.PointTool.Move)) {
                if (editor.tool != SplineEditor.PointTool.Move) editor.ToggleMoveTool();
                else editor.tool = SplineEditor.PointTool.None;
            }
            if (SplineEditorGUI.BigButton(new Rect(190 * scale, 5 * scale, 35 * scale, 35 * scale), rotateButtonContent, editor.HasSelection(), editor.tool == SplineEditor.PointTool.Rotate))
            {
                if (editor.tool != SplineEditor.PointTool.Rotate) editor.ToggleRotateTool();
                else editor.tool = SplineEditor.PointTool.None;
            }
            if (SplineEditorGUI.BigButton(new Rect(230 * scale, 5 * scale, 35 * scale, 35 * scale), scaleButtonContent, editor.HasSelection(), editor.tool == SplineEditor.PointTool.Scale))
            {
                if (editor.tool != SplineEditor.PointTool.Scale) editor.ToggleScaleTool();
                else editor.tool = SplineEditor.PointTool.None;
            }
            if (SplineEditorGUI.BigButton(new Rect(270 * scale, 5 * scale, 35 * scale, 35 * scale), normalsButtonContent, editor.HasSelection(), editor.tool == SplineEditor.PointTool.NormalEdit))
            {
                if (editor.tool != SplineEditor.PointTool.NormalEdit) editor.tool = SplineEditor.PointTool.NormalEdit;
                else editor.tool = SplineEditor.PointTool.None;
            }


            int operation = 0;
            List<string> options = new List<string>();
            options.Add("Operations");
            if (editor.selectedPointsCount > 0) { options.Add("Center to Transform"); }
            if (editor.selectedPointsCount >= 2)
            {
                options.Add("Flat X");
                options.Add("Flat Y");
                options.Add("Flat Z");
                options.Add("Mirror X");
                options.Add("Mirror Y");
                options.Add("Mirror Z");
            }
            if (editor.selectedPointsCount >= 3) options.Add("Distribute evenly");
            bool hover = SplineEditorGUI.DropDown(new Rect(Screen.width - (190 * scale+100), 10 * scale, 150 * scale, 25 * scale), SplineEditorGUI.defaultButton, options.ToArray(), editor.HasSelection(), ref operation);
            if (hover) mouseHovers = true;
            if (operation > 0)
            {
                if (editor.selectedPointsCount >= 2)
                {
                    if (operation == 1) editor.CenterSelection();
                    else if (operation <= 4) editor.FlatSelection(operation - 2);
                    else if (operation <= 7) editor.MirrorSelection(operation - 5);
                    else if (operation == 8) editor.DistributeEvenly();
                } 
            }
            GUI.color = SplineEditorGUI.activeColor;
            ((SplineComputer)editor.target).editorPathColor = EditorGUI.ColorField(new Rect(Screen.width - (30 * scale + 100), 13 * scale, 40 * scale, 20 * scale), ((SplineComputer)editor.target).editorPathColor);
        }

        private void Create(int verticalOffset)
        {
            Rect barRect = new Rect(0f, verticalOffset * scale, Screen.width, 35 * scale);
            if (barRect.Contains(Event.current.mousePosition)) mouseHovers = true;
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            GUI.Box(barRect, "", SplineEditorGUI.whiteBox);
            GUI.color = SplineEditorGUI.activeColor;
            SplineEditorGUI.Label(new Rect(5 * scale, verticalOffset+5 * scale, 105 * scale, 25 * scale), "Place method:", true);
            bool hover = SplineEditorGUI.DropDown(new Rect(115 * scale, verticalOffset+5 * scale, 160 * scale, 25 * scale), SplineEditorGUI.defaultButton, new string[] { "Camera Plane", "Insert", "Surface", "Plane-X", "Plane-Y", "Plane-Z" }, true, ref editor.createPointMode);
            if (hover) mouseHovers = true;
            SplineEditorGUI.Label(new Rect(300 * scale, verticalOffset+5 * scale, 135 * scale, 25 * scale), "Normal orientation:", true);
            hover = SplineEditorGUI.DropDown(new Rect(440 * scale, verticalOffset+5 * scale, 160 * scale, 25 * scale), SplineEditorGUI.defaultButton, new string[] { "Auto", "Look at Camera", "Align with Camera", "Calculate", "Left", "Right", "Up", "Down", "Forward", "Back" }, true, ref editor.createNormalMode);
            if (hover) mouseHovers = true;
            bool showNormalField = false;
            if (editor.createPointMode == 0)
            {
                SplineEditorGUI.Label(new Rect(620 * scale, verticalOffset+5 * scale, 80 * scale, 30 * scale), "Far plane:", true);
                showNormalField = true;
            }
            

            if (editor.createPointMode == 2)
            {
                SplineEditorGUI.Label(new Rect(620 * scale, verticalOffset+5 * scale, 100 * scale, 30 * scale), "Normal offset:", true);
                showNormalField = true;
            }

            if (editor.createPointMode >= 3 && editor.createPointMode <= 5)
            {
                SplineEditorGUI.Label(new Rect(620 * scale, verticalOffset+5 * scale, 80 * scale, 30 * scale), "Grid offset:", true);
                showNormalField = true;
            }
            

            if (showNormalField) {
                editor.createPointOffset = SplineEditorGUI.FloatField(new Rect(720 * scale, verticalOffset+5 * scale, 70 * scale, 25 * scale), editor.createPointOffset);
                editor.createPointOffset = SplineEditorGUI.FloatDrag(new Rect(620 * scale, verticalOffset+5 * scale, 80 * scale, 25 * scale), editor.createPointOffset);
                if (editor.createPointOffset < 0f && editor.createPointMode < 3) editor.createPointOffset = 0f;
            }

        }

        private void Normals(int verticalOffset)
        {
            Rect barRect = new Rect(0f, verticalOffset * scale, Screen.width, 35 * scale);
            if (barRect.Contains(Event.current.mousePosition)) mouseHovers = true;
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            GUI.Box(barRect, "", SplineEditorGUI.whiteBox);
            GUI.color = SplineEditorGUI.activeColor;
            if(SplineEditorGUI.Button(new Rect(5 * scale, verticalOffset+5 * scale, 130 * scale, 25 * scale), "Set Normals:"))
            {
                editor.SetSelectedNormals(setNormalMode + 1);
            }
            bool hover =SplineEditorGUI.DropDown(new Rect(160 * scale, verticalOffset+5 * scale, 150 * scale, 25 * scale), SplineEditorGUI.defaultButton, new string[] {"At Camera", "Align with Camera", "Calculate", "Left", "Right", "Up", "Down", "Forward", "Back", "Inverse"}, true, ref setNormalMode);
            if (hover) mouseHovers = true;
        }


        private void Scale(int verticalOffset)
        {
            Rect barRect = new Rect(0f, verticalOffset, Screen.width, 35 * scale);
            if (barRect.Contains(Event.current.mousePosition)) mouseHovers = true;
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            GUI.Box(barRect, "", SplineEditorGUI.whiteBox);
            GUI.color = SplineEditorGUI.activeColor;
            SplineEditorGUI.Label(new Rect(5 * scale, verticalOffset + 5 * scale, 90 * scale, 25 * scale), "Scale sizes", true);
            editor.scaleSize = GUI.Toggle(new Rect(100 * scale, verticalOffset + 10 * scale, 25 * scale, 25 * scale), editor.scaleSize, "");

            SplineEditorGUI.Label(new Rect(110 * scale, verticalOffset + 5 * scale, 120 * scale, 25 * scale), "Scale tangents", true);
            editor.scaleTangents = GUI.Toggle(new Rect(235 * scale, verticalOffset + 10 * scale, 25 * scale, 25 * scale), editor.scaleTangents, "");
        }

        private void Rotate(int verticalOffset)
        {
            Rect barRect = new Rect(0f, verticalOffset, Screen.width, 35 * scale);
            if (barRect.Contains(Event.current.mousePosition)) mouseHovers = true;
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            GUI.Box(barRect, "", SplineEditorGUI.whiteBox);
            GUI.color = SplineEditorGUI.activeColor;
            SplineEditorGUI.Label(new Rect(5 * scale, verticalOffset + 5 * scale, 120 * scale, 25 * scale), "Rotate normals", true);
            editor.rotateNormal = GUI.Toggle(new Rect(130 * scale, verticalOffset + 10 * scale, 25 * scale, 25 * scale), editor.rotateNormal, "");

            SplineEditorGUI.Label(new Rect(140 * scale, verticalOffset + 5 * scale, 120 * scale, 25 * scale), "Rotate tangents", true);
            editor.rotateTangents = GUI.Toggle(new Rect(265 * scale, verticalOffset + 10 * scale, 25 * scale, 25 * scale), editor.rotateTangents, "");
        }
    }
}
