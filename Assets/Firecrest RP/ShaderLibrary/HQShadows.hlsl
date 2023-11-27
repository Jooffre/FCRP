#ifndef FIRECREST_HQ_SHADOWS_INCLUDED
#define FIRECREST_HQ_SHADOWS_INCLUDED


// PCF Filtering
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

// PCSS
#define N_SAMPLE 64

static float2 poissonDisk[N_SAMPLE] = {
    float2(-0.5119625f, -0.4827938f),
    float2(-0.2171264f, -0.4768726f),
    float2(-0.7552931f, -0.2426507f),
    float2(-0.7136765f, -0.4496614f),
    float2(-0.5938849f, -0.6895654f),
    float2(-0.3148003f, -0.7047654f),
    float2(-0.42215f, -0.2024607f),
    float2(-0.9466816f, -0.2014508f),
    float2(-0.8409063f, -0.03465778f),
    float2(-0.6517572f, -0.07476326f),
    float2(-0.1041822f, -0.02521214f),
    float2(-0.3042712f, -0.02195431f),
    float2(-0.5082307f, 0.1079806f),
    float2(-0.08429877f, -0.2316298f),
    float2(-0.9879128f, 0.1113683f),
    float2(-0.3859636f, 0.3363545f),
    float2(-0.1925334f, 0.1787288f),
    float2(0.003256182f, 0.138135f),
    float2(-0.8706837f, 0.3010679f),
    float2(-0.6982038f, 0.1904326f),
    float2(0.1975043f, 0.2221317f),
    float2(0.1507788f, 0.4204168f),
    float2(0.3514056f, 0.09865579f),
    float2(0.1558783f, -0.08460935f),
    float2(-0.0684978f, 0.4461993f),
    float2(0.3780522f, 0.3478679f),
    float2(0.3956799f, -0.1469177f),
    float2(0.5838975f, 0.1054943f),
    float2(0.6155105f, 0.3245716f),
    float2(0.3928624f, -0.4417621f),
    float2(0.1749884f, -0.4202175f),
    float2(0.6813727f, -0.2424808f),
    float2(-0.6707711f, 0.4912741f),
    float2(0.0005130528f, -0.8058334f),
    float2(0.02703013f, -0.6010728f),
    float2(-0.1658188f, -0.9695674f),
    float2(0.4060591f, -0.7100726f),
    float2(0.7713396f, -0.4713659f),
    float2(0.573212f, -0.51544f),
    float2(-0.3448896f, -0.9046497f),
    float2(0.1268544f, -0.9874692f),
    float2(0.7418533f, -0.6667366f),
    float2(0.3492522f, 0.5924662f),
    float2(0.5679897f, 0.5343465f),
    float2(0.5663417f, 0.7708698f),
    float2(0.7375497f, 0.6691415f),
    float2(0.2271994f, -0.6163502f),
    float2(0.2312844f, 0.8725659f),
    float2(0.4216993f, 0.9002838f),
    float2(0.4262091f, -0.9013284f),
    float2(0.2001408f, -0.808381f),
    float2(0.149394f, 0.6650763f),
    float2(-0.09640376f, 0.9843736f),
    float2(0.7682328f, -0.07273844f),
    float2(0.04146584f, 0.8313184f),
    float2(0.9705266f, -0.1143304f),
    float2(0.9670017f, 0.1293385f),
    float2(0.9015037f, -0.3306949f),
    float2(-0.5085648f, 0.7534177f),
    float2(0.9055501f, 0.3758393f),
    float2(0.7599946f, 0.1809109f),
    float2(-0.2483695f, 0.7942952f),
    float2(-0.4241052f, 0.5581087f),
    float2(-0.1020106f, 0.6724468f)
};


float BilinearSample(TEXTURE2D_PARAM(ShadowMap, sampler_ShadowMap), float2 uv)
{
    // shadow coord's range is [0, 1], and it needs to be scaled to the shadow atlas size [0, 2048 - 1]
    //float2 uv = shadowCoord.xy * (2048 - 1);
    //float2 uv = shadowCoord.xy;
    float u0 = floor(uv.x);
    float v0 = floor(uv.y);
    float u1 = u0 + 1;
    float v1 = v0 + 1;

    float Da = SAMPLE_TEXTURE2D(ShadowMap, sampler_ShadowMap, float2(u0/2048, v0/2048)).r;
    float Db = SAMPLE_TEXTURE2D(ShadowMap, sampler_ShadowMap, float2(u1/2048, v0/2048)).r;
    float Dc = SAMPLE_TEXTURE2D(ShadowMap, sampler_ShadowMap, float2(u0/2048, v1/2048)).r;
    float Dd = SAMPLE_TEXTURE2D(ShadowMap, sampler_ShadowMap, float2(u1/2048, v1/2048)).r;
    float xVal0 = lerp(Da, Db, uv.x - u0);
    float xVal1 = lerp(Dc, Dd, uv.x - u0);

    //float xVal0 = lerp(rwtexture[int2(u0,v0)], rwtexture[int2(u1,v0)], uv.x - u0);
    //float xVal1 = lerp(rwtexture[int2(u0,v1)], rwtexture[int2(u1,v1)], uv.x - u0);

    return lerp(xVal0, xVal1, uv.y - v0);
}


float2 RotateVector(float2 v, float angle)
{
    float s = sin(angle);
    float c = cos(angle);

    return float2(v.x*c+v.y*s, -v.x*s+v.y*c);
}


#endif