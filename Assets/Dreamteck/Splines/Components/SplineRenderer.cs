using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Dreamteck.Splines;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Spline Renderer")]
    [ExecuteInEditMode]
    public class SplineRenderer : SplineMeshGeneration
    {
        public int slices
        {
            get { return _slices; }
            set
            {
                if (value != _slices)
                {
                    if (value < 1) value = 1;
                    _slices = value;
                    Rebuild(false);
                }
            }
        }

        public Vector2 uvScale
        {
            get { return _uvScale; }
            set
            {
                if (value != _uvScale)
                {
                    _uvScale = value;
                    Rebuild(false);
                }
            }
        }

        public Vector2 uvOffset
        {
            get { return _uvOffset; }
            set
            {
                if (value != _uvOffset)
                {
                    _uvOffset = value;
                    Rebuild(false);
                }
            }
        }

        public bool uniformUVs
        {
            get { return _uniformUVs; }
            set
            {
                if (value != _uniformUVs)
                {
                    _uniformUVs = value;
                    Rebuild(false);
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private int _slices = 1;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvScale = Vector3.one;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvOffset = Vector2.zero;
        [SerializeField]
        [HideInInspector]
        private Vector3 vertexDirection = Vector3.up;
        [SerializeField]
        [HideInInspector]
        private bool _uniformUVs = false;
        private bool orthographic = false;
        private bool init = false;

        protected override void Awake()
        {
            base.Awake();
            mesh.name = "spline";
        }

        void Start()
        {
            if (Camera.current != null) orthographic = Camera.current.orthographic;
        }

        protected override void BuildMesh()
        {
            base.BuildMesh();
            GenerateVertices(vertexDirection, orthographic);
            tsMesh.triangles = MeshUtility.GeneratePlaneTriangles(_slices, clippedSamples.Length, false);
        }

        public void RenderWithCamera(Camera cam)
        {
            if (samples.Length == 0) return;
            if (cam != null)
            {
                if (cam.orthographic) vertexDirection = -cam.transform.forward;
                else vertexDirection = cam.transform.position;
            }
            orthographic = cam.orthographic;
            BuildMesh();
            WriteMesh();
        }

        void OnWillRenderObject()
        {
            if (!Application.isPlaying)
            {
                if (!init)
                {
                    Awake();
                    init = true;
                }
            }
            RenderWithCamera(Camera.current);
        }

        public void GenerateVertices(Vector3 vertexDirection, bool orthoGraphic)
        {
            int vertexCount = (_slices + 1) * clippedSamples.Length;
            if (tsMesh.vertexCount != vertexCount)
            {
                tsMesh.vertices = new Vector3[vertexCount];
                tsMesh.normals = new Vector3[vertexCount];
                tsMesh.colors = new Color[vertexCount];
                tsMesh.uv = new Vector2[vertexCount];
            }
            int vertexIndex = 0;
            float totalLength = 0f;
            for (int i = 0; i < clippedSamples.Length; i++)
            {
                Vector3 center = clippedSamples[i].position;
                if (offset != Vector3.zero) center += offset.x * -Vector3.Cross(clippedSamples[i].direction, clippedSamples[i].normal) + offset.y * clippedSamples[i].normal + offset.z * clippedSamples[i].direction;
                Vector3 vertexNormal;
                if(orthoGraphic) vertexNormal = vertexDirection;
                else vertexNormal = (vertexDirection - center).normalized;
                Vector3 vertexRight = Vector3.Cross(clippedSamples[i].direction, vertexNormal).normalized;
                if (_uniformUVs && i > 0) totalLength += Vector3.Distance(clippedSamples[i].position, clippedSamples[i - 1].position);
                for (int n = 0; n < _slices + 1; n++)
                {
                    float slicePercent = ((float)n / _slices);
                    tsMesh.vertices[vertexIndex] = center - vertexRight * clippedSamples[i].size * 0.5f * size + vertexRight * clippedSamples[i].size * slicePercent * size;
                    if (_uniformUVs) tsMesh.uv[vertexIndex] = new Vector2(1f-slicePercent * _uvScale.x, totalLength * _uvScale.y) + _uvOffset;
                    else tsMesh.uv[vertexIndex] = new Vector2(1f-slicePercent * _uvScale.x, (float)clippedSamples[i].percent * _uvScale.y) + _uvOffset;
                    tsMesh.normals[vertexIndex] = vertexNormal;
                    tsMesh.colors[vertexIndex] = clippedSamples[i].color * color;
                    vertexIndex++;
                }
            }
        }


    }
}
