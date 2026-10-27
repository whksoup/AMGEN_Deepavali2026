using UnityEngine;

[ExecuteAlways] // Allows the script to run in edit mode too
public class DynamicZoom : MonoBehaviour
{
    [Header("Camera Zoom Lerp Reference")]
    public CameraZoomLerp zoomLerp;

    [Header("Target Orthographic Size")]
    public float targetSize = 5f;

    void Update()
    {
        if (zoomLerp != null)
        {
            zoomLerp.SetTargetSize(targetSize);
        }
    }
}
