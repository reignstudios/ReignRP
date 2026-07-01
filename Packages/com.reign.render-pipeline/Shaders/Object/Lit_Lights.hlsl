#ifndef REIGN_LIGHTS
#define REIGN_LIGHTS

#include "../_Shared/Reign_LightingUtils.hlsl"

// =====================================
// Materials
// =====================================
struct MaterialParams
{
    #ifdef SS_UV
    float2 ssUV;
    #endif
    
    #ifdef ENABLE_SHADOWS
    float2 shadowUV;
    #endif
    
	real4 color;
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    real4 specular;// X = Intensity, Y = Roughness, Z = Metallic, W = Fresnel
    real4 specularInv;
    #endif
    
	real3 normal, normalObj;
    
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
float directionalLight_Bias;

#define MAX_POINT_LIGHT_COUNT 4
float4 pointLight_Positions[MAX_POINT_LIGHT_COUNT];
float4 pointLight_Colors[MAX_POINT_LIGHT_COUNT];
float4 pointLight_Flags[MAX_POINT_LIGHT_COUNT];// X = lightmap-mode=1
float pointLight_Count;

// === Directional ===
#ifndef REIGN_ProcessDiffuse_DirectionalLight_OVERRIDE
inline real4 ProcessDiffuse_DirectionalLight(MaterialParams materialParams, real3 direction, real4 lightColor)
{
	return saturate(dot(-direction, materialParams.normal)) * lightColor;
}
#endif

#ifndef REIGN_ProcessSpecular_DirectionalLight_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
inline real4 ProcessSpecular_DirectionalLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real4 lightColor)
{
	real d = saturate(dot(-direction, eyeRef));
	return pow(d, (50.0 * materialParams.specularInv.y) + 1.0) * lightColor;
}
#endif
#endif

#ifndef REIGN_Process_DirectionalLights_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
void Process_DirectionalLights(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, inout real4 lightDiffuse, inout real4 lightSpecular)
#else
void Process_DirectionalLights(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, inout real4 lightDiffuse)
#endif
{
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        #ifdef LIGHTMAP_ON
        [branch] if (directionalLight_Direction.w <= .5) lightDiffuse += ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
        #endif
        lightSpecular = ProcessSpecular_DirectionalLight(materialParams, eyeDir, eyeRef, directionalLight_Direction.xyz, directionalLight_Color);
    #else
        #ifdef LIGHTMAP_ON
        [branch] if (directionalLight_Direction.w <= .5) lightDiffuse += ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
        #else
        lightDiffuse += ProcessDiffuse_DirectionalLight(materialParams, directionalLight_Direction.xyz, directionalLight_Color);
        #endif
    #endif
}
#endif

// === Point ===
#ifndef REIGN_ProcessDiffuse_PointLight_OVERRIDE
inline real4 ProcessDiffuse_PointLight(MaterialParams materialParams, real3 direction, real rangeNormalizedInv, real4 lightColor)
{
    return saturate(dot(-direction, materialParams.normal)) * lightColor * pow(rangeNormalizedInv, 2.0);
}
#endif

#ifndef REIGN_ProcessSpecular_PointLight_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
inline real4 ProcessSpecular_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real rangeNormalizedInv, real4 lightColor)
{
    real d = saturate(dot(-direction, eyeRef));
    return pow(d, (50.0 * materialParams.specularInv.y) + 1.0) * lightColor * rangeNormalizedInv;
}
#endif
#endif

#ifndef REIGN_Process_PointLight_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
inline void Process_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real distance, real4 lightColor, real4 flags, real lightRadius, inout real4 lightDiffuse, inout real4 lightSpecular)
#else
inline void Process_PointLight(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, real3 direction, real distance, real4 lightColor, real4 flags, real lightRadius, inout real4 lightDiffuse)
#endif
{
    distance = 1.0 - saturate(distance / lightRadius);
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        #ifdef LIGHTMAP_ON
            [branch] if (flags.w <= .5)
            {
                lightDiffuse += ProcessDiffuse_PointLight(materialParams, direction, distance, lightColor);
            }
        #else
            lightDiffuse += ProcessDiffuse_PointLight(materialParams, direction, distance, lightColor);
        #endif
    
        lightSpecular += ProcessSpecular_PointLight(materialParams, eyeDir, eyeRef, direction, distance, lightColor);
    #else
        #ifdef LIGHTMAP_ON
        [branch] if (flags.w <= .5)
        {
            lightDiffuse += ProcessDiffuse_PointLight(materialParams, direction, distance, lightColor);
        }
        #else
        lightDiffuse += ProcessDiffuse_PointLight(materialParams, direction, distance, lightColor);
        #endif
    #endif
}
#endif

