Shader "Hidden/Custom/FaceHighlight"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _HighlightTex("HighlightTex", 2D) = "white" {}
        _Threshold("Threshold", Float) = 0.8
        _UseColorKey("UseColorKey", Float) = 0
        _KeyColor("KeyColor", Color) = (1,1,1,1)
        _ColorTolerance("ColorTolerance", Float) = 0.1
        _EdgeSharpness("EdgeSharpness", Float) = 4
        _Intensity("Intensity", Float) = 1
        _HighlightOffset("HighlightOffset", Float) = 0
        _Blend("Blend", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "FaceHighlight"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            sampler2D _HighlightTex;

            float _Threshold;
            float _UseColorKey;
            float4 _KeyColor;
            float _ColorTolerance;
            float _EdgeSharpness;
            float _Intensity;
            float _HighlightOffset;
            float _Blend;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float luminance(float3 c)
            {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            float computeMask(float2 uv)
            {
                float3 col = tex2D(_MainTex, uv).rgb;

                // Brightness mask
                float lum = luminance(col);
                float lumMask = step(_Threshold, lum);

                // Color key mask
                float dist = distance(col, _KeyColor.rgb);
                float colorMask = 1.0 - saturate(dist / _ColorTolerance);

                // Pick mode
                float mask = lerp(lumMask, colorMask, _UseColorKey);

                return saturate(mask);
            }

            float fragEdge(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy;

                float m00 = computeMask(uv + texel * float2(-1, -1));
                float m10 = computeMask(uv + texel * float2( 0, -1));
                float m20 = computeMask(uv + texel * float2( 1, -1));

                float m01 = computeMask(uv + texel * float2(-1,  0));
                float m11 = computeMask(uv + texel * float2( 0,  0));
                float m21 = computeMask(uv + texel * float2( 1,  0));

                float m02 = computeMask(uv + texel * float2(-1,  1));
                float m12 = computeMask(uv + texel * float2( 0,  1));
                float m22 = computeMask(uv + texel * float2( 1,  1));

                float gx = -m00 - 2.0 * m01 - m02 + m20 + 2.0 * m21 + m22;
                float gy = -m00 - 2.0 * m10 - m20 + m02 + 2.0 * m12 + m22;

                float g = sqrt(gx * gx + gy * gy);

                // Sharpen edges
                float edge = saturate(g * _EdgeSharpness);

                // Optional further shaping if you want super thin lines:
                // edge = pow(edge, 1.0 / max(0.001, _EdgeSharpness));

                return edge;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                fixed4 baseCol = tex2D(_MainTex, uv);

                float edge = fragEdge(uv);
                if (edge <= 0.0001)
                {
                    return baseCol;
                }

                // Streak texture UV
                float2 hUv = uv;

                // Offset along X using slider range -1..1 (full screen width)
                hUv.x += _HighlightOffset;

                fixed3 highlightTexCol = tex2D(_HighlightTex, hUv).rgb;

                // Final highlight contribution
                fixed3 highlight = highlightTexCol * edge * _Intensity;

                fixed4 result;
                result.rgb = baseCol.rgb + highlight;
                result.a   = baseCol.a;

                // Blend with original frame
                result.rgb = lerp(baseCol.rgb, result.rgb, saturate(_Blend));

                return result;
            }
            ENDCG
        }
    }
}
