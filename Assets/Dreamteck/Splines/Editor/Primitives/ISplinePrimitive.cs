using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    public interface ISplinePrimitive
    {
        string GetName();
        void Init(SplineComputer computer);
        void Draw();
        void Cancel();
        void SetOrigin(Vector3 origin);
    }
}
