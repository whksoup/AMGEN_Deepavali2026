Shader "Custom/AnimatedGradient"
{
    Properties
    {
        _ColorA ("Primary Color", Color) = (0.26, 0.72, 1.0, 1)     // ~#43B7FF
        _ColorB ("Secondary Color", Color) = (0.69, 0.97, 1.0, 1)   // ~#AEF6FF
        _Speed  ("Motion Speed", Range(0.0, 2.0)) = 0.25
        _Radius ("Blob Radius", Range(0.05, 1.2)) = 0.6
        _Amount ("Blob Amount (strength)", Range(0.5, 3.0)) = 1.4
        _Vignette ("Vignette", Range(0.0, 1.0)) = 0.15
        _Jitter  ("Organic Jitter", Range(0.0, 1.0)) = 0.35
        _Seed    ("Seed (phases)", Vector) = (1.10, 2.17, 3.33, 4.48)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Pass
        {
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 _ColorA, _ColorB;
            float  _Speed, _Radius, _Amount, _Vignette, _Jitter;
            float4 _Seed; // 4 different phase seeds

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // Simple 2D hash to decorrelate phases
            float hash21(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float gaussian(float2 p, float2 c, float r)
            {
                float2 d = p - c;
                float dist2 = dot(d, d);
                // smooth Gaussian falloff
                return exp(-dist2 / (r*r));
            }

            half4 frag (Varyings i) : SV_Target
            {
                // center UV around 0 and keep aspect-correct
                float2 uv = i.uv * 2.0 - 1.0;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;

                float t = _Time.y * _Speed;

                // Blob centers (4) move on different lissajous-ish paths
                // Amplitude is ~0.6 in screen space, scaled by aspect
                float amp = 0.60;
                float2 c0 = float2( amp * sin(t + _Seed.x), amp * 0.6 * cos(1.3*t + _Seed.y));
                float2 c1 = float2(-amp * cos(0.9*t + _Seed.y), amp * 0.5 * sin(1.6*t + _Seed.z));
                float2 c2 = float2( amp * 0.7 * sin(1.7*t + _Seed.z), -amp * cos(1.1*t + _Seed.w));
                float2 c3 = float2(-amp * 0.5 * cos(1.4*t + _Seed.x), -amp * 0.7 * sin(1.2*t + _Seed.w));

                // Add subtle “jitter” to avoid perfect loops
                float j = _Jitter * 0.08;
                c0 += j * float2( sin(3.1*t + 2.0), cos(2.7*t + 1.0) );
                c1 += j * float2( sin(2.3*t + 0.7), cos(3.4*t + 0.2) );
                c2 += j * float2( sin(2.9*t + 4.1), cos(2.1*t + 5.0) );
                c3 += j * float2( sin(3.7*t + 3.2), cos(2.5*t + 2.4) );

                // Combine four soft blobs
                float R = _Radius;
                float v  = 0.0;
                v += gaussian(uv, c0, R);
                v += gaussian(uv, c1, R);
                v += gaussian(uv, c2, R);
                v += gaussian(uv, c3, R);

                // Normalize and shape
                v = saturate(v * _Amount * 0.5);       // overall intensity
                v = smoothstep(0.0, 1.0, v);
                v = pow(v, 0.85);                       // tone curve

                // Blend between the two fixed palette colors
                float3 col = lerp(_ColorB.rgb, _ColorA.rgb, v);

                // Soft vignette for depth
                float vign = smoothstep(1.4, 0.2, length(uv));
                col = lerp(col, col * vign, _Vignette);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
