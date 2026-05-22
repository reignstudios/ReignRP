Shader "Hidden/Blit"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"

            struct appdata
            {
                float2 uv : TEXCOORD0;
                float3 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            SAMPLER(sampler_BlitTex);
            TEXTURE2D(_BlitTex);

            v2f vert (appdata v)
            {
                v2f o;

                // instancing 
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // XR
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = float4(v.positionOS.xy, .5, 1);
                o.uv = v.uv;
                return o;
            }

            struct PSOUT
            {
                float4 color : SV_Target0;
            };

            PSOUT frag(v2f i)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                PSOUT o;
                o.color = SAMPLE_TEXTURE2D(_BlitTex, sampler_BlitTex, i.uv);
                return o;
            }
            ENDHLSL
        }
    }
}
