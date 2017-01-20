using UnityEngine;
using System.Collections;


namespace Dreamteck.Splines.Examples
{
    public class FlashSplineRenderer : MonoBehaviour
    {
        [System.Serializable]
        public class FlashElement
        {
            public float delay = 0f;
            public float speed = 1f;
            public float life = 1f;
        }
        public FlashElement[] elements;
        private float colorAlpha = 0f;
        private SplineMeshGeneration meshGen;
        // Use this for initialization
        void Start()
        {
            StartCoroutine(Flash());
            meshGen = GetComponent<SplineMeshGeneration>();
            colorAlpha = meshGen.color.a;
            Color col = meshGen.color;
            col.a = 0f;
            meshGen.color = col;
        }

        IEnumerator Flash()
        {
            for (int i = 0; i < elements.Length; i++)
            {
                yield return new WaitForSeconds(elements[i].delay);
                float alpha = 0f;
                while(alpha < 1f)
                {
                    alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime * elements[i].speed);
                    Color col = meshGen.color;
                    col.a = colorAlpha * alpha;
                    meshGen.color = col;
                    yield return null;
                }
                yield return new WaitForSeconds(elements[i].life);
                while (alpha > 0f)
                {
                    alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime * elements[i].speed);
                    Color col = meshGen.color;
                    col.a = colorAlpha * alpha;
                    meshGen.color = col;
                    yield return null;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
