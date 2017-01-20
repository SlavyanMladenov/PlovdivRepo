using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class EnvironmentSwitch : MonoBehaviour
    {
        [System.Serializable]
        public struct Environment
        {
            public Color skyColor;
            public Color lightColor;
            public float fogDensity;
        }

        public float changeSpeed = 3f;
        public Color skyColor;
        public Color lightColor;
        public float fogDensity = 0f;
        private float lerpPercent = 1f;
        private Color prevSkyColor;
        private Color prevLightColor;
        private float prevFogDensity = 0f;
        public Light sunLight;

        public Environment[] environments;


        // Use this for initialization
        void Start()
        {
            skyColor = SkyCamera.cam.backgroundColor;
            lightColor = sunLight.color;
            fogDensity = RenderSettings.fogDensity;
        }

        public void SetEnvironment(int index)
        {
            lerpPercent = 0f;
            prevSkyColor = skyColor;
            prevLightColor = lightColor;
            prevFogDensity = fogDensity;
            skyColor = environments[index].skyColor;
            lightColor = environments[index].lightColor;
            fogDensity = environments[index].fogDensity;
        }

        public void SetSkyColor(Color col)
        {
            lerpPercent = 0f;
            prevSkyColor = skyColor;
            skyColor = col;
        }

        public void SetLightColor(Color col)
        {
            lerpPercent = 0f;
            prevLightColor = lightColor;
            lightColor = col;
        }

        public void SetFogDensity(float density)
        {
            lerpPercent = 0f;
            prevFogDensity = fogDensity;
            fogDensity = density;
            prevSkyColor = skyColor;
        }

        // Update is called once per frame
        void Update()
        {
            if (lerpPercent < 1f)
            {
                SkyCamera.cam.backgroundColor = Color.Lerp(prevSkyColor, skyColor, lerpPercent);
                RenderSettings.fogColor = SkyCamera.cam.backgroundColor;
                sunLight.color = Color.Lerp(prevLightColor, lightColor, lerpPercent);
                RenderSettings.fogDensity = Mathf.Lerp(prevFogDensity, fogDensity, lerpPercent);
                lerpPercent = Mathf.MoveTowards(lerpPercent, 1f, changeSpeed * Time.deltaTime);
            }

        }
    }
}