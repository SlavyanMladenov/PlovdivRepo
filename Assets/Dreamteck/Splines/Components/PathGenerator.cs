using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Path Generator")]
    public class PathGenerator : SplineMeshGeneration
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

        public bool useShapeCurve
        {
            get { return _useShapeCurve; }
            set
            {
                if (value != _useShapeCurve)
                {
                    _useShapeCurve = value;
                    if (_useShapeCurve)
                    {
                        _shape = new AnimationCurve();
                        _shape.AddKey(new Keyframe(0, 0));
                        _shape.AddKey(new Keyframe(1, 0));
                    } else _shape = null;
                    Rebuild(false);
                }
            }
        }

        public float shapeExposure
        {
            get { return _shapeExposure; }
            set
            {
                if (computer != null && value != _shapeExposure)
                {
                    _shapeExposure = value;
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

        public AnimationCurve shape
        {
            get { return _shape; }
            set
            {
                if(_lastShape == null) _lastShape = new AnimationCurve();
                bool keyChange = false;
                if (value.keys.Length != _lastShape.keys.Length) keyChange = true;
                else
                {
                    for (int i = 0; i < value.keys.Length; i++)
                    {
                        if (value.keys[i].inTangent != _lastShape.keys[i].inTangent || value.keys[i].outTangent != _lastShape.keys[i].outTangent || value.keys[i].time != _lastShape.keys[i].time || value.keys[i].value != value.keys[i].value)
                        {
                            keyChange = true;
                            break;
                        }
                    }
                }
                if (keyChange) Rebuild(false);
                _lastShape.keys = new Keyframe[value.keys.Length];
                value.keys.CopyTo(_lastShape.keys, 0);
                _lastShape.preWrapMode = value.preWrapMode;
                _lastShape.postWrapMode = value.postWrapMode;
                _shape = value;

            }
        }

        [SerializeField]
        [HideInInspector]
        private int _slices = 1;
        [SerializeField]
        [HideInInspector]
        private bool _useShapeCurve = false;
        [SerializeField]
        [HideInInspector]
        private AnimationCurve _shape;
        [SerializeField]
        [HideInInspector]
        private AnimationCurve _lastShape;
        [SerializeField]
        [HideInInspector]
        private float _shapeExposure = 1f;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvScale = Vector3.one;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvOffset = Vector2.zero;
        [SerializeField]
        [HideInInspector]
        private bool _uniformUVs = false;


        protected override void Awake()
        {
            base.Awake();
            mesh.name = "path";
        }

        protected override void Reset()
        {
            base.Reset();
        }


        protected override void BuildMesh()
        {
           if (computer == null) return; 
           if (computer.pointCount == 0) return;
           base.BuildMesh();
           GenerateVertices();
           tsMesh.triangles = MeshUtility.GeneratePlaneTriangles(_slices, clippedSamples.Length, flipFaces && !doubleSided);
           if (doubleSided) MeshUtility.MakeDoublesided(tsMesh);
           MeshUtility.CalculateTangents(tsMesh);
        }


        void GenerateVertices()
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
                Vector3 center = Vector3.zero;
                try
                {
                   center = clippedSamples[i].position;
                } catch (System.Exception ex) { Debug.Log(ex.Message + " for i = " + i); return; }
                Vector3 right = clippedSamples[i].right;
                if (offset != Vector3.zero) center += offset.x * right + offset.y * clippedSamples[i].normal + offset.z * clippedSamples[i].direction;
                float fullSize = size * clippedSamples[i].size;
                Vector3 lastVertPos = Vector3.zero;
                Quaternion rot = Quaternion.AngleAxis(rotation, clippedSamples[i].direction);
                if (_uniformUVs && i > 0) totalLength +=  Vector3.Distance(clippedSamples[i].position, clippedSamples[i - 1].position);
                for (int n = 0; n < _slices + 1; n++)
                {
                    float slicePercent = ((float)n / _slices);
                    float shapeEval = 0f;
                    if (_useShapeCurve) shapeEval = _shape.Evaluate(slicePercent);
                    tsMesh.vertices[vertexIndex] = center + rot * right * fullSize * 0.5f - rot * right * fullSize * slicePercent + rot * clippedSamples[i].normal * shapeEval * _shapeExposure;
                    if (_uniformUVs) tsMesh.uv[vertexIndex] = new Vector2(1f-slicePercent * _uvScale.x, totalLength * _uvScale.y) + _uvOffset;
                    else tsMesh.uv[vertexIndex] = new Vector2(1f-slicePercent * _uvScale.x, (float)clippedSamples[i].percent * _uvScale.y) + _uvOffset;
                    if (_slices > 1)
                    {
                        if (n < _slices)
                        {
                            float forwardPercent = ((float)(n + 1) / _slices);
                            shapeEval = 0f;
                            if (_useShapeCurve) shapeEval = _shape.Evaluate(forwardPercent);
                            Vector3 nextVertPos = center + rot * right * fullSize * 0.5f - rot * right * fullSize * forwardPercent + rot * clippedSamples[i].normal * shapeEval * _shapeExposure;
                            Vector3 cross1 = -Vector3.Cross(clippedSamples[i].direction, nextVertPos - tsMesh.vertices[vertexIndex]).normalized;

                            if (n > 0)
                            {
                                Vector3 cross2 = -Vector3.Cross(clippedSamples[i].direction, tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                                tsMesh.normals[vertexIndex] = Vector3.Slerp(cross1, cross2, 0.5f);
                            } else tsMesh.normals[vertexIndex] = cross1;
                        }
                        else   tsMesh.normals[vertexIndex] = -Vector3.Cross(clippedSamples[i].direction, tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                    }
                    else
                    {
                        tsMesh.normals[vertexIndex] = clippedSamples[i].normal;
                        if (rotation != 0f) tsMesh.normals[vertexIndex] = rot * tsMesh.normals[vertexIndex];
                    }
                    if (flipFaces && !doubleSided) tsMesh.normals[vertexIndex] *= -1f;
                    tsMesh.colors[vertexIndex] = clippedSamples[i].color * color;
                    lastVertPos = tsMesh.vertices[vertexIndex];
                    vertexIndex++;
                }
            }
        }
    }
}
