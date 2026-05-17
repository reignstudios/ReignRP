Shader "ReignRP/Sky"
{
    Properties
    {
        _MainTex ("Texture", CUBE) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off
        Cull Off
        ZTest Less

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"

            #pragma multi_compile _ REIGN_MOTIONBLUR_ENABLED
            #if defined(REIGN_MOTIONBLUR_ENABLED)
            #define ENABLE_MOTIONVECTORS
            #endif

            struct appdata
            {
                float3 positionOS : POSITION;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;

                #if defined(ENABLE_MOTIONVECTORS)
                float4 posCS : TEXCOORD1;
                float4 posCS_Last : TEXCOORD2;
                #endif

                float4 positionCS : SV_POSITION;
            };

            SAMPLER(sampler_MainTex);
            TEXTURECUBE(_MainTex);

            #if defined(ENABLE_MOTIONVECTORS)
            float4x4 UNITY_MATRIX_VP_LAST;
            #endif

            v2f vert (appdata v)
            {
                v2f o;

                float3 pos = TransformObjectToWorld(v.positionOS);

                #if defined(ENABLE_MOTIONVECTORS)
                    o.positionCS = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
                    float4 positionCS_Last = mul(UNITY_MATRIX_VP_LAST, float4(pos, 1.0));
                    o.posCS = o.positionCS;
                    o.posCS_Last = positionCS_Last;
                #else
                    o.positionCS = TransformWorldToHClip(pos);
                #endif

                o.uv = v.positionOS;
                return o;
            }

            /*#if defined(SHADER_API_GLES)
            #define REIGN_COLORSPACE_GAMMA
            #endif*/

            struct PSOUT
            {
                float4 color : SV_Target0;

                #if defined(ENABLE_MOTIONVECTORS)
                float4 velocity : SV_Target1;
                #endif
            };

            PSOUT frag(v2f i)
            {
                PSOUT o;
                o.color = SAMPLE_TEXTURECUBE(_MainTex, sampler_MainTex, i.uv);
                /*#ifdef REIGN_COLORSPACE_GAMMA
                color = pow(color, 1.0 / 2.2);
                #endif*/

                // motion vectors
                #if defined(ENABLE_MOTIONVECTORS)
                o.velocity = float4((i.posCS.xy / i.posCS.w) - (i.posCS_Last.xy / i.posCS_Last.w), 0, 0);
                #endif
                return o;
            }
            ENDHLSL
        }
    }
}
