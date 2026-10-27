Shader "Universal Render Pipeline/Custom/Dissolve Lit (PBR) (NoFog v2)"
{
    Properties
    {
        // Base
        _BaseMap    ("Base Map", 2D) = "white" {}
        _BaseColor  ("Base Color (Tint/Alpha)", Color) = (1,1,1,1)

        // PBR
        _Metallic   ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.6

        // Normal map
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpMap    ("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,2)) = 1

        // Emission
        [Toggle(_EMISSION)] _UseEmission ("Use Emission", Float) = 0
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)

        // Dissolve
        _DissolveTex    ("Dissolve (Noise) Map", 2D) = "gray" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _EdgeWidth      ("Edge Width", Range(0,0.3)) = 0.08
        _EdgeColor      ("Edge Color", Color) = (1,0.5,0,1)
        _EdgeBoost      ("Edge Emission", Range(0,5)) = 1

        // Dissolve UV control
        _UseWorldUV ("Use World-space UV (0/1)", Float) = 0
        _DissolveTiling ("Dissolve Tiling (xy)", Vector) = (1,1,0,0)
        _DissolveOffset ("Dissolve Offset (xy)", Vector) = (0,0,0,0)

        // Render state (transparent)
        _Cull    ("Cull Mode", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            // Feature keywords
            #pragma multi_compile _ _EMISSION
            #pragma multi_compile _ _NORMALMAP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv0        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 tangentWS  : TEXCOORD2;
                float3 bitangentWS: TEXCOORD3;
                float2 uv         : TEXCOORD4;
                float3 viewDirWS  : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_DissolveTex);  SAMPLER(sampler_DissolveTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;

                float  _Metallic;
                float  _Smoothness;

                float  _UseNormalMap;
                float  _NormalScale;

                float4 _EmissionColor;

                float  _DissolveAmount;
                float  _EdgeWidth;
                float4 _EdgeColor;
                float  _EdgeBoost;

                float  _UseWorldUV;
                float4 _DissolveTiling;  // xy
                float4 _DissolveOffset;  // xy

                float  _Cull;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);

                // Build TBN in world space
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float  sign = IN.tangentOS.w * GetOddNegativeScale();
                float3 bWS = cross(nWS, tWS) * sign;

                OUT.normalWS    = normalize(nWS);
                OUT.tangentWS   = normalize(tWS);
                OUT.bitangentWS = normalize(bWS);

                OUT.uv = TRANSFORM_TEX(IN.uv0, _BaseMap);

                float3 camPosWS = GetCameraPositionWS();
                OUT.viewDirWS = camPosWS - OUT.positionWS;
                return OUT;
            }

            float2 GetDissolveUV(Varyings IN)
            {
                if (_UseWorldUV > 0.5)
                {
                    float2 wuv = IN.positionWS.xz;
                    return wuv * _DissolveTiling.xy + _DissolveOffset.xy;
                }
                else
                {
                    return IN.uv * _DissolveTiling.xy + _DissolveOffset.xy;
                }
            }

            // Tangent-space normal map to world space
            float3 GetNormalWS(Varyings IN)
            {
                float3 n = normalize(IN.normalWS);

                #if defined(_NORMALMAP)
                float3 t = normalize(IN.tangentWS);
                float3 b = normalize(IN.bitangentWS);

                float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _NormalScale);
                float3x3 TBN = float3x3(t, b, n);
                n = normalize(mul(nTS, TBN));
                #endif

                return n;
            }

            // Simple PBR lighting using URP BRDF helpers
            half4 Frag(Varyings IN) : SV_Target
            {
                float3 posWS = IN.positionWS;
                float3 V = normalize(IN.viewDirWS);
                float3 N = GetNormalWS(IN);
                float2 uv = IN.uv;

                // Base color/alpha
                half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half4 baseCol = albedoTex * _BaseColor;

                // --- Dissolve ---
                float2 duv = GetDissolveUV(IN);
                half noise  = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, duv).r;
                half diff   = noise - _DissolveAmount;

                // edge only on kept side
                half edge = saturate(1.0h - (diff / max(_EdgeWidth, 1e-4h)));
                edge = saturate(edge) * step(0.0h, diff);

                // clip fully dissolved fragments
                clip(diff); // < 0 → discard

                // PBR inputs
                SurfaceData surface;
                surface.albedo      = baseCol.rgb;
                surface.metallic    = _Metallic;
                surface.specular    = 0;          // unused with metallic workflow
                surface.smoothness  = _Smoothness;
                surface.occlusion   = 1;
                surface.emission    = 0;
                surface.alpha       = baseCol.a;  // transparent

                // Emission (plus edge glow as additive emission)
                #if defined(_EMISSION)
                    float3 eTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;
                    surface.emission += eTex * _EmissionColor.rgb;
                #endif
                surface.emission += _EdgeColor.rgb * edge * _EdgeBoost;

                // Build InputData for lighting
                InputData inputData;
                inputData.positionWS = posWS;
                inputData.normalWS   = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(posWS);
                inputData.fogCoord    = 0; // no fog
                inputData.vertexLighting = 0;
                inputData.bakedGI = 0;

                // Main light
                Light mainLight = GetMainLight(inputData.shadowCoord);
                half3 color = half3(0,0,0);

                // BRDF
                BRDFData brdf;
                InitializeBRDFData(surface.albedo, surface.metallic, 0 /*specular*/, surface.smoothness, surface.alpha, brdf);

                // Direct lighting from main light
                half3 radiance = mainLight.color.rgb * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                color += LightingPhysicallyBased(brdf, N, V, mainLight.direction, radiance, /*occlusion*/ surface.occlusion);

                // Additional lights
                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint i = 0u; i < count; i++)
                {
                    Light l = GetAdditionalLight(i, posWS);
                    half3 rad = l.color.rgb * l.distanceAttenuation * l.shadowAttenuation;
                    color += LightingPhysicallyBased(brdf, N, V, l.direction, rad, surface.occlusion);
                }
                #endif

                // Emission + edge
                color += surface.emission;

                return half4(color, surface.alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
