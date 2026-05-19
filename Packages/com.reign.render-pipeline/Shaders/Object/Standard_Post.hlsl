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
        materialParams.metallic = real4(_Metallic, _MetallicGloss, _MetallicReflection, 0.0);
        #elif defined(_METALLIC_MAP)
        real4 m = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, i.uv);
        materialParams.metallic = real4(m.x * _Metallic, m.y * _MetallicGloss, m.z * _MetallicReflection, 0.0);
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
    
    // lightmap
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_lightmap
        #if defined(LIGHTMAP_ON)
        materialParams.lightmap = SampleLightmap(i.lightmapUV);
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
    real4 light;
    #ifndef REIGN_DIRECTIONAL_LIGHTS_DISABLE
    light = Process_DirectionalLights(materialParams, eyeDir, eyeRef);
    #else
    light = 0.0;
    #endif
    
    /*#if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
    o.color += SampleEnvironmentMaterial(materialParams, eyeDir, eyeRef);
    #endif*/
    
    #ifndef REIGN_POINT_LIGHTS_DISABLE
    light += Process_PointLights(materialParams, eyeDir, eyeRef, pos);
    #endif
    
    #if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
        real4 e = SampleEnvironment(eyeRef, 1.0 - materialParams.metallic.y) * materialParams.metallic.z;
        #if defined(LIGHTMAP_ON)
        light += e + materialParams.lightmap;
        #else
        light += e + SampleEnvironment(eyeRef, .9);
        #endif
    
        #ifdef ENABLE_METALLIC_PBR
            real f = saturate(dot(-eyeDir, materialParams.normal));// slope
            real4 metallic = lerp(light * materialParams.color * (1.0 - saturate(f - .25)), light * materialParams.color, materialParams.metallic.x);// metallic lerp
            light = lerp(light, metallic, pow(saturate(f * 2.0), .5));// fresnel lerp
            light = lerp(metallic, light, materialParams.metallic.z);
        #else
            light = lerp((light * materialParams.color + e) * .5, light * materialParams.color, materialParams.metallic.x);
        #endif
    
        #if defined(LIGHTMAP_ON)
        //light = dot(materialParams.lightmap, real4(.3333, .3333, .3333, 0.0));// * 4.0;
        //light *= materialParams.lightmap * 4.0;
        #endif
    #endif
    
    #ifdef ENABLE_OCCLUSION
    light *= materialParams.ao;
    #endif
    
    #ifdef ENABLE_EMISSION
    light += materialParams.emissive;
    #endif
    
    o.color = light;

    // custom outs
    #ifdef REIGN_frag_CUSTOM_OUTS
    frag_CustomColors(i, o);
    #endif
    
    return o;
}
#endif

#endif