// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LedText"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [Header(General)]_Colorhdr("Color hdr", Float) = 1
        _TextDilate("TextDilate", Range( 0 , 1)) = 0.7164568
        _TextDilateSmoothness("TextDilateSmoothness", Range( 0 , 1)) = 0.7164568
        [Header(LED Settings)]_LedsAmount("LedsAmount", Float) = 300
        _LedMaks("LedMaks", 2D) = "white" {}
        _LedsScale("LedsScale", Range( 0 , 2)) = 0.7
        [Header(Scrolling)]_ScrollSpeed("Scroll Speed", Float) = 1
        _ScrollDirection("Scroll Direction", Vector) = (1,0,0,0)
        _PixelStep("Pixel Step Size", Float) = 1
        _WorldPixelSize("World Pixel Size", Float) = 0.02   // <-- add
    }

    SubShader
    {
		LOD 0

        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
        	Ref [_Stencil]
        	ReadMask [_StencilReadMask]
        	WriteMask [_StencilWriteMask]
        	Comp [_StencilComp]
        	Pass [_StencilOp]
        }


        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        
        Pass
        {
            Name "Default"
        CGPROGRAM
            #define ASE_VERSION 19801

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define ASE_NEEDS_FRAG_COLOR


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
                
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
                        uniform float _WorldPixelSize;   // <-- add

            uniform float _Colorhdr;
            uniform float _TextDilate;
            uniform float _TextDilateSmoothness;
            uniform float _LedsAmount;
            uniform sampler2D _LedMaks;
            uniform float _LedsScale;
            uniform float _ScrollSpeed;
            uniform float2 _ScrollDirection;
            uniform float _PixelStep;


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                

                v.vertex.xyz +=  float3( 0, 0, 0 ) ;

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                
                // world position in world space, not object space
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                OUT.worldPosition = worldPos;

                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = v.texcoord;
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN ) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                float temp_output_29_0 = (1.0 - _TextDilate);

                // Normal SDF sampling for TMP atlas (do NOT scroll or snap this)
                float2 texCoord4 = IN.texcoord.xy;
                float4 tex2DNode2 = tex2D(_MainTex, texCoord4);

                // World-space LED UVs
                float2 worldXY = IN.worldPosition.xy;

                // How many "world units" per LED cell
                float cellSize = _WorldPixelSize;

                // Convert world pos to "LED grid space"
                float2 ledGrid = worldXY / cellSize;

                // Repeat single LED tile in world space
                float2 ledUVBase = frac(ledGrid + float2(0.5, 0.5));

                // Rescale by _LedsScale like before
                float2 temp_cast_1 = (( (0.5 / _LedsScale) * (1.0 - _LedsScale))).xx;

                float smoothstepResult38 = smoothstep(
                    temp_output_29_0,
                    (temp_output_29_0 + _TextDilateSmoothness),
                    tex2DNode2.a);

                // Use world-space LED UVs instead of scrolledTexCoord
                float2 ledUV = (ledUVBase / _LedsScale) - temp_cast_1;
                float ledMask = tex2D(_LedMaks, ledUV).r;

                float4 appendResult6 = float4(
                    (IN.color * pow(2.0, _Colorhdr)).rgb,
                    smoothstepResult38 * ledMask
                );


                half4 color = appendResult6;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                // make as hdr emnissive

                color.rgb = pow( color.rgb , 2.2 );

                return color;
            }
        ENDCG
        }
    }
    CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-1776,48;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;7;-1904,368;Inherit;False;Property;_LedsAmount;LedsAmount;3;1;[Header];Create;True;1;LED Settings;0;0;False;0;False;300;150;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-1440,240;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-1280,144;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-1440,-96;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-1728,496;Inherit;False;Property;_LedsScale;LedsScale;5;0;Create;True;0;0;0;False;0;False;0.7;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;9;-1152,80;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RoundOpNode;23;-1280,-176;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-1088,496;Inherit;False;Property;_TextDilate;TextDilate;1;0;Create;True;0;0;0;False;0;False;0.7164568;0.65;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;43;-1440,432;Inherit;False;2;0;FLOAT;0.5;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;47;-1440,528;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;13;-1136,-96;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;1;-1056,-272;Inherit;False;0;0;_MainTex;Shader;False;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;29;-806.7538,591.8702;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-1008,784;Inherit;False;Property;_TextDilateSmoothness;TextDilateSmoothness;2;0;Create;True;0;0;0;False;0;False;0.7164568;0.65;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;44;-1200,288;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-1200,400;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;40;-640,720;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;46;-1024,368;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;2;-816,64;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;50;-355.6599,-341.3766;Inherit;False;Property;_Colorhdr;Color hdr;0;1;[Header];Create;True;1;General;0;0;False;0;False;1;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;38;-304,304;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;36;-816,272;Inherit;True;Property;_LedMaks;LedMaks;4;0;Create;True;0;0;0;False;0;False;-1;None;9ad34ed99e47e6a40bbb45cac1f88df3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.VertexColorNode;22;-336,-704;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;49;-115.6599,-501.3766;Inherit;False;False;2;0;FLOAT;2;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-64,96;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;16,-672;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;26;-1664,272;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;6;224,-80;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FloorOpNode;16;-1280,-256;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;12;-288,464;Inherit;True;Step Antialiasing;-1;;6;2a825e80dfb3290468194f83380797bd;0;2;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;464,-64;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;LedText;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;8;0;4;0
WireConnection;8;1;7;0
WireConnection;27;0;8;0
WireConnection;14;0;4;0
WireConnection;14;1;7;0
WireConnection;9;0;27;0
WireConnection;23;0;14;0
WireConnection;43;1;42;0
WireConnection;47;0;42;0
WireConnection;13;0;23;0
WireConnection;13;1;7;0
WireConnection;29;0;28;0
WireConnection;44;0;9;0
WireConnection;44;1;42;0
WireConnection;45;0;43;0
WireConnection;45;1;47;0
WireConnection;40;0;29;0
WireConnection;40;1;41;0
WireConnection;46;0;44;0
WireConnection;46;1;45;0
WireConnection;2;0;1;0
WireConnection;2;1;13;0
WireConnection;38;0;2;4
WireConnection;38;1;29;0
WireConnection;38;2;40;0
WireConnection;36;1;46;0
WireConnection;49;1;50;0
WireConnection;11;0;38;0
WireConnection;11;1;36;1
WireConnection;48;0;22;0
WireConnection;48;1;49;0
WireConnection;26;1;7;0
WireConnection;6;0;48;0
WireConnection;6;3;11;0
WireConnection;16;0;14;0
WireConnection;12;1;29;0
WireConnection;12;2;2;4
WireConnection;0;0;6;0
ASEEND*/
//CHKSM=DCB23B711E080256CA385339441582930108CC11