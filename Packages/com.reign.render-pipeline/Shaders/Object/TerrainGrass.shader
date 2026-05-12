Shader "ReignRP/TerrainGrass"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "LightMode" = "Reign_Opaque" "Queue" = "Geometry" "RenderType" = "Opaque" }
        Cull Off
        ZTest Less
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Standard_Pre.hlsl"
            #include "Standard_Post.hlsl"
            ENDHLSL
        }

        /*Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // ==================
            // standard prep
            // ==================
            #define REIGN_ProcessDiffuse_DirectionalLight_OVERRIDE
            #define REIGN_VS_IN_OVERRIDE
            #define REIGN_VS_OUT_OVERRIDE
            #define REIGN_GetVertexOutput_OVERRIDE
            #define REIGN_GetMaterialProperties_OVERRIDE
            #define TERRAIN_GRASS

            #include "Standard_Pre.hlsl"

            // ==================
            // VS in
            // ==================
            struct VS_IN
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ==================
            // VS out
            // ==================
            struct VS_OUT
            {
                float2 uv : TEXCOORD0;
                float3 pos : TEXCOORD1;
                real3 normal : TEXCOORD2;

                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ==================
            // standard
            // ==================
            inline real4 ProcessDiffuse_DirectionalLight(MaterialParams materialParams, real3 direction, real4 color)
            {
	            real d = dot(-direction, materialParams.normal);
	            real4 result = materialParams.color * color * max(0.0, d);

	            // grass sss
	            d = pow(max(0.0, -d), 1.0);
	            result += materialParams.color * color * d * .25;

	            d = pow(abs(1.0 - d), 9.0);
	            result += materialParams.color * color * d * .1;

	            return result;
            }

            inline void GetVertexOutput(VS_IN i, inout VS_OUT o)
            {
                // position
                float3 pos = i.positionOS;
                o.pos = TransformObjectToWorld(pos);

                float offset = sin((o.pos.x * .2) - (_Time.z * 1.5)) * pos.y * .2;
                if (offset < 0.0) offset = -pow(-offset, 1.25);
                o.pos.x += offset;
            
                offset = sin((o.pos.z * 1.5) - (_Time.z * 2.0)) * pos.y * .1;
                if (offset < 0.0) offset = -pow(-offset, 1.5);
                o.pos.z += offset;

                o.positionCS = TransformWorldToHClip(o.pos);
            
                // uv
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
            
                // surface
                o.normal = mul(i.normal, unity_WorldToObject);
                float3 baseNormal = _WorldSpaceCameraPos - o.pos;
                baseNormal.y = 0;
                o.normal = lerp(normalize(baseNormal), o.normal, saturate(pos.y));
            }

            inline MaterialParams GetMaterialProperties(VS_OUT i)
            {
                MaterialParams materialParams;

                // base color
                materialParams.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                // emissive
                materialParams.emissive = 0;

                // grass
                clip(materialParams.color.a - .5);

                // get normal
                materialParams.normal = normalize(i.normal);

                return materialParams;
            }

            #include "Standard_Post.hlsl"
            ENDHLSL
        }*/
    }
}
