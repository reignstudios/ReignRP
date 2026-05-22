#ifndef REIGN_POST
#define REIGN_POST

// =====================================
// Base
// =====================================
#ifndef REIGN_GetVertexOutput_OVERRIDE
inline void GetVertexOutput(VS_IN i, inout VS_OUT o)
{
    // uv
    o.uv = (i.uv * _UVScaleOffset.xy) + _UVScaleOffset.zw;
    
    #ifdef LIGHTMAP_ON
    o.lightmapUV = TransformLightmapUV(i.lightmapUV);
    #endif

    // finish
    #ifdef REIGN_GetVertexOutput_OVERRIDE_LOCAL_POS
    o.positionCS = GetVertexOutput_OverrideLocalPos(i, i.positionOS);
    #else
    o.positionCS = TransformWorldToHClip(TransformObjectToWorld(i.positionOS));
    #endif
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
        #elif defined(_COLOR_ALBEDO)
        o.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
        #else
        o.color = 1.0;
        #endif
    #else
        o.color = GetMaterialProperties_Override_Color(i);
    #endif
    
    // lightmap
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_lightmap
        #if defined(LIGHTMAP_ON)
        o.color.rgb *= SampleLightmap(i.lightmapUV).rgb;// maintain alpha
        #endif
    #else
        o.color *= GetMaterialProperties_Override_Lightmap(i);
    #endif

    // custom outs
    #ifdef REIGN_frag_CUSTOM_OUTS
    frag_CustomColors(i, o);
    #endif
    
    return o;
}
#endif

#endif