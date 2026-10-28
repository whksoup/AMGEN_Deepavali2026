Shader "Amgen/Deepavali/Screen Restoration"
{
    Properties
    {
        _RestorationMask("Restored Coverage", 2D) = "black" {}
        _DarkBrightness("Unrestored Brightness", Range(0,1)) = 0.24
        _DarkSaturation("Unrestored Saturation", Range(0,1)) = 0
        _CompleteReveal("Completion Reveal", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Restore Screen Color"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D(_RestorationMask); SAMPLER(sampler_RestorationMask);
            CBUFFER_START(UnityPerMaterial)
                float _DarkBrightness, _DarkSaturation, _CompleteReveal;
            CBUFFER_END
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float2 maskUV = input.texcoord;
                // Blitter supplies viewport-oriented UVs; an extra graphics-API flip mirrors the mask.
                float restored = SAMPLE_TEXTURE2D(_RestorationMask, sampler_RestorationMask, maskUV).r;
                restored = lerp(restored, 1, _CompleteReveal);
                if (restored >= 1) return scene;
                half grey = dot(scene.rgb, half3(.2126, .7152, .0722));
                half3 dormant = lerp(grey.xxx, scene.rgb, _DarkSaturation) * _DarkBrightness;
                return half4(lerp(dormant, scene.rgb, restored), scene.a);
            }
            ENDHLSL
        }
    }
}
