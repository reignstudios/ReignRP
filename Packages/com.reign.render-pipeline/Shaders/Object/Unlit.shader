Shader "ReignRP/Unlit"
{
    Properties
    {
        // Clip Options
        [Toggle(ENABLE_ALPHACLIP)] _ENABLE_ALPHACLIP ("Enable Alpha Clip", Float) = 0
        _AlphaClip ("Alpha Clip", Range(0.0, 1.0)) = 0.1
        [Space(10)]

        // normal
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2// Backface culling
        _UVScaleOffset("UV Scale Offset", Vector) = (1,1,0,0)

        [KeywordEnum(Color, Texture, Both)] _COLOR ("Color Mode", Float) = 0
        [MainColor] [HDR] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

        [Toggle(ENABLE_SHADOW_RECEIVE)] _ENABLE_SHADOW_RECEIVE ("Enable Shadow Receive", Float) = 0
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
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ REIGN_SHADOW_HARD REIGN_SHADOW_SOFT_BLUR

            #pragma shader_feature _COLOR_COLOR _COLOR_TEXTURE _COLOR_BOTH
            #pragma shader_feature _ ENABLE_ALPHACLIP
            #pragma shader_feature _ ENABLE_SHADOW_RECEIVE

            #include "Unlit_Pre.hlsl"
            #include "Unlit_Post.hlsl"
            ENDHLSL
        }
    }
}
