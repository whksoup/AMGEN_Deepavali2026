using UnityEngine;

[ExecuteAlways] // optional if you want updates in edit mode too
public class DynamicCameraSize : MonoBehaviour
{
    [Header("Reference Camera")]
    public Camera cameraA;   // The camera to follow

    [Header("Scale Factor")]
    public float scaleFactor = 0.5f; // multiply Camera A's size

    private Camera cameraB;

    void Awake()
    {
        cameraB = GetComponent<Camera>();
        if (cameraB == null)
            Debug.LogError("DynamicCameraSize must be attached to a Camera!");
    }

    void Update()
    {
        if (cameraA != null && cameraB != null)
        {
            cameraB.orthographicSize = cameraA.orthographicSize * scaleFactor;
        }
    }
}
