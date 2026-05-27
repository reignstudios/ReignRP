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

        Pass// Load Blit
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

            Texture2D<float4> _BlitTex;
            float4 _BlitTex_TexelSize;

            float4 srcRect, dstRect;
            float srcMipLvl;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = float4((v.positionOS.xy * dstRect.zw) + dstRect.xy, .5, 1);
                o.uv = (v.uv * srcRect.zw) + srcRect.xy;
                return o;
            }

            struct PSOUT
            {
                float4 color : SV_Target0;
            };

            PSOUT frag(v2f i)
            {
                PSOUT o;
                o.color = _BlitTex.Load(int3(i.uv * _BlitTex_TexelSize.zw, srcMipLvl));
                return o;
            }
            ENDHLSL
        }

        Pass// Sampler Blit
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

            SAMPLER(sampler_BlitTex);
            TEXTURE2D(_BlitTex);

            float4 srcRect, dstRect;
            float srcMipLvl;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = float4((v.positionOS.xy * dstRect.zw) + dstRect.xy, .5, 1);
                o.uv = (v.uv * srcRect.zw) + srcRect.xy;
                return o;
            }

            struct PSOUT
            {
                float4 color : SV_Target0;
            };

            PSOUT frag(v2f i)
            {
                PSOUT o;
                o.color = SAMPLE_TEXTURE2D_LOD(_BlitTex, sampler_BlitTex, i.uv, srcMipLvl);
                return o;
            }
            ENDHLSL
        }

        Pass// 2X MSAA Blit
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MSAA_XX 2
            #include "BlitMSAA.hlsl"
            ENDHLSL
        }

        Pass// 4X MSAA Blit
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MSAA_XX 4
            #include "BlitMSAA.hlsl"
            ENDHLSL
        }

        Pass// 8X MSAA Blit
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MSAA_XX 8
            #include "BlitMSAA.hlsl"
            ENDHLSL
        }
    }
}
