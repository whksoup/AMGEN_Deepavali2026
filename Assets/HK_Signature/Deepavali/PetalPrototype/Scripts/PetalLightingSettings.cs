using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    [Serializable]
    public sealed class PetalLightingSettings
    {
        [Range(0, 1)] public float shimmer = 0.65f;
        [Range(0, 1)] public float gapCoverage = 0.48f;
        [Range(0, 1)] public float gapDepth = 0.98f;
        [Range(0, 2)] public float speed = 0.6f;
        public bool growth = true;
        [Range(0, 1)] public float progress = 1;
        [Range(0, 20)] public float tipBrightness = 10;
        [Range(.05f, 1)] public float tipReach = .14f;
        [Range(0, 20)] public float iconBrightness;
        [Range(0, 20)] public float edgeBrightness = 7;
        [Range(0.02f, 0.8f)] public float washWidth = 0.25f;
        [Range(0, 1)] public float washStrength = 0.22f;
        public Color color = new Color(1f, 0.65f, 0.22f);
        public float seed = 1;

        private static float Safe(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        public void Clamp()
        {
            shimmer = Safe(shimmer, 0, 1, .65f); gapCoverage = Safe(gapCoverage, 0, 1, .48f);
            gapDepth = Safe(gapDepth, 0, 1, .98f); speed = Safe(speed, 0, 2, .6f);
            progress = Safe(progress, 0, 1, 1); tipBrightness = Safe(tipBrightness, 0, 20, 10);
            tipReach = Safe(tipReach, .05f, 1, .14f); iconBrightness = Safe(iconBrightness, 0, 20, 0);
            edgeBrightness = Safe(edgeBrightness, 0, 20, 7); washWidth = Safe(washWidth, .02f, .8f, .25f);
            washStrength = Safe(washStrength, 0, 1, .22f); seed = Safe(seed, 0, 1000, 1);
            color = new Color(Safe(color.r, 0, 1, 1), Safe(color.g, 0, 1, .65f), Safe(color.b, 0, 1, .22f), 1);
        }
    }
}
