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
    #else
        materialParams.normal = GetMaterialProperties_Override_Color(i);
    #endif
    
    // specular
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_specular
        #if defined(_SPECULAR_SLIDERS)
        materialParams.specular = real4(_SpecularIntensity, _SpecularRoughness, _SpecularMetallic, _SpecularFresnel);
        materialParams.specularInv = 1.0 - materialParams.specular;
        #elif defined(_SPECULAR_MAP)
        real4 m = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, i.uv);
        materialParams.specular = m * real4(_SpecularIntensity, _SpecularRoughness, _SpecularMetallic, _SpecularFresnel);
        materialParams.specularInv = 1.0 - materialParams.specular;
        #endif
    #else
        materialParams.normal = GetMaterialProperties_Override_Specular(i);
    #endif

    // normal
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_normal
        #if defined(ENABLE_NORMAL)
        materialParams.normal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv);
        materialParams.normal.xy -= .5;
        materialParams.normal = normalize(mul(materialParams.normal, i.surfaceMatrix));
        materialParams.normalObj = i.surfaceMatrix[2];
        #else
        materialParams.normal = materialParams.normalObj = normalize(i.normal);
        #endif
    #else
        GetMaterialProperties_Override_Normal(i, materialParams.normal, materialParams.normalObj);
    #endif
    
    // ao
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_ao
        #if defined(ENABLE_OCCLUSION)
        materialParams.ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, i.uv);
        #endif
    #else
        materialParams.normal = GetMaterialProperties_Override_AO(i);
    #endif
    
    // emissive
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_emissive
        #if defined(ENABLE_EMISSION)
        materialParams.emissive = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv) * _EmissionColor;
        #endif
    #else
        materialParams.normal = GetMaterialProperties_Override_Emissive(i);
    #endif
    
    // lightmap
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_lightmap
        #if defined(LIGHTMAP_ON)
        materialParams.lightmap = SampleLightmap(i.lightmapUV);
        #endif
    #else
        materialParams.normal = GetMaterialProperties_Override_Lightmap(i);
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
    real4 lightDiffuse = 0.0;
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    real4 lightSpecular = 0.0;
    #endif
    
    #ifndef REIGN_DIRECTIONAL_LIGHTS_DISABLE
        #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        Process_DirectionalLights(materialParams, eyeDir, eyeRef, lightDiffuse, lightSpecular);
        #else
        Process_DirectionalLights(materialParams, eyeDir, eyeRef, lightDiffuse);
        #endif
    #endif
    
    #ifndef REIGN_POINT_LIGHTS_DISABLE
        #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        Process_PointLights(materialParams, eyeDir, eyeRef, pos, lightDiffuse, lightSpecular);
        #else
        Process_PointLights(materialParams, eyeDir, eyeRef, pos, lightDiffuse);
        #endif
    #endif
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    Process_Environment(materialParams, eyeDir, eyeRef, lightDiffuse, lightSpecular);
    #else
    Process_Environment(materialParams, eyeDir, lightDiffuse);
    #endif
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    Process_LightMaterial(materialParams, eyeDir, eyeRef, lightDiffuse, lightSpecular);
    #else
    Process_LightMaterial(materialParams, lightDiffuse);
    #endif
    
    #ifdef ENABLE_OCCLUSION
        lightDiffuse *= materialParams.ao;
        #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        lightSpecular *= materialParams.ao;
        #endif
    #endif
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    o.color = lightDiffuse + lightSpecular;
    #else
    o.color = lightDiffuse;
    #endif
    
    #ifdef ENABLE_EMISSION
    o.color += materialParams.emissive;
    #endif

    // custom outs
    #ifdef REIGN_frag_CUSTOM_OUTS
    frag_CustomColors(i, o);
    #endif
    
    return o;
}
#endif

#endif