using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Amgen.Deepavali
{
    /// <summary>Routes Game.unity's native touch/sensor screen coordinates into the wall's shared pointer.</summary>
    [DefaultExecutionOrder(-550), DisallowMultipleComponent]
    public sealed class DeepavaliTouchInput : MonoBehaviour
    {
        public ScreenRestorationController restoration;
        [Tooltip("Temporary mouse hover preview. Leave off for the installed sensor/touch input.")]
        public bool mousePreview;
        public bool HasSensorContact => trackedFinger != null;
        public int ContactId => trackedFinger != null ? trackedId : -1;

        private Finger trackedFinger;
        private int trackedId;
        private double trackedStartTime;
        private bool previousMouseInput, previousPreview, touchSupportEnabled;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (restoration == null) restoration = GetComponent<ScreenRestorationController>();
            if (restoration == null) { enabled = false; return; }
            previousMouseInput = restoration.useMouseInput;
            previousPreview = mousePreview;
            restoration.useMouseInput = mousePreview;
            restoration.ClearPointer();
            // Same native touch API used by Game.unity's TouchAreaButton; do not simulate mouse touches.
            EnhancedTouchSupport.Enable();
            touchSupportEnabled = true;
        }

        private void Update()
        {
            if (restoration == null || restoration.interactionCamera == null) return;
            if (mousePreview != previousPreview)
            {
                ReleaseContact();
                previousPreview = mousePreview;
            }
            restoration.useMouseInput = mousePreview;
            if (mousePreview) return;

            Touch selected = default;
            var touches = Touch.activeTouches;
            // Retain one contact until it ends; another visitor cannot steal a brush or a partly filled dwell.
            foreach (var touch in touches)
                if (IsSensorTouch(touch) && touch.finger == trackedFinger && touch.touchId == trackedId && touch.startTime == trackedStartTime)
                { selected = touch; break; }
            if (!selected.valid)
            {
                foreach (var touch in touches)
                    if (IsSensorTouch(touch) && AcceptsPoint(touch.screenPosition))
                    { selected = touch; break; }
            }
            if (!selected.valid) { ReleaseContact(); return; }

            if (selected.finger != trackedFinger || selected.touchId != trackedId || selected.startTime != trackedStartTime)
            {
                // Break interpolation and restart dwell even when replacement contacts are close together.
                restoration.ClearPointer();
                trackedFinger = selected.finger;
                trackedId = selected.touchId;
                trackedStartTime = selected.startTime;
            }
            restoration.SetPointerScreen(selected.screenPosition, AcceptsPoint(selected.screenPosition));
        }

        private bool IsSensorTouch(Touch touch) => touch.valid && touch.isInProgress &&
            touch.finger.screen.added && touch.finger.screen.enabled &&
            touch.displayIndex == restoration.interactionCamera.targetDisplay &&
            (TouchSimulation.instance == null || touch.finger.screen != TouchSimulation.instance.simulatedTouchscreen);

        private bool AcceptsPoint(Vector2 point) => float.IsFinite(point.x) && float.IsFinite(point.y) &&
            restoration.interactionCamera.pixelRect.Contains(point) &&
            !(restoration.BlocksMouseScreenPoint?.Invoke(point) ?? false);

        private void ReleaseContact()
        {
            // Clear on release, cancellation, device loss and mode changes; never leave a last XY held down.
            if (trackedFinger != null || restoration.HasPointer) restoration.ClearPointer();
            trackedFinger = null;
            trackedId = 0;
            trackedStartTime = 0;
        }

        private void OnDisable()
        {
            if (!touchSupportEnabled) return;
            if (restoration != null)
            {
                ReleaseContact();
                restoration.useMouseInput = previousMouseInput;
            }
            if (touchSupportEnabled) EnhancedTouchSupport.Disable();
            touchSupportEnabled = false;
        }
    }
}
