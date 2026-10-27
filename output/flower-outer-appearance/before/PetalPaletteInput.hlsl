#ifndef DEEPAVALI_PETAL_PALETTE_INPUT
#define DEEPAVALI_PETAL_PALETTE_INPUT
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

// Identical layout in every pass keeps the shader compatible with the SRP Batcher.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
float4 _SymbolAtlas_TexelSize;
float4 _SymbolRect;
float4 _TargetSymbolRect;
float _SymbolMaxMip;
half _SymbolEnabled;
half _TargetSymbolEnabled;
float4 _ArtworkUvToRoot;
float _ArtworkRadius;
float _ArtworkProgress;
float _ArtworkSoftness;
half4 _BaseColor;
half4 _PatternColor;
half4 _AccentColor;
half4 _TargetBaseColor;
half4 _TargetPatternColor;
half4 _TargetAccentColor;
float4 _RestUvToFlowerX;
float4 _RestUvToFlowerZ;
float _WaveProgress;
float _WaveRadius;
float _WaveSoftness;
half _EdgeIntensity;
half _TargetEdgeIntensity;
half _EmissionScale;
half _CenterSurface;
half _Smoothness;
half _Surface;
CBUFFER_END

TEXTURE2D(_SymbolAtlas);
SAMPLER(sampler_SymbolAtlas);

half3 SampleArtwork(float2 uv, float2 uvDx, float2 uvDy, float4 rect, half enabled,
    half3 original, half3 fill, half3 main, half3 accent, half border)
{
    if (enabled < 0.5h) return original;
    float2 atlasUV = saturate(uv) * rect.xy + rect.zw;
    // Derivatives are computed before the reveal's per-pixel branch.
    float2 dx = uvDx * rect.xy * _SymbolAtlas_TexelSize.zw;
    float2 dy = uvDy * rect.xy * _SymbolAtlas_TexelSize.zw;
    float lod = min(_SymbolMaxMip, 0.5 * log2(max(1, max(dot(dx,dx), dot(dy,dy)))));
    half4 symbol = SAMPLE_TEXTURE2D_LOD(_SymbolAtlas, sampler_SymbolAtlas, atlasUV, lod);
    half3 replacement = lerp(fill, lerp(accent, main, symbol.g), symbol.r) * max(0.5h, symbol.b * 1.15h);
    // Erase blank spaces and old pigment detail; preserve the geometric border.
    // Restore compressed near-opaque alpha to 1 to prevent old ornament bleeding through.
    half eraseCoverage = saturate((symbol.a - 0.01h) / 0.98h);
    return lerp(original, replacement, eraseCoverage * (1 - border));
}

void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData surface)
{
    float3 coord = float3(uv, 1);
    float2 rest = float2(dot(coord, _RestUvToFlowerX.xyz), dot(coord, _RestUvToFlowerZ.xyz));
    float softness = max(0.001, _WaveSoftness);
    float front = lerp(-softness, _WaveRadius + softness, saturate(_WaveProgress));
    half blend = 1 - smoothstep(front - softness, front + softness, length(rest));
    half3 fill = lerp(_BaseColor.rgb, _TargetBaseColor.rgb, blend);
    half3 main = lerp(_PatternColor.rgb, _TargetPatternColor.rgb, blend);
    half3 accent = lerp(_AccentColor.rgb, _TargetAccentColor.rgb, blend);
    surface = (SurfaceData)0;
    surface.smoothness = _Smoothness;
    surface.normalTS = half3(0,0,1);
    surface.occlusion = 1;
    surface.alpha = 1;
    if (_CenterSurface > 0.5h)
    {
        surface.albedo = fill;
        return;
    }
    // Linear data: R ornament coverage, G primary/secondary ornament blend,
    // B neutral pigment detail, A edge emission. One texture sample for all states.
    half4 regions = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half3 ornament = lerp(accent, main, regions.g);
    half detail = max(0.5h, regions.b * 1.15h);
    half3 original = lerp(fill, ornament, regions.r) * detail;
    // Radial distance from this petal's unanimated root, independent of the palette wave.
    float2 rootPosition = uv * _ArtworkUvToRoot.xy + _ArtworkUvToRoot.zw;
    float artworkSoftness = max(0.001, _ArtworkSoftness);
    float artworkFront = lerp(-artworkSoftness, _ArtworkRadius + artworkSoftness, saturate(_ArtworkProgress));
    half artworkBlend = 1 - smoothstep(artworkFront - artworkSoftness, artworkFront + artworkSoftness, length(rootPosition));
    half border = smoothstep(0.08h, 0.25h, regions.a);
    float2 uvDx = ddx(uv), uvDy = ddy(uv);
    if (artworkBlend >= 1)
        surface.albedo = SampleArtwork(uv, uvDx, uvDy, _TargetSymbolRect, _TargetSymbolEnabled, original, fill, main, accent, border);
    else if (artworkBlend <= 0)
        surface.albedo = SampleArtwork(uv, uvDx, uvDy, _SymbolRect, _SymbolEnabled, original, fill, main, accent, border);
    else
    {
        half3 from = SampleArtwork(uv, uvDx, uvDy, _SymbolRect, _SymbolEnabled, original, fill, main, accent, border);
        half3 to = SampleArtwork(uv, uvDx, uvDy, _TargetSymbolRect, _TargetSymbolEnabled, original, fill, main, accent, border);
        surface.albedo = lerp(from, to, artworkBlend);
    }
    surface.emission = regions.a * fill * lerp(_EdgeIntensity, _TargetEdgeIntensity, blend) * _EmissionScale;
}
#endif
