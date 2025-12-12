Shader "Hidden/PostProcessing/ScreenSpaceHighlights"
{
    HLSLINCLUDE
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_StreakTex, sampler_StreakTex);
        float4 _MainTex_TexelSize;
        
        float _Threshold;
        float _HighlightIntensity;
        float _EmissionBoost;
        float _StreakOffset;
        float _EdgeThickness;
        float4 _ColorMask;
        float _ColorTolerance;
        float _UseColorMask;
        float4 _StreakColor;

        float Luminance(float3 color)
        {
            return dot(color, float3(0.299, 0.587, 0.114));
        }

        float ColorMatch(float3 color, float3 mask, float tolerance)
        {
            float dist = distance(color, mask);
            return 1.0 - saturate(dist / tolerance);
        }

        float DetectEdge(float2 uv)
        {
            float2 texelSize = _MainTex_TexelSize.xy * _EdgeThickness;
            
            // Sample surrounding pixels
            float tl = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, texelSize.y)).rgb);
            float tm = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texelSize.y)).rgb);
            float tr = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, texelSize.y)).rgb);
            
            float ml = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, 0)).rgb);
            float mr = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, 0)).rgb);
            
            float bl = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, -texelSize.y)).rgb);
            float bm = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -texelSize.y)).rgb);
            float br = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, -texelSize.y)).rgb);
            
            // Sobel operator
            float gx = -tl + tr - 2.0 * ml + 2.0 * mr - bl + br;
            float gy = tl + 2.0 * tm + tr - bl - 2.0 * bm - br;
            
            return sqrt(gx * gx + gy * gy);
        }

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            
            // Determine if this pixel is bright enough or matches color
            float brightnessMask = 0;
            
            if (_UseColorMask > 0.5)
            {
                // Use color matching
                brightnessMask = ColorMatch(col.rgb, _ColorMask.rgb, _ColorTolerance);
                brightnessMask = step(_Threshold, brightnessMask);
            }
            else
            {
                // Use brightness threshold
                float brightness = Luminance(col.rgb);
                brightnessMask = step(_Threshold, brightness);
            }
            
            // Detect edges
            float edge = DetectEdge(i.texcoord);
            
            // Combine brightness/color mask with edge detection
            float mask = brightnessMask * edge;
            
            // Sample streak texture with offset
            float2 streakUV = i.texcoord;
            streakUV.x += _StreakOffset;
            streakUV.x = frac(streakUV.x);
            
            float4 streakColor = SAMPLE_TEXTURE2D(_StreakTex, sampler_StreakTex, streakUV);
            
            // Apply HDR color tint to streak
            float3 tintedStreak = streakColor.rgb * _StreakColor.rgb;
            
            // Apply highlight with HDR intensity and emission boost
            float3 highlight = tintedStreak * mask * _HighlightIntensity;
            highlight *= (1.0 + _EmissionBoost);
            
            // Add highlight to original color (preserves HDR for bloom)
            col.rgb += highlight;
            
            return col;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
