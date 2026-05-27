#include "../_Shared/Common.hlsl"

struct appdata
{
    float2 uv : TEXCOORD0;
    float3 positionOS : POSITION;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;
};

Texture2DMS<float4, MSAA_XX> _BlitTex;
float4 _BlitTex_TexelSize;

float4 srcRect, dstRect;
float srcMipLvl;

v2f vert (appdata v)
{
    v2f o;
    o.positionCS = float4((v.positionOS.xy * dstRect.zw) + dstRect.xy, .5, 1);
    o.uv = (v.uv * srcRect.zw) + srcRect.xy;
    return o;
}

struct PSOUT
{
    float4 color : SV_Target0;
};

PSOUT frag(v2f i)
{
    PSOUT o;
    int2 uv = i.uv * _BlitTex_TexelSize.zw;
    #if MSAA_XX == 2
    o.color = _BlitTex.Load(uv, 0);
    o.color += _BlitTex.Load(uv, 1);
    o.color *= 1.0 / 2.0;
    #elif MSAA_XX == 4
    o.color = _BlitTex.Load(uv, 0);
    o.color += _BlitTex.Load(uv, 1);
    o.color += _BlitTex.Load(uv, 2);
    o.color += _BlitTex.Load(uv, 3);
    o.color *= 1.0 / 4.0;
    #elif MSAA_XX == 8
    o.color = _BlitTex.Load(uv, 0);
    o.color += _BlitTex.Load(uv, 1);
    o.color += _BlitTex.Load(uv, 2);
    o.color += _BlitTex.Load(uv, 3);
    o.color += _BlitTex.Load(uv, 4);
    o.color += _BlitTex.Load(uv, 5);
    o.color += _BlitTex.Load(uv, 6);
    o.color += _BlitTex.Load(uv, 7);
    o.color *= 1.0 / 8.0;
    #else
    o.color = 0.0;
    #endif
    return o;
}