Shader "ReignRP/Upscaler/TubeDisplayGlass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

         /*// Row V = 0
        _P00 ("P00", Vector) = (0.0,      0.0,      0, 0)
        _P10 ("P10", Vector) = (0.333333, 0.0,      0, 0)
        _P20 ("P20", Vector) = (0.666667, 0.0,      0, 0)
        _P30 ("P30", Vector) = (1.0,      0.0,      0, 0)

        // Row V = 1/3
        _P01 ("P01", Vector) = (0.0,      0.333333, 0, 0)
        _P11 ("P11", Vector) = (0.333333, 0.333333, 0, 0)
        _P21 ("P21", Vector) = (0.666667, 0.333333, 0, 0)
        _P31 ("P31", Vector) = (1.0,      0.333333, 0, 0)

        // Row V = 2/3
        _P02 ("P02", Vector) = (0.0,      0.666667, 0, 0)
        _P12 ("P12", Vector) = (0.333333, 0.666667, 0, 0)
        _P22 ("P22", Vector) = (0.666667, 0.666667, 0, 0)
        _P32 ("P32", Vector) = (1.0,      0.666667, 0, 0)

        // Row V = 1
        _P03 ("P03", Vector) = (0.0,      1.0,      0, 0)
        _P13 ("P13", Vector) = (0.333333, 1.0,      0, 0)
        _P23 ("P23", Vector) = (0.666667, 1.0,      0, 0)
        _P33 ("P33", Vector) = (1.0,      1.0,      0, 0)*/
    }
    SubShader
    {
        Tags { "RenderType"="Upscaler" }
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

            sampler2D _MainTex;

            float4 _P00;
            float4 _P10;
            float4 _P20;
            float4 _P30;

            float4 _P01;
            float4 _P11;
            float4 _P21;
            float4 _P31;

            float4 _P02;
            float4 _P12;
            float4 _P22;
            float4 _P32;

            float4 _P03;
            float4 _P13;
            float4 _P23;
            float4 _P33;

            float4 BezierBasis(float t)
            {
                float it = 1.0 - t;

                float it2 = it * it;
                float t2 = t * t;

                return float4(
                    it2 * it,
                    3.0 * it2 * t,
                    3.0 * it * t2,
                    t2 * t
                );
            }

            float2 BezierPatchUV(float2 uv)
            {
                float4 bu = BezierBasis(uv.x);
                float4 bv = BezierBasis(uv.y);

                // Evaluate the four cubic Bézier curves in U.
                float2 r0 =
                      _P00.xy * bu.x
                    + _P10.xy * bu.y
                    + _P20.xy * bu.z
                    + _P30.xy * bu.w;

                float2 r1 =
                      _P01.xy * bu.x
                    + _P11.xy * bu.y
                    + _P21.xy * bu.z
                    + _P31.xy * bu.w;

                float2 r2 =
                      _P02.xy * bu.x
                    + _P12.xy * bu.y
                    + _P22.xy * bu.z
                    + _P32.xy * bu.w;

                float2 r3 =
                      _P03.xy * bu.x
                    + _P13.xy * bu.y
                    + _P23.xy * bu.z
                    + _P33.xy * bu.w;

                // Bézier interpolation between those four curves in V.
                return
                      r0 * bv.x
                    + r1 * bv.y
                    + r2 * bv.z
                    + r3 * bv.w;
            }

            float2 MirrorUV(float2 uv)
            {
                return 1.0 - abs(frac(uv * 0.5) * 2.0 - 1.0);
            }

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453) - .5;
            }


            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS));
                o.uv = v.uv;

                return o;
            }

            #define fade .99
            float4 frag (v2f i) : SV_Target
            {
                float2 uv = BezierPatchUV(i.uv);

                float4 color;
                [branch] if (uv.x >= 0.0 && uv.y >= 0.0 && uv.x <= 1.0 && uv.y <= 1.0)
                {
                    color = tex2D(_MainTex, uv);

                    // fade to black
                    uv = abs((uv - .5) * 2.0);
                    float v = max(uv.x, uv.y);
                    color *= 1.0 - (saturate(v - fade) / (1.0 - fade));
                }
                else
                {
                    // outer reflection
                    float2 reflectedUV = MirrorUV(uv);
                    float dis = distance(uv, reflectedUV);
                    reflectedUV += random(i.uv) * dis * .5;
                    color = tex2D(_MainTex, reflectedUV);
                    color = max(0.0, color - .1) * .5 * dis;
                }

                return color;
            }
            ENDHLSL
        }
    }
}
