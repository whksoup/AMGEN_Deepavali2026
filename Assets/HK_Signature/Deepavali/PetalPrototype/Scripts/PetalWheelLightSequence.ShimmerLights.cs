using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class PetalWheelLightSequence
    {
        [Header("Shimmer point lights")]
        public bool shimmerPointLights = true;
        [Tooltip("Faint real-time colored light above each petal, multiplied by its shimmer level.")]
        [Range(0, .5f)] public float shimmerPointIntensity = .12f;
        [Tooltip("Height above the petal, as a fraction of its length.")]
        [Range(.1f, 1)] public float shimmerPointHeight = .25f;
        [Tooltip("Spherical light range, as a fraction of the petal's length.")]
        [Range(.3f, 2)] public float shimmerPointRange = .85f;

        private GameObject shimmerLightRoot;
        private Light[] shimmerLights;
        private Bounds[] shimmerPetalBounds;
        private static readonly int ShimmerFill = Shader.PropertyToID("_BaseColor");
        private static readonly int ShimmerTargetFill = Shader.PropertyToID("_TargetBaseColor");
        private static readonly int ShimmerPaletteAlpha = Shader.PropertyToID("_PatternPaletteAlpha");

        private void EnsureShimmerLights()
        {
            if (shimmerLights != null || !shimmerPointLights) return;
            shimmerLightRoot = new GameObject("Shimmer Petal Point Lights") { hideFlags = HideFlags.DontSave };
            shimmerLightRoot.transform.SetParent(transform, false);
            shimmerLights = new Light[flower.PetalCount];
            shimmerPetalBounds = new Bounds[flower.PetalCount];
            for (int i = 0; i < shimmerLights.Length; i++)
            {
                var surface = flower.GetPetalRenderer(i);
                var skinned = surface as SkinnedMeshRenderer;
                var filter = surface.GetComponent<MeshFilter>();
                var mesh = skinned != null ? skinned.sharedMesh : filter != null ? filter.sharedMesh : null;
                // Skinned renderer bounds are padded for all poses; use the actual resting mesh.
                shimmerPetalBounds[i] = mesh != null ? mesh.bounds : surface.localBounds;
                var go = new GameObject($"Petal {i + 1:00} Shimmer Light") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(shimmerLightRoot.transform, false);
                var point = go.AddComponent<Light>();
                point.type = LightType.Point;
                point.lightmapBakeType = LightmapBakeType.Realtime;
                point.shadows = LightShadows.None;
                point.bounceIntensity = 0;
                point.intensity = 0;
                point.enabled = false;
                shimmerLights[i] = point;
            }
        }

        private void UpdateShimmerLight(int index, float level)
        {
            if (shimmerLights == null) return;
            var point = shimmerLights[index];
            // Carry the shimmer gently into the entry pulse, then switch off for the wheel/result.
            float envelope = Pattern.Mode == PetalWheelMode.Shimmer ? 1 :
                Pattern.Mode == PetalWheelMode.Pulse ? 1 - PetalWheelPattern.Ease(
                    Pattern.StageTime / Mathf.Max(.001f, timing.pulseLength)) : 0;
            float intensity = shimmerPointLights ? Mathf.Max(0, shimmerPointIntensity) * Mathf.Clamp01(level) * envelope : 0;
            point.intensity = intensity;
            point.enabled = intensity > .0001f && flower.isActiveAndEnabled;
            if (!point.enabled) return;

            var surface = flower.GetPetalRenderer(index);
            var bounds = shimmerPetalBounds[index];
            float length = Mathf.Max(.01f, surface.transform.TransformVector(Vector3.up * bounds.size.y).magnitude);
            point.transform.position = surface.transform.TransformPoint(bounds.center)
                + flower.transform.up * (length * shimmerPointHeight);
            point.range = Mathf.Max(.01f, length * shimmerPointRange);

            // Use the petal's body color, so the gold edge does not turn every light gold.
            var material = surface.sharedMaterial;
            float blend = flower.TransitionProgress * material.GetFloat(ShimmerPaletteAlpha);
            Color hue = Color.Lerp(material.GetColor(ShimmerFill), material.GetColor(ShimmerTargetFill), blend);
            float peak = Mathf.Max(hue.r, Mathf.Max(hue.g, hue.b));
            hue = peak > .0001f ? hue / peak : Color.black;
            hue.a = 1;
            point.color = hue;
        }

        private void ReleaseShimmerLights()
        {
            if (shimmerLights != null)
                foreach (var point in shimmerLights) if (point != null) point.enabled = false;
            if (shimmerLightRoot != null) Destroy(shimmerLightRoot);
            shimmerLights = null;
            shimmerPetalBounds = null;
            shimmerLightRoot = null;
        }
    }
}
