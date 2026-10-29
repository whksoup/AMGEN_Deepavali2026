using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class DeepavaliSessionController
    {
        [Header("Happy Deepavali finale")]
        public HappyDeepavaliOverlay greetingOverlay;
        [Tooltip("Fully written greeting hold, excluding its opening and closing animations. Four seconds is one idle loop.")]
        [Min(0)] public float greetingHoldSeconds = 4;
        private double greetingPhaseStarted;

        // Once the result is ready, finish this bounded presentation before returning to restoration.
        // Explicit EndSequence / disable always cancel immediately; media watchdogs remain active.
        private bool IsResultPresentation => Phase == DeepavaliSessionPhase.ScrollDelay ||
            Phase == DeepavaliSessionPhase.ScrollOpening || Phase == DeepavaliSessionPhase.ScrollHolding ||
            Phase == DeepavaliSessionPhase.ScrollClosing || Phase == DeepavaliSessionPhase.HoldingResult ||
            Phase == DeepavaliSessionPhase.GreetingOpening || Phase == DeepavaliSessionPhase.GreetingHolding ||
            Phase == DeepavaliSessionPhase.GreetingClosing;

        private void BeginGreeting()
        {
            HideResultLogo();
            if (greetingOverlay == null || !greetingOverlay.isActiveAndEnabled)
            { endSequence(); return; }
            greetingOverlay.showOnStart = greetingOverlay.toggleOnMouseClick = false;
            greetingOverlay.CancelImmediate();
            SetGreetingPhase(DeepavaliSessionPhase.GreetingOpening);
            greetingOverlay.Show();
        }

        private void SetGreetingPhase(DeepavaliSessionPhase phase)
        {
            Phase = phase;
            greetingPhaseStarted = Time.realtimeSinceStartupAsDouble;
        }

        private void TickGreeting()
        {
            if (greetingOverlay == null || !greetingOverlay.isActiveAndEnabled ||
                !string.IsNullOrEmpty(greetingOverlay.LastError) ||
                greetingOverlay.CurrentState == HappyDeepavaliOverlay.State.Hidden)
            { endSequence(); return; }
            if (Phase == DeepavaliSessionPhase.GreetingOpening &&
                greetingOverlay.CurrentState == HappyDeepavaliOverlay.State.Shown)
                SetGreetingPhase(DeepavaliSessionPhase.GreetingHolding);
            else if (Phase == DeepavaliSessionPhase.GreetingHolding &&
                Time.realtimeSinceStartupAsDouble - greetingPhaseStarted >= DeepavaliSessionSettings.Safe(greetingHoldSeconds, 4))
            {
                SetGreetingPhase(DeepavaliSessionPhase.GreetingClosing);
                greetingOverlay.Hide();
            }
        }

        private void CancelGreeting()
        {
            if (greetingOverlay != null) greetingOverlay.CancelImmediate();
            greetingPhaseStarted = 0;
        }
    }
}
