#ifndef DEEPAVALI_PETAL_PALETTE_FORWARD
#define DEEPAVALI_PETAL_PALETTE_FORWARD
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half4 DeepavaliSurfaceLighting(InputData inputData, SurfaceData surfaceData)
{
    // The center is matte colored artwork. Diffuse lighting preserves its palette
    // hue without the neutral dielectric reflections that washed out dark fills.
    // This keeps URP light/shadow handling, baked GI, and the explicit ring/icon emission.
    half4 color = half4(0, 0, 0, surfaceData.alpha);
    if (_CenterSurface > 0.5h)
        color = UniversalFragmentBlinnPhong(inputData, surfaceData);
    else
        color = UniversalFragmentPBR(inputData, surfaceData);
    return color;
}

// Reuse the standard Lit vertex/fragment setup, fog, and rendering-layer output.
// Lighting.hlsl is already included, so this redirects only the forward-pass call.
#define UniversalFragmentPBR DeepavaliSurfaceLighting
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
#undef UniversalFragmentPBR
#endif
