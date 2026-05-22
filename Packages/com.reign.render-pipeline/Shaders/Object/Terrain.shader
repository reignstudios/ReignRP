Shader "ReignRP/Terrain"
{
    Properties
    {
        [HideInInspector] [ToggleUI] _EnableHeightBlend("EnableHeightBlend", Float) = 0.0
        _HeightTransition("Height Transition", Range(0, 1.0)) = 0.0

        // Layer count is passed down to guide height-blend enable/disable, due
        // to the fact that heigh-based blend will be broken with multipass.
        //[HideInInspector][PerRendererData] _NumLayersCount("Total Layer Count", Float) = 1.0

        // Input Textures
        [HideInInspector] _Control0("Control (0) (RGBA)", 2D) = "red" {}
        [HideInInspector] _Control1("Control (1) (RGBA)", 2D) = "black" {}

        [HideInInspector] _Splat0("Layer (0) D", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer (1) C", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer (2) B", 2D) = "white" {}
        [HideInInspector] _Splat3("Layer (3) A", 2D) = "white" {}
        [HideInInspector] _Splat4("Layer (4) E", 2D) = "white" {}
        [HideInInspector] _Splat5("Layer (5) F", 2D) = "white" {}
        [HideInInspector] _Splat6("Layer (6) G", 2D) = "white" {}
        [HideInInspector] _Splat7("Layer (7) H", 2D) = "white" {}

        [HideInInspector] _Normal0("Layer (0) D", 2D) = "bump" {}
        [HideInInspector] _Normal1("Layer (1) C", 2D) = "bump" {}
        [HideInInspector] _Normal2("Layer (2) B", 2D) = "bump" {}
        [HideInInspector] _Normal3("Layer (3) A", 2D) = "bump" {}
        [HideInInspector] _Normal4("Layer (4) E", 2D) = "bump" {}
        [HideInInspector] _Normal5("Layer (5) F", 2D) = "bump" {}
        [HideInInspector] _Normal6("Layer (6) G", 2D) = "bump" {}
        [HideInInspector] _Normal7("Layer (7) H", 2D) = "bump" {}

        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask4("Mask 4 (R)", 2D) = "grey" {}
        [HideInInspector] _Mask5("Mask 5 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask6("Mask 6 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask7("Mask 7 (A)", 2D) = "grey" {}

        // Input Texture Values
        /*[HideInInspector] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic4("Metallic 4", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic5("Metallic 5", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic6("Metallic 6", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic7("Metallic 7", Range(0.0, 1.0)) = 0.0

        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness4("Smoothness 4", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness5("Smoothness 5", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness6("Smoothness 6", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness7("Smoothness 7", Range(0.0, 1.0)) = 0.5*/

        // used in fallback on old cards & base map
        //[HideInInspector] _BaseMap("BaseMap (RGB)", 2D) = "grey" {}
        //[HideInInspector] _BaseColor("Main Color", Color) = (1,1,1,1)

        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}

        [ToggleUI] _EnableInstancedPerPixelNormal("Enable Instanced per-pixel normal", Float) = 1.0
    }

    SubShader
    {
        Tags { "LightMode" = "Reign_Opaque" "Queue" = "Geometry" "RenderType" = "Opaque" "SplatCount" = "8" }
        Cull Back
        ZTest Less
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Lit_Pre.hlsl"
            #include "Lit_Post.hlsl"
            ENDHLSL
        }

        /*Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Terrain builtin keywords
            #pragma shader_feature_local _TERRAIN_8_LAYERS
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _MASKMAP
            #pragma shader_feature_local _SPECULAR_OCCLUSION_NONE
            #pragma shader_feature_local _TERRAIN_BLEND_HEIGHT
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL

            // ==================
            // standard prep
            // ==================
            #define REIGN_VS_OUT_OVERRIDE
            #define REIGN_INPUTS_OVERRIDE
            #define REIGN_GetVertexOutput_OVERRIDE
            #define REIGN_GetMaterialProperties_OVERRIDE
            #define TERRAIN_LAND

            #include "Standard_Pre.hlsl"

            // ==================
            // VS out
            // ==================
            struct VS_OUT
            {
                float2 uvC0 : TEXCOORD0;
                float2 uvS0 : TEXCOORD1;
                float2 uvS1 : TEXCOORD2;
                float2 uvS2 : TEXCOORD3;
                float2 uvS3 : TEXCOORD4;
                #ifdef _TERRAIN_8_LAYERS
                float2 uvS4 : TEXCOORD5;
                float2 uvS5 : TEXCOORD6;
                float2 uvS6 : TEXCOORD7;
                float2 uvS7 : TEXCOORD8;

                float3 pos : TEXCOORD9;
                real3x3 surfaceMatrix : TEXCOORD10;
                #else
                float3 pos : TEXCOORD5;
                real3x3 surfaceMatrix : TEXCOORD6;
                #endif

                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ==================
            // inputs
            // ==================
            SAMPLER(sampler_Control0);
            SAMPLER(sampler_Splat0);
            TEXTURE2D(_Control0);
            TEXTURE2D(_Control1);
            TEXTURE2D(_Splat0);
            TEXTURE2D(_Splat1);
            TEXTURE2D(_Splat2);
            TEXTURE2D(_Splat3);
            #ifdef _TERRAIN_8_LAYERS
            TEXTURE2D(_Splat4);
            TEXTURE2D(_Splat5);
            TEXTURE2D(_Splat6);
            TEXTURE2D(_Splat7);
            #endif

            float4 _Control0_ST;
            float4  _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
            #ifdef _TERRAIN_8_LAYERS
            float4  _Splat4_ST, _Splat5_ST, _Splat6_ST, _Splat7_ST;
            #endif

    #ifndef SHADER_API_GLES
            TEXTURE2D(_Normal0);
            TEXTURE2D(_Normal1);
            TEXTURE2D(_Normal2);
            TEXTURE2D(_Normal3);
            #ifdef _TERRAIN_8_LAYERS
            TEXTURE2D(_Normal4);
            TEXTURE2D(_Normal5);
            TEXTURE2D(_Normal6);
            TEXTURE2D(_Normal7);
            #endif

            TEXTURE2D(_Mask0);
            TEXTURE2D(_Mask1);
            TEXTURE2D(_Mask2);
            TEXTURE2D(_Mask3);
            #ifdef _TERRAIN_8_LAYERS
            TEXTURE2D(_Mask4);
            TEXTURE2D(_Mask5);
            TEXTURE2D(_Mask6);
            TEXTURE2D(_Mask7);
            #endif
    #endif

            // ==================
            // standard
            // ==================
            inline void GetVertexOutput(VS_IN i, inout VS_OUT o)
            {
                // position
                float3 pos = i.positionOS;
                o.pos = TransformObjectToWorld(pos);
                o.positionCS = TransformWorldToHClip(o.pos);

                // uv
                o.uvC0 = TRANSFORM_TEX(i.uv, _Control0);
                o.uvS0 = TRANSFORM_TEX(i.uv, _Splat0);
                o.uvS1 = TRANSFORM_TEX(i.uv, _Splat1);
                o.uvS2 = TRANSFORM_TEX(i.uv, _Splat2);
                o.uvS3 = TRANSFORM_TEX(i.uv, _Splat3);
            #ifdef _TERRAIN_8_LAYERS
                o.uvS4 = TRANSFORM_TEX(i.uv, _Splat4);
                o.uvS5 = TRANSFORM_TEX(i.uv, _Splat5);
                o.uvS6 = TRANSFORM_TEX(i.uv, _Splat6);
                o.uvS7 = TRANSFORM_TEX(i.uv, _Splat7);
            #endif

                // surface
                o.surfaceMatrix = real3x3
                (
                    i.tangent,
                    cross(i.tangent, i.normal),
                    i.normal
                );
                o.surfaceMatrix = mul(o.surfaceMatrix, unity_WorldToObject);
            }

            inline MaterialParams GetMaterialProperties(VS_OUT i)
            {
                MaterialParams materialParams;

                // base color
                real4 c = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, i.uvC0);
                real4 s0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, i.uvS0);
                real4 s1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, i.uvS1);
                real4 s2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, i.uvS2);
                real4 s3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, i.uvS3);
                materialParams.color = (s0 * c.r) + (s1 * c.g) + (s2 * c.b) + (s3 * c.a);

                #ifdef _TERRAIN_8_LAYERS
                c = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, i.uvC0);
                s0 = SAMPLE_TEXTURE2D(_Splat4, sampler_Splat0, i.uvS4);
                s1 = SAMPLE_TEXTURE2D(_Splat5, sampler_Splat0, i.uvS5);
                s2 = SAMPLE_TEXTURE2D(_Splat6, sampler_Splat0, i.uvS6);
                s3 = SAMPLE_TEXTURE2D(_Splat7, sampler_Splat0, i.uvS7);
                materialParams.color += (s0 * c.r) + (s1 * c.g) + (s2 * c.b) + (s3 * c.a);
                #endif

                // emissive
                materialParams.emissive = 0;

                // get normal
                materialParams.normal = normalize(i.surfaceMatrix[2]);

                return materialParams;
            }

            #include "Standard_Post.hlsl"
            ENDHLSL
        }*/
    }
}
