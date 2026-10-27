using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Amgen.Deepavali
{
    /// <summary>Routes pointer presses to the nearest visible petal, including simultaneous touches.</summary>
    [DisallowMultipleComponent]
    public sealed class PetalLayerPointerInput : MonoBehaviour
    {
        public event System.Action<PetalPickTarget> PetalPressed;
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private PetalPickTarget[] petals;
        [Tooltip("Static geometry that blocks petal picking, such as the center disc.")]
        [SerializeField] private Collider[] occluders;
        [Tooltip("Optional shared edge-light feedback after a successful petal pick.")]
        [SerializeField] private FlowerEmissionController flowerEmission;
        [Tooltip("Optional screen-space overlay UI areas that should consume pointer presses.")]
        [SerializeField] private RectTransform[] blockedScreenAreas;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool touching = false;
            var screen = Touchscreen.current;
            if (screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    touching |= touch.press.isPressed || touch.press.wasReleasedThisFrame;
                    if (touch.press.wasPressedThisFrame) TryPulseAtScreenPoint(touch.position.ReadValue());
                }
            }

            // Avoid processing a mouse event emulated from the same touch.
            if (!touching && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TryPulseAtScreenPoint(Mouse.current.position.ReadValue());
#else
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began) TryPulseAtScreenPoint(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0)) TryPulseAtScreenPoint(Input.mousePosition);
#endif
        }

        /// <summary>Screen pixels, bottom-left origin. Returns false for the disc, gaps, or background.</summary>
        public bool TryPulseAtScreenPoint(Vector2 screenPosition)
        {
            if (!isActiveAndEnabled || interactionCamera == null || petals == null ||
                !interactionCamera.pixelRect.Contains(screenPosition)) return false;
            if (blockedScreenAreas != null)
                foreach (var area in blockedScreenAreas)
                    if (area != null && area.gameObject.activeInHierarchy &&
                        RectTransformUtility.RectangleContainsScreenPoint(area, screenPosition)) return false;

            Ray ray = interactionCamera.ScreenPointToRay(screenPosition);
            float nearest = float.PositiveInfinity;
            if (occluders != null)
            {
                foreach (var occluder in occluders)
                    if (occluder != null && occluder.enabled && occluder.gameObject.activeInHierarchy &&
                        occluder.Raycast(ray, out RaycastHit hit, nearest)) nearest = hit.distance;
            }

            PetalPickTarget selected = null;
            foreach (var petal in petals)
            {
                if (petal == null || !petal.TryRaycast(ray, out float distance) || distance >= nearest) continue;
                selected = petal;
                nearest = distance;
            }

            if (selected == null) return false;
            selected.Motion.TriggerPulse();
            PetalPressed?.Invoke(selected);
            if (flowerEmission != null) flowerEmission.TriggerFlash();
            return true;
        }
    }
}
