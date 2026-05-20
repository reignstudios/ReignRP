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
    
    float3 normal : NORMAL0;
    
    #ifdef ENABLE_NORMAL
    float3 tangent : TANGENT0;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif

#ifndef REIGN_VS_OUT_OVERRIDE
struct VS_OUT
{
    float2 uv : TEXCOORD0;
    float3 pos : TEXCOORD1;
    
    #if defined(ENABLE_NORMAL)
    float3x3 surfaceMatrix : TEXCOORD2;
    #else
    float3 normal : TEXCOORD2;
    #endif
    
    #ifdef LIGHTMAP_ON
    float2 lightmapUV : TEXCOORD8;
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

#ifndef _SPECULAR_OFF
half _SpecularIntensity, _SpecularRoughness, _SpecularMetallic, _SpecularFresnel;
#endif

#ifdef _SPECULAR_MAP
SAMPLER(sampler_SpecularMap);
TEXTURE2D(_SpecularMap);
#endif

#if defined(ENABLE_NORMAL)
SAMPLER(sampler_BumpMap);
TEXTURE2D(_BumpMap);
#endif

#if defined(ENABLE_OCCLUSION)
SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_OcclusionMap);
#endif

#if defined(ENABLE_EMISSION)
half4 _EmissionColor;
SAMPLER(sampler_EmissionMap);
TEXTURE2D(_EmissionMap);
#endif
#endif

// =====================================
// Lights
// =====================================
#include "Standard_Lights.hlsl"

#endif
