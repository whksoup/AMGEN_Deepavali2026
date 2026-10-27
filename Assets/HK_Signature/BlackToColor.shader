Shader "Custom/BlackToColorSmooth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ReplaceColor ("Replace Color", Color) = (0,1,0,1) // default green
        _Threshold ("Black Threshold", Range(0,1)) = 0.05
        _EdgeSmooth ("Edge Smooth Amount", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _ReplaceColor;
            float _Threshold;
            float _EdgeSmooth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // First, convert white pixels to transparent
                float brightness = (col.r + col.g + col.b) / 3.0;
                if (brightness > 0.95) // white pixel
                {
                    return fixed4(0,0,0,0); // make transparent
                }

                // If pixel is considered "black"
                if (brightness < _Threshold)
                {
                    // Sample neighbors
                    float2 pixelSize = 1.0 / _ScreenParams.xy; // 1 pixel in UV space
                    fixed4 sum = 0;
                    int count = 0;

                    // Loop over 8 neighbors
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y == 0) continue;

                            fixed4 neighborCol = tex2D(_MainTex, i.uv + float2(x, y) * pixelSize);
                            float nbBrightness = (neighborCol.r + neighborCol.g + neighborCol.b) / 3.0;

                            // Treat white pixels as transparent in neighbor sampling
                            if (nbBrightness >= _Threshold && nbBrightness <= 0.95) // non-black and non-white neighbor
                            {
                                sum += neighborCol;
                                count++;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        // Blend replace color toward neighbor average
                        fixed4 avgNeighbor = sum / count;
                        return lerp(_ReplaceColor, avgNeighbor, _EdgeSmooth);
                    }

                    // Fully inside replaced area
                    return _ReplaceColor;
                }

                // Keep other colors as is
                return col;
            }
            ENDCG
        }
    }
}