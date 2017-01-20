using UnityEngine;
namespace Dreamteck.Splines
{

    public interface ISplineTool
    {
        string GetName();
        void Draw(Rect rect);
        void Close();
    }
}
