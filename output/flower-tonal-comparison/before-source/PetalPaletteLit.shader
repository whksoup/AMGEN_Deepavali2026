Shader "Amgen/Deepavali/Petal Palette Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Region Mask (Ornament / Main / Detail / Edge)", 2D) = "white" {}
        [MainColor] _BaseColor("Petal Fill", Color) = (0.5,0.05,0.15,1)
        _PatternColor("Main Ornament", Color) = (1,0.7,0.3,1)
        _AccentColor("Accent Ornament", Color) = (1,0.3,0.02,1)
        _TertiaryMap("Minor Inlay / Colored Tip Mask", 2D) = "black" {}
        _TertiaryColor("Tertiary Inlay", Color) = (0.1,0.4,0.8,1)
        _EdgeIntensity("Edge Intensity", Float) = 3
        _EdgeColor("Edge Tint (Alpha 0 Uses Fill)", Color) = (0,0,0,0)
        _TargetEdgeColor("Target Edge Tint", Color) = (0,0,0,0)
        _StartupAtlas("Initial Medallion Artwork", 2D) = "black" {}
        _StartupRect("Initial Artwork UV Scale / Offset", Vector) = (1,1,0,0)
        _StartupMaxMip("Initial Artwork Safe Mip", Float) = 3
        _StartupSymbolEnabled("Initial Artwork Assigned", Float) = 0
        _StartupAppearance("Source Uses Initial Appearance", Float) = 0
        _TargetStartupAppearance("Target Uses Initial Appearance", Float) = 0
        _EmissionScale("Petal Emission Scale", Float) = 1
        _MainPatternIntensity("Main Ornament Glow", Float) = 0
        _AccentPatternIntensity("Accent Ornament Glow", Float) = 0
        _TargetMainPatternIntensity("Target Main Ornament Glow", Float) = 0
        _TargetAccentPatternIntensity("Target Accent Ornament Glow", Float) = 0
        _TargetBaseColor("Target Fill", Color) = (0.5,0.05,0.15,1)
        _TargetPatternColor("Target Main Ornament", Color) = (1,0.7,0.3,1)
        _TargetAccentColor("Target Accent Ornament", Color) = (1,0.3,0.02,1)
        _TargetTertiaryColor("Target Tertiary Inlay", Color) = (0.1,0.4,0.8,1)
        _TargetEdgeIntensity("Target Edge Intensity", Float) = 3
        _WaveProgress("Wave Progress", Range(0,1)) = 1
        _WaveRadius("Flower Rest Radius", Float) = 0.36
        _WaveSoftness("Wave Softness", Float) = 0.025
        _RestUvToFlowerX("Rest UV To Flower X", Vector) = (0.182,0,-0.091,0)
        _RestUvToFlowerZ("Rest UV To Flower Z", Vector) = (0,-0.263,-0.082,0)
        [HideInInspector] _CenterSurface("Center Surface", Float) = 0
        _CenterIconScale("Center Icon Scale", Float) = 0.65
        _CenterRingRadius("Center Ring Radius", Float) = 0.92
        _CenterRingWidth("Center Ring Half Width", Float) = 0.012
        _CenterRingGlow("Center Ring Glow", Float) = 1.3
        _CenterIconGlow("Center Icon Glow", Float) = 1.3
        _CenterDotGlow("Center Dots Glow", Float) = 0.5
        _CenterRingColor("Center Ring Fill Color", Color) = (0.5,0.05,0.15,1)
        _TargetCenterRingColor("Target Center Ring Fill Color", Color) = (0.5,0.05,0.15,1)
        _SymbolAtlas("Symbols (Coverage / Main / Detail / Erase)", 2D) = "black" {}
        _SymbolRect("Symbol UV Scale / Offset", Vector) = (1,1,0,0)
        _SymbolEnabled("Replace Interior Artwork", Float) = 0
        _TargetSymbolRect("Target Symbol UV Scale / Offset", Vector) = (1,1,0,0)
        _TargetSymbolEnabled("Target Replaces Interior Artwork", Float) = 0
        _SymbolMaxMip("Symbol Safe Maximum Mip", Float) = 3
        _ArtworkProgress("Artwork Reveal Progress", Range(0,1)) = 1
        _ArtworkSoftness("Artwork Reveal Softness", Float) = 0.04
        _ArtworkUvToRoot("Artwork UV To Petal Root", Vector) = (0.692,1,-0.346,0)
        _ArtworkRadius("Artwork Root Radius", Float) = 1.059
        _Smoothness("Smoothness", Range(0,1)) = 0.22
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Render Face", Float) = 2
        [HideInInspector] _Surface("Surface", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "UniversalMaterialType"="Lit" }
        LOD 200
        Cull [_Cull]
        ZWrite On
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "PetalPaletteInput.hlsl"
            #include "PetalPaletteForward.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "PetalPaletteInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "PetalPaletteInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "PetalPaletteInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit
            #include "PetalPaletteInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
