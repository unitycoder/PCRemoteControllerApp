Shader "Hidden/PostProcessing/ScreenSpaceHighlightsEdgeOnly"
{
    HLSLINCLUDE
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_StreakTex, sampler_StreakTex);
        float4 _MainTex_TexelSize;
        
        float _EdgeThreshold;
        float _EdgeSensitivity;
        float _MinLuminosityDifference;
        float _HighlightIntensity;
        float _EmissionBoost;
        float _StreakOffset;
        float _EdgeThickness;
        float _EdgeSharpness;
        float4 _StreakColor;

        float Luminance(float3 color)
        {
            return dot(color, float3(0.299, 0.587, 0.114));
        }

        float DetectLuminosityEdge(float2 uv)
        {
            float2 texelSize = _MainTex_TexelSize.xy * _EdgeThickness;
            
            // Sample center pixel
            float3 centerSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
            float centerLum = Luminance(centerSample);
            
            // 3x3 Kernel sampling
            float lum[9];
            lum[0] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, texelSize.y)).rgb);
            lum[1] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texelSize.y)).rgb);
            lum[2] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, texelSize.y)).rgb);
            lum[3] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, 0)).rgb);
            lum[4] = centerLum;
            lum[5] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, 0)).rgb);
            lum[6] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, -texelSize.y)).rgb);
            lum[7] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -texelSize.y)).rgb);
            lum[8] = Luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, -texelSize.y)).rgb);
            
            // Sobel operator for edge detection
            float gx = -lum[0] + lum[2] - 2.0 * lum[3] + 2.0 * lum[5] - lum[6] + lum[8];
            float gy = lum[0] + 2.0 * lum[1] + lum[2] - lum[6] - 2.0 * lum[7] - lum[8];
            
            float edge = sqrt(gx * gx + gy * gy);
            
            // Normalize by edge sensitivity
            edge = edge / _EdgeSensitivity;
            
            // Apply threshold
            edge = step(_EdgeThreshold, edge);
            
            // Apply sharpness
            edge = pow(saturate(edge), _EdgeSharpness);
            
            // Check minimum luminosity difference
            float maxLumDiff = 0;
            for (int j = 0; j < 9; j++)
            {
                float diff = abs(lum[j] - centerLum);
                maxLumDiff = max(maxLumDiff, diff);
            }
            
            // Only keep edges where there's actual luminosity change
            float lumChangeMask = step(_MinLuminosityDifference, maxLumDiff);
            edge *= lumChangeMask;
            
            return edge;
        }

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            
            // Detect luminosity edges
            float edge = DetectLuminosityEdge(i.texcoord);
            
            // Sample streak texture with offset
            float2 streakUV = i.texcoord;
            streakUV.x += _StreakOffset;
            streakUV.x = frac(streakUV.x);
            
            float4 streakColor = SAMPLE_TEXTURE2D(_StreakTex, sampler_StreakTex, streakUV);
            
            // Apply HDR color tint to streak
            float3 tintedStreak = streakColor.rgb * _StreakColor.rgb;
            
            // Apply highlight with HDR intensity and emission boost
            float3 highlight = tintedStreak * edge * _HighlightIntensity;
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
