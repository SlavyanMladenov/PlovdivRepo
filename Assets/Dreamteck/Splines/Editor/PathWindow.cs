using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Dreamteck.Splines {
    public class PathWindow : SplineEditorWindow {
        private SplineUser user;
        private Vector2 scroll = Vector2.zero;
        private Vector2 scroll2 = Vector2.zero;

        public override void init(Editor input, string name, Vector2 minSize)
        {
            base.init(input, name, minSize);
            user = (SplineUser)editor.target;
        }

        void OnGUI()
        {
            if (user == null) Close();
            Rect rect = new Rect(5, 5, position.width * 0.4f-10, 200);
            GUI.Box(rect, "Current Address");
            SplineComputer[] computers = user.address.GetComputers();
            Rect viewRect = new Rect(0, 0, rect.width - 20, user.address.depth + 1 * 20);
            scroll = GUI.BeginScrollView(rect, scroll, viewRect);
            for (int i = 0; i < user.address.depth; i++)
            {
                GUI.Label(new Rect(0, 50 + 20 * i, viewRect.width*0.75f, 20), computers[i+1].name);
                if(GUI.Button(new Rect(viewRect.width * 0.75f, 50 + 20 * i, viewRect.width * 0.25f, 20), "x"))
                {
                    user.ExitAddress(user.address.depth - i);
                    break;
                }
            }
            GUI.EndScrollView();
            rect = new Rect(position.width * 0.4f, 5, position.width * 0.6f-5, 200);
            GUI.Box(rect, "Available junctions");
            SplineComputer lastComp = user.address.GetLastComputer();
            float pos = 0f;
            if (user.address.depth > 0)
            {
                SplineComputer[] comps = user.address.GetComputers();
                pos = (float)comps[comps.Length - 2].nodeLinks[user.address.elements[user.address.depth - 1].junctionIndex].node.GetConnections()[user.address.elements[user.address.depth - 1].connectionIndex].pointIndex / (lastComp.pointCount - 1);
            }
            SplineJunctionAddress.Element[] available = lastComp.GetAvailableJunctionsAtPosition(pos, Spline.Direction.Forward);
            viewRect = new Rect(0, 0, rect.width - 20, user.address.depth + 1 * 20);
            scroll2 = GUI.BeginScrollView(rect, scroll2, viewRect);
            for (int i = 0; i < available.Length; i++) {
                if (GUI.Button(new Rect(0, 30 + 20 * i, viewRect.width, viewRect.height), lastComp.nodeLinks[available[i].junctionIndex].node.GetConnections()[available[i].connectionIndex].computer.name + " at point " + lastComp.nodeLinks[available[i].junctionIndex].pointIndex))
                {
                    user.EnterAddress(available[i]);
                    break;
                }
            }
            GUI.EndScrollView();
        }
    }
}
