using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the gameplay panel so mouse collection is active only during gameplay.
/// Disable this component to turn off mouse prototyping.
/// </summary>
public class MouseGameInteraction : MonoBehaviour
{
    [SerializeField] private Camera interactionCamera;

    private void Update()
    {
        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        // TouchSimulation consumes Input System mouse events; the legacy backend remains available.
        // Let the existing touch handler own real touchscreen input.
        if (!Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            return;

        var cameraToUse = interactionCamera != null ? interactionCamera : Camera.main;
        if (cameraToUse == null)
            return;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var item = hit.collider.GetComponentInParent<GameItem>();
            if (item != null)
                item.OnTouch();
        }
    }
}
