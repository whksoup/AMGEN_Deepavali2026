using UnityEngine;

namespace Amgen.Deepavali
{
    [CreateAssetMenu(menuName = "Deepavali/Session Settings")]
    public sealed class DeepavaliSessionSettings : ScriptableObject
    {
        [Min(.01f)] public float openingDwellDuration = .65f, spinDwellDuration = .15f;
        [Range(.1f, 1)] public float requiredCoverage = .8f;
        [Min(0)] public float coloredHoldDuration = 10;
        [Range(.02f, .4f)] public float centerRevealRadius = .16f;
        [Min(0)] public float centerLightIntensity = .6f;
        [Min(.01f)] public float centerLightFadeDuration = .6f;
        [Header("Restoration")]
        [Tooltip("Brush radius as a fraction of viewport height. .32 is twice the original .16 radius.")]
        [Range(.02f, .4f)] public float restorationBrushRadius = .32f;
        [Header("Inactivity (seconds)")]
        [Min(.1f)] public float inactivityTimeout = 30;
        [Min(.1f)] public float inactivityFadeDuration = 2;
        [Header("Scroll (seconds)")]
        [Min(0)] public float scrollDelayAfterResultSeconds = 2;
        [Tooltip("Time fully open, excluding the opening and closing clips.")]
        [Min(0)] public float scrollHoldSeconds = 15;
        [Min(0)] public float greetingHoldSeconds = 4;
        [Header("Session presentation (does not edit the shared wheel preset)")]
        [Range(-45, 20)] public float cameraPitchOffset;
        [Min(.1f)] public float spinSpeed = 16;
        [Min(0)] public float spinCruiseSeconds = 1.2f;
        [Min(.1f)] public float spinSlowdownSeconds = 3.6f;
        [Min(.01f)] public float logoFadeSeconds = 1;

        public void CaptureFrom(DeepavaliSessionController s)
        {
            openingDwellDuration = s.openingDwellDuration; spinDwellDuration = s.spinDwellDuration;
            requiredCoverage = s.requiredCoverage; coloredHoldDuration = s.coloredHoldDuration;
            centerRevealRadius = s.centerRevealRadius; centerLightIntensity = s.centerLightIntensity;
            centerLightFadeDuration = s.centerLightFadeDuration;
            if (s.restoration != null) restorationBrushRadius = s.restoration.brushRadius;
            inactivityTimeout = s.inactivityTimeout; inactivityFadeDuration = s.inactivityFadeDuration;
            scrollDelayAfterResultSeconds = s.scrollDelayAfterResultSeconds; scrollHoldSeconds = s.scrollHoldSeconds;
            greetingHoldSeconds = s.greetingHoldSeconds;
            cameraPitchOffset = s.cameraPitchOffset;
            spinSpeed = s.spinSpeed; spinCruiseSeconds = s.spinCruiseSeconds; spinSlowdownSeconds = s.spinSlowdownSeconds;
            logoFadeSeconds = s.logoFadeSeconds;
            Sanitize();
        }
        public void ApplyTo(DeepavaliSessionController s)
        {
            s.openingDwellDuration = Safe(openingDwellDuration, .65f, .01f);
            s.spinDwellDuration = Safe(spinDwellDuration, .15f, .01f);
            s.requiredCoverage = Mathf.Clamp(Safe(requiredCoverage, .8f), .1f, 1);
            s.coloredHoldDuration = Safe(coloredHoldDuration, 10);
            s.centerRevealRadius = Mathf.Clamp(Safe(centerRevealRadius, .16f), .02f, .4f);
            s.centerLightIntensity = Safe(centerLightIntensity, .6f);
            s.centerLightFadeDuration = Safe(centerLightFadeDuration, .6f, .01f);
            if (s.restoration != null)
                s.restoration.brushRadius = Mathf.Clamp(Safe(restorationBrushRadius, .32f), .02f, .4f);
            s.inactivityTimeout = Safe(inactivityTimeout, 30, .1f);
            s.inactivityFadeDuration = Safe(inactivityFadeDuration, 2, .1f);
            s.scrollDelayAfterResultSeconds = Safe(scrollDelayAfterResultSeconds, 2);
            s.scrollHoldSeconds = Safe(scrollHoldSeconds, 15);
            s.greetingHoldSeconds = Safe(greetingHoldSeconds, 4);
            s.cameraPitchOffset = float.IsFinite(cameraPitchOffset) ? Mathf.Clamp(cameraPitchOffset, -45, 20) : 0;
            s.spinSpeed = Safe(spinSpeed, 16, .1f);
            s.spinCruiseSeconds = Safe(spinCruiseSeconds, 1.2f);
            s.spinSlowdownSeconds = Safe(spinSlowdownSeconds, 3.6f, .1f);
            s.logoFadeSeconds = Safe(logoFadeSeconds, 1, .01f);
            s.ApplyPresentationSettings();
        }
        public static float Safe(float value, float fallback, float min = 0) => float.IsFinite(value) ? Mathf.Max(min, value) : fallback;
        private void Sanitize()
        {
            restorationBrushRadius = Mathf.Clamp(Safe(restorationBrushRadius, .32f), .02f, .4f);
            inactivityTimeout = Safe(inactivityTimeout, 30, .1f);
            inactivityFadeDuration = Safe(inactivityFadeDuration, 2, .1f);
            scrollDelayAfterResultSeconds = Safe(scrollDelayAfterResultSeconds, 2);
            scrollHoldSeconds = Safe(scrollHoldSeconds, 15);
            greetingHoldSeconds = Safe(greetingHoldSeconds, 4);
            cameraPitchOffset = float.IsFinite(cameraPitchOffset) ? Mathf.Clamp(cameraPitchOffset, -45, 20) : 0;
            spinSpeed = Safe(spinSpeed, 16, .1f);
            spinCruiseSeconds = Safe(spinCruiseSeconds, 1.2f);
            spinSlowdownSeconds = Safe(spinSlowdownSeconds, 3.6f, .1f);
            logoFadeSeconds = Safe(logoFadeSeconds, 1, .01f);
        }
        private void OnValidate() => Sanitize();
    }
}
