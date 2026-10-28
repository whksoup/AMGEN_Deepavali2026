Shader "Amgen/Deepavali/Diya Flame"
{
    Properties
    {
        [HDR] _Tint("Flame Radiance", Color) = (5,1.8,0.2,1)
        _Restoration("Restored Flame", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Restoration;
            CBUFFER_END
            struct A { float4 vertex: POSITION; float2 uv: TEXCOORD0; };
            struct V { float4 position: SV_POSITION; float2 uv: TEXCOORD0; float phase: TEXCOORD1; };
            V Vert(A v) { V o; o.position=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; o.phase=unity_ObjectToWorld._m03*31+unity_ObjectToWorld._m23*17; return o; }
            half4 Frag(V i): SV_Target
            {
                float y = i.uv.y;
                float sway = sin(_Time.y*5.7+i.phase)*0.055*y*y + sin(_Time.y*11+i.phase)*0.012*y;
                float x = i.uv.x - 0.5 - sway;
                float width = 0.28 * pow(max(0.0,sin(y*3.14159)),0.72) * (1-0.65*y);
                float coverage = 1-smoothstep(width*0.65,width+0.022,abs(x));
                coverage *= smoothstep(0,0.08,y) * (1-smoothstep(0.91,1,y));
                float core = (1-smoothstep(0.01,0.09,abs(x))) * (1-smoothstep(0.3,0.72,y));
                half3 radiance=lerp(_Tint.rgb,half3(7,5,1.9),core);
                return half4(radiance,coverage * _Restoration);
            }
            ENDHLSL
        }
    }
}
