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
    
    #ifdef ENABLE_EMISSION
	real4 emissive;
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
float pointLight_Count;

// === PBR Util ===
real4 SampleEnvironment(real3 direction, real roughness)
{
    real4 encoded = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, direction, PerceptualRoughnessToMipmapLevel(roughness));
    return real4(DecodeHDREnvironment(encoded, unity_SpecCube0_HDR), 1);
}

#if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
real4 SampleEnvironmentMaterial(MaterialParams materialParams, real3 eyeDir, real3 eyeRef)
{
    real4 m = SampleEnvironment(eyeRef, materialParams.metallic.w);
    #ifdef ENABLE_METALLIC_FRESNEL
    return lerp(m, m * materialParams.color, saturate(materialParams.metallic.x * pow(1.0 - saturate(dot(eyeDir, eyeRef)), .5)));
    #else
    return lerp(m, m * materialParams.color, materialParams.metallic.x);
    #endif
}
#endif

// === Directional ===
#ifndef REIGN_ProcessDiffuse_DirectionalLight_OVERRIDE
inline real4 ProcessDiffuse_DirectionalLight(MaterialParams materialParams, real3 direction, real4 lightColor)
{
	real d = dot(-direction, materialParams.normal);
	return materialParams.color * lightColor * max(0.0, d);
}
#endif

#ifndef REIGN_ProcessMetallic_DirectionalLight_OVERRIDE
#if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
inline real4 ProcessMetallic_DirectionalLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real4 lightColor)
{
	real d = max(0.0, dot(-direction, eyeRef));
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
    real4 d = ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
    real4 s = ProcessMetallic_DirectionalLight(materialParams, eyeDir, eyeRef, directionalLight_Direction.xyz, directionalLight_Color);
    return d + s;
    #else
    return ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
    #endif
}
#endif

// === Point ===
#ifndef REIGN_ProcessDiffuse_PointLight_OVERRIDE
inline real4 ProcessDiffuse_PointLight(MaterialParams materialParams, real3 direction, real4 lightColor)
{
    real d = dot(-direction, materialParams.normal);
    return materialParams.color * lightColor * max(0.0, d);
}
#endif

#ifndef REIGN_ProcessMetallic_PointLight_OVERRIDE
#if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
inline real4 ProcessMetallic_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real4 lightColor)
{
    real d = max(0.0, dot(-direction, eyeRef));
    d = pow(d, (200.0 * materialParams.metallic.y) + 1.0) * materialParams.metallic.y;
    d *= lightColor;
    return lerp(d, materialParams.color * d, materialParams.metallic.x);
}
#endif
#endif

#ifndef REIGN_Process_PointLight_OVERRIDE
inline real4 Process_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real distance, real4 lightColor, real lightRadius)
{
    #if defined(_METALLIC_SLIDERS) || defined(_METALLIC_MAP)
    real4 d = ProcessDiffuse_PointLight(materialParams, direction, lightColor);
    real4 s = ProcessMetallic_PointLight(materialParams, eyeDir, eyeRef, direction, lightColor);
    distance = 1.0 - saturate(distance / lightRadius);
    return (d * pow(distance, 2.0)) + (s * distance);
    #else
    real4 d = ProcessDiffuse_PointLight(materialParams, direction, lightColor);
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
        real4 lightColor = pointLight_Colors[i];

        real3 vec = pos - lightPos.xyz;
        real distance = length(vec);
        [branch] if (distance >= lightPos.w) continue;

        light += Process_PointLight(materialParams, eyeDir, eyeRef, normalize(vec), distance, lightColor, lightPos.w);
    }
    return light;
}
#endif

// === Ambient ===
#ifndef REIGN_Process_AmbientLight_OVERRIDE
inline real4 Process_AmbientLight(MaterialParams materialParams)
{
    #if defined(REIGN_AMBIENT_MODE_SKYBOX)
    real4 pbr = SampleEnvironment(materialParams.normal, .9) * unity_AmbientSky.x;
    return materialParams.color * pbr;
    #elif defined(REIGN_AMBIENT_MODE_GRADIENT)
    return materialParams.color * lerp(lerp(unity_AmbientEquator, unity_AmbientGround, saturate(-materialParams.normal.y)), unity_AmbientSky, saturate(materialParams.normal.y));
    #elif defined(REIGN_AMBIENT_MODE_COLOR)
    return materialParams.color * unity_AmbientSky;
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
#endif

#endif
