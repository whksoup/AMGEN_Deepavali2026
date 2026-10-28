using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Amgen.Deepavali
{
    /// <summary>Test-scene input only. The reusable petal prefab has no input dependency.</summary>
    public sealed class PetalPulseInput : MonoBehaviour
    {
        [SerializeField] private PetalMotionController petal;

        private void Update()
        {
            if (petal == null) return;

#if ENABLE_INPUT_SYSTEM
            bool pressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
#else
            bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)
                || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#endif
            if (pressed) petal.TriggerPulse();
        }
    }
}
