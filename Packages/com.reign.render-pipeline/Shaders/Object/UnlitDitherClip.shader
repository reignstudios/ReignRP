Shader "ReignRP/Unlit DitherClip"
{
    Properties
    {
        [KeywordEnum(Dither, Pattern, Random)] _CLIP_MODE ("Clip Mode", Float) = 0
        [Space(10)]

        // main
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2// Backface culling
        _UVScaleOffset("UV Scale Offset", Vector) = (1,1,0,0)

        [KeywordEnum(Color, Texture, Both)] _COLOR ("Color Mode", Float) = 0
        [MainColor] [HDR] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
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

            #pragma shader_feature _COLOR_COLOR _COLOR_TEXTURE _COLOR_BOTH
            #pragma shader_feature _ _CLIP_MODE_DITHER _CLIP_MODE_PATTERN _CLIP_MODE_RANDOM

            #define SS_UV
            #define ENABLE_SS_DITHERALPHA
            #include "Unlit_Pre.hlsl"
            #include "Unlit_Post.hlsl"
            ENDHLSL
        }
    }
}
