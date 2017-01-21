using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AutoTile : MonoBehaviour
{
    private Renderer rend;

    public float coeficent = 2f;
    public EScaleAxis scaleXByAxis = EScaleAxis.X;
    public EScaleAxis scaleYByAxis = EScaleAxis.Y;

    public enum EScaleAxis
    {
        X,
        Y,
        Z,
    }

    void Start()
    {
		Setup();
    }

    [ContextMenu("Setup")]
    public void Setup()
    {
        rend = GetComponent<Renderer>();
        float scaleX = coeficent;
        float scaleY = coeficent;

        switch(scaleXByAxis)
        {
            case EScaleAxis.X: scaleX *= transform.lossyScale.x; break;
            case EScaleAxis.Y: scaleX *= transform.lossyScale.y; break;
            case EScaleAxis.Z: scaleX *= transform.lossyScale.z; break;
        }

        switch(scaleYByAxis)
        {
            case EScaleAxis.X: scaleY *= transform.lossyScale.x; break;
            case EScaleAxis.Y: scaleY *= transform.lossyScale.y; break;
            case EScaleAxis.Z: scaleY *= transform.lossyScale.z; break;
        }

        rend.sharedMaterial.mainTextureScale = new Vector2(scaleX, scaleY);
    }
}
