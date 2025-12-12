// Upgrade NOTE: upgraded instancing buffer 'LedImage' to new syntax.

// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LedImage"
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
        _ClipValue("ClipValue", Range( 0 , 1)) = 0
        [Header(LED Settings)]_LedsAmount("LedsAmount", Float) = 300
        _LedMaks("LedMaks", 2D) = "white" {}
        _LedsScale("LedsScale", Range( 0 , 1)) = 300
        _RotationVariationAmount("RotationVariationAmount", Float) = 0
        _RotationVariationNoiseScale("RotationVariationNoiseScale", Float) = 0
        [Header(Mip level blur (requires mip level enabled on texture ))][IntRange]_MipLevel("MipLevel", Range( 0 , 10)) = 1

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
            #pragma multi_compile_instancing


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

            uniform float _LedsAmount;
            uniform float _Colorhdr;
            uniform sampler2D _LedMaks;
            uniform float _LedsScale;
            uniform float _RotationVariationNoiseScale;
            uniform float _RotationVariationAmount;
            uniform float _ClipValue;
            UNITY_INSTANCING_BUFFER_START(LedImage)
            	UNITY_DEFINE_INSTANCED_PROP(float, _MipLevel)
#define _MipLevel_arr LedImage
            UNITY_INSTANCING_BUFFER_END(LedImage)
            float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
            float snoise( float2 v )
            {
            	const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
            	float2 i = floor( v + dot( v, C.yy ) );
            	float2 x0 = v - i + dot( i, C.xx );
            	float2 i1;
            	i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
            	float4 x12 = x0.xyxy + C.xxzz;
            	x12.xy -= i1;
            	i = mod2D289( i );
            	float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
            	float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
            	m = m * m;
            	m = m * m;
            	float3 x = 2.0 * frac( p * C.www ) - 1.0;
            	float3 h = abs( x ) - 0.5;
            	float3 ox = floor( x + 0.5 );
            	float3 a0 = x - ox;
            	m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
            	float3 g;
            	g.x = a0.x * x0.x + h.x * x0.y;
            	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            	return 130.0 * dot( m, g );
            }
            


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                

                v.vertex.xyz +=  float3( 0, 0, 0 ) ;

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
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

                float2 texCoord4 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 temp_output_14_0 = ( texCoord4 * _LedsAmount );
                float2 temp_output_13_0 = ( round( temp_output_14_0 ) / _LedsAmount );
                float _MipLevel_Instance = UNITY_ACCESS_INSTANCED_PROP(_MipLevel_arr, _MipLevel);
                float2 temp_cast_0 = (( ( 0.5 / _LedsScale ) * ( 1.0 - _LedsScale ) )).xx;
                float simplePerlin2D43 = snoise( temp_output_13_0*_RotationVariationNoiseScale );
                simplePerlin2D43 = simplePerlin2D43*0.5 + 0.5;
                float temp_output_44_0 = ( simplePerlin2D43 * _RotationVariationAmount );
                float cos42 = cos( temp_output_44_0 );
                float sin42 = sin( temp_output_44_0 );
                float2 rotator42 = mul( ( ( frac( ( ( texCoord4 * _LedsAmount ) + float2( 0.5,0.5 ) ) ) / _LedsScale ) - temp_cast_0 ) - float2( 0.5,0.5 ) , float2x2( cos42 , -sin42 , sin42 , cos42 )) + float2( 0.5,0.5 );
                float4 tex2DNode38 = tex2D( _LedMaks, rotator42 );
                float4 tex2DNode2 = tex2Dlod( _MainTex, float4( temp_output_13_0, 0, 0.0) );
                float4 appendResult6 = (float4(( tex2Dlod( _MainTex, float4( temp_output_13_0, 0, _MipLevel_Instance) ) * ( IN.color * pow( 2.0 , _Colorhdr ) ) * tex2DNode38.r * tex2DNode2.a ).rgb , ( step( _ClipValue , tex2DNode2.a ) * tex2DNode38.r * IN.color.a )));
                

                half4 color = appendResult6;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

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
Node;AmplifyShaderEditor.RangedFloatNode;7;-1856,496;Inherit;False;Property;_LedsAmount;LedsAmount;2;1;[Header];Create;True;1;LED Settings;0;0;False;0;False;300;75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-1440,-96;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-1440,192;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RoundOpNode;23;-1280,-176;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-1264,208;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-1696,576;Inherit;False;Property;_LedsScale;LedsScale;4;0;Create;True;0;0;0;False;0;False;300;0.892;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;13;-1136,-96;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-1808,832;Inherit;False;Property;_RotationVariationNoiseScale;RotationVariationNoiseScale;6;0;Create;True;0;0;0;False;0;False;0;209.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;9;-1152,192;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;59;-1408,512;Inherit;False;2;0;FLOAT;0.5;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;61;-1408,608;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;43;-1472,736;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-1536,1088;Inherit;False;Property;_RotationVariationAmount;RotationVariationAmount;5;0;Create;True;0;0;0;False;0;False;0;5.71;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;50;-1168,368;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;-1168,480;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;1;-1120,-304;Inherit;False;0;0;_MainTex;Shader;False;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-1193.218,888.4778;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;58;-992,448;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-576,-256;Inherit;False;Property;_Colorhdr;Color hdr;0;1;[Header];Create;True;1;General;0;0;False;0;False;1;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-832,128;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;41;-614.9742,-1.957489;Inherit;False;Property;_ClipValue;ClipValue;1;0;Create;True;0;0;0;False;0;False;0;0.108;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-1088,624;Inherit;False;InstancedProperty;_MipLevel;MipLevel;7;2;[Header];[IntRange];Create;True;1;Mip level blur (requires mip level enabled on texture );0;0;False;0;False;1;3;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;42;-992,720;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;22;-576,-512;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;63;-336,-416;Inherit;False;False;2;0;FLOAT;2;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;36;-832,320;Inherit;True;Property;_TextureSample1;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;38;-784,544;Inherit;True;Property;_LedMaks;LedMaks;3;0;Create;True;0;0;0;False;0;False;-1;None;9ad34ed99e47e6a40bbb45cac1f88df3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.StepOpNode;40;-352,80;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-224,-560;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;80,-400;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;36.32471,78.6424;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;16;-1280,-256;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;47;-984.2183,1047.478;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;6;224,-80;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;464,-64;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;LedImage;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;14;0;4;0
WireConnection;14;1;7;0
WireConnection;8;0;4;0
WireConnection;8;1;7;0
WireConnection;23;0;14;0
WireConnection;27;0;8;0
WireConnection;13;0;23;0
WireConnection;13;1;7;0
WireConnection;9;0;27;0
WireConnection;59;1;49;0
WireConnection;61;0;49;0
WireConnection;43;0;13;0
WireConnection;43;1;46;0
WireConnection;50;0;9;0
WireConnection;50;1;49;0
WireConnection;60;0;59;0
WireConnection;60;1;61;0
WireConnection;44;0;43;0
WireConnection;44;1;45;0
WireConnection;58;0;50;0
WireConnection;58;1;60;0
WireConnection;2;0;1;0
WireConnection;2;1;13;0
WireConnection;42;0;58;0
WireConnection;42;2;44;0
WireConnection;63;1;31;0
WireConnection;36;0;1;0
WireConnection;36;1;13;0
WireConnection;36;2;37;0
WireConnection;38;1;42;0
WireConnection;40;0;41;0
WireConnection;40;1;2;4
WireConnection;62;0;22;0
WireConnection;62;1;63;0
WireConnection;30;0;36;0
WireConnection;30;1;62;0
WireConnection;30;2;38;1
WireConnection;30;3;2;4
WireConnection;39;0;40;0
WireConnection;39;1;38;1
WireConnection;39;2;22;4
WireConnection;16;0;14;0
WireConnection;47;0;44;0
WireConnection;6;0;30;0
WireConnection;6;3;39;0
WireConnection;0;0;6;0
ASEEND*/
//CHKSM=957B09AE0DB1A1D5DC013BF2DB509C584223FA52