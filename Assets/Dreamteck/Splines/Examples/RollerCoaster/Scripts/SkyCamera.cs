using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class SkyCamera : MonoBehaviour
    {
        public static Camera cam;
        private static Camera mainCam;
        bool lastFog = false;

        // Use this for initialization
        void Awake()
        {
            mainCam = Camera.main;
            cam = GetComponent<Camera>();
        }

        void OnPreRender()
        {
            lastFog = RenderSettings.fog;
            RenderSettings.fog = false;
        }

        void OnPostRender()
        {
            RenderSettings.fog = lastFog;
        }

        // Update is called once per frame
        void Update()
        {
            cam.fieldOfView = mainCam.fieldOfView;
            cam.transform.rotation = mainCam.transform.rotation;
        }
    }
}