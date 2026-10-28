#ifndef DEEPAVALI_PETAL_SPATIAL_LIGHTING
#define DEEPAVALI_PETAL_SPATIAL_LIGHTING

float PetalLightHash(float n) { return frac(sin(n * 127.1 + 311.7) * 43758.5453); }
float PetalLightNoise(float x)
{
    float i = floor(x), f = frac(x);
    f = f * f * (3 - 2 * f);
    return lerp(PetalLightHash(i), PetalLightHash(i + 1), f);
}

// The map is in artwork UV space, so the light follows the skinned petal.
// R = nearest outline distance / (0.15 * petal length).
// G = arc length along either side, root=0, tip=1. B = authored gap variation.
// A = arrival time: sides first, then inward, finishing at the tip.
void ApplyPetalSpatialLighting(float2 uv, half edgeCoverage, half3 oldRim, inout SurfaceData surface)
{
    float4 data = SAMPLE_TEXTURE2D(_PetalLightData, sampler_PetalLightData, uv);
    float seed = _PetalLightStyle.z;
    float clock = _PetalLightStyle.w * _PetalLightShape.w;
    // Persistent dark gaps do not slide around with the shimmer.
    float threshold = lerp(-0.12, 1.12, saturate(_PetalLightShape.y));
    float pools = smoothstep(threshold - 0.10, threshold + 0.10, data.b);
    float gaps = lerp(1, pools, saturate(_PetalLightShape.z));
    float sideOffset = smoothstep(0.47, 0.53, uv.x) * 8 * (1 - smoothstep(0.85, 1, data.g));
    float broad = PetalLightNoise(data.g * 11 + sideOffset + seed + clock * 0.35);
    float fine = PetalLightNoise(data.g * 31 + sideOffset + seed * 3 - clock * 0.8);
    float shimmer = lerp(1, 0.38 + 1.15 * broad + 0.9 * pow(fine, 8), saturate(_PetalLightShape.x));

    float progress = saturate(_PetalLightTravel.x);
    float front = lerp(-0.08, 1.08, progress);
    float growth = lerp(1, 1 - smoothstep(front - 0.055, front + 0.055, data.a), saturate(_PetalLightTravel.w));
    float tipEnvelope = lerp(1, smoothstep(0.72, 1, progress), saturate(_PetalLightTravel.w));
    float tip = smoothstep(1 - clamp(_PetalLightTipReach, 0.05, 1), 0.995, uv.y) * tipEnvelope;
    float core = max(edgeCoverage, 1 - smoothstep(0.008, 0.035, data.r));
    float wash = (1 - smoothstep(0.015, max(0.02, _PetalLightStyle.x), data.r)) * _PetalLightStyle.y;
    float intensity = _PetalLightTravel.z * gaps * shimmer * growth;
    // Tip is a separately directed highlight, intentionally able to overcome a dark gap.
    float tipIntensity = tip * _PetalLightTravel.y;
    half3 warm = _PetalLightColor.rgb;
    half3 hot = lerp(warm, half3(1, 0.82, 0.46), 0.55h);
    half3 spatial = warm * (core + wash) * intensity + hot * (core + wash * 0.6) * tipIntensity;
    // Some artwork has emissive gold lines in addition to the explicit rim mask.
    // Let those follow the same arrival field, and suppress their edge contribution
    // in a dark gap; otherwise the outer petal would still have a continuous halo.
    float nearEdge = 1 - smoothstep(0.06, 0.3, data.r);
    half3 ornament = max(0, surface.emission - oldRim) * lerp(0.12, 1, growth) * lerp(1, gaps, nearEdge);
    surface.emission = ornament + spatial * _EmissionScale;
}
#endif
