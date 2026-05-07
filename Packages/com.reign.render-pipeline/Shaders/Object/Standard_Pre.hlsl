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
    float3 normal : NORMAL0;
    float3 tangent : TANGENT0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif

#ifndef REIGN_VS_OUT_OVERRIDE
struct VS_OUT
{
    float2 uv : TEXCOORD0;
    float3 pos : TEXCOORD1;
    float3x3 surfaceMatrix : TEXCOORD2;

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
SAMPLER(sampler_BaseMap);
TEXTURE2D(_BaseMap);
float4 _BaseMap_ST;

SAMPLER(sampler_BumpMap);
TEXTURE2D(_BumpMap);
float4 _BumpMap_ST;
#endif

// =====================================
// Lights
// =====================================
#include "Standard_Lights.hlsl"

#endif
