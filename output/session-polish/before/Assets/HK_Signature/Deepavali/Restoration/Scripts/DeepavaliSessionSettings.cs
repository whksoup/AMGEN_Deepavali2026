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
        [Header("Scroll (seconds)")]
        [Min(0)] public float scrollDelayAfterResultSeconds = 2;
        [Tooltip("Time fully open, excluding the opening and closing clips.")]
        [Min(0)] public float scrollHoldSeconds = 15;

        public void CaptureFrom(DeepavaliSessionController s)
        {
            openingDwellDuration = s.openingDwellDuration; spinDwellDuration = s.spinDwellDuration;
            requiredCoverage = s.requiredCoverage; coloredHoldDuration = s.coloredHoldDuration;
            centerRevealRadius = s.centerRevealRadius; centerLightIntensity = s.centerLightIntensity;
            centerLightFadeDuration = s.centerLightFadeDuration;
            scrollDelayAfterResultSeconds = s.scrollDelayAfterResultSeconds; scrollHoldSeconds = s.scrollHoldSeconds;
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
            s.scrollDelayAfterResultSeconds = Safe(scrollDelayAfterResultSeconds, 2);
            s.scrollHoldSeconds = Safe(scrollHoldSeconds, 15);
        }
        public static float Safe(float value, float fallback, float min = 0) => float.IsFinite(value) ? Mathf.Max(min, value) : fallback;
        private void Sanitize()
        {
            scrollDelayAfterResultSeconds = Safe(scrollDelayAfterResultSeconds, 2);
            scrollHoldSeconds = Safe(scrollHoldSeconds, 15);
        }
        private void OnValidate() => Sanitize();
    }
}
