using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class ParentObject : MonoBehaviour
    {
        public Transform targetChild;
        public Transform targetParent;

        void Start()
        {
            Parent();
        }

        public void Parent()
        {
            Quaternion rot = targetChild.localRotation;
            Vector3 pos = targetChild.localPosition;
            targetChild.parent = targetParent;
            targetChild.localRotation = rot;
            targetChild.localPosition = pos;
        }
    }
}