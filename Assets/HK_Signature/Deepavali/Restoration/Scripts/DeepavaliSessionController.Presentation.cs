using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class DeepavaliSessionController
    {
        [Header("Presentation tuning (session only)")]
        [Tooltip("Pitch offset from the authored camera, orbiting around the flower. Zero restores the authored view.")]
        [Range(-45, 20)] public float cameraPitchOffset;
        [Tooltip("Peak chase speed in inner-petal slots per second. Applied to the next spin.")]
        [Min(.1f)] public float spinSpeed = 16;
        [Tooltip("Minimum cruise time. Landing alignment can add up to one revolution.")]
        [Min(0)] public float spinCruiseSeconds = 1.2f;
        [Min(.1f)] public float spinSlowdownSeconds = 3.6f;
        [Header("Result logo")]
        [Tooltip("Screen-space Image with a non-interactive CanvasGroup. Replace its sprite with the approved logo later.")]
        public CanvasGroup resultLogo;
        [Min(.01f)] public float logoFadeSeconds = 1;

        private Transform pitchCamera;
        private Vector3 authoredCameraPosition, pitchPivot;
        private Quaternion authoredCameraRotation;
        private float appliedPitch = float.NaN;

        public void ApplyPresentationSettings()
        {
            if (sequence != null && sequence.timing != null)
            {
                // Keep the live pattern's timing reference; never write the shared wheel preset.
                sequence.timing.speed = DeepavaliSessionSettings.Safe(spinSpeed, 16, .1f);
                sequence.timing.cruise = DeepavaliSessionSettings.Safe(spinCruiseSeconds, 1.2f);
                sequence.timing.slowdown = DeepavaliSessionSettings.Safe(spinSlowdownSeconds, 3.6f, .1f);
            }
            ApplyCameraPitch();
        }

        private void ApplyCameraPitch()
        {
            if (restoration == null || restoration.interactionCamera == null || flower == null) return;
            var target = restoration.interactionCamera.transform;
            if (pitchCamera != target)
            {
                RestoreCameraPitch();
                pitchCamera = target;
                authoredCameraPosition = target.position;
                authoredCameraRotation = target.rotation;
                pitchPivot = flower.transform.position;
            }
            float pitch = float.IsFinite(cameraPitchOffset) ? Mathf.Clamp(cameraPitchOffset, -45, 20) : 0;
            if (pitch == appliedPitch) return;
            var orbit = Quaternion.AngleAxis(pitch, authoredCameraRotation * Vector3.right);
            target.SetPositionAndRotation(pitchPivot + orbit * (authoredCameraPosition - pitchPivot),
                orbit * authoredCameraRotation);
            appliedPitch = pitch;
        }

        private void RestoreCameraPitch()
        {
            if (pitchCamera != null) pitchCamera.SetPositionAndRotation(authoredCameraPosition, authoredCameraRotation);
            pitchCamera = null;
            appliedPitch = float.NaN;
        }

        private void TickResultLogo()
        {
            if (resultLogo == null) return;
            bool visible = Phase == DeepavaliSessionPhase.ScrollHolding && scrollOverlay != null &&
                scrollOverlay.isActiveAndEnabled && scrollOverlay.CurrentState == DiwaliScrollOverlay.State.Shown;
            resultLogo.alpha = visible ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(ScrollPhaseElapsed /
                DeepavaliSessionSettings.Safe(logoFadeSeconds, 1, .01f))) : 0;
        }

        private void HideResultLogo()
        {
            if (resultLogo != null) resultLogo.alpha = 0;
        }
    }
}
