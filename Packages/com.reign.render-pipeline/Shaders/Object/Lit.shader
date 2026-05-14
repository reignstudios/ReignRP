Shader "ReignRP/Lit"
{
    Properties
    {
        _UVScaleOffset("UV Scale Offset", Vector) = (1,1,0,0)

        [KeywordEnum(Color, Albedo, Both)] _COLOR ("Color Mode", Float) = 0
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

        [KeywordEnum(Off, Sliders, Map)] _METALLIC ("Metallic Mode", Float) = 0
        [Toggle(ENABLE_METALLIC_FRESNEL)] _ENABLE_METALLIC_FRESNEL ("Enable HQ Fresnel", Float) = 0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGloss("Metallic Gloss", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

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
        Cull Back
        ZTest Less
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ REIGN_POINT_LIGHTS_DISABLE
            #pragma multi_compile _ REIGN_AMBIENT_MODE_DISABLE REIGN_AMBIENT_MODE_SKYBOX REIGN_AMBIENT_MODE_GRADIENT REIGN_AMBIENT_MODE_COLOR

            #pragma shader_feature _COLOR_COLOR _COLOR_ALBEDO _COLOR_BOTH
            #pragma shader_feature _ _METALLIC_OFF _METALLIC_SLIDERS _METALLIC_MAP
            #pragma shader_feature _ ENABLE_METALLIC_FRESNEL
            #pragma shader_feature _ ENABLE_NORMAL
            #pragma shader_feature _ ENABLE_OCCLUSION
            #pragma shader_feature _ ENABLE_EMISSION

            #include "Standard_Pre.hlsl"
            #include "Standard_Post.hlsl"
            ENDHLSL
        }
    }
}
