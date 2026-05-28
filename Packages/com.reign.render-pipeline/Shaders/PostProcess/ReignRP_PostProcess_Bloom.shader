Shader "Hidden/ReignRP/PostProcess_Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="PostProcess" }
        ZWrite Off
        ZTest Always

        Pass// high-pass
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

            sampler2D _MainTex;
            float highPassRange;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                real4 color = tex2D(_MainTex, i.uv);
                return max(0.0, color - highPassRange);
            }
            ENDHLSL
        }

        Pass// blur radial
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

            sampler2D _MainTex;
            float4 _MainTex_ST, _MainTex_TexelSize;
            
            float4 args;// direction X, direction Y, loop-count, strength
            float4 args2;// sample-texel-offset X

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                real4 color = tex2D(_MainTex, i.uv);
                float2 texelStep = _MainTex_TexelSize.xy * args.xy;
                float2 texelStepHalf = texelStep * args2.x;
                float samples = 1;
                int ringPixels = 8;
                int ringCount = args.z;
                [loop] for (int r = 0; r != ringCount; ++r)
                {
                    float rot = 0.0;
                    float radius = (r + 1);
                    [loop] for (int x = 0; x != ringPixels; ++x)
                    {
                        float2 dir = float2(cos(rot), sin(rot));
                        float2 uv = i.uv + (dir * texelStep * radius);
                        uv += dir * texelStepHalf;
                        rot += 6.283185307179586476925286766559 / ringPixels;

                        color += tex2D(_MainTex, uv);
                        samples += 1.0;
                    }

                    ringPixels *= 2;
                }

                return (color / samples) * args.w;
            }
            ENDHLSL
        }

        Pass// blur classic
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

            sampler2D _MainTex;
            float4 _MainTex_ST, _MainTex_TexelSize;
            
            float4 args;// direction X, direction Y, loop-count, strength
            float4 args2;// sample-texel-offset X

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                real4 color = 0.0;
                int startIndex = -args.z;
                int endIndex = args.z + 1;
                float2 texelStep = _MainTex_TexelSize.xy * args.xy;
                float2 texelStepHalf = texelStep * args2.x;
                [loop] for (int x = startIndex; x != endIndex; ++x)
                {
                    float2 uv = i.uv + (texelStep * x);
                    float dir = x >= 0 ? 1.0 : -1.0;
                    dir = x == 0 ? 0.0 : dir;
                    uv += texelStepHalf * dir;
                    color += tex2D(_MainTex, uv);
                }
                return color * args.w;
            }
            ENDHLSL
        }

        Pass// size down / copy
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

            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDHLSL
        }

        Pass// composite
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../_Shared/Common.hlsl"

            #pragma multi_compile MODE_NORMAL MODE_HIGHPASS_ONLY
            #pragma multi_compile LVL_1X LVL_2X LVL_4X LVL_8X LVL_16X

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

            sampler2D _MainTex, _MainTex1;
            
            #if defined(LVL_2X) || defined(LVL_4X) || defined(LVL_8X) || defined(LVL_16X)
            sampler2D _MainTex2;
            #endif
            
            #if defined(LVL_4X) || defined(LVL_8X) || defined(LVL_16X)
            sampler2D _MainTex4;
            #endif
            
            #if defined(LVL_8X) || defined(LVL_16X)
            sampler2D _MainTex8;
            #endif
            
            #if defined(LVL_16X)
            sampler2D _MainTex16;
            #endif

            float4 mulArgs1;// 1x, 2x, 4x, 8x
            float4 mulArgs2;// 16x, strenth

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                real4 color = tex2D(_MainTex1, i.uv) * mulArgs1.x;
                
                #if defined(LVL_2X) || defined(LVL_4X) || defined(LVL_8X) || defined(LVL_16X)
                color += tex2D(_MainTex2, i.uv) * mulArgs1.y;
                #endif

                #if defined(LVL_4X) || defined(LVL_8X) || defined(LVL_16X)
                color += tex2D(_MainTex4, i.uv) * mulArgs1.z;
                #endif

                #if defined(LVL_8X) || defined(LVL_16X)
                color += tex2D(_MainTex8, i.uv) * mulArgs1.w;
                #endif

                #if defined(LVL_16X)
                color += tex2D(_MainTex16, i.uv) * mulArgs2.x;
                #endif

                #ifdef MODE_NORMAL
                return tex2D(_MainTex, i.uv) + (color * mulArgs2.y);
                #endif

                #ifdef MODE_HIGHPASS_ONLY
                return color * mulArgs2.y;
                #endif

                return 0;
            }
            ENDHLSL
        }
    }
}
