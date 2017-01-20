using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Extrude Mesh")]
    public class ExtrudeMesh : SplineMeshGeneration
    {

        public class VertexGroup
        {
            public float value;
            public int[] ids;

            public VertexGroup(float val, int[] vertIds)
            {
                value = val;
                ids = vertIds;
            }

            public void AddId(int id)
            {
                int[] newIds = new int[ids.Length + 1];
                ids.CopyTo(newIds, 0);
                newIds[newIds.Length - 1] = id;
                ids = newIds;
            }
        }

        public enum Axis { X, Y, Z }

        public Axis axis
        {
            get { return _axis; }
            set
            {
                if (computer != null && value != _axis)
                {
                    _axis = value;
                    SetMesh();
                    Rebuild(false);
                }
                else _axis = value;
            }
        }

        public Mesh sourceMesh
        {
            get { return _sourceMesh; }
            set
            {
                if (computer != null && value != _sourceMesh)
                {
                    _sourceMesh = value;
                    SetMesh();
                    Rebuild(false);
                }
                else _sourceMesh = value;
            }
        }

        public int repeat
        {
            get { return _repeat; }
            set
            {
                if (computer != null && value != _repeat)
                {
                    _repeat = value;
                    Rebuild(false);
                }
                else _repeat = value;
            }
        }

        public double spacing
        {
            get { return _spacing; }
            set
            {
                if (computer != null && value != _spacing)
                {
                    _spacing = value;
                    Rebuild(false);
                }
                else _spacing = value;
            }
        }

        public bool removeInnerFaces
        {
            get { return _removeInnerFaces; }
            set
            {
                if (value != _removeInnerFaces)
                {
                    _removeInnerFaces = value;
                    SetMesh();
                    Rebuild(false);
                }
            }
        }

        public Vector2 scale
        {
            get { return _scale; }
            set
            {
                if (computer != null && value != _scale)
                {
                    _scale = value;
                    Rebuild(false);
                }
                else _scale = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        private Mesh _sourceMesh = null;
        [SerializeField]
        [HideInInspector]
        private Axis _axis = Axis.Z;
        [SerializeField]
        [HideInInspector]
        private int _repeat = 1;
        [SerializeField]
        [HideInInspector]
        private double _spacing = 0.0;
        [SerializeField]
        [HideInInspector]
        private bool _removeInnerFaces = false;
        [SerializeField]
        [HideInInspector]
        private Vector2 _scale = Vector2.one;
        [SerializeField]
        [HideInInspector]
        private TS_Mesh inputMesh = null;
        [SerializeField]
        [HideInInspector]
        private TS_Mesh middleMesh = null;
        [SerializeField]
        [HideInInspector]
        private TS_Mesh startMesh = null;
        [SerializeField]
        [HideInInspector]
        private TS_Mesh endMesh = null;
        [SerializeField]
        [HideInInspector]
        private List<VertexGroup> vertexGroups = new List<VertexGroup>();
        private SplineResult lastResult = new SplineResult();
        private bool useLastResult = false;
        private TS_Mesh[] combineMeshes = new TS_Mesh[0];

#if UNITY_EDITOR
        public override void EditorAwake()
        {
            base.EditorAwake();
            if (inputMesh != null) inputMesh = TS_Mesh.Copy(inputMesh);
            if (middleMesh != null) middleMesh =  TS_Mesh.Copy(middleMesh);
            if (startMesh != null) startMesh = TS_Mesh.Copy(startMesh);
            if (endMesh != null) endMesh = TS_Mesh.Copy(endMesh);
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            mesh.name = "Stretch Mesh";
        }

        void SetMesh()
        {
            inputMesh = new TS_Mesh(sourceMesh);
            GroupVertices();
            if (_removeInnerFaces) GenerateInnerMesh();
            else middleMesh = startMesh = endMesh = null;
        }

        protected override void BuildMesh()
        {
            if (computer == null) return;
            if (_sourceMesh == null) return;
            if (inputMesh == null) SetMesh();
            base.BuildMesh();
            Generate();
        }

        public void GroupVertices()
        {
            vertexGroups = new List<VertexGroup>();
            int ax = (int)_axis;
            if (ax > 2) ax -= 2;
            for (int i = 0; i < inputMesh.vertices.Length; i++)
            {
                float value = 0f;
                switch (ax)
                {
                    case 0: value = inputMesh.vertices[i].x; break;
                    case 1: value = inputMesh.vertices[i].y; break;
                    case 2: value = inputMesh.vertices[i].z; break;
                }
                int index = FindInsertIndex(inputMesh.vertices[i], value);
                if (index >= vertexGroups.Count) vertexGroups.Add(new VertexGroup(value, new int[] { i }));
                else
                {
                    if (Mathf.Approximately(vertexGroups[index].value, value)) vertexGroups[index].AddId(i);
                    else if (vertexGroups[index].value < value) vertexGroups.Insert(index, new VertexGroup(value, new int[] { i }));
                    else
                    {
                        if (index < vertexGroups.Count - 1) vertexGroups.Insert(index + 1, new VertexGroup(value, new int[] { i }));
                        else vertexGroups.Add(new VertexGroup(value, new int[] { i }));
                    }
                }
            }
        }

        private int FindInsertIndex(Vector3 pos, float value)
        {
            int lower = 0;
            int upper = vertexGroups.Count - 1;

            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                if (vertexGroups[middle].value == value) return middle;
                else if (vertexGroups[middle].value < value) upper = middle - 1;
                else lower = middle + 1;
            }
            return lower;
        }

        public void RemoveInnerMesh()
        {
            middleMesh = null;
            startMesh = null;
            endMesh = null;
        }

        public void GenerateInnerMesh()
        {
            int[] beginIndices = new int[vertexGroups[0].ids.Length];
            int[] endIndices = new int[vertexGroups[vertexGroups.Count - 1].ids.Length];
            vertexGroups[0].ids.CopyTo(beginIndices, 0);
            vertexGroups[vertexGroups.Count - 1].ids.CopyTo(endIndices, 0);
            //First run through the faces and find the ones that belong only to the end vertices and begin vertices
            List<int> startTriangles = new List<int>();
            List<int> endTriangles = new List<int>();
            for (int i = 0; i < inputMesh.triangles.Length; i += 3)
            {
                bool removeTriangle = false;

                int found = 0;
                for (int n = 0; n < beginIndices.Length; n++)
                {
                    if (inputMesh.triangles[i] == beginIndices[n] || inputMesh.triangles[i + 1] == beginIndices[n] || inputMesh.triangles[i + 2] == beginIndices[n]) found++;
                    if (found == 3)
                    {
                        removeTriangle = true;
                        break;
                    }
                }
                if (removeTriangle)
                {
                    startTriangles.Add(i);
                    continue;
                }

                removeTriangle = false;
                found = 0;
                for (int n = 0; n < endIndices.Length; n++)
                {
                    if (inputMesh.triangles[i] == endIndices[n] || inputMesh.triangles[i + 1] == endIndices[n] || inputMesh.triangles[i + 2] == endIndices[n]) found++;
                    if (found == 3)
                    {
                        removeTriangle = true;
                        break;
                    }
                }
                if (removeTriangle) endTriangles.Add(i);
            }
            middleMesh = TS_Mesh.Copy(inputMesh);
            startMesh = TS_Mesh.Copy(inputMesh);
            endMesh = TS_Mesh.Copy(inputMesh);
            List<int> all = new List<int>();
            all.AddRange(startTriangles);
            all.AddRange(endTriangles);
            StripFaces(startMesh, startTriangles);
            StripFaces(middleMesh, all);
            StripFaces(endMesh, endTriangles);
        }

        private void StripFaces(TS_Mesh input, List<int> toStrip)
        {
            int[] newTris = new int[input.triangles.Length - toStrip.Count * 3];
            int removed = 0;
            toStrip.Sort();

            for (int i = 0; i < input.triangles.Length; i += 3)
            {

                if (removed < toStrip.Count)
                {
                    if (i == toStrip[removed])
                    {
                        removed++;
                        continue;
                    }
                }
                if (i - removed * 3 >= newTris.Length - 2) break;
                // Debug.Log("Face: " + (i - removed * 3) + ", " + (i - removed * 3 + 1) + ", " + (i - removed * 3 + 2) + " total faces: " + newTris.Length + " total removed so far: " + removed * 3 + " out of " + toStrip.Count*3);
                newTris[i - removed * 3] = input.triangles[i];
                newTris[i + 1 - removed * 3] = input.triangles[i + 1];
                newTris[i + 2 - removed * 3] = input.triangles[i + 2];
            }

            removed = 0;
            for (int i = 0; i < input.subMeshes.Count; i++)
            {
                List<int> submesh = new List<int>();
                for (int n = 0; n < input.subMeshes[i].Length; n += 3)
                {
                    if (removed < toStrip.Count)
                    {
                        if (input.subMeshes[i][n] == input.triangles[toStrip[removed]])
                        {

                            if (input.subMeshes[i][n + 1] == input.triangles[toStrip[removed] + 1])
                            {
                                if (input.subMeshes[i][n + 2] == input.triangles[toStrip[removed] + 2])
                                {
                                    removed++;
                                    continue;
                                }
                            }
                        }
                    }
                    submesh.Add(input.subMeshes[i][n]);
                    submesh.Add(input.subMeshes[i][n + 1]);
                    submesh.Add(input.subMeshes[i][n + 2]);
                }
                input.subMeshes[i] = submesh.ToArray();
            }
            input.triangles = newTris;
        }


        void Generate()
        {
            double step = span / _repeat;
            double space = step * _spacing * 0.5;
            useLastResult = false;
            if(combineMeshes.Length != _repeat) combineMeshes = new TS_Mesh[_repeat];


            for (int i = 0; i < _repeat; i++)
            {
                if (combineMeshes[i] == null) combineMeshes[i] = new TS_Mesh();
                double from = clipFrom + i * step + space;
                double to = clipFrom + i * step + step - space;
                if (middleMesh != null && space == 0f)
                {
                    if (computer.isClosed && span >= 1f) combineMeshes[i].Absorb(middleMesh);
                    else if (i > 0 && i < _repeat - 1) combineMeshes[i].Absorb(middleMesh);
                    else if (i == 0) combineMeshes[i].Absorb(startMesh);
                    else if (i == _repeat - 1) combineMeshes[i].Absorb(endMesh);
                    else combineMeshes[i].Absorb(inputMesh);
                } else combineMeshes[i].Absorb(inputMesh);
                    combineMeshes[i] = Stretch(combineMeshes[i], from, to);
                if (_spacing == 0f) useLastResult = true;
            }
            tsMesh = new TS_Mesh();
            tsMesh.Combine(combineMeshes);
        }

        private TS_Mesh Stretch(TS_Mesh mesh, double from, double to)
        {
            SplineResult result = new SplineResult();
            if (_axis == Axis.X)
            {
                for (int i = 0; i < vertexGroups.Count; i++)
                {
                    //Get the group's percent in the bounding box
                    double xPercent = DMath.Clamp01(Mathf.InverseLerp(mesh.bounds.min.x, mesh.bounds.max.x, vertexGroups[i].value));

                    if (useLastResult && i == vertexGroups.Count) result = lastResult;
                    else Evaluate(DMath.Lerp(from, to, xPercent), ref result);
                    if (i == 0) lastResult.Absorb(result);

                    for (int n = 0; n < vertexGroups[i].ids.Length; n++)
                    {
                        int index = vertexGroups[i].ids[n];
                        float yPercent = Mathf.Clamp01(Mathf.InverseLerp(mesh.bounds.min.y, mesh.bounds.max.y, mesh.vertices[index].y));
                        float zPercent = Mathf.Clamp01(Mathf.InverseLerp(mesh.bounds.min.z, mesh.bounds.max.z, mesh.vertices[index].z));
                        Quaternion rot = Quaternion.AngleAxis(rotation, result.direction);
                        Vector3 right = Vector3.Cross(result.direction, result.normal);
                        mesh.vertices[index] = result.position + rot * right * Mathf.Lerp(mesh.bounds.min.z, mesh.bounds.max.z, zPercent) * result.size * _scale.x - right * offset.x;
                        mesh.vertices[index] += rot * result.normal * Mathf.Lerp(mesh.bounds.min.y, mesh.bounds.max.y, yPercent) * result.size * _scale.y + result.normal * offset.y;
                        mesh.vertices[index] += result.direction * offset.z;
                        //Apply all rotations to the normal
                        mesh.normals[index] = rot * result.rotation * Quaternion.AngleAxis(-90f, Vector3.up) * Quaternion.FromToRotation(Vector3.up, result.normal) * mesh.normals[index];
                    }
                }
            }

            if (_axis == Axis.Y)
            {
                for (int i = 0; i < vertexGroups.Count; i++)
                {
                    double yPercent = DMath.Clamp01(Mathf.InverseLerp(mesh.bounds.min.y, mesh.bounds.max.y, vertexGroups[i].value));
                    if (useLastResult && i == vertexGroups.Count) result = lastResult;
                    else Evaluate(DMath.Lerp(from, to, yPercent), ref result);
                    if (i == 0) lastResult.Absorb(result);

                    for (int n = 0; n < vertexGroups[i].ids.Length; n++)
                    {
                        int index = vertexGroups[i].ids[n];
                        float xPercent = Mathf.Clamp01(Mathf.InverseLerp(mesh.bounds.min.x, mesh.bounds.max.x, mesh.vertices[index].x));
                        float zPercent = Mathf.Clamp01(Mathf.InverseLerp(mesh.bounds.min.z, mesh.bounds.max.z, mesh.vertices[index].z));
                        Quaternion rot = Quaternion.AngleAxis(rotation, result.direction);
                        Vector3 right = Vector3.Cross(result.direction, result.normal);
                        mesh.vertices[index] = result.position - rot * right * Mathf.Lerp(mesh.bounds.min.x, mesh.bounds.max.x, xPercent) * result.size * _scale.x - right * offset.x;
                        mesh.vertices[index] -= rot * result.normal * Mathf.Lerp(mesh.bounds.min.z, mesh.bounds.max.z, zPercent) * result.size * _scale.y - result.normal * offset.y;
                        mesh.vertices[index] += result.direction * offset.z;
                        mesh.normals[index] = rot * result.rotation * Quaternion.AngleAxis(90f, Vector3.right) * Quaternion.FromToRotation(Vector3.up, result.normal) * mesh.normals[index];
                    }

                }
            }

            if (_axis == Axis.Z)
            {
                for (int i = 0; i < vertexGroups.Count; i++)
                {
                    double zPercent = DMath.Clamp01(Mathf.InverseLerp(mesh.bounds.min.z, mesh.bounds.max.z, vertexGroups[i].value));
                    if (useLastResult && i == vertexGroups.Count) result = lastResult;
                    else Evaluate(DMath.Lerp(from, to, zPercent), ref result);
                    if (i == 0) lastResult.Absorb(result);
                    for (int n = 0; n < vertexGroups[i].ids.Length; n++)
                    {
                        int index = vertexGroups[i].ids[n];
                        float xPercent = Mathf.Clamp01(Mathf.InverseLerp(mesh.bounds.min.x, mesh.bounds.max.x, mesh.vertices[index].x));
                        float yPercent = Mathf.Clamp01(Mathf.InverseLerp(mesh.bounds.min.y, mesh.bounds.max.y, mesh.vertices[index].y));
                        Quaternion rot = Quaternion.AngleAxis(rotation, result.direction);
                        Vector3 right = Vector3.Cross(result.direction, result.normal);
                        mesh.vertices[index] = result.position - rot * right * Mathf.Lerp(mesh.bounds.min.x, mesh.bounds.max.x, xPercent) * result.size * _scale.x - right * offset.x;
                        mesh.vertices[index] += rot * result.normal * Mathf.Lerp(mesh.bounds.min.y, mesh.bounds.max.y, yPercent) * result.size * _scale.y + result.normal * offset.y;
                        mesh.vertices[index] += result.direction * offset.z;
                        mesh.normals[index] = rot * result.rotation * mesh.normals[index];
                    }
                }

            }
            return mesh;
        }



    }
}
