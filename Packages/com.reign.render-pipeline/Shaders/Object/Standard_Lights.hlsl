#ifndef REIGN_LIGHTS
#define REIGN_LIGHTS

// =====================================
// Materials
// =====================================
struct MaterialParams
{
	real4 color;
    
    #if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
    real4 metallic;// X = Metalic, Y = Gloss, Z = MetalicInv, W = GlossInv
    #endif
    
	real3 normal;
    
    #if defined(ENABLE_OCCLUSION)
    real4 ao;
    #endif
    
    #if defined(ENABLE_EMISSION)
	real4 emissive;
    #endif
    
    #if defined(LIGHTMAP_ON)
    real4 lightmap;
    #endif
};

// =====================================
// Lights
// =====================================
float4 directionalLight_Direction;
float4 directionalLight_Color;

#define MAX_POINT_LIGHT_COUNT 4
float4 pointLight_Positions[MAX_POINT_LIGHT_COUNT];
float4 pointLight_Colors[MAX_POINT_LIGHT_COUNT];
float4 pointLight_Flags[MAX_POINT_LIGHT_COUNT];// X = lightmap-mode=1
float pointLight_Count;

// === PBR Util ===
real4 SampleEnvironment(real3 direction, real roughness)
{
    #if defined(REIGN_AMBIENT_MODE_SKYBOX)
    real4 encoded = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, direction, PerceptualRoughnessToMipmapLevel(roughness));
    encoded = real4(DecodeHDREnvironment(encoded, unity_SpecCube0_HDR), 1) * unity_AmbientSky.x;
    return encoded;//pow(encoded * 2.0, 1.0);
    #elif defined(REIGN_AMBIENT_MODE_GRADIENT)
    return lerp(lerp(unity_AmbientEquator, unity_AmbientGround, saturate(-direction.y)), unity_AmbientSky, saturate(direction.y));
    #elif defined(REIGN_AMBIENT_MODE_COLOR)
    return unity_AmbientSky;
    #else
    return 0;
    #endif
    
    // TODO: light-probe
    /*real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;
    return real4(SampleSH9(SHCoefficients, materialParams.normal), 0.0);*/
}

#if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
real4 SampleEnvironmentMaterial(MaterialParams materialParams, real3 eyeDir, real3 eyeRef)
{
    real4 e = SampleEnvironment(eyeRef, materialParams.metallic.w);

    #ifdef ENABLE_METALLIC_PBR
    real f = saturate(dot(-eyeDir, materialParams.normal));// slope
    real4 metallic = lerp(e * materialParams.color * (1.0 - saturate(f - .25)), e * materialParams.color, materialParams.metallic.x);// metallic lerp
    e = lerp(e, metallic, pow(saturate(f * 2.0), .5));// fresnel lerp
    #else
    real4 f = e * materialParams.color;
    e = lerp(e, f, materialParams.metallic.x);
    #endif

    #ifdef ENABLE_OCCLUSION
    return e * materialParams.ao;
    #else
    return e;
    #endif
}
#endif

// === Directional ===
#ifndef REIGN_ProcessDiffuse_DirectionalLight_OVERRIDE
inline real4 ProcessDiffuse_DirectionalLight(MaterialParams materialParams, real3 direction, real4 lightColor)
{
	real d = saturate(dot(-direction, materialParams.normal));
    #ifdef ENABLE_OCCLUSION
    d *= materialParams.ao;
    #endif
	return materialParams.color * lightColor * d;
}
#endif

#ifndef REIGN_ProcessMetallic_DirectionalLight_OVERRIDE
#if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
inline real4 ProcessMetallic_DirectionalLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real4 lightColor)
{
	real d = saturate(dot(-direction, eyeRef));
	d = pow(d, (200.0 * materialParams.metallic.y) + 1.0) * materialParams.metallic.y;
    d *= lightColor;
    return lerp(d, materialParams.color * d, materialParams.metallic.x);
}
#endif
#endif

#ifndef REIGN_Process_DirectionalLights_OVERRIDE
real4 Process_DirectionalLights(MaterialParams materialParams, real3 eyeDir, real3 eyeRef)
{
    #if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
    real4 d;
    #ifdef LIGHTMAP_ON
    [branch] if (directionalLight_Direction.w >= .5) d = materialParams.lightmap * materialParams.color;
    else // use next line
    #endif
    d = ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
    
    real4 s = ProcessMetallic_DirectionalLight(materialParams, eyeDir, eyeRef, directionalLight_Direction.xyz, directionalLight_Color);
    return d + s;
    #else
    #ifdef LIGHTMAP_ON
    [branch] if (directionalLight_Direction.w <= .5) return materialParams.lightmap * materialParams.color;
    else // use next line
    #endif
    return ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
    #endif
}
#endif

// === Point ===
#ifndef REIGN_ProcessDiffuse_PointLight_OVERRIDE
inline real4 ProcessDiffuse_PointLight(MaterialParams materialParams, real3 direction, real4 lightColor)
{
    real d = saturate(dot(-direction, materialParams.normal));
    #ifdef ENABLE_OCCLUSION
    d *= materialParams.ao;
    #endif
    return materialParams.color * lightColor * d;
}
#endif

#ifndef REIGN_ProcessMetallic_PointLight_OVERRIDE
#if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
inline real4 ProcessMetallic_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real4 lightColor)
{
    real d = saturate(dot(-direction, eyeRef));
    d = pow(d, (200.0 * materialParams.metallic.y) + 1.0) * materialParams.metallic.y;
    d *= lightColor;
    return lerp(d, materialParams.color * d, materialParams.metallic.x);
}
#endif
#endif

#ifndef REIGN_Process_PointLight_OVERRIDE
inline real4 Process_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real distance, real4 lightColor, real4 flags, real lightRadius)
{
    #if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
    real4 d;
    #ifdef LIGHTMAP_ON
    [branch] if (flags.w >= .5) d = materialParams.lightmap * materialParams.color;
    else // use next line
    #endif
    d = ProcessDiffuse_PointLight(materialParams, direction, lightColor);
    
    real4 s = ProcessMetallic_PointLight(materialParams, eyeDir, eyeRef, direction, lightColor);
    distance = 1.0 - saturate(distance / lightRadius);
    return (d * pow(distance, 2.0)) + (s * distance);
    #else
    real4 d;
    #ifdef LIGHTMAP_ON
    [branch] if (flags.w >= .5) d = materialParams.lightmap * materialParams.color;
    else // use next line
    #endif
    d = ProcessDiffuse_PointLight(materialParams, direction, lightColor);
    
    distance = 1.0 - saturate(distance / lightRadius);
    return d * pow(distance, 2.0);
    #endif
}
#endif

#ifndef REIGN_Process_PointLights_OVERRIDE
real4 Process_PointLights(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, float3 pos)
{
    real4 light = real4(0, 0, 0, 0);
    [loop] for (int i = 0; i < pointLight_Count; ++i)
    {
        float4 lightPos = pointLight_Positions[i];
        real3 vec = pos - lightPos.xyz;
        real distance = length(vec);
        [branch] if (distance >= lightPos.w) continue;

        light += Process_PointLight(materialParams, eyeDir, eyeRef, normalize(vec), distance, pointLight_Colors[i], pointLight_Flags[i], lightPos.w);
    }
    return light;
}
#endif

#endif
