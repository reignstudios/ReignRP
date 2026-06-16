Shader "Hidden/ReignRP/Shadow"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "LightMode" = "Reign_Shadow" "Queue" = "Geometry" "RenderType"="Opaque" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass// Opaque
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"

            struct appdata
            {
                //float2 uv : TEXCOORD0;
                float3 positionOS : POSITION;
            };

            struct v2f
            {
                //float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
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

        Pass// Alpha-Clip
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"

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

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
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
                //clip();// TODO
                o.color = 0;
                return o;
            }
            ENDHLSL
        }
    }
}
