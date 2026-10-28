using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        // Opt-in clock control for the copied scene. Reuses the existing radial palette shader.
        public bool BeginPatternPalette(FlowerPaletteState state)
        {
            if (!TransitionTo(state)) return false;
            transitioning = false;
            SetProgress(0);
            return true;
        }

        public void SetPatternPaletteProgress(float value, float alpha = 1)
        {
            if (!Ready || !Finite(value) || !Finite(alpha)) return;
            SetProgress(Mathf.Clamp01(value));
            foreach (var material in runtimePetals) material.SetFloat("_PatternPaletteAlpha", Mathf.Clamp01(alpha));
            runtimeCenter.SetFloat("_PatternPaletteAlpha", Mathf.Clamp01(alpha));
            if (value >= 1) Array.Copy(targets, settled, targets.Length);
        }

        public void SetPatternLightHandoff(float value)
        {
            if (!Ready || !Finite(value)) return;
            foreach (var material in runtimePetals) material.SetFloat("_PatternLightHandoff", Mathf.Clamp01(value));
        }

        public void SetPatternPetalIllumination(int index, float intensity, float surfaceGlow)
        {
            if (!ValidPetal(index) || !Finite(intensity) || !Finite(surfaceGlow)) return;
            runtimePetals[index].SetFloat("_PatternEmissionGain", Mathf.Max(0, intensity));
            runtimePetals[index].SetFloat("_PatternSurfaceGlow", Mathf.Max(0, surfaceGlow));
        }

        public void SetPatternCenterIntensity(float intensity)
        {
            if (Ready && Finite(intensity)) runtimeCenter.SetFloat("_PatternEmissionGain", Mathf.Max(0, intensity));
        }
    }
}
