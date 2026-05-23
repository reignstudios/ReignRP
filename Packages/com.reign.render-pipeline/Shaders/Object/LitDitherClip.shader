Shader "ReignRP/Lit DitherClip"
{
    Properties
    {
        [KeywordEnum(Dither, Pattern, Random)] _CLIP_MODE ("Clip Mode", Float) = 0
        [Space(10)]

        // main
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2// Backface culling
        _UVScaleOffset("UV Scale Offset", Vector) = (1,1,0,0)

        [KeywordEnum(Color, Albedo, Both)] _COLOR ("Color Mode", Float) = 0
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

        [KeywordEnum(Off, Sliders, Map)] _SPECULAR ("Specular Mode", Float) = 1
        [Toggle(ENABLE_SPECULAR_HQ)] _ENABLE_SPECULAR_HQ ("Specular HQ", Float) = 0
        _SpecularIntensity("Specular Intensity", Range(0.0, 1.0)) = 1.0
        _SpecularRoughness("Specular Roughness", Range(0.0, 1.0)) = 1.0
        _SpecularMetallic("Specular Metallic", Range(0.0, 1.0)) = 1.0
        _SpecularFresnel("Specular Fresnel", Range(0.0, 1.0)) = 1.0
        _SpecularMap("Specular", 2D) = "white" {}

        [Toggle(ENABLE_NORMAL)] _ENABLE_NORMAL ("Enable Normal", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}

        [Toggle(ENABLE_OCCLUSION)] _ENABLE_OCCLUSION ("Enable Occlusion", Float) = 0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [Toggle(ENABLE_EMISSION)] _ENABLE_EMISSION ("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "LightMode" = "Reign_Opaque" "Queue" = "Geometry" "RenderType" = "Opaque" }
        
        Cull [_Cull]
        ZTest LEqual
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ REIGN_DIRECTIONAL_LIGHTS_DISABLE
            #pragma multi_compile _ REIGN_POINT_LIGHTS_DISABLE
            #pragma multi_compile _ REIGN_AMBIENT_MODE_DISABLE REIGN_AMBIENT_MODE_SKYBOX REIGN_AMBIENT_MODE_GRADIENT REIGN_AMBIENT_MODE_COLOR

            #pragma shader_feature _COLOR_COLOR _COLOR_ALBEDO _COLOR_BOTH
            #pragma shader_feature _ _SPECULAR_OFF _SPECULAR_SLIDERS _SPECULAR_MAP
            #pragma shader_feature _ ENABLE_SPECULAR_HQ
            #pragma shader_feature _ ENABLE_NORMAL
            #pragma shader_feature _ ENABLE_OCCLUSION
            #pragma shader_feature _ ENABLE_EMISSION

            #pragma shader_feature _ _CLIP_MODE_DITHER _CLIP_MODE_PATTERN _CLIP_MODE_RANDOM

            #define SS_UV
            #define ENABLE_SS_DITHERALPHA
            #include "Lit_Pre.hlsl"
            #include "Lit_Post.hlsl"
            ENDHLSL
        }
    }
}
