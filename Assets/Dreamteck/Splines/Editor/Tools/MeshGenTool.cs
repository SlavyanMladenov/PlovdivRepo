using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

namespace Dreamteck.Splines
{
    public class MeshGenTool : SplineTool
    {
        protected GameObject obj;
        protected SplineMeshGeneration generator;
        protected MeshFilter meshFilter;
        protected MeshRenderer meshRenderer;
        protected Vector3 uvScale = Vector3.one;
        protected Vector2 uvOffset = Vector2.zero;
        protected TS_Mesh tsMesh;
        protected MeshUtility meshUtility;
        protected Material material;
        protected int normalMethod = 0;
        protected string[] normalMethods = new string[] {"Recalculate", "Spline normals"};

        protected bool showSize = true;
        protected bool showColor = true;
        protected bool showDoubleSided = true;
        protected bool showFlipFaces = true;
        protected bool showRotation = true;
        protected bool showInfo = false;
        protected bool showNormalMethod = true;

        protected override void DrawGUI()
        {
            base.DrawGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
            material = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vertices", EditorStyles.boldLabel);
            if (showSize) generator.size = EditorGUILayout.FloatField("Size", generator.size);
            if (showColor) generator.color = EditorGUILayout.ColorField("Color", generator.color);
            if (showNormalMethod) generator.normalMethod = EditorGUILayout.Popup("Normal Method", generator.normalMethod, normalMethods);
            generator.offset = EditorGUILayout.Vector3Field("Offset", generator.offset);
            if (showRotation) generator.rotation = EditorGUILayout.Slider("Rotation", generator.rotation, -180f, 180f);

            if (showDoubleSided || showFlipFaces)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Faces", EditorStyles.boldLabel);
                if (showDoubleSided) generator.doubleSided = EditorGUILayout.Toggle("Double-sided", generator.doubleSided);
                if (!generator.doubleSided && showFlipFaces) generator.flipFaces = EditorGUILayout.Toggle("Flip faces", generator.flipFaces);
            }

            if (GUI.changed)
            {
                BuildMesh();
                EditorUtility.SetDirty(generator.gameObject);
            }
        }

        protected override void LoadValues(string prefix)
        {
            base.LoadValues(prefix);
            if (EditorPrefs.HasKey(prefix + "_size")) generator.size = EditorPrefs.GetFloat(prefix + "_size");
            if (EditorPrefs.HasKey(prefix + "_flip")) generator.flipFaces = EditorPrefs.GetInt(prefix + "_flip")==1;
            if (EditorPrefs.HasKey(prefix + "_double")) generator.doubleSided = EditorPrefs.GetInt(prefix + "_double")==1;
            if (EditorPrefs.HasKey(prefix + "_colorR")) generator.color = new Color(EditorPrefs.GetFloat(prefix + "_colorR"), EditorPrefs.GetFloat(prefix + "_colorG"), EditorPrefs.GetFloat(prefix + "_colorB"));
            if (EditorPrefs.HasKey(prefix + "_offsetX")) generator.offset = new Vector3(EditorPrefs.GetFloat(prefix + "_offsetX"), EditorPrefs.GetFloat(prefix + "_offsetY"), EditorPrefs.GetFloat(prefix + "_offsetY"));
            if (EditorPrefs.HasKey(prefix + "_uvScaleX")) uvScale = new Vector3(EditorPrefs.GetFloat(prefix + "_uvScaleX"), EditorPrefs.GetFloat(prefix + "_uvScaleY"), EditorPrefs.GetFloat(prefix + "_uvScaleZ"));
            if (EditorPrefs.HasKey(prefix + "_uvOffsetX")) uvOffset = new Vector2(EditorPrefs.GetFloat(prefix + "_uvOffsetX"), EditorPrefs.GetFloat(prefix + "_uvOffsetY"));
            if (EditorPrefs.HasKey(prefix + "_normalMethod")) normalMethod = EditorPrefs.GetInt(prefix + "_normalMethod");
            if (EditorPrefs.HasKey(prefix + "_rotation")) generator.rotation = EditorPrefs.GetFloat(prefix + "_rotation");
        }

