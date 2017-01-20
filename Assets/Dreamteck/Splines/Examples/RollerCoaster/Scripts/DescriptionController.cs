using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class DescriptionController : MonoBehaviour
    {
        private TextMesh textMesh;
        [TextArea(3, 10)]
        public string[] texts;
        public float[] durations;

        // Use this for initialization
        void Start()
        {
            textMesh = GetComponent<TextMesh>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ShowText(int index)
        {
            StartCoroutine(TextRoutine(index));
        }

        IEnumerator TextRoutine(int index)
        {
            float alpha = 0f;
            textMesh.text = texts[index];
            while (alpha < 1f)
            {
                alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime * 2f);
                textMesh.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            yield return new WaitForSeconds(durations[index]);
            while (alpha > 0f)
            {
                alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime * 2f);
                textMesh.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }
    }
}
