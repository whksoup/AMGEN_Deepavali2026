using UnityEngine;

public class BlurCameraFollower : MonoBehaviour
{
    public Camera mainCamera;
    private Camera blurCamera;

    void Awake()
    {
        blurCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Copy transform
        transform.position = mainCamera.transform.position;
        transform.rotation = mainCamera.transform.rotation;

        // Copy projection
        blurCamera.fieldOfView = mainCamera.fieldOfView;
        blurCamera.nearClipPlane = mainCamera.nearClipPlane;
        blurCamera.farClipPlane = mainCamera.farClipPlane;
        blurCamera.orthographic = mainCamera.orthographic;
        blurCamera.orthographicSize = mainCamera.orthographicSize;
    }
}
