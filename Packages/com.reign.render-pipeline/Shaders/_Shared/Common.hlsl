#ifndef REIGN_COMMON
#define REIGN_COMMON

// ======================================
// base
// ======================================
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// re-define "real" to min percision (TODO: bug in SampleSH9 preventing this)
/*#define real min10float
#define real2 min10float2
#define real3 min10float3
#define real4 min10float4

#define real2x2 min10float2x2
#define real2x3 min10float2x3
#define real2x4 min10float2x4
#define real3x2 min10float3x2
#define real3x3 min10float3x3
#define real3x4 min10float3x4
#define real4x3 min10float4x3
#define real4x4 min10float4x4*/

/*CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;// this must be defined for SRP batching to work
real4 unity_WorldTransformParams;

// these must be defined for SRP batching to work
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
float3 _WorldSpaceCameraPos;

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

// stereo
//#if defined(USING_STEREO_MATRICES)
//CBUFFER_START(UnityStereoGlobals)
//float4x4 unity_StereoMatrixP[2];
//float4x4 unity_StereoMatrixV[2];
//float4x4 unity_StereoMatrixInvV[2];
//float4x4 unity_StereoMatrixVP[2];
//
//float4x4 unity_StereoCameraProjection[2];
//float4x4 unity_StereoCameraInvProjection[2];
//float4x4 unity_StereoWorldToCamera[2];
//float4x4 unity_StereoCameraToWorld[2];
//
//float3 unity_StereoWorldSpaceCameraPos[2];
//float4 unity_StereoScaleOffset[2];
//CBUFFER_END
//#endif

#ifdef STEREO_MULTIVIEW_ON
CBUFFER_START(UnityStereoEyeIndices)
    float4 unity_StereoEyeIndices[2];
CBUFFER_END
#endif

//#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
//// OVR_multiview
//// In order to convey this info over the DX compiler, we wrap it into a cbuffer.
//#if !defined(UNITY_DECLARE_MULTIVIEW)
//#define UNITY_DECLARE_MULTIVIEW(number_of_views) GLOBAL_CBUFFER_START(OVR_multiview) uint gl_ViewID; uint numViews_##number_of_views; GLOBAL_CBUFFER_END
//#define UNITY_VIEWID gl_ViewID
//#endif
//#endif

//#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
//#define unity_StereoEyeIndex UNITY_VIEWID
//UNITY_DECLARE_MULTIVIEW(2);
//#elif defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#ifdef STEREO_MULTIVIEW_ON
static uint unity_StereoEyeIndex; 
//#elif defined(UNITY_SINGLE_PASS_STEREO)
#else
CBUFFER_START(UnityStereoEyeIndex)
int unity_StereoEyeIndex;
CBUFFER_END
#endif*/

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "UnityInput.hlsl"// pulled from URP (NOTE: this should be kept up to date with newer URP versions to ensure feature completion)

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   ERROR_UNITY_MATRIX_I_P_IS_NOT_DEFINED
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  _InvCameraViewProj
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)

#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// global platform settings
#if !defined(SHADER_API_GLES) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_OPENGL) && !defined(SHADER_API_GLCORE)
#define REIGN_UV_STARTS_AT_TOP
#else
#define REIGN_DEPTH_OLD_RANGE
#endif

#if defined(SHADER_API_GLES)
#define REIGN_COLORSPACE_GAMMA
#endif

// sampler states
SamplerState sampler_linear_repeat;
SamplerState sampler_point_repeat;

// depth resources
TEXTURE2D_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
float4x4 clipToWorld;//, worldToClip;

// compositing
float4 targetSize;

// ======================================
// lightmapping
// ======================================
#ifdef LIGHTMAP_ON
/*#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);*/

inline real4 SampleLightmap(float2 uv)
{
	real3 result = SampleSingleLightmap
	(
		TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), uv,
		float4(1.0, 1.0, 0.0, 0.0),
		#ifdef UNITY_LIGHTMAP_FULL_HDR
		false,// not compressed
		#else
		true,// is compressed
		#endif
		float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
	);
	return real4(result, 1.0);
}

inline float2 TransformLightmapUV(float2 lightmapUV)
{
	return lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
}
#endif

// ======================================
// util
// ======================================
inline float3x3 SurfaceMatrix(float3 normal, float3 tangent)
{
	return float3x3
    (
        tangent,
		cross(tangent, normal),
        normal
    );
}

inline float3 ComputeScreenPos(float4 posCS)
{
	float3 uv = posCS.xyz / posCS.w;
	return (uv * .5) + .5;
}

inline float3 ComputeScreenNormal(float3 normalWS)
{
	return mul(unity_MatrixV, float4(normalWS, 0.0)).xyz;
}

float LinearEyeDepth(float depth)
{
	return 1.0 / (_ZBufferParams.z * depth + _ZBufferParams.w);
}

#endif

// << REF >>
// UnityPerMaterial: per material constant buffer
// UnityPerDraw: per object constant buffer

//CBUFFER_START(UnityPerMaterial)// this is used for batching optimisation only
//float4 _Color;
//CBUFFER_END

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)// this is used for batching and instancing optimisations
//UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
