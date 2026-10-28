#ifndef DEEPAVALI_PETAL_PALETTE_INPUT
#define DEEPAVALI_PETAL_PALETTE_INPUT
// Match the original nonmetallic 0.04 reflectance by default, but allow the
// dark pigment to have a softer sheen independently of the gold artwork.
#define _SPECULAR_SETUP 1
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

// Identical layout in every pass keeps the shader compatible with the SRP Batcher.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
float4 _SymbolAtlas_TexelSize;
float4 _StartupAtlas_TexelSize;
float4 _StartupRect;
float _StartupMaxMip;
half _StartupSymbolEnabled;
half _StartupAppearance;
half _TargetStartupAppearance;
half4 _EdgeColor;
half4 _TargetEdgeColor;
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
half4 _TertiaryColor;
half4 _TargetBaseColor;
half4 _TargetPatternColor;
half4 _TargetAccentColor;
half4 _TargetTertiaryColor;
float4 _RestUvToFlowerX;
float4 _RestUvToFlowerZ;
float _WaveProgress;
float _WaveRadius;
float _WaveSoftness;
half _EdgeIntensity;
half _TargetEdgeIntensity;
half _EmissionScale;
half _MainPatternIntensity;
half _AccentPatternIntensity;
half _TargetMainPatternIntensity;
half _TargetAccentPatternIntensity;
half _Cull;
half _CenterSurface;
float _CenterIconScale;
float _CenterRingRadius;
float _CenterRingWidth;
half _CenterRingGlow;
half _CenterIconGlow;
half _CenterDotGlow;
half4 _CenterRingColor;
half4 _TargetCenterRingColor;
half _Smoothness;
half _Surface;
half _BodyShadeStrength;
half _BodyOcclusionStrength;
half _BodySpecularStrength;
half _RimGlowScale;
half _OrnamentGlowScale;
half _PetalLightEnabled;
half4 _PetalLightColor;
float4 _PetalLightShape;
float4 _PetalLightTravel;
float4 _PetalLightStyle;
CBUFFER_END

TEXTURE2D(_PetalLightData);
SAMPLER(sampler_PetalLightData);
#include "PetalSpatialLighting.hlsl"

TEXTURE2D(_StartupAtlas);
SAMPLER(sampler_StartupAtlas);

TEXTURE2D(_SymbolAtlas);
SAMPLER(sampler_SymbolAtlas);
TEXTURE2D(_TertiaryMap);
SAMPLER(sampler_TertiaryMap);
// Linear data: R broad body shading, G recessed ambient occlusion.
TEXTURE2D(_BodyShadingMap);
SAMPLER(sampler_BodyShadingMap);
TEXTURE2D(_CenterShadingMap);
SAMPLER(sampler_CenterShadingMap);

void ApplyBodyShading(float2 uv, half bodyCoverage, bool center, inout SurfaceData surface)
{
    half2 shading;
    if (center) shading = SAMPLE_TEXTURE2D(_CenterShadingMap, sampler_CenterShadingMap, uv).rg;
    else shading = SAMPLE_TEXTURE2D(_BodyShadingMap, sampler_BodyShadingMap, uv).rg;
    // Shade the non-ornament substrate only. Emission is deliberately independent.
    surface.albedo *= lerp(1.0h, shading.r, saturate(bodyCoverage * _BodyShadeStrength));
    surface.occlusion = lerp(1.0h, shading.g, saturate(bodyCoverage * _BodyOcclusionStrength));
    surface.specular *= lerp(1.0h, _BodySpecularStrength, saturate(bodyCoverage));
}

half CenterIconCoverage(float2 uv, float2 uvDx, float2 uvDy, float4 rect, half enabled)
{
    if (enabled < 0.5h) return 0;
    float2 atlasUV = saturate(uv) * rect.xy + rect.zw;
    float2 dx = uvDx * rect.xy * _SymbolAtlas_TexelSize.zw;
    float2 dy = uvDy * rect.xy * _SymbolAtlas_TexelSize.zw;
    float lod = min(_SymbolMaxMip, 0.5 * log2(max(1, max(dot(dx,dx), dot(dy,dy)))));
    half mask = SAMPLE_TEXTURE2D_LOD(_SymbolAtlas, sampler_SymbolAtlas, atlasUV, lod).r;
    // Ignore near-black source/compression noise, keeping antialiased line edges.
    return saturate((mask - 0.03h) / 0.94h);
}

struct ArtworkSample
{
    half3 albedo;
    half3 glowCoverage;
};

ArtworkSample SampleArtwork(float2 uv, float2 uvDx, float2 uvDy, float4 rect, half enabled,
    half3 original, half3 originalCoverage, half3 fill, half3 main, half3 accent, half border)
{
    ArtworkSample result;
    result.albedo = original;
    result.glowCoverage = originalCoverage;
    if (enabled < 0.5h) return result;
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
    half erase = eraseCoverage * (1 - border);
    result.albedo = lerp(original, replacement, erase);
    result.glowCoverage = lerp(originalCoverage, half3(symbol.r * symbol.g, symbol.r * (1 - symbol.g), 0), erase);
    return result;
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
    half3 tertiary = lerp(_TertiaryColor.rgb, _TargetTertiaryColor.rgb, blend);
    surface = (SurfaceData)0;
    surface.specular = _CenterSurface > 0.5h ? 0.0h : 0.04h;
    surface.smoothness = _Smoothness;
    surface.normalTS = half3(0,0,1);
    surface.occlusion = 1;
    surface.alpha = 1;
    if (_CenterSurface > 0.5h)
    {
        float2 disc = (uv - 0.5) * 2;
        float radial = length(disc);
        float aa = max(fwidth(radial), 0.001);
        half ring = 1 - smoothstep(_CenterRingWidth - aa, _CenterRingWidth + aa, abs(radial - _CenterRingRadius));
        // Discrete circular dots inside the rim, with derivative-based antialiasing.
        float angle = atan2(disc.y, disc.x);
        float nearestAngle = floor(angle * (48.0 / TWO_PI) + 0.5) * (TWO_PI / 48.0);
        float dotRadius = max(0.1, _CenterRingRadius - 0.11);
        float2 dotCenter = float2(cos(nearestAngle), sin(nearestAngle)) * dotRadius;
        half dots = (1 - smoothstep(0.009 - aa, 0.009 + aa, length(disc - dotCenter))) * step(0.001h, _CenterDotGlow);
        float2 iconUV = (uv - 0.5) / max(0.1, _CenterIconScale) + 0.5;
        float2 iconDx = ddx(iconUV), iconDy = ddy(iconUV);
        half icon;
        if (blend <= 0) icon = CenterIconCoverage(iconUV, iconDx, iconDy, _SymbolRect, _SymbolEnabled);
        else if (blend >= 1) icon = CenterIconCoverage(iconUV, iconDx, iconDy, _TargetSymbolRect, _TargetSymbolEnabled);
        else icon = lerp(CenterIconCoverage(iconUV, iconDx, iconDy, _SymbolRect, _SymbolEnabled),
            CenterIconCoverage(iconUV, iconDx, iconDy, _TargetSymbolRect, _TargetSymbolEnabled), blend);
        surface.albedo = lerp(fill, main, saturate(ring + dots + icon));
        ApplyBodyShading(uv, 1 - saturate(ring + dots + icon), true, surface);
        half3 rimHue = lerp(_CenterRingColor.rgb, _TargetCenterRingColor.rgb, blend);
        surface.emission = ring * rimHue * lerp(_EdgeIntensity, _TargetEdgeIntensity, blend) * _CenterRingGlow * _RimGlowScale
            + main * (icon * _CenterIconGlow + dots * _CenterDotGlow);
        return;
    }
    // Linear data: R ornament coverage, G primary/secondary ornament blend,
    // B neutral pigment detail, A edge emission. Optional minor-inlay mask is independent.
    half4 regions = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half3 ornament = lerp(accent, main, regions.g);
    half detail = max(0.5h, regions.b * 1.15h);
    half3 original = lerp(fill, ornament, regions.r) * detail;
    half inlay = SAMPLE_TEXTURE2D(_TertiaryMap, sampler_TertiaryMap, uv).r;
    original = lerp(original, tertiary * detail, inlay);
    half3 originalCoverage = half3(regions.r * regions.g * (1 - inlay), regions.r * (1 - regions.g) * (1 - inlay), inlay);
    // Radial distance from this petal's unanimated root, independent of the palette wave.
    float2 rootPosition = uv * _ArtworkUvToRoot.xy + _ArtworkUvToRoot.zw;
    float artworkSoftness = max(0.001, _ArtworkSoftness);
    float artworkFront = lerp(-artworkSoftness, _ArtworkRadius + artworkSoftness, saturate(_ArtworkProgress));
    half artworkBlend = 1 - smoothstep(artworkFront - artworkSoftness, artworkFront + artworkSoftness, length(rootPosition));
    half border = smoothstep(0.08h, 0.25h, regions.a);
    float2 uvDx = ddx(uv), uvDy = ddy(uv);
    ArtworkSample artwork;
    if (artworkBlend >= 1)
        artwork = SampleArtwork(uv, uvDx, uvDy, _TargetSymbolRect, _TargetSymbolEnabled, original, originalCoverage, fill, main, accent, border);
    else if (artworkBlend <= 0)
        artwork = SampleArtwork(uv, uvDx, uvDy, _SymbolRect, _SymbolEnabled, original, originalCoverage, fill, main, accent, border);
    else
    {
        ArtworkSample from = SampleArtwork(uv, uvDx, uvDy, _SymbolRect, _SymbolEnabled, original, originalCoverage, fill, main, accent, border);
        ArtworkSample to = SampleArtwork(uv, uvDx, uvDy, _TargetSymbolRect, _TargetSymbolEnabled, original, originalCoverage, fill, main, accent, border);
        artwork.albedo = lerp(from.albedo, to.albedo, artworkBlend);
        artwork.glowCoverage = lerp(from.glowCoverage, to.glowCoverage, artworkBlend);
    }
    // Opening medallion, colors and edge tint share exactly the same outward wave.
    // The normal artwork clock remains independent and is revealed beneath it.
    half initialWeight = _StartupSymbolEnabled * lerp(_StartupAppearance, _TargetStartupAppearance, blend);
    if (initialWeight > 0)
    {
        float2 atlasUV = saturate(uv) * _StartupRect.xy + _StartupRect.zw;
        float2 dx = uvDx * _StartupRect.xy * _StartupAtlas_TexelSize.zw;
        float2 dy = uvDy * _StartupRect.xy * _StartupAtlas_TexelSize.zw;
        float lod = min(_StartupMaxMip, 0.5 * log2(max(1, max(dot(dx,dx), dot(dy,dy)))));
        half4 mask = SAMPLE_TEXTURE2D_LOD(_StartupAtlas, sampler_StartupAtlas, atlasUV, lod);
        half coverage = saturate((mask.r - 0.015h) / 0.97h);
        half3 initialAlbedo = lerp(fill, main, coverage) * max(0.5h, mask.b * 1.15h);
        artwork.albedo = lerp(artwork.albedo, initialAlbedo, initialWeight);
        artwork.glowCoverage = lerp(artwork.glowCoverage, half3(coverage, 0, 0), initialWeight);
    }
    surface.albedo = artwork.albedo;
    half bodyCoverage = 1 - saturate(artwork.glowCoverage.x + artwork.glowCoverage.y + artwork.glowCoverage.z + border);
    ApplyBodyShading(uv, bodyCoverage, false, surface);
    half3 fromEdge = lerp(_BaseColor.rgb, _EdgeColor.rgb, saturate(_EdgeColor.a));
    half3 toEdge = lerp(_TargetBaseColor.rgb, _TargetEdgeColor.rgb, saturate(_TargetEdgeColor.a));
    // BC7 can introduce tiny alpha values alongside the old gold coverage. A bright
    // yellow opening edge would illuminate those otherwise invisible codec remnants.
    // Preserve the existing phase emission exactly once the opening wave has passed.
    half edgeCoverage = lerp(regions.a, saturate((regions.a - 0.03h) / 0.97h), initialWeight);
    half3 borderGlow = edgeCoverage * lerp(fromEdge, toEdge, blend) * lerp(_EdgeIntensity, _TargetEdgeIntensity, blend);
    half3 mainGlow = artwork.glowCoverage.x * main * lerp(_MainPatternIntensity, _TargetMainPatternIntensity, blend);
    // Inlays share the secondary glow control, keeping the two-region lighting workflow.
    half3 accentGlow = (artwork.glowCoverage.y * accent + artwork.glowCoverage.z * tertiary) * lerp(_AccentPatternIntensity, _TargetAccentPatternIntensity, blend);
    // Keep the border legible while the dense interior ornament stays quieter.
    half ornamentScale = lerp(_OrnamentGlowScale, _RimGlowScale, border);
    surface.emission = (borderGlow * _RimGlowScale + (mainGlow + accentGlow) * ornamentScale) * _EmissionScale;
    if (_PetalLightEnabled > 0.5h)
        ApplyPetalSpatialLighting(uv, edgeCoverage, borderGlow * _RimGlowScale * _EmissionScale, surface);
}
#endif
