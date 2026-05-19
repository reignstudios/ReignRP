#ifndef REIGN_POST
#define REIGN_POST

// =====================================
// Base
// =====================================
#ifndef REIGN_GetVertexOutput_OVERRIDE
inline void GetVertexOutput(VS_IN i, inout VS_OUT o)
{
    // get local position
    o.pos = i.positionOS;

    // custom local pos
    #ifdef REIGN_GetVertexOutput_OVERRIDE_LOCAL_POS
    GetVertexOutput_OverrideLocalPos(i, o.pos);
    #endif

    // get world position
    o.pos = TransformObjectToWorld(o.pos);

    // uv
    o.uv = (i.uv * _UVScaleOffset.xy) + _UVScaleOffset.zw;
    
    #ifdef LIGHTMAP_ON
    o.lightmapUV = TransformLightmapUV(i.lightmapUV);
    #endif

    // surface
    #ifndef REIGN_GetVertexOutput_OVERRIDE_surfaceMatrix
    #if defined(ENABLE_NORMAL)
    o.surfaceMatrix = float3x3
    (
        normalize(i.tangent),
        normalize(cross(i.tangent, i.normal)),
        normalize(i.normal)
    );
    o.surfaceMatrix = mul(o.surfaceMatrix, unity_WorldToObject);
    #else
    o.normal = mul(i.normal, unity_WorldToObject);
    #endif
    #else
    o.surfaceMatrix = GetVertexOutput_OVERRIDE_surfaceMatrix(i, o);
    #endif

    // custom world pos
    #ifdef REIGN_GetVertexOutput_OVERRIDE_WORLD_POS
    GetVertexOutput_OverrideWorldPos(i, o, o.pos);
    #endif

    // finish
    o.positionCS = TransformWorldToHClip(o.pos);
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

#ifndef REIGN_GetMaterialProperties_OVERRIDE
inline MaterialParams GetMaterialProperties(VS_OUT i)
{
    MaterialParams materialParams;

    // color
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_color
        #if defined(_COLOR_BOTH)
        materialParams.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
        #elif defined(_COLOR_COLOR)
        materialParams.color = _BaseColor;
        #elif defined(_COLOR_ALBEDO)
        materialParams.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
        #else
        materialParams.color = 1.0;
        #endif
    #endif
    
    // metallic
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_metallic
        #if defined(_METALLIC_SLIDERS)
        materialParams.metallic = real4(_Metallic, _MetallicGloss, 1.0 - _Metallic, 1.0 - _MetallicGloss);
        #elif defined(_METALLIC_MAP)
        real4 m = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, i.uv);
        materialParams.metallic.xy = real2(m.x * _Metallic, m.y * _MetallicGloss);
        materialParams.metallic.zw = real2(1.0 - materialParams.metallic.x, 1.0 - materialParams.metallic.y);
        #endif
    #endif

    // normal
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_normal
        #if defined(ENABLE_NORMAL)
        materialParams.normal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv);
        materialParams.normal.xy -= .5;
        materialParams.normal = normalize(mul(materialParams.normal, i.surfaceMatrix));
        #else
        materialParams.normal = normalize(i.normal);
        #endif
        #else
        materialParams.normal = GetMaterialProperties_Override_Normal(i);
    #endif
    
    // ao
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_ao
        #if defined(ENABLE_OCCLUSION)
        materialParams.ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, i.uv);
        #endif
    #endif
    
    // emissive
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_emissive
        #if defined(ENABLE_EMISSION)
        materialParams.emissive = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv) * _EmissionColor;
        #endif
    #endif

    return materialParams;
}
#endif

#ifndef REIGN_frag_OVERRIDE
PS_OUT frag(VS_OUT i)
{
    // XR
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    // material params
    MaterialParams materialParams = GetMaterialProperties(i);
    PS_OUT o;
    
    // get position
    float3 pos = i.pos;
    //float2 posUV = i.positionCS.xy / targetSize.xy;

    // get eye direction
    real3 eyeDir = normalize(pos - _WorldSpaceCameraPos);
    real3 eyeRef = reflect(eyeDir, materialParams.normal);

    // compute shade
    o.color = Process_DirectionalLights(materialParams, eyeDir, eyeRef);
    
    #if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
    o.color += SampleEnvironmentMaterial(materialParams, eyeDir, eyeRef);
    #endif
    
    #ifndef REIGN_POINT_LIGHTS_DISABLE
    o.color += Process_PointLights(materialParams, eyeDir, eyeRef, pos);
    #endif
    
    #ifdef ENABLE_EMISSION
    o.color += materialParams.emissive;
    #endif
    
    #ifdef LIGHTMAP_ON
    real4 l = SampleLightmap(i.lightmapUV);
    o.color.rgb += l.rgb * materialParams.color.rgb;
    #endif

    // custom outs
    #ifdef REIGN_frag_CUSTOM_OUTS
    frag_CustomColors(i, o);
    #endif
    
    return o;
}
#endif

#endif