Shader "ReignRP/Upscaler/DotsDisplay"
{
    Properties
    {
        [Toggle(ENABLE_SIN)] _ENABLE_SIN ("Enable Alpha Clip", Float) = 0

        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _PalletTex ("Pallet", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Upscaler" }
        ZWrite Off
        ZTest Always

        Pass// resize
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _ENABLE_SIN

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

            sampler2D _MainTex, _PalletTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.uv);

                // apply display color
                half grayscale = dot(color.rgb, half3(.3333, .3333, .3333));
                #ifdef _ENABLE_SIN
                grayscale = sin(grayscale * 3.14 * 2.0);
                #endif
                return tex2D(_PalletTex, half2(grayscale, 0.0));
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

            sampler2D _MainTex, _MaskTex, _PalletTex;
            float4 _MainTex_TexelSize, _MaskTex_TexelSize;
            float4 upscaleTargetSize;
            float maskScale;
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
                color = lerp(saturate(tex2D(_PalletTex, half2(0.0, 0.0)) * 1.1), color, mask);

                // add shadow
                float2 offset = float2(upscaleTargetSize.x, -upscaleTargetSize.y);
                half4 shadow = 0.0;
                [loop] for (int x = 0; x < shadowSamples; x++)
                {
                    shadow += tex2D(_MainTex, uv - offset * (x + 1.0));
                }
                color = (color + (shadow * .25)) * .5;

                return color;
            }
            ENDHLSL
        }
    }
}
