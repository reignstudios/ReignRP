#ifndef REIGN_LIGHTINGUTILS
#define REIGN_LIGHTINGUTILS

// shadow resources
/*sampler2D _ShadowTex1, _ShadowTex2, _ShadowTex3, _ShadowTex4;
float4 _ShadowTex1_TexelSize;
float4x4 shadowMatrix1, shadowMatrix2, shadowMatrix3, shadowMatrix4;
float4 shadowCascades;
float shadowBias;*/
TEXTURE2D_FLOAT(_ShadowTex);
SAMPLER(sampler_ShadowTex);
float4 _ShadowTex_TexelSize;
float4x4 shadowMatrix;
float4 shadowColor;

float4 TransformWorldToHClipShadow(float3 positionWS)
{
	return mul(shadowMatrix, float4(positionWS, 1.0));
}

// fog resources
float4 fogColor;
float fogDitherStrength, fogFalloff, fogStrength;
float fogStart, fogEnd;

// helper methods
inline float FresnelSchlick(float3 eyeDir, float3 n, float F0)
{
	return F0 + (1.0 - F0) * pow(1.0 - saturate(dot(-eyeDir, n)), 5.0);
}

inline float FresnelSchlickZero(float3 eyeDir, float3 n)
{
	return pow(1.0 - saturate(dot(-eyeDir, n)), 5.0);
}

#endif