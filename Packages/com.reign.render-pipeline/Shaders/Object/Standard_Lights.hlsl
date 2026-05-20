#ifndef REIGN_LIGHTS
#define REIGN_LIGHTS

// =====================================
// Materials
// =====================================
struct MaterialParams
{
	real4 color;
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    real4 specular;// X = Intensity, Y = Roughness, Z = Metallic, W = Fresnel
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
    return real4(DecodeHDREnvironment(encoded, unity_SpecCube0_HDR), 1) * unity_AmbientSky.x;
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

// === Directional ===
#ifndef REIGN_ProcessDiffuse_DirectionalLight_OVERRIDE
inline real4 ProcessDiffuse_DirectionalLight(MaterialParams materialParams, real3 direction, real4 lightColor)
{
	real d = saturate(dot(-direction, materialParams.normal));
	return materialParams.color * lightColor * d;
}
#endif

#ifndef REIGN_ProcessSpecular_DirectionalLight_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
inline real4 ProcessSpacular_DirectionalLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real4 lightColor)
{
	real d = saturate(dot(-direction, eyeRef));
	d = pow(d, (200.0 * materialParams.specular.y) + 1.0);
    d *= lightColor;
    return lerp(d, materialParams.color * d, materialParams.specular.z);
}
#endif
#endif

#ifndef REIGN_Process_DirectionalLights_OVERRIDE
real4 Process_DirectionalLights(MaterialParams materialParams, real3 eyeDir, real3 eyeRef)
{
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        #ifdef LIGHTMAP_ON
            real4 d = 0.0;
            [branch] if (directionalLight_Direction.w <= .5) d = ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
        #else
            real4 d = 0;//ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
        #endif
        real4 s = ProcessSpecular_DirectionalLight(materialParams, eyeDir, eyeRef, directionalLight_Direction.xyz, directionalLight_Color);
        return d + s;
    #else
        #ifdef LIGHTMAP_ON
        [branch] if (directionalLight_Direction.w >= .5) return 0.0;
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
    return materialParams.color * lightColor * d;
}
#endif

#ifndef REIGN_ProcessSpecular_PointLight_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
inline real4 ProcessSpecular_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real4 lightColor)
{
    real d = saturate(dot(-direction, eyeRef));
    d = pow(d, (200.0 * materialParams.specular.y) + 1.0);
    d *= lightColor;
    return lerp(d, materialParams.color * d, materialParams.specular.z);
}
#endif
#endif

#ifndef REIGN_Process_PointLight_OVERRIDE
inline real4 Process_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real distance, real4 lightColor, real4 flags, real lightRadius)
{
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        #ifdef LIGHTMAP_ON
            real4 d = 0.0;
            [branch] if (flags.w <= .5) d = ProcessDiffuse_PointLight(materialParams, direction, lightColor);
        #else
            real4 d = ProcessDiffuse_PointLight(materialParams, direction, lightColor);
        #endif
    
        real4 s = ProcessSpecular_PointLight(materialParams, eyeDir, eyeRef, direction, lightColor);
        distance = 1.0 - saturate(distance / lightRadius);
        return (d * pow(distance, 2.0)) + (s * distance);
    #else
        #ifdef LIGHTMAP_ON
        [branch] if (flags.w >= .5) return 0.0;
        #endif
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
        real3 vec = pos - lightPos.xyz;
        real distance = length(vec);
        [branch] if (distance >= lightPos.w) continue;

        light += Process_PointLight(materialParams, eyeDir, eyeRef, normalize(vec), distance, pointLight_Colors[i], pointLight_Flags[i], lightPos.w);
    }
    return light;
}
#endif

#endif
