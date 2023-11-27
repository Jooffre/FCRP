#ifndef FIRECREST_SIMPLE_FXAA_INCLUDED
#define FIRECREST_SIMPLE_FXAA_INCLUDED


#define LOAD_TEXTURE2D(textureName, icoord2) textureName.Load(int3(icoord2, 0))
#define FXAA_SPAN_MAX   (8.0)
#define FXAA_REDUCE_MUL (1.0 / 8.0)
#define FXAA_REDUCE_MIN (1.0 / 128.0)

SamplerState sampler_LinearClamp;

float3 FXAAFetch(float2 coords, float2 offset, TEXTURE2D(inputTexture))
{
    float2 uv = coords + offset;
    return SAMPLE_TEXTURE2D(inputTexture, sampler_LinearClamp, uv).xyz;
}


float3 FXAALoad(int2 icoords, int idx, int idy, float4 sourceSize, TEXTURE2D(inputTexture))
{
    //#if SHADER_API_GLES
    //float2 uv = (icoords + int2(idx, idy)) * sourceSize.zw;
    //return SAMPLE_TEXTURE2D_X(inputTexture, sampler_PointClamp, uv).xyz;
    //#else
    return LOAD_TEXTURE2D(inputTexture, clamp(icoords + int2(idx, idy), 0, sourceSize.xy - 1.0)).xyz;
    //#endif
}


float3 ApplyFXAA(float3 color, float2 positionNDC, int2 positionSS, float4 sourceSize, TEXTURE2D(inputTexture))
{
    // Edge detection
    float3 rgbNW = FXAALoad(positionSS, -1, -1, sourceSize, inputTexture);
    float3 rgbNE = FXAALoad(positionSS,  1, -1, sourceSize, inputTexture);
    float3 rgbSW = FXAALoad(positionSS, -1,  1, sourceSize, inputTexture);
    float3 rgbSE = FXAALoad(positionSS,  1,  1, sourceSize, inputTexture);

    rgbNW = saturate(rgbNW);
    rgbNE = saturate(rgbNE);
    rgbSW = saturate(rgbSW);
    rgbSE = saturate(rgbSE);
    color = saturate(color);

    float lumaNW = Luminance(rgbNW);
    float lumaNE = Luminance(rgbNE);
    float lumaSW = Luminance(rgbSW);
    float lumaSE = Luminance(rgbSE);
    float lumaM = Luminance(color);

    float2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));

    float lumaSum = lumaNW + lumaNE + lumaSW + lumaSE;
    float dirReduce = max(lumaSum * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
    float rcpDirMin = rcp(min(abs(dir.x), abs(dir.y)) + dirReduce);

    dir = min((FXAA_SPAN_MAX).xx, max((-FXAA_SPAN_MAX).xx, dir * rcpDirMin)) * sourceSize.zw;

    // Blur
    float3 rgb03 = FXAAFetch(positionNDC, dir * (0.0 / 3.0 - 0.5), inputTexture);
    float3 rgb13 = FXAAFetch(positionNDC, dir * (1.0 / 3.0 - 0.5), inputTexture);
    float3 rgb23 = FXAAFetch(positionNDC, dir * (2.0 / 3.0 - 0.5), inputTexture);
    float3 rgb33 = FXAAFetch(positionNDC, dir * (3.0 / 3.0 - 0.5), inputTexture);

    rgb03 = saturate(rgb03);
    rgb13 = saturate(rgb13);
    rgb23 = saturate(rgb23);
    rgb33 = saturate(rgb33);

    float3 rgbA = 0.5 * (rgb13 + rgb23);
    float3 rgbB = rgbA * 0.5 + 0.25 * (rgb03 + rgb33);

    float lumaB = Luminance(rgbB);

    float lumaMin = Min3(lumaM, lumaNW, Min3(lumaNE, lumaSW, lumaSE));
    float lumaMax = Max3(lumaM, lumaNW, Max3(lumaNE, lumaSW, lumaSE));

    return ((lumaB < lumaMin) || (lumaB > lumaMax)) ? rgbA : rgbB;
}

#endif