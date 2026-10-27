using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Keeps mouse-to-touch simulation available to TouchAreaButton across panel changes.
/// Attach to an always-active scene object, not a panel that gets hidden.
/// </summary>
[DefaultExecutionOrder(10000)]
public class MouseTouchPrototype : MonoBehaviour
{
    [SerializeField] private bool logHeldMousePosition = true;

    private void OnEnable()
    {
        TouchSimulation.Enable();
    }

    private void Update()
    {
        // TouchAreaButton.OnDisable disables the global simulator when a panel closes.
        // Enable is idempotent; restore it after the other scripts update their panels.
        TouchSimulation.Enable();

        // TouchSimulation consumes Input System mouse events, so use the enabled legacy backend for logging.
        if (logHeldMousePosition && Input.GetMouseButton(0))
        {
            Vector2 position = Input.mousePosition;
            Debug.Log($"[MouseTouchPrototype] Left held | frame {Time.frameCount} | screen pixels ({position.x:F1}, {position.y:F1})", this);
        }
    }

    private void OnDisable()
    {
        TouchSimulation.Disable();
    }
}
