Shader "Unlit/Combine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainTex2 ("Texture2", 2D) = "white" {}
    }

    SubShader
    {
        //Tags { "RenderType"="Opaque" }
        Tags { "LightMode" = "SRPDefaultUnlit" "Queue" = "Geometry" "RenderType" = "Opaque" }
        Cull Off
        ZTest Always
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.reign.render-pipeline/Shaders/_Shared/Common.hlsl"

            // ==================
            // VS in
            // ==================
            struct VS_IN
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            // ==================
            // VS out
            // ==================
            struct VS_OUT
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            VS_OUT vert(VS_IN i)
            {
                VS_OUT o;
                //o.positionCS = TransformWorldToHClip(TransformObjectToWorld(i.positionOS));
                o.positionCS = float4(i.positionOS.xy, .5, 1);
                o.uv = i.uv;
                return o;
            }

            sampler2D _MainTex, _MainTex2;

            float4 frag(VS_OUT i) : SV_Target
            {
                return float4(1, 0, 0, 1);
            }
            ENDHLSL

            /*CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex, _MainTex2;
            float4 _MainTex_ST, _MainTex2_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                return c;
            }
            ENDCG*/
        }
    }
}