#ifndef REIGN_Process_PointLights_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
void Process_PointLights(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, float3 pos, inout real4 lightDiffuse, inout real4 lightSpecular)
#else
void Process_PointLights(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, float3 pos, inout real4 lightDiffuse)
#endif
{
    [loop] for (int i = 0; i < pointLight_Count; ++i)
    {
        float4 lightPos = pointLight_Positions[i];
        real3 vec = pos - lightPos.xyz;
        real distance = length(vec);
        [branch] if (distance >= lightPos.w) continue;

        #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
        Process_PointLight(materialParams, eyeDir, eyeRef, normalize(vec), distance, pointLight_Colors[i], pointLight_Flags[i], lightPos.w, lightDiffuse, lightSpecular);
        #else
        Process_PointLight(materialParams, eyeDir, eyeRef, normalize(vec), distance, pointLight_Colors[i], pointLight_Flags[i], lightPos.w, lightDiffuse);
        #endif
    }
}
#endif

// === Environment ===
inline real4 SampleEnvironment(real3 direction, real roughness)
{
    #if defined(REIGN_AMBIENT_MODE_SKYBOX)
    real4 encoded = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, direction, PerceptualRoughnessToMipmapLevel(roughness));
    return real4(DecodeHDREnvironment(encoded, unity_SpecCube0_HDR), 1) * unity_AmbientSky.x;
    #elif defined(REIGN_AMBIENT_MODE_GRADIENT)
    return lerp(lerp(unity_AmbientEquator, unity_AmbientGround, saturate(-direction.y)), unity_AmbientSky, saturate(direction.y));
    #elif defined(REIGN_AMBIENT_MODE_COLOR)
    return unity_AmbientSky;
    #else
    return 0.0;
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

#ifndef REIGN_Process_Environment_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
void Process_Environment(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, inout real4 lightDiffuse, inout real4 lightSpecular)
#else
inline void Process_Environment(MaterialParams materialParams, real3 eyeDir, inout real4 lightDiffuse)
#endif
{
    #if defined(LIGHTMAP_ON)
    lightDiffuse += materialParams.lightmap;
    #else
    lightDiffuse += SampleEnvironment(materialParams.normal, .9) * 3.0;
    #endif
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    lightSpecular += SampleEnvironment(eyeRef, materialParams.specular.y);
    #endif
}
#endif

// === Light Material ===
#ifndef REIGN_Process_LightMaterial_OVERRIDE
#if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
void Process_LightMaterial(MaterialParams materialParams, real3 eyeDir, real3 eyeRef, inout real4 lightDiffuse, inout real4 lightSpecular)
#else
inline void Process_LightMaterial(MaterialParams materialParams, inout real4 lightDiffuse)
#endif
{
    lightDiffuse *= materialParams.color;
    
    #if defined(_SPECULAR_SLIDERS) || defined(_SPECULAR_MAP)
    real4 l = lightSpecular;
    lightSpecular = lerp(l, l * materialParams.color, materialParams.specular.z) * materialParams.specular.x;
    
    #ifdef ENABLE_SPECULAR_HQ
    l = SampleEnvironment(eyeRef, 0.0);// sample more accurate fresnel reflection
    l.rgb *= saturate(dot(materialParams.normal, materialParams.normalObj));
    #endif
    
    real f = saturate(dot(-eyeDir, materialParams.normal));
    lightSpecular = lerp(lightSpecular, l, pow(1.0 - f, 4.0) * materialParams.specular.w);
    #endif
}
#endif

// === Shadows ===
#ifndef REIGN_SampleShadow_OVERRIDE
inline float SampleShadow(float2 shadowUV)
{
    #ifdef ENABLE_SHADOWS
    return SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, shadowUV).x;
    #else
    return 1.0;
    #endif
}
#endif

#ifndef REIGN_Process_Shadow_OVERRIDE
inline real4 Process_Shadow(float4 shadowCS, float2 shadowUV)
{
    #if defined(REIGN_SHADOW_HARD)
        float d = SampleShadow(shadowUV);
        return (shadowCS.z + directionalLight_Bias - d) < 0.0 ? 0.0 : 1.0;
    #elif defined(REIGN_SHADOW_SOFT_BLUR)
        real shadowMul = 0.0;
        [branch] if (shadowCS.z >= 0.0)
        {
            shadowCS.z += directionalLight_Bias;
            float d = SampleShadow(shadowUV);
            for (int x = 0; x != 8; ++x)// inner pass
            {
                float rot = (x * (1.0 / 8.0)) * 6.28;
                float2 rUV = float2(cos(rot), sin(rot)) * _ShadowTex_TexelSize.xy * 1.5;
                rUV += shadowUV;
                d = SampleShadow(rUV);
                if (shadowCS.z - d >= 0.0) shadowMul += 1.0;
            }

            for (int x = 0; x != 12; ++x)// outter pass
            {
                float rot = (x * (1.0 / 12.0)) * 6.28;
                //rot += rot2;
                float2 rUV = float2(cos(rot), sin(rot)) * _ShadowTex_TexelSize.xy * 2.5;
                rUV += shadowUV;
                d = SampleShadow(rUV);
                if (shadowCS.z - d >= 0.0) shadowMul += 1.0;
            }

            return lerp(shadowColor, 1.0, shadowMul / 21.0);
        }
        return 1.0;
    #else
        return 1.0;
    #endif
}
#endif

#endif