        protected override void SaveValues(string prefix)
        {
            base.SaveValues(prefix);
            EditorPrefs.SetFloat(prefix + "_size", generator.size);
            EditorPrefs.SetInt(prefix + "_flip", generator.flipFaces ? 1 : 0);
            EditorPrefs.SetInt(prefix + "_double", generator.doubleSided ? 1 : 0);
            EditorPrefs.SetFloat(prefix + "_colorR", generator.color.r);
            EditorPrefs.SetFloat(prefix + "_colorG", generator.color.g);
            EditorPrefs.SetFloat(prefix + "_colorB", generator.color.b);
            EditorPrefs.SetFloat(prefix + "_offsetX", generator.offset.x);
            EditorPrefs.SetFloat(prefix + "_offsetY", generator.offset.y);
            EditorPrefs.SetFloat(prefix + "_offsetZ", generator.offset.z);
            EditorPrefs.SetFloat(prefix + "_uvScaleX", uvScale.x);
            EditorPrefs.SetFloat(prefix + "_uvScaleY", uvScale.y);
            EditorPrefs.SetFloat(prefix + "_uvScaleZ", uvScale.z);
            EditorPrefs.SetFloat(prefix + "_uvOffsetX", uvOffset.x);
            EditorPrefs.SetFloat(prefix + "_uvOffsetY", uvOffset.y);
            EditorPrefs.SetInt(prefix + "_normalMethod", normalMethod);
            EditorPrefs.SetFloat(prefix + "_rotation", generator.rotation);
        }

        protected void Save()
        {
            if (EditorUtility.DisplayDialog("Generate lightmap UVs ?", "Do you want to generate lightmap UVs for baked lighting ?", "Yes", "No"))
            {
                Unwrapping.GenerateSecondaryUVSet(meshFilter.sharedMesh);
            }
            if (EditorUtility.DisplayDialog("Save mesh as asset ?", "Do you want to save this mesh as an asset for use in other scenes and prefabs ?", "Yes", "No"))
            {
                string path = EditorUtility.SaveFilePanel("Save "+ meshFilter.sharedMesh.name+ ".obj", Application.dataPath, meshFilter.sharedMesh.name + ".obj", "obj");
                string relativepath = "Assets" + path.Substring(Application.dataPath.Length);
                string objString = MeshUtility.ToOBJString(meshFilter.sharedMesh, meshRenderer.sharedMaterials);
                File.WriteAllText(path, objString);
                AssetDatabase.ImportAsset(relativepath, ImportAssetOptions.ForceSynchronousImport);
#if UNITY_5_0
                meshFilter.sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(relativepath, typeof(Mesh));
#else 
                meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(relativepath);
#endif
                //AssetDatabase.CreateAsset(mesh, relativepath);
            }
            meshFilter.hideFlags = HideFlags.None;
            meshRenderer.hideFlags = HideFlags.None;
            GameObject.DestroyImmediate(generator); 
            obj.transform.parent = computer.transform;
            obj = null;
            meshFilter = null;
            meshRenderer = null;
            tsMesh = null;
        }

        protected void Cancel()
        {
            SplineUser user = obj.GetComponent<SplineUser>();
            if (user != null) user.computer.Unsubscribe(user);
            GameObject.DestroyImmediate(obj);
        }

        protected virtual void BuildObject(SplineComputer computer, string name)
        {
            if (obj != null) GameObject.DestroyImmediate(obj);
            obj = new GameObject(name);
            obj.transform.position = computer.transform.position;
            obj.transform.rotation = computer.transform.rotation;
            obj.transform.localScale = computer.transform.localScale;
            meshFilter = obj.AddComponent<MeshFilter>();
            meshRenderer = obj.AddComponent<MeshRenderer>();
            if(material == null) material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            meshRenderer.sharedMaterial = material;
            obj.transform.parent = computer.transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }

        protected virtual void BuildMesh()
        {
            generator.resolution = resolution;
            generator.clipFrom = clipFrom;
            generator.clipTo = clipTo;
            meshRenderer.material = material;
            generator.enabled = false;
            generator.Rebuild(true);
        }

        protected override void Rebuild()
        {
            base.Rebuild();
            if (obj != null && generator != null) BuildMesh();
        }
    }
}
