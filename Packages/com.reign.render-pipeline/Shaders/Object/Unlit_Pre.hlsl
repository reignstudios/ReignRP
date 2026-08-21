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
    
    #ifdef ENABLE_UV
    float2 uv : TEXCOORD0;
    #endif
    
    #ifdef ENABLE_COLOR
    float4 color : COLOR;
    #endif
    
    #if !defined(_EXTRUDE_OFF) || defined(ENABLE_NORMAL)
    float3 normal : NORMAL;
    #endif
    
    #ifdef LIGHTMAP_ON
    float2 lightmapUV : TEXCOORD1;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif

#ifndef REIGN_VS_OUT_OVERRIDE
struct VS_OUT
{
    #ifdef ENABLE_UV
    float2 uv : TEXCOORD0;
    #endif
    
    #ifdef ENABLE_POS
    float3 pos : TEXCOORD1;
    #endif
    
    #ifdef ENABLE_POS_LOCAL
    float3 posLocal : TEXCOORD2;
    #endif
    
    #ifdef ENABLE_COLOR
    float4 color : TEXCOORD3;
    #endif
    
    #ifdef ENABLE_NORMAL
    float3 normal : TEXCOORD4;
    #endif
    
    #ifdef ENABLE_SHADOWS
    float4 shadowCS : TEXCOORD7;
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
    
#if defined(_COLOR_TEXTURE) || defined(_COLOR_BOTH)
SAMPLER(sampler_BaseMap);
TEXTURE2D(_BaseMap);
#endif

#ifdef ENABLE_ALPHACLIP
float _AlphaClip;
#endif

#ifndef _EXTRUDE_OFF
float _ExtrudeValue;
#endif
#endif

#endif
