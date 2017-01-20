using UnityEngine;
using System.Collections;
using System.Threading;
namespace Dreamteck.Splines
{

    public class SplineMeshGeneration : SplineUser
    {
        public float size
        {
            get { return _size; }
            set
            {
                if (computer != null && value != _size)
                {
                    _size = value;
                    Rebuild(false);
                } else _size = value;
            }
        }

        public Color color
        {
            get { return _color; }
            set
            {
                if (computer != null && value != _color)
                {
                    _color = value;
                    Rebuild(false);
                } else _color = value;
            }
        }

        public Vector3 offset
        {
            get { return _offset; }
            set
            {
                if (computer != null && value != _offset)
                {
                    _offset = value;
                    Rebuild(false);
                }
                else _offset = value;
            }
        }

        public int normalMethod
        {
            get { return _normalMethod; }
            set
            {
                if (computer != null && value != _normalMethod)
                {
                    _normalMethod = value;
                    Rebuild(false);
                }
                else _normalMethod = value;
            }
        }

        public float rotation
        {
            get { return _rotation; }
            set
            {
                if (computer != null && value != _rotation)
                {
                    _rotation = value;
                    Rebuild(false);
                }
                else _rotation = value;
            }
        }

        public bool flipFaces
        {
            get { return _flipFaces; }
            set
            {
                if (computer != null && value != _flipFaces)
                {
                    _flipFaces = value;
                    Rebuild(false);
                }
                else _flipFaces = value;
            }
        }

        public bool doubleSided
        {
            get { return _doubleSided; }
            set
            {
                if (computer != null && value != _doubleSided)
                {
                    _doubleSided = value;
                    Rebuild(false);
                }
                else _doubleSided = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        private float _size = 1f;
        [SerializeField]
        [HideInInspector]
        private Color _color = Color.white;
        [SerializeField]
        [HideInInspector]
        private Vector3 _offset = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private int _normalMethod = 1;
        [SerializeField]
        [HideInInspector]
        private float _rotation = 0f;
        [SerializeField]
        [HideInInspector]
        private bool _flipFaces = false;
        [SerializeField]
        [HideInInspector]
        private bool _doubleSided = false;

        [SerializeField]
        [HideInInspector]
        protected MeshCollider meshCollider;
        [SerializeField]
        [HideInInspector]
        protected MeshFilter filter;
        [SerializeField]
        [HideInInspector]
        protected MeshRenderer meshRenderer;


        [SerializeField]
        [HideInInspector]
        protected TS_Mesh tsMesh = new TS_Mesh();
        [SerializeField]
        [HideInInspector]
        protected Mesh mesh;
        [HideInInspector]
        public float colliderUpdateRate = 0.2f;
        [SerializeField]
        [HideInInspector]
        public bool optimize = false;
        protected bool updateCollider = false;
        protected float lastUpdateTime = 0f;


#if UNITY_EDITOR
        public override void EditorAwake()
        {
            base.EditorAwake();
            //Make copies of the meshes.
            if (tsMesh != null) tsMesh = TS_Mesh.Copy(tsMesh);
            else tsMesh = new TS_Mesh();
            if (mesh != null) mesh = (Mesh)Instantiate(mesh);
            else mesh = new Mesh();
            Awake();
        }
#endif

        protected override void Awake()
        {
            if (mesh == null) mesh = new Mesh();
            base.Awake();
            filter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }


        protected override void Reset()
        {
            base.Reset();
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null) filter.hideFlags = HideFlags.HideInInspector;
            MeshRenderer rend = GetComponent<MeshRenderer>();
            if (rend != null) rend.hideFlags = HideFlags.None;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer rend = GetComponent<MeshRenderer>();
            if (filter != null)  filter.hideFlags = HideFlags.None;
            if (rend != null)  rend.hideFlags = HideFlags.None;
        }


        public void UpdateCollider()
        {
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = this.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
        }

        protected override void Run()
        {
            base.Run();
            if (updateCollider)
            {
                if (meshCollider != null)
                {
                    if (Time.time - lastUpdateTime >= colliderUpdateRate)
                    {
                        lastUpdateTime = Time.time;
                        updateCollider = false;
                        meshCollider.sharedMesh = filter.sharedMesh;
                    }
                }
            }
        }

        protected override void Build()
        {
            base.Build();
            if (samples.Length > 0) BuildMesh();
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            WriteMesh();
        }

        protected virtual void BuildMesh()
        {
            //Logic for mesh generation, automatically called in the Build method
        }

        protected virtual void WriteMesh() 
        {
            MeshUtility.InverseTransformMesh(tsMesh, this.transform);
            tsMesh.WriteMesh(ref mesh);
            if (_normalMethod == 0) mesh.RecalculateNormals();
            if (optimize) ;
            if (filter != null) filter.sharedMesh = mesh;
            updateCollider = true;
        }
    }

  
}
