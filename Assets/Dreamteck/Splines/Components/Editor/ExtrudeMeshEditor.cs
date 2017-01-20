#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(ExtrudeMesh), true)]
    public class ExtrudeMeshEditor : MeshGenEditor
    {

        public override void BaseGUI()
        {
            ExtrudeMesh user = (ExtrudeMesh)target;
            base.BaseGUI();
           
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);
            Object obj = user.sourceMesh;
            obj = EditorGUILayout.ObjectField("Source Mesh", obj, typeof(Object), true);
            if(user.sourceMesh == null)
            {
                EditorGUILayout.HelpBox("No mesh selected. Select a mesh from the field above.", MessageType.Warning);
            }
            if (obj != null)
            {
                if (obj is Mesh) user.sourceMesh = (Mesh)obj;
                else if (obj is GameObject)
                {
                    GameObject gameObj = (GameObject)obj;
                    MeshFilter filter = gameObj.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null) user.sourceMesh = filter.sharedMesh;
                    MeshRenderer meshRend = gameObj.GetComponent<MeshRenderer>();
                    if (meshRend != null)
                    {
                        MeshRenderer userRend = user.GetComponent<MeshRenderer>();
                        if (meshRend.sharedMaterials != null) userRend.sharedMaterials = meshRend.sharedMaterials;
                        else if (meshRend.materials != null) userRend.materials = meshRend.materials;
                    }
                }
            }
            
            user.axis = (ExtrudeMesh.Axis)EditorGUILayout.EnumPopup("Axis", user.axis);
            user.removeInnerFaces = EditorGUILayout.Toggle("Remove Inner Faces", user.removeInnerFaces);
            user.repeat = EditorGUILayout.IntField("Repeat", user.repeat);
            if (user.repeat < 1) user.repeat = 1;
            user.spacing = EditorGUILayout.Slider("Spacing", (float)user.spacing, 0f, 1f);
            user.scale = EditorGUILayout.Vector2Field("Scale", user.scale);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(user);
            }
        }

        public override void OnInspectorGUI()
        {
            showSize = false;
            showColor = false;
            showDoubleSided = false;
            showFlipFaces = false;
            BaseGUI();
            ShowInfo();
        }
    }
}
#endif