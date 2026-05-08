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
    o.uv = TRANSFORM_TEX(i.uv, _BaseMap);

    // surface
    #ifndef REIGN_GetVertexOutput_OVERRIDE_surfaceMatrix
    o.surfaceMatrix = float3x3
    (
        normalize(i.tangent),
        normalize(cross(i.tangent, i.normal)),
        normalize(i.normal)
    );
    o.surfaceMatrix = mul(o.surfaceMatrix, unity_WorldToObject);
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
    materialParams.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

    // emissive color
    //materialParams.emissive = 0;// TODO

    // get normal
    #ifndef REIGN_GetMaterialProperties_OVERRIDE_normal
    materialParams.normal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv);
    materialParams.normal.xy -= .5;
    materialParams.normal = normalize(mul(materialParams.normal, i.surfaceMatrix));
    #else
    materialParams.normal = GetMaterialProperties_Override_Normal(i);
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
    //float2 posUV = i.positionCS.xy / compositingSize.xy;

    // get eye direction
    real3 eyeDir = normalize(pos - _WorldSpaceCameraPos);

    // compute shade
    o.color = Process_DirectionalLights(materialParams, eyeDir);
    #ifndef REIGN_POINT_LIGHTS_DISABLE
    o.color += Process_PointLights(materialParams, eyeDir, pos);
    #endif
    o.color += Process_AmbientLight(materialParams);
    //o.color += materialParams.emissive;

    // custom outs
    #ifdef REIGN_frag_CUSTOM_OUTS
    frag_CustomColors(i, o);
    #endif
    //o.color = real4(materialParams.normal, 1);
    return o;
}
#endif

#endif