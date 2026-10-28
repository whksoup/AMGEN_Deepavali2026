Shader "Amgen/Deepavali/Festival Soil"
{
    Properties
    {
        _BaseMap("Yughues Soil Albedo", 2D) = "white" {}
        _BumpMap("Yughues Soil Normal", 2D) = "bump" {}
        _BaseColor("Earth Tint", Color) = (0.65,0.50,0.37,1)
        _BumpScale("Soil Relief", Range(0,2)) = 0.65
        _Tiling("Repeats Per Metre", Float) = 3.5
        _PoolStrength("Warm Lamplight On Soil", Range(0,1)) = 0.08
        _LampRestoration("Restored Lamps", Vector) = (1,1,1,1)
        _ViewBounds("View Centre / Half Size", Vector) = (0,0,1.4,0.6)
        _Lamp0("Lamp 0", Vector) = (-1,-0.08,-0.4,1)
        _Lamp1("Lamp 1", Vector) = (1,-0.08,-0.4,1)
        _Lamp2("Lamp 2", Vector) = (-1,-0.08,0.4,1)
        _Lamp3("Lamp 3", Vector) = (1,-0.08,0.4,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _ViewBounds, _Lamp0, _Lamp1, _Lamp2, _Lamp3;
                float4 _LampRestoration;
                float _Tiling, _BumpScale, _PoolStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS: POSITION; float3 normalOS: NORMAL; float4 tangentOS: TANGENT; };
            struct Varyings { float4 positionCS: SV_POSITION; float3 positionWS: TEXCOORD0; half3 normalWS: TEXCOORD1; half4 tangentWS: TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.normalWS = n.normalWS; o.tangentWS = half4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                return o;
            }
            float Pool(float2 p, float3 lamp) { float2 d = p - lamp.xz; return exp(-dot(d,d) / 0.014); }
            half4 Frag(Varyings v): SV_Target
            {
                float2 uv = v.positionWS.xz * _Tiling;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                // World-space texture axes: U = X, V = Z on the horizontal soil.
                half3 normalWS = normalize(half3(normalTS.x, normalTS.z, normalTS.y));
                float2 screen = abs((v.positionWS.xz - _ViewBounds.xy) / max(_ViewBounds.zw, 0.01));
                float vignette = 1 - 0.72 * smoothstep(0.45, 1.32, length(screen * float2(0.8, 0.65)));
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb * vignette;
                surface.alpha = 1; surface.occlusion = 1; surface.smoothness = 0.035; surface.specular = half3(0.04,0.04,0.04);
                float warmth = Pool(v.positionWS.xz,_Lamp0.xyz)*_LampRestoration.x
                    +Pool(v.positionWS.xz,_Lamp1.xyz)*_LampRestoration.y
                    +Pool(v.positionWS.xz,_Lamp2.xyz)*_LampRestoration.z
                    +Pool(v.positionWS.xz,_Lamp3.xyz)*_LampRestoration.w;
                surface.emission = surface.albedo * half3(1,0.34,0.055) * warmth * _PoolStrength * 20;
                InputData input = (InputData)0;
                input.positionWS = v.positionWS; input.normalWS = normalWS;
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(v.positionWS);
                input.shadowCoord = TransformWorldToShadowCoord(v.positionWS);
                input.bakedGI = SampleSH(normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(v.positionCS);
                input.shadowMask = half4(1,1,1,1);
                return UniversalFragmentPBR(input, surface);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
