using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Small palette-driven light rig for colored spill onto the floor and nearby geometry.</summary>
    [DisallowMultipleComponent]
    public sealed class FlowerPaletteLighting : MonoBehaviour
    {
        [Serializable]
        public sealed class Sector
        {
            public Light light;
            [Tooltip("Petal indices contributing to this sector, including independent palette changes.")]
            public int[] petals = Array.Empty<int>();
        }

        [SerializeField] private FlowerPaletteController flower;
        [SerializeField] private Sector[] sectors = Array.Empty<Sector>();
        [Tooltip("Spill strength at a reference radiance of 3. Independent of bloom.")]
        [SerializeField, Min(0f)] private float intensity = 0.12f;
        [Tooltip("Seconds of smoothing as the palette wave crosses a petal.")]
        [SerializeField, Min(0f)] private float responseTime = 0.12f;
        private Color[] smoothed;
        private bool initialized;

        private void OnEnable()
        {
            smoothed = new Color[sectors.Length];
            initialized = false;
        }

        private void LateUpdate()
        {
            if (flower == null || !flower.isActiveAndEnabled)
            {
                SwitchOff();
                initialized = false;
                return;
            }
            if (smoothed == null || smoothed.Length != sectors.Length)
            {
                smoothed = new Color[sectors.Length];
                initialized = false;
            }
            float blend = !initialized || responseTime <= 0f ? 1f :
                1f - Mathf.Exp(-Time.deltaTime / responseTime);
            for (int i = 0; i < sectors.Length; i++)
            {
                var sector = sectors[i];
                if (sector == null || sector.light == null) continue;
                Color radiance = Color.black;
                int count = 0;
                if (sector.petals != null)
                    foreach (int petal in sector.petals)
                        if (flower.TryGetPetalLightSample(petal, out var sample))
                        {
                            radiance += sample;
                            count++;
                        }
                if (count > 0) radiance /= count;
                smoothed[i] = Color.Lerp(smoothed[i], radiance, blend);
                float peak = Mathf.Max(smoothed[i].r, Mathf.Max(smoothed[i].g, smoothed[i].b));
                sector.light.enabled = peak > 0.0001f && intensity > 0f;
                if (!sector.light.enabled) continue;
                Color hue = (smoothed[i] / peak).gamma;
                hue.a = 1f;
                sector.light.color = hue;
                sector.light.intensity = intensity * peak / 3f;
            }
            initialized = true;
        }

        private void OnDisable() => SwitchOff();

        private void SwitchOff()
        {
            foreach (var sector in sectors)
                if (sector != null && sector.light != null) sector.light.enabled = false;
        }
    }
}
