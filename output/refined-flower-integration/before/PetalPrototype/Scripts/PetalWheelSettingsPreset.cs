using UnityEngine;

namespace Amgen.Deepavali
{
    [CreateAssetMenu(menuName = "Deepavali/Petal Wheel Settings")]
    public sealed class PetalWheelSettingsPreset : ScriptableObject
    {
        public PetalWheelTiming timing = new PetalWheelTiming();
        [Header("Light visibility")]
        [Range(1, 8)] public float shimmerIntensity = 2.99f;
        [Range(0, 8)] public float outerLightIntensity = 1.9f;
        [Range(0, 8)] public float outerShimmerIntensity = .36f;
        [Range(0, 8)] public float outerSurfaceGlow = 3;
        [Header("Shimmer point lights")]
        public bool shimmerPointLights = true;
        [Range(0, .5f)] public float shimmerPointIntensity = .12f;
        [Range(.1f, 1)] public float shimmerPointHeight = .25f;
        [Range(.3f, 2)] public float shimmerPointRange = .85f;
        [Header("Preview")]
        public int variationSeed = 137;
        [Min(.1f)] public float previewShimmerSeconds = 6, previewReadySeconds = 3;

        public void CaptureFrom(PetalWheelLightSequence source)
        {
            // Copy the complete timing object: live sliders must never edit the asset implicitly.
            timing = JsonUtility.FromJson<PetalWheelTiming>(JsonUtility.ToJson(source.timing));
            shimmerIntensity = source.shimmerIntensity;
            outerLightIntensity = source.outerLightIntensity;
            outerShimmerIntensity = source.outerShimmerIntensity;
            outerSurfaceGlow = source.outerSurfaceGlow;
            shimmerPointLights = source.shimmerPointLights;
            shimmerPointIntensity = source.shimmerPointIntensity;
            shimmerPointHeight = source.shimmerPointHeight;
            shimmerPointRange = source.shimmerPointRange;
            variationSeed = source.variationSeed;
            previewShimmerSeconds = source.previewShimmerSeconds;
            previewReadySeconds = source.previewReadySeconds;
        }

        public void ApplyTo(PetalWheelLightSequence destination)
        {
            // Preserve the live pattern's timing reference when loading during a preview.
            if (destination.timing == null) destination.timing = new PetalWheelTiming();
            if (timing != null) JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(timing), destination.timing);
            destination.shimmerIntensity = shimmerIntensity;
            destination.outerLightIntensity = outerLightIntensity;
            destination.outerShimmerIntensity = outerShimmerIntensity;
            destination.outerSurfaceGlow = outerSurfaceGlow;
            destination.shimmerPointLights = shimmerPointLights;
            destination.shimmerPointIntensity = shimmerPointIntensity;
            destination.shimmerPointHeight = shimmerPointHeight;
            destination.shimmerPointRange = shimmerPointRange;
            destination.variationSeed = variationSeed;
            destination.previewShimmerSeconds = previewShimmerSeconds;
            destination.previewReadySeconds = previewReadySeconds;
        }
    }
}
