using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        private static readonly int SpatialEnabled = Shader.PropertyToID("_PetalLightEnabled");
        private static readonly int SpatialData = Shader.PropertyToID("_PetalLightData");
        private static readonly int SpatialColor = Shader.PropertyToID("_PetalLightColor");
        private static readonly int SpatialShape = Shader.PropertyToID("_PetalLightShape");
        private static readonly int SpatialTravel = Shader.PropertyToID("_PetalLightTravel");
        private static readonly int SpatialStyle = Shader.PropertyToID("_PetalLightStyle");
        private static readonly int SpatialTipReach = Shader.PropertyToID("_PetalLightTipReach");
        private static readonly int SpatialIconBrightness = Shader.PropertyToID("_PetalLightIconBrightness");

        // Material ownership remains here. Preview code never instantiates or replaces materials.
        public bool SetSpatialLighting(int index, PetalLightingSettings settings, Texture2D map, float clock, bool active)
        {
            if (!ValidPetal(index)) return false;
            var material = runtimePetals[index];
            material.SetFloat(SpatialEnabled, active && map != null && settings != null ? 1 : 0);
            if (!active || map == null || settings == null) return true;
            settings.Clamp();
            material.SetTexture(SpatialData, map);
            material.SetColor(SpatialColor, settings.color.linear);
            material.SetVector(SpatialShape, new Vector4(settings.shimmer, settings.gapCoverage, settings.gapDepth, settings.speed));
            material.SetVector(SpatialTravel, new Vector4(settings.progress, settings.tipBrightness, settings.edgeBrightness, settings.growth ? 1 : 0));
            material.SetVector(SpatialStyle, new Vector4(settings.washWidth, settings.washStrength, settings.seed, Finite(clock) ? clock : 0));
            material.SetFloat(SpatialTipReach, settings.tipReach);
            material.SetFloat(SpatialIconBrightness, settings.iconBrightness);
            return true;
        }

        public Renderer GetPetalRenderer(int index) => petals != null && index >= 0 && index < petals.Length ? petals[index] : null;
    }
}
