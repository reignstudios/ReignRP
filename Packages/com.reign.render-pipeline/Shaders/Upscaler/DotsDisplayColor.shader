Shader "ReignRP/Upscaler/DotsDisplayColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _BackgroundTex ("Background", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Upscaler" }
        ZWrite Off
        ZTest Always

        Pass// resize & apply color
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ENABLE_SIN

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

            sampler2D _MainTex, _BackgroundTex;
            float displayBit;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = saturate(tex2D(_MainTex, i.uv));

                // apply display color
                half4 background = tex2D(_BackgroundTex, i.uv);
                color = floor(color * displayBit) / displayBit;// lower pixel bit
                color.r = lerp(background.r, color.r, 1.0 - color.r);
                color.g = lerp(background.g, color.g, 1.0 - color.g);
                color.b = lerp(background.b, color.b, 1.0 - color.b);

                return color;
            }
            ENDHLSL
        }

        Pass// mask and pixel processing
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
                float2 pixelScale : TEXCOORD1;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex, _MaskTex;
            float4 _MainTex_TexelSize, _MaskTex_TexelSize;
            float4 upscaleTargetSize;
            float shadowSamples;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                o.pixelScale = _MainTex_TexelSize.zw / (upscaleTargetSize.zw / _MaskTex_TexelSize.zw);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                half4 color = tex2D(_MainTex, uv);

                // apply mask
                half4 mask = tex2D(_MaskTex, ((i.uv * upscaleTargetSize.zw) / _MaskTex_TexelSize.zw) * i.pixelScale);
                color *= mask;

                // add shadow
                float2 offset = float2(upscaleTargetSize.x, -upscaleTargetSize.y);
                half4 shadow = 0.0;
                bool maskHit = false;
                [loop] for (int x = 0; x < shadowSamples; x++)
                {
                    float2 uvOffset = uv - offset * (x + 1.0);
                    half4 s = tex2D(_MainTex, uvOffset);
                    shadow += s * tex2D(_MaskTex, ((uvOffset * upscaleTargetSize.zw) / _MaskTex_TexelSize.zw) * i.pixelScale);
                }
                color = (color + (shadow / shadowSamples)) * .5;

                return color;
            }
            ENDHLSL
        }
    }
}
