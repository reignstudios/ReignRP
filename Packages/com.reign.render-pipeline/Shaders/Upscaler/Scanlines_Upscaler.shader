Shader "ReignRP/PostProcess/Scanlines_Upscaler"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="PostProcess" }
        ZWrite Off
        ZTest Always

        Pass// mask
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"

            struct appdata
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex, _MaskTex;
            float4 _MainTex_TexelSize, _MaskTex_TexelSize;
            float4 args;// brightness, contrast, pow, post-contrast
            float4 upscaleTargetSize;
            float maskScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);

                // apply mask
                float2 maskSizeDiv = _MaskTex_TexelSize.zw * maskScale;
                color *= tex2D(_MaskTex, (i.uv * upscaleTargetSize.zw) / maskSizeDiv);

                // brightness contrast
                color = max(0.0, color + args.x);// brightness
                color *= args.y;// contrast
                color = pow(color, args.z);// pow
                color *= args.w;// post-contrast

                return color;
            }
            ENDHLSL
        }

        Pass// bloom
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"

            struct appdata
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex, _MaskTex;
            float4 _MainTex_TexelSize, _MaskTex_TexelSize;
            float4 bloomCounts;
            float4 upscaleTargetSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 color = tex2D(_MainTex, uv);

                // PosX
                [loop] for (int x = 0; x < bloomCounts.x; x++)
                {
                    half f = x / (bloomCounts.x + 1.0);
                    float2 uvOffset = float2(upscaleTargetSize.x * (x + 1.0), 0.0);
                    color += saturate(tex2D(_MainTex, uv + uvOffset)) * f;
                }

                // NegX
                [loop] for (int x = 0; x < bloomCounts.y; x++)
                {
                    half f = x / (bloomCounts.y + 1.0);
                    float2 uvOffset = float2(upscaleTargetSize.x * (x + 1.0), 0.0);
                    color += saturate(tex2D(_MainTex, uv - uvOffset)) * f;
                }

                // PosY
                [loop] for (int y = 0; y < bloomCounts.z; y++)
                {
                    half f = y / (bloomCounts.z + 1.0);
                    float2 uvOffset = float2(0.0, upscaleTargetSize.y * (y + 1.0));
                    color += saturate(tex2D(_MainTex, uv + uvOffset)) * f;
                }

                // NegY
                [loop] for (int y = 0; y < bloomCounts.w; y++)
                {
                    half f = y / (bloomCounts.w + 1.0);
                    float2 uvOffset = float2(0.0, upscaleTargetSize.y * (y + 1.0));
                    color += saturate(tex2D(_MainTex, uv - uvOffset)) * f;
                }

                return color;
            }
            ENDHLSL
        }
    }
}
