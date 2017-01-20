using UnityEngine;
using System.Collections;


namespace Dreamteck.Splines.Examples
{
    public class SpectrumVisualizer : MonoBehaviour
    {
        public int samples = 1024;
        public float spectrumPercent = 0.75f; //Used to filter out the highest frequencies
        public float increaseSpeed = 50f;
        public float decreaseSpeed = 10f;
        public float maxOffset = 10f;
        private AudioSource source;
        private SplineComputer computer;
        private Vector3[] positions;
        public AnimationCurve spectrumMultiply; //lower frequencies have bigger values, this is used to even the values
        private float[] spectrumLerp;
        
        // Use this for initialization
        void Start()
        {
            source = GetComponent<AudioSource>();
            computer = GetComponent<SplineComputer>();
            SplinePoint[] points = computer.GetPoints();
            positions = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                positions[i] = points[i].position;
            }
            spectrumLerp = new float[points.Length];
        }

        // Update is called once per frame
        void Update()
        {
            float[] left = new float[samples];
            float[] right = new float[samples];
            source.GetSpectrumData(left, 0, FFTWindow.Hanning);
            source.GetSpectrumData(right, 1, FFTWindow.Hanning);
            float[] spectrum = new float[left.Length];
            for (int i = 0; i < spectrum.Length; i++)
            {
                spectrum[i] = (left[i] + right[i])/2f;
            }
            SplinePoint[] points = computer.GetPoints();
            if (spectrumPercent > 1f) spectrumPercent = 1f;
            int samplesPerPoint = Mathf.FloorToInt((spectrum.Length / points.Length) * spectrumPercent);
            for (int i = 0; i < points.Length; i++)
            {
                float avg = 0f;
                for (int n = samplesPerPoint * i; n < samplesPerPoint * i + samplesPerPoint; n++)
                {
                    avg += spectrum[n];
                }
                avg /= samplesPerPoint;
                if (avg > spectrumLerp[i]) spectrumLerp[i] = Mathf.Lerp(spectrumLerp[i], avg, Time.deltaTime * increaseSpeed);
                else spectrumLerp[i] = Mathf.Lerp(spectrumLerp[i], avg, Time.deltaTime * decreaseSpeed);
               
                float percent = (float)i / (points.Length - 1);
                points[i].position = positions[i] + Vector3.up * maxOffset * spectrumLerp[i] * spectrumMultiply.Evaluate(percent);
            }
            computer.SetPoints(points);
        }
    }
}
