#ifndef REIGN_PRE
#define REIGN_PRE

#include "../_Shared/Common.hlsl"

// =====================================
// IN/OUT
// =====================================
#ifndef REIGN_VS_IN_OVERRIDE
struct VS_IN
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    
    #ifdef LIGHTMAP_ON
    float2 lightmapUV : TEXCOORD1;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif

#ifndef REIGN_VS_OUT_OVERRIDE
struct VS_OUT
{
    float2 uv : TEXCOORD0;
    
    #ifdef LIGHTMAP_ON
    float2 lightmapUV : TEXCOORD1;
    #endif

    float4 positionCS : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
#endif

#ifndef REIGN_PS_OUT_OVERRIDE
struct PS_OUT
{
    real4 color : SV_Target0;
};
#endif

#ifndef REIGN_INPUTS_OVERRIDE
float4 _UVScaleOffset;

#if defined(_COLOR_COLOR) || defined(_COLOR_BOTH)
half4 _BaseColor;
#endif
    
#if defined(_COLOR_ALBEDO) || defined(_COLOR_BOTH)
SAMPLER(sampler_BaseMap);
TEXTURE2D(_BaseMap);
#endif
#endif

#endif
