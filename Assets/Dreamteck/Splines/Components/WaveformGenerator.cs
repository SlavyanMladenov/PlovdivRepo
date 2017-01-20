using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Waveform Generator")]
    public class WaveformGenerator : SplineMeshGeneration
    {
        public enum Axis { X, Y, Z }
        public enum UVWrapMode { Clamp, RepeatX, RepeatY, RepeatXY }

        public Axis axis
        {
            get { return _axis; }
            set
            {
                if (computer != null && value != _axis)
                {
                    _axis = value;
                    Rebuild(false);
                }
                else _axis = value;
            }
        }

        public bool symmetry
        {
            get { return _symmetry; }
            set
            {
                if (computer != null && value != _symmetry)
                {
                    _symmetry = value;
                    Rebuild(false);
                }
                else _symmetry = value;
            }
        }

        public UVWrapMode uvWrapMode
        {
            get { return _uvWrapMode; }
            set
            {
                if (computer != null && value != _uvWrapMode)
                {
                    _uvWrapMode = value;
                    Rebuild(false);
                }
                else _uvWrapMode = value;
            }
        }

        public int slices
        {
            get { return _slices; }
            set
            {
                if (computer != null && value != _slices)
                {
                    if (value < 1) value = 1;
                    _slices = value;
                    Rebuild(false);
                }
                else _slices = value;
            }
        }

        public Vector2 uvScale
        {
            get { return _uvScale; }
            set
            {
                if (computer != null && value != _uvScale)
                {
                    _uvScale = value;
                    Rebuild(false);
                }
                else _uvScale = value;
            }
        }

        public Vector2 uvOffset
        {
            get { return _uvOffset; }
            set
            {
                if (computer != null && value != _uvOffset)
                {
                    _uvOffset = value;
                    Rebuild(false);
                }
                else _uvOffset = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        private Axis _axis = Axis.Y;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvScale = Vector3.one;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvOffset = Vector2.zero;
        [SerializeField]
        [HideInInspector]
        private bool _symmetry = false;
        [SerializeField]
        [HideInInspector]
        private UVWrapMode _uvWrapMode = UVWrapMode.Clamp;
        [SerializeField]
        [HideInInspector]
        private int _slices = 1;

        protected override void Awake()
        {
            base.Awake();
            mesh.name = "waveform";
        }

        protected override void BuildMesh()
        {
            base.BuildMesh();
            Generate();
        }

        protected override void Build()
        {

            base.Build();
        }

        protected override void Run()
        {
            base.Run();
        }

        void Generate()
        {
            if (_symmetry) GenerateSymmetrical();
            else GenerateDefault();
            if (doubleSided) MeshUtility.MakeDoublesided(tsMesh);
            MeshUtility.CalculateTangents(tsMesh);
        }

        private void GenerateDefault()
        {
            int vertexCount = clippedSamples.Length * (_slices + 1);
            if (tsMesh.vertexCount != vertexCount)
            {
                tsMesh.vertices = new Vector3[vertexCount];
                tsMesh.normals = new Vector3[vertexCount];
                tsMesh.uv = new Vector2[vertexCount];
                tsMesh.colors = new Color[vertexCount];
            }
            int vertIndex = 0;
            float avgTop = 0f;
            float avgBottom = 0f;
            float totalLength = 0f;
            for (int i = 0; i < clippedSamples.Length; i++)
            {
                Vector3 top = clippedSamples[i].position;
                Vector3 bottom = top;
                Vector3 normal = Vector3.right;
                float heightPercent = 1f;
                if (_uvWrapMode == UVWrapMode.RepeatX || _uvWrapMode == UVWrapMode.RepeatXY)
                {
                    if (i > 0) totalLength += Vector3.Distance(clippedSamples[i].position, clippedSamples[i - 1].position);
                }
                switch (_axis)
                {
                    case Axis.X: avgBottom = bottom.x = computer.position.x; heightPercent = uvScale.y * Mathf.Abs(top.x - bottom.x); avgTop += top.x; break;
                    case Axis.Y: avgBottom = bottom.y = computer.position.y; heightPercent = uvScale.y * Mathf.Abs(top.y - bottom.y); normal = Vector3.up; avgTop += top.y; break;
                    case Axis.Z: avgBottom = bottom.z = computer.position.z; heightPercent = uvScale.y * Mathf.Abs(top.z - bottom.z); normal = Vector3.forward; avgTop += top.z; break;
                }
                Vector3 right = Vector3.Cross(normal, clippedSamples[i].direction).normalized;
                Vector3 offsetRight = Vector3.Cross(clippedSamples[i].normal, clippedSamples[i].direction);
                for (int n = 0; n < _slices + 1; n++)
                {
                    float slicePercent = ((float)n / _slices);
                    tsMesh.vertices[vertIndex] = Vector3.Lerp(bottom, top, slicePercent) + normal * offset.y + offsetRight * offset.x;
                    tsMesh.normals[vertIndex] = right;
                    if (flipFaces) tsMesh.normals[vertIndex] *= -1;
                    switch (_uvWrapMode)
                    {
                        case UVWrapMode.Clamp: tsMesh.uv[vertIndex] = new Vector2((float)clippedSamples[i].percent * uvScale.x + uvOffset.x, slicePercent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.RepeatX: tsMesh.uv[vertIndex] = new Vector2(totalLength * uvScale.x + uvOffset.x, slicePercent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.RepeatY: tsMesh.uv[vertIndex] = new Vector2((float)clippedSamples[i].percent * uvScale.x + uvOffset.x, heightPercent * slicePercent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.RepeatXY: tsMesh.uv[vertIndex] = new Vector2(totalLength * uvScale.x + uvOffset.x, heightPercent * slicePercent * uvScale.y + uvOffset.y); break;
                    }
                    tsMesh.colors[vertIndex] = clippedSamples[i].color * color;
                    vertIndex++;
                }
            }
            if (clippedSamples.Length > 0) avgTop /= clippedSamples.Length;
            bool flip = flipFaces;
            if (avgTop < avgBottom) flip = !flip;
            tsMesh.triangles = MeshUtility.GeneratePlaneTriangles(_slices, clippedSamples.Length, flip);
        }

        private void GenerateSymmetrical()
        {
            tsMesh.vertices = new Vector3[clippedSamples.Length * _slices * 2];
            tsMesh.normals = new Vector3[tsMesh.vertices.Length];
            tsMesh.uv = new Vector2[tsMesh.vertices.Length];
            tsMesh.colors = new Color[tsMesh.vertices.Length];
            int vertIndex = 0;
            float avgTop = 0f;
            float avgBottom = 0f;
            for (int i = 0; i < clippedSamples.Length; i++)
            {
                Vector3 top = clippedSamples[i].position;
                Vector3 bottom = top;
                Vector3 normal = Vector3.right;
                float heightPercent = 1f;
                switch (_axis)
                {
                    case Axis.X: bottom.x = computer.position.x + (computer.position.x - top.x); heightPercent = uvScale.y * Mathf.Abs(top.x - bottom.x); avgTop += top.x; avgBottom = computer.position.x; break;
                    case Axis.Y: bottom.y = computer.position.y + (computer.position.y - top.y); heightPercent = uvScale.y * Mathf.Abs(top.y - bottom.y); normal = Vector3.up; avgTop += top.y; avgBottom = computer.position.y; break;
                    case Axis.Z: bottom.z = computer.position.z + (computer.position.z - top.z); heightPercent = uvScale.y * Mathf.Abs(top.z - bottom.z); normal = Vector3.forward; avgTop += top.z; avgBottom = computer.position.z; break;
                } 
                Vector3 right = Vector3.Cross(normal, clippedSamples[i].direction).normalized;
                Vector3 offsetRight = Vector3.Cross(clippedSamples[i].normal, clippedSamples[i].direction);
                for (int n = 0; n < _slices * 2; n++)
                {
                    float slicePercent = ((float)n / _slices);
                    tsMesh.vertices[vertIndex] = Vector3.Lerp(bottom, top, slicePercent) + normal * offset.y + offsetRight * offset.x;
                    tsMesh.normals[vertIndex] = right;
                    if (flipFaces) tsMesh.normals[vertIndex] *= -1;
                    if (_uvWrapMode == UVWrapMode.Clamp) tsMesh.uv[vertIndex] = new Vector2((float)clippedSamples[i].percent * uvScale.x + uvOffset.x, slicePercent * uvScale.y + uvOffset.y);
                    else tsMesh.uv[vertIndex] = new Vector2((float)clippedSamples[i].percent * uvScale.x + uvOffset.x, (0.5f - 0.5f * heightPercent + heightPercent * slicePercent) * uvScale.y + uvOffset.y);
                    
                    tsMesh.colors[vertIndex] = clippedSamples[i].color * color;
                    vertIndex++;
                }
            }
            if (clippedSamples.Length > 0) avgTop /= clippedSamples.Length;
            bool flip = flipFaces;
            if (avgTop * 2f < avgBottom) flip = !flip;
            tsMesh.triangles = MeshUtility.GeneratePlaneTriangles(_slices * 2 - 1, clippedSamples.Length, flip);
        }

    }
}