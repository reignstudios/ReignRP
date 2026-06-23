Shader "Hidden/ReignRP/Shadow"
{
    Properties
    {
        //[KeywordEnum(Color, Texture, Both)] _COLOR ("Color Mode", Float) = 0
        //[MainColor] [HDR] _BaseColor("Color", Color) = (1,1,1,1)
        //[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "LightMode" = "Reign_Shadow" "Queue" = "Geometry" "RenderType"="Opaque" }
        ZTest LEqual
        ZWrite On

        Pass// Opaque
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"
            #include "../_Shared/Reign_LightingUtils.hlsl"

            struct appdata
            {
                float3 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClipShadow(TransformObjectToWorld(v.positionOS));
                return o;
            }

            struct PSOUT
            {
                float4 color : SV_Target0;
            };

            PSOUT frag(v2f i)
            {
                PSOUT o;
                o.color = 0;
                return o;
            }
            ENDHLSL
        }

        /*Pass// Alpha-Clip
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"
            #include "../_Shared/Reign_LightingUtils.hlsl"

            struct appdata
            {
                float2 uv : TEXCOORD0;
                float3 positionOS : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            float4 _UVScaleOffset;
            
            #if defined(_COLOR_COLOR) || defined(_COLOR_BOTH)
            half4 _BaseColor;
            #endif
                
            #if defined(_COLOR_TEXTURE) || defined(_COLOR_BOTH)
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BaseMap);
            #endif

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClipShadow(TransformObjectToWorld(v.positionOS));
                o.uv = (v.uv * _UVScaleOffset.xy) + _UVScaleOffset.zw;
                return o;
            }

            struct PSOUT
            {
                float4 color : SV_Target0;
            };

            PSOUT frag(v2f i)
            {
                PSOUT o;
                
                #if defined(_COLOR_BOTH)
                o.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                #elif defined(_COLOR_COLOR)
                o.color = _BaseColor;
                #elif defined(_COLOR_TEXTURE)
                o.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                #else
                o.color = 1.0;
                #endif
                
                clip(.5 - o.color.a);
                return o;
            }
            ENDHLSL
        }*/
    }
}
