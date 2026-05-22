Shader "ReignRP/Unlit Transparent"
{
    Properties
    {
        _UVScaleOffset("UV Scale Offset", Vector) = (1,1,0,0)

        [KeywordEnum(Color, Albedo, Both)] _COLOR ("Color Mode", Float) = 0
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

        // Blend Options
        [Toggle(ENABLE_ALPHACLIP)] _ENABLE_ALPHACLIP ("Enable Alpha Clip", Float) = 1
        _AlphaClip ("Alpha Clip", Range(0.0, 1.0)) = 0.1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5// SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10// OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4// 4 = LessEqual (default)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2// Backface culling
    }

    SubShader
    {
        Tags { "LightMode" = "Reign_Transparent" "Queue" = "Transparent" "RenderType" = "Transparent" }
        
        Cull [_Cull]
        ZWrite Off
        ZTest [_ZTest]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma shader_feature _COLOR_COLOR _COLOR_ALBEDO _COLOR_BOTH
            #pragma shader_feature _ ENABLE_ALPHACLIP

            #include "Unlit_Pre.hlsl"
            #include "Unlit_Post.hlsl"
            ENDHLSL
        }
    }
}
