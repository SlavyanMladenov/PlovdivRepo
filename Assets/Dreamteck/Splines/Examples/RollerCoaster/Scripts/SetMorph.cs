using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class SetMorph : MonoBehaviour
    {
        public float morphSpeed = 10f;
        public int currentMorph = 0;

        private SplineComputer comp;

        public void Set(int index)
        {
            currentMorph = index;
        }

        // Use this for initialization
        void Start()
        {
            comp = GetComponent<SplineComputer>();
        }

        // Update is called once per frame
        void Update()
        {
            float value = comp.morph.GetWeight(currentMorph);
            if (value != 1f)
            {
                value = Mathf.MoveTowards(value, 1f, Time.deltaTime * morphSpeed);
                comp.SetMorphState(currentMorph, value);
            }
        }
    }
}