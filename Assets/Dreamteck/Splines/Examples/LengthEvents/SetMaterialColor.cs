using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class SetMaterialColor : MonoBehaviour
    {
        public string colorName = "";
        public Color[] colors;
        public MeshRenderer rend;

        public void SetColor(int index)
        {
            if (!Application.isPlaying) return;
            rend.material.SetColor(colorName, colors[index]);
        }

    }
}
