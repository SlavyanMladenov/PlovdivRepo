#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(SplineMeshGeneration))]
    public class MeshGenEditor : SplineUserEditor
    {
        protected bool showSize = true;
        protected bool showColor = true;
        protected bool showDoubleSided = true;
        protected bool showFlipFaces = true;
        protected bool showRotation = true;
        protected bool showInfo = false;
        protected bool showOffset = true;
        protected bool showNormalMethod = true;
        protected string[] normalMethods = new string[] { "Recalculate", "Spline normals" };
        private int framesPassed = 0;
        SplineMeshGeneration generator = null;

        public virtual void ShowInfo()
        {
            generator = (SplineMeshGeneration)target;
            EditorGUILayout.Space();
            showInfo = EditorGUILayout.Foldout(showInfo, "Info & Components");
            if (showInfo)
            {
                MeshFilter filter = generator.GetComponent<MeshFilter>();
                if (filter == null) return;
                MeshRenderer renderer = generator.GetComponent<MeshRenderer>();
                string str = "Vertices: " + filter.sharedMesh.vertexCount + "\r\nTriangles: " + (filter.sharedMesh.triangles.Length / 3);
                EditorGUILayout.HelpBox(str, MessageType.Info);
                bool showFilter = filter.hideFlags == HideFlags.None;
                bool last = showFilter;
                showFilter = EditorGUILayout.Toggle("Show Mesh Filter", showFilter);
                if(last != showFilter)
                {
                    if (showFilter) filter.hideFlags = HideFlags.None;
                    else filter.hideFlags = HideFlags.HideInInspector;
                }

                bool showRenderer = renderer.hideFlags == HideFlags.None;
                last = showRenderer;
                showRenderer = EditorGUILayout.Toggle("Show Mesh Renderer", showRenderer);
                if (last != showRenderer)
                {
                    if (showRenderer) renderer.hideFlags = HideFlags.None;
                    else renderer.hideFlags = HideFlags.HideInInspector;
                }
            }

        }
        
        public override void BaseGUI()
        {
            generator = (SplineMeshGeneration)target;
            base.BaseGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vertices", EditorStyles.boldLabel);
            if (showSize) generator.size = EditorGUILayout.FloatField("Size", generator.size);
            if(showColor) generator.color = EditorGUILayout.ColorField("Color", generator.color);
            if(showNormalMethod)generator.normalMethod = EditorGUILayout.Popup("Normal Method", generator.normalMethod, normalMethods);
            generator.optimize = EditorGUILayout.Toggle("Optimize", generator.optimize);
            if(showOffset) generator.offset = EditorGUILayout.Vector3Field("Offset", generator.offset);
            if(showRotation) generator.rotation = EditorGUILayout.Slider("Rotation", generator.rotation, -180f, 180f);

            if (showDoubleSided || showFlipFaces)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Faces", EditorStyles.boldLabel);
                if (showDoubleSided) generator.doubleSided = EditorGUILayout.Toggle("Double-sided", generator.doubleSided);
                if (!generator.doubleSided && showFlipFaces) generator.flipFaces = EditorGUILayout.Toggle("Flip faces", generator.flipFaces);
            }

            if(generator.GetComponent<MeshCollider>() != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mesh Collider", EditorStyles.boldLabel);
                generator.colliderUpdateRate = EditorGUILayout.FloatField("Collider Update Iterval", generator.colliderUpdateRate);
            }
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            generator = (SplineMeshGeneration)target;
            if (Application.isPlaying) return;
            framesPassed++;
            if(framesPassed >= 100)
            {
                framesPassed = 0;
                if (generator != null && generator.GetComponent<MeshCollider>() != null) generator.UpdateCollider();
            }
        }
        
        
        public override void OnInspectorGUI()
        {
            BaseGUI();
        }
        
        
        protected override void Awake()
        {
            generator = (SplineMeshGeneration)target;
            MeshRenderer rend = generator.GetComponent<MeshRenderer>();
            if (rend == null) return;
            if (rend.sharedMaterial == null) rend.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            base.Awake();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SplineMeshGeneration gen = (SplineMeshGeneration)target;
            if (gen == null) return;
            if (gen.GetComponent<MeshCollider>() != null) generator.UpdateCollider();
        }

        protected override void OnDelete()
        {
            base.OnDelete();
            if (generator == null) return;
            MeshFilter filter = generator.GetComponent<MeshFilter>();
            if (filter != null) filter.hideFlags = HideFlags.None;
            MeshRenderer renderer = generator.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.hideFlags = HideFlags.None;
        }
        
    }
}
#endif