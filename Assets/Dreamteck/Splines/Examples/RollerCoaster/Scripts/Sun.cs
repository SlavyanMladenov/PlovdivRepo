using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class Sun : MonoBehaviour
    {
        public float distance = 20f;
        public Light directionalLight;
        public Renderer sunRenderer;
        private Material material;

        // Use this for initialization
        void Start()
        {
            material = sunRenderer.sharedMaterial;
        }

        // Update is called once per frame
        void Update()
        {
            this.transform.position = SkyCamera.cam.transform.position - directionalLight.transform.forward * distance;
            this.transform.LookAt(SkyCamera.cam.transform.position);
            material.SetColor("_Color", directionalLight.color);
        }
    }
}