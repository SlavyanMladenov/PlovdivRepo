using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples
{
    public class OffsetResult : MonoBehaviour
    {
        public SplineFollower follower;
        public float percentOffset = 0f;

        // Update is called once per frame
        void LateUpdate()
        {
            double percent = follower.followResult.percent;
            percent += percentOffset;
            SplineResult result = follower.computer.Evaluate(percent, follower.address);
            this.transform.position = result.position;
            this.transform.rotation = result.rotation;
        }
    }
}
