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

        Pass
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
            float4 args;// brightness, contrast, pow, maskScale
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
                float4 color = 0.0;

                // read mask
                float2 maskSizeDiv = _MaskTex_TexelSize.zw * args.w;
                real mask = tex2D(_MaskTex, (uv * upscaleTargetSize.zw) / maskSizeDiv).x;

                // mask Y
                [branch] if (mask <= .5)
                {
                    //return 0.0;
                    float2 offset = float2(0.0, upscaleTargetSize.y);
                    for (int x = 0; x < 4; x++)
                    {
                        half f = x / (4.0 - 1.0);
                        f = (1.0 - f) * (1.0 / 4.0);

                        float2 uvOffset = uv + offset;
                        real maskY = tex2D(_MaskTex, (uvOffset * upscaleTargetSize.zw) / maskSizeDiv).x;
                        [branch] if (maskY > .5) color += saturate(tex2D(_MainTex, uvOffset)) * f;

                        uvOffset = uv - offset;
                        maskY = tex2D(_MaskTex, (uvOffset * upscaleTargetSize.zw) / maskSizeDiv).x;
                        [branch] if (maskY > .5) color += saturate(tex2D(_MainTex, uvOffset)) * f;

                        offset += float2(0.0, upscaleTargetSize.y);
                    }

                    //return color;// CRT mode this should be off
                }
                else
                {
                    color = tex2D(_MainTex, uv);
                }

                for (int x = 0; x < 8; x++)
                {
                    half f = x / (8.0 - 1.0);
                    f = (1.0 - f) * (1.0 / 8.0);

                    float2 uvOffset = float2(upscaleTargetSize.x * (x + 1), 0.0);

                    color += saturate(tex2D(_MainTex, uv - uvOffset)) * f;
                    color += saturate(tex2D(_MainTex, uv + uvOffset)) * f;
                }

                // brightness contrast
                color = saturate(color + args.x);
                color *= args.y;
                color = pow(color, args.z);

                return color;
            }
            ENDHLSL
        }
    }
}
