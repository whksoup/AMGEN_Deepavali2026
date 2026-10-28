Shader "Amgen/Deepavali/UI Hand Guidance"
{
    Properties
    {
        [HDR] _EmissionColor("Gold Emission", Color) = (4, 1.8, 0.35, 1)
        _BackgroundColor("Dark Backing", Color) = (0.035, 0.016, 0.012, 1)
        _LoadProgress("Loading Progress", Range(0,1)) = 0
        _Loading("Loading Active", Float) = 0
        _Dissolve("Disintegrate Progress", Range(0,1)) = 0
        [HideInInspector] _GraphicScale("Graphic Scale", Float) = 1
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil
        {
            Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp]
            ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float4 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float4 uv:TEXCOORD0; float4 local:TEXCOORD1; float3 particle:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            float4 _EmissionColor, _BackgroundColor, _ClipRect;
            float _LoadProgress, _Loading, _Dissolve, _GraphicScale;
            float hash21(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.particle = 0;
                if (v.uv.x < -1.5)
                {
                    float seed = hash21(v.uv.zw * 197);
                    float age = saturate((_Dissolve - seed * 0.35) / 0.65);
                    float2 corner = float2(v.uv.x < -2.5 ? 1 : -1, v.uv.y);
                    float2 drift = float2((seed - 0.5) * 60, 20 + hash21(v.uv.wz * 91) * 55);
                    v.vertex.xy += (drift * age + corner * lerp(1.6,0.3,age)) * _GraphicScale;
                    o.particle = float3(corner, sin(age * UNITY_PI));
                }
                o.local = v.vertex; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o;
            }
            half4 frag(v2f i):SV_Target
            {
                half4 c;
                if (i.uv.x < -1.5)
                {
                    float shape = 1 - smoothstep(0.35,1,length(i.particle.xy));
                    c = half4(_EmissionColor.rgb * 1.4, shape * i.particle.z);
                    clip(c.a - 0.005);
                }
                else
                {
                    float light = i.uv.x;
                    if (i.uv.y > 0.5 && _Loading > 0.5)
                    {
                        // Top is zero; advance clockwise around the dotted ring.
                        float2 p = i.uv.zw - 0.5;
                        float angle = frac(atan2(p.x,-p.y) / (2 * UNITY_PI) + 1);
                        light *= angle <= _LoadProgress ? 2.8 : 0.2;
                    }
                    c = i.uv.x > 0.0001 ? half4(_EmissionColor.rgb * light,1) : _BackgroundColor;
                    float noise = hash21(floor(i.uv.zw * 55));
                    float threshold = _Dissolve * 1.2 - 0.1;
                    clip(noise - threshold);
                    float edge = (1 - smoothstep(0,0.07,noise-threshold)) * step(0.001,_Dissolve);
                    c.rgb += _EmissionColor.rgb * edge * 0.45;
                }
                c *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
