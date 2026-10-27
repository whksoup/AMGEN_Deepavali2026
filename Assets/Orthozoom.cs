using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraZoomLerp : MonoBehaviour
{
    private Camera cam;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f; // higher = faster
    private float targetSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetSize = cam.orthographicSize; // start from current size
    }

    private void Update()
    {
        if (Mathf.Abs(cam.orthographicSize - targetSize) > 0.01f)
        {
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize,
                targetSize,
                Time.deltaTime * zoomSpeed
            );
        }
    }

    /// <summary>
    /// Call this from other scripts to trigger a smooth zoom.
    /// </summary>
    public void SetTargetSize(float newSize)
    {
        targetSize = Mathf.Max(0.01f, newSize); // avoid invalid 0 size
    }
}
