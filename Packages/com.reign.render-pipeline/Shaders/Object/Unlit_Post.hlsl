#ifndef REIGN_POST
#define REIGN_POST

#ifdef ENABLE_SHADOWS
#include "Lit_Lights.hlsl"
#endif

// =====================================
// Base
// =====================================
#ifndef REIGN_GetVertexOutput_OVERRIDE
inline void GetVertexOutput(VS_IN i, inout VS_OUT o)
{
    // get local position
    float3 pos = i.positionOS;

    // custom local pos
    #ifdef REIGN_GetVertexOutput_OVERRIDE_LOCAL_POS
    GetVertexOutput_OverrideLocalPos(i, pos);
    #endif
    
    // get world position
    pos = TransformObjectToWorld(pos);
    
    // uv
    o.uv = (i.uv * _UVScaleOffset.xy) + _UVScaleOffset.zw;
    
    // shadow
    #ifdef ENABLE_SHADOWS
    o.shadowCS = TransformWorldToHClipShadow(pos);
    #endif
    
    // lightmap
    #ifdef LIGHTMAP_ON
    o.lightmapUV = TransformLightmapUV(i.lightmapUV);
    #endif

    // finish
    o.positionCS = TransformWorldToHClip(pos);
}
#endif

#ifndef REIGN_vert_OVERRIDE
VS_OUT vert(VS_IN i)
{
    VS_OUT o;

    // instancing 
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_TRANSFER_INSTANCE_ID(i, o);

    // XR
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // get vertex output
    GetVertexOutput(i, o);

    return o;
}
#endif

#ifndef REIGN_frag_OVERRIDE
PS_OUT frag(VS_OUT i)
{
    // XR
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    // material params
    PS_OUT o;
    
    // color
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_color
        #if defined(_COLOR_BOTH)
        o.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
        #elif defined(_COLOR_COLOR)
        o.color = _BaseColor;
        #elif defined(_COLOR_TEXTURE)
        o.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
        #else
        o.color = 1.0;
        #endif
    #else
        o.color = GetMaterialProperties_Override_Color(i);
    #endif
    
    // clip
    #ifdef ENABLE_ALPHACLIP
    clip(o.color.a - _AlphaClip);
    #endif
    
    #ifdef SS_UV
    float2 ssUV = i.positionCS.xy * targetSize.xy;
    #endif
    
    #ifdef ENABLE_SS_DITHERALPHA
        #if defined(_CLIP_MODE_DITHER)
        SSDitherClip(o.color.a, ssUV);
        #elif defined(_CLIP_MODE_PATTERN)
        SSPatternClip(o.color.a, ssUV);
        #else
        SSRandomClip(o.color.a, ssUV);
        #endif
    #endif
    
    // lightmap
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_lightmap
        #if defined(LIGHTMAP_ON)
        o.color.rgb *= SampleLightmap(i.lightmapUV).rgb;// maintain alpha
        #endif
    #else
        o.color *= GetMaterialProperties_Override_Lightmap(i);
    #endif
    
    // shadows
    #ifdef ENABLE_SHADOWS
        float2 shadowUV = (i.shadowCS.xy + 1.0) * .5;
        #ifdef UNITY_UV_STARTS_AT_TOP
        shadowUV.y = 1.0 - shadowUV.y;
        #endif
        o.color *= Process_Shadow(i.shadowCS, shadowUV);
    #endif

    // custom outs
    #ifdef REIGN_frag_CUSTOM_OUTS
    frag_CustomColors(i, o);
    #endif
    
    return o;
}
#endif

#endif