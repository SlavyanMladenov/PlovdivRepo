using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dreamteck.Splines
{
    [AddComponentMenu("Dreamteck/Splines/Object Controller")]
    public class ObjectController : SplineUser
    {
        [System.Serializable]
        internal class ObjectControl
        {
            public bool isNull
            {
                get
                {
                    return gameObject == null;
                }
            }
            public Transform transform
            {
                get {
                    if (gameObject == null) return null;
                    return gameObject.transform;  
                }
            }
            public GameObject gameObject;
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
            public bool active = true;

            public Vector3 baseScale = Vector3.one;

            public ObjectControl(GameObject input)
            {
                gameObject = input;
                baseScale = gameObject.transform.localScale;
            }

            public void Destroy()
            {
                if (gameObject == null) return;
                GameObject.Destroy(gameObject);
            }

            public void DestroyImmediate()
            {
                if (gameObject == null) return;
                GameObject.DestroyImmediate(gameObject);
            }

            public void Apply()
            {
                if (gameObject == null) return;
                transform.position = position;
                transform.rotation = rotation;
                transform.localScale = scale;
                gameObject.SetActive(active);
            }

        }

        public enum ObjectMethod { Instantiate, GetChildren }
        public enum Positioning { Stretch, Clip }
        public enum Iteration { Ordered, Random }

        [SerializeField]
        [HideInInspector]
        public GameObject[] objects = new GameObject[0];

        public ObjectMethod objectMethod
        {
            get { return _objectMethod; }
            set
            {
                if (value != _objectMethod)
                {
                    if (value == ObjectMethod.GetChildren)
                    {
                        _objectMethod = value;
                        Spawn();
                    }
                    else _objectMethod = value;
                }
            }
        }

        public int spawnCount
        {
            get { return _spawnCount; }
            set
            {
                if (computer != null && value != _spawnCount)
                {
                    if (value < 0) value = 0;
                    if (_objectMethod == ObjectMethod.Instantiate)
                    {
                        if (value < _spawnCount)
                        {
                            _spawnCount = value;
                            Remove();
                        }
                        else
                        {
                            _spawnCount = value;
                            Spawn();
                        }
                    }
                    else _spawnCount = value;
                }
                else _spawnCount = value;
            }
        }

        public Positioning objectPositioning
        {
            get { return _objectPositioning; }
            set
            {
                if (computer != null && value != _objectPositioning)
                {
                    _objectPositioning = value;
                    Rebuild(false);
                }
                else _objectPositioning = value;
            }
        }

        public Iteration iteration
        {
            get { return _iteration; }
            set
            {
                if (computer != null && value != _iteration)
                {
                    _iteration = value;
                    Rebuild(false);
                }
                else _iteration = value;
            }
        }

        public int randomSeed
        {
            get { return _randomSeed; }
            set
            {
                if (computer != null && value != _randomSeed)
                {
                    _randomSeed = value;
                    Rebuild(false);
                }
                else _randomSeed = value;
            }
        }

        public Vector2 offset
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

        public bool randomizeOffset
        {
            get { return _randomizeOffset; }
            set
            {
                if (computer != null && value != _randomizeOffset)
                {
                    _randomizeOffset = value;
                    Rebuild(false);
                }
                else _randomizeOffset = value;
            }
        }

        public bool useRandomOffsetRotation
        {
            get { return _useRandomOffsetRotation; }
            set
            {
                if (computer != null && value != _useRandomOffsetRotation)
                {
                    _useRandomOffsetRotation = value;
                    Rebuild(false);
                }
                else _useRandomOffsetRotation = value;
            }
        }

        public bool shellOffset
        {
            get { return _shellOffset; }
            set
            {
                if (computer != null && value != _shellOffset)
                {
                    _shellOffset = value;
                    Rebuild(false);
                }
                else _shellOffset = value;
            }
        }

        public bool randomOffset
        {
            get { return _randomOffset; }
            set
            {
                if (computer != null && value != _randomOffset)
                {
                    _randomOffset = value;
                    Rebuild(false);
                }
                else _randomOffset = value;
            }
        }

        public bool applyRotation
        {
            get { return _applyRotation; }
            set
            {
                if (computer != null && value != _applyRotation)
                {
                    _applyRotation = value;
                    Rebuild(false);
                }
                else _applyRotation = value;
            }
        }

        public bool applyScale
        {
            get { return _applyScale; }
            set
            {
                if (computer != null && value != _applyScale)
                {
                    _applyScale = value;
                    Rebuild(false);
                }
                else _applyScale = value;
            }
        }

        public Vector2 randomSize
        {
            get { return _randomSize; }
            set
            {
                if (computer != null && value != _randomSize)
                {
                    _randomSize = value;
                    Rebuild(false);
                }
                else _randomSize = value;
            }
        }


        public float positionOffset
        {
            get { return _positionOffset; }
            set
            {
                if (computer != null && value != _positionOffset)
                {
                    _positionOffset = value;
                    Rebuild(false);
                }
                else _positionOffset = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        private float _positionOffset = 0f;
        [SerializeField]
        [HideInInspector]
        private int _spawnCount = 0;
        [SerializeField]
        [HideInInspector]
        private Positioning _objectPositioning = Positioning.Stretch;
        [SerializeField]
        [HideInInspector]
        private Iteration _iteration = Iteration.Ordered;
        [SerializeField]
        [HideInInspector]
        private int _randomSeed = 1;
        [SerializeField]
        [HideInInspector]
        private Vector2 _randomSize = Vector2.one;
        [SerializeField]
        [HideInInspector]
        private Vector2 _offset = Vector2.zero;
        [SerializeField]
        [HideInInspector]
        private bool _randomizeOffset = false;
        [SerializeField]
        [HideInInspector]
        private bool _useRandomOffsetRotation = false;
        [SerializeField]
        [HideInInspector]
        private bool _shellOffset = true;
        [SerializeField]
        [HideInInspector]
        private bool _randomOffset = false;
        [SerializeField]
        [HideInInspector]
        private bool _applyRotation = true;
        [SerializeField]
        [HideInInspector]
        private bool _applyScale = false;
        [SerializeField]
        [HideInInspector]
        private ObjectMethod _objectMethod = ObjectMethod.Instantiate;

        [HideInInspector]
        public bool delayedSpawn = false;
        [HideInInspector]
        public float spawnDelay = 0.1f;
        [SerializeField]
        [HideInInspector]
        private int lastChildCount = 0;
        [SerializeField]
        [HideInInspector]
        private ObjectControl[] spawned = new ObjectControl[0];

        public void Clear()
        {
            for (int i = 0; i < spawned.Length; i++)
            {
                if (spawned[i] == null) continue;
                spawned[i].transform.localScale = spawned[i].baseScale;
                if (_objectMethod == ObjectMethod.GetChildren) spawned[i].gameObject.SetActive(false);
                else
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) spawned[i].DestroyImmediate();
                    else spawned[i].Destroy();
#else
                    spawned[i].Destroy();
#endif

                }
            }
            spawned = new ObjectControl[0];
        }

        private void Remove()
        {
#if UNITY_EDITOR
            if (PrefabUtility.GetPrefabType(this.gameObject) == PrefabType.Prefab) return;
#endif
            if (_spawnCount >= spawned.Length) return;
            for (int i = spawned.Length - 1; i >= _spawnCount; i--)
            {
                if (i >= spawned.Length) break;
                if (spawned[i] == null) continue;
                spawned[i].transform.localScale = spawned[i].baseScale;
                if (_objectMethod == ObjectMethod.GetChildren) spawned[i].gameObject.SetActive(false);
                else
                {
                    if (Application.isEditor) spawned[i].DestroyImmediate();
                    else spawned[i].Destroy();

                }
            }
            ObjectControl[] newSpawned = new ObjectControl[_spawnCount];
            for (int i = 0; i < newSpawned.Length; i++)
            {
                newSpawned[i] = spawned[i];
            }
            spawned = newSpawned;
            Rebuild(false);
        }

        public void GetAll()
        {
            ObjectControl[] newSpawned = new ObjectControl[this.transform.childCount];
            int index = 0;
            foreach (Transform child in this.transform)
            {
                if (newSpawned[index] == null)
                {
                    newSpawned[index++] = new ObjectControl(child.gameObject);
                    continue;
                }
                bool found = false;
                for (int i = 0; i < spawned.Length; i++)
                {
                    if (spawned[i].gameObject == child.gameObject)
                    {
                        newSpawned[index++] = spawned[i];
                        found = true;
                        break;
                    }
                }
                if (!found) newSpawned[index++] = new ObjectControl(child.gameObject);
            }
            spawned = newSpawned;
        }

        public void Spawn()
        {
#if UNITY_EDITOR
            if (PrefabUtility.GetPrefabType(this.gameObject) == PrefabType.Prefab) return;
#endif
            if (_objectMethod == ObjectMethod.Instantiate)
            {
                if (delayedSpawn && Application.isPlaying)
                {
                    StopCoroutine("InstantiateAllWithDelay");
                    StartCoroutine(InstantiateAllWithDelay());
                }
                else InstantiateAll();
            }
            else GetAll();
            Rebuild(false);
        }

        protected override void Run()
        {
            base.Run();
            if (_objectMethod == ObjectMethod.GetChildren && lastChildCount != this.transform.childCount)
            {
                Spawn();
                lastChildCount = this.transform.childCount;
            }
        }


        IEnumerator InstantiateAllWithDelay()
        {
            if (computer == null) yield break;
            if (objects.Length == 0) yield break;
            for (int i = spawned.Length; i <= spawnCount; i++)
            {
                InstantiateSingle();
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        private void InstantiateAll()
        {
            if (computer == null) return;
            if (objects.Length == 0) return;
            for (int i = spawned.Length; i < spawnCount; i++)
            {
                InstantiateSingle();
            }
        }

        private void InstantiateSingle()
        {
            if (objects.Length == 0) return;
            int index = 0;
            if (_iteration == Iteration.Ordered)
            {
                index = spawned.Length - Mathf.FloorToInt(spawned.Length / objects.Length) * objects.Length;
            }
            else index = Random.Range(0, objects.Length);
            if (objects[index] == null) return;

            ObjectControl[] newSpawned = new ObjectControl[spawned.Length + 1];
            spawned.CopyTo(newSpawned, 0);

            newSpawned[newSpawned.Length - 1] = new ObjectControl((GameObject)Instantiate(objects[index], this.transform.position, this.transform.rotation));
            newSpawned[newSpawned.Length - 1].transform.parent = this.transform;
            spawned = newSpawned;
        }

        protected override void Build()
        {
            base.Build();
            System.Random randomizer = new System.Random(_randomSeed);
            System.Random randomizer2 = new System.Random(_randomSeed + 1);
            for (int i = 0; i < spawned.Length; i++)
            {
                if (spawned[i] == null)
                {
                    Clear();
                    Spawn();
                    break;
                }
                float percent = 0f;
                if (spawned.Length > 1) percent = (float)i / (spawned.Length - 1);
                percent += positionOffset;
                if (percent > 1f) percent -= 1f;
                else if (percent < 0f) percent += 1f;
                SplineResult result;
                if (objectPositioning == Positioning.Clip) result = Evaluate(percent);
                else result = Evaluate(DMath.Lerp(clipFrom, clipTo, percent));
                spawned[i].position = result.position;
                if (_applyRotation && (!_randomizeOffset || !_useRandomOffsetRotation)) spawned[i].rotation = result.rotation;
                if (_applyScale) spawned[i].scale = spawned[i].baseScale * result.size;
                else spawned[i].scale = spawned[i].baseScale;
                Vector3 right = Vector3.Cross(result.direction, result.normal).normalized;
                spawned[i].position += -right * _offset.x + result.normal * _offset.y;
                if (_randomizeOffset)
                {
                    float distance = (float)randomizer.NextDouble();
                    float angleInRadians = (float)randomizer2.NextDouble() * 360f * Mathf.Deg2Rad;
                    Vector2 randomCircle = new Vector2(distance * Mathf.Cos(angleInRadians), distance * Mathf.Sin(angleInRadians));
                    if (_shellOffset) randomCircle.Normalize();
                    else randomCircle = Vector2.ClampMagnitude(randomCircle, 1f);
                    Vector3 center = spawned[i].position;
                    spawned[i].position += randomCircle.x * right * _randomSize.x * result.size * 0.5f + randomCircle.y * result.normal * _randomSize.y * result.size * 0.5f;
                    if (_useRandomOffsetRotation) spawned[i].rotation = Quaternion.LookRotation(result.direction, spawned[i].position - center);
                }

                if (_objectPositioning == Positioning.Clip)
                {
                    if (percent < clipFrom || percent > clipTo) spawned[i].active = false;
                    else spawned[i].active = true;
                }
            }
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            for (int i = 0; i < spawned.Length; i++)
            {
                spawned[i].Apply();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (objectMethod == ObjectMethod.Instantiate)
            {
                Clear();
            }
        }
    }
}