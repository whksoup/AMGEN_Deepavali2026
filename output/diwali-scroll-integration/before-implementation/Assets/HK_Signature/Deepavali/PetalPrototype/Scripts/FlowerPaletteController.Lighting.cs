using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        /// <summary>
        /// Approximate linear radiance for a local spill light. Samples the same resting
        /// artwork coordinates and outward wave as the shader, including per-petal tones.
        /// This is an artistic light proxy, not a GI calculation.
        /// </summary>
        public bool TryGetPetalLightSample(int index, out Color radiance)
        {
            radiance = Color.black;
            if (!ValidPetal(index) || !TryAppearance(PaletteFor(index), settled[index], StartupFor(index), out var from) ||
                !TryAppearance(PaletteFor(index), targets[index], StartupFor(index), out var to)) return false;

            var mat = runtimePetals[index];
            Vector4 x = mat.GetVector(RestX), z = mat.GetVector(RestZ);
            float softness = Mathf.Max(0.001f, activeSoftness);
            float front = Mathf.Lerp(-softness, maxRadius + softness, progress);
            float blend = 0f;
            // Average along the petal so a narrow wave does not abruptly recolor a broad light.
            for (int sample = 0; sample < 3; sample++)
            {
                float v = 0.15f + sample * 0.35f;
                float radius = new Vector2(x.x * 0.5f + x.y * v + x.z,
                    z.x * 0.5f + z.y * v + z.z).magnitude;
                float t = Mathf.InverseLerp(front - softness, front + softness, radius);
                blend += 1f - t * t * (3f - 2f * t);
            }
            float rimScale = Mathf.Max(0f, mat.GetFloat("_RimGlowScale"));
            float ornamentScale = Mathf.Max(0f, mat.GetFloat("_OrnamentGlowScale"));
            radiance = Color.Lerp(SpillRadiance(from, rimScale, ornamentScale), SpillRadiance(to, rimScale, ornamentScale), blend / 3f)
                * Mathf.Max(0f, petalAppearances[index].emissionScale);
            radiance.a = 1f;
            return true;
        }

        private static Color SpillRadiance(FlowerPalette palette, float rimScale, float ornamentScale)
        {
            // The colored outline dominates; gold ornament contributes a little warmth.
            return EdgeHue(palette) * (Mathf.Max(0f, palette.edgeIntensity) * rimScale)
                + palette.ornament.linear * (Mathf.Max(0f, palette.mainPatternIntensity) * 0.15f * ornamentScale)
                + palette.accent.linear * (Mathf.Max(0f, palette.accentPatternIntensity) * 0.1f * ornamentScale)
                + (palette.tertiary.a > 0f ? palette.tertiary.linear : Color.black) * (Mathf.Max(0f, palette.accentPatternIntensity) * 0.05f * ornamentScale);
        }
    }
}
