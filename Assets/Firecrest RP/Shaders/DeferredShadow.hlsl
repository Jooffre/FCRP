// Shadows shading main


#ifndef FIRECREST_DEFEERD_SHADOW_INCLUDED
#define FIRECREST_DEFEERD_SHADOW_INCLUDED


#include "../ShaderLibrary/LitInput.hlsl"
#include "../ShaderLibrary/HQShadows.hlsl"
#include "../ShaderLibrary/LitInput.hlsl"
//#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/GeometryBuffer.hlsl" 


/*
#ifdef _SOFTSHADOW_PCF7
	#define PCF_SAMPLES 16
	#define PCF_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#else
	#define PCF_SAMPLES 9
	#define PCF_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#endif
*/

#define MAX_SHADOW_CASCADES 4
//#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= 0.0 || shadowCoord.z >= 1.0


//TEXTURE2D_SHADOW(_MainLightShadowmapTexture);
//SAMPLER_CMP(sampler_MainLightShadowmapTexture);
TEXTURE2D(_MainLightShadowmapTexture);
SAMPLER(sampler_MainLightShadowmapTexture);

#ifndef SHADER_API_GLES3
CBUFFER_START(MainLightShadows)
#endif

float4x4    _MainLightWorldToShadow[MAX_SHADOW_CASCADES + 1];
float4      _MainLightShadowParams;   // (x: shadowStrength, y: 1.0 if soft shadows, 0.0 otherwise, z: main light fade scale, w: main light fade bias)
float4      _CascadeShadowSplitSpheres[MAX_SHADOW_CASCADES];
float4      _CascadeShadowSplitSphereRadii;
float4      _CascadeZDistance;

float4      _MainLightShadowOffset0;
float4      _MainLightShadowOffset1;
float4      _MainLightShadowOffset2;
float4      _MainLightShadowOffset3;
float4      _MainLightShadowmapSize;  // (xy: 1/width and 1/height, zw: width and height)

#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

float4 _ShadowBias; // x: depth bias, y: normal bias, z & w : 0
float3 _LightDirection;


struct ShadowSamplingData
{
    half4   shadowOffset0;
    half4   shadowOffset1;
    half4   shadowOffset2;
    half4   shadowOffset3;
    float4  shadowmapSize;
};


// ===================================================================================================================

half ComputeCascadeIndex(float3 positionWS)
{
    float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    float4 sphereDistSquare = _CascadeShadowSplitSphereRadii * _CascadeShadowSplitSphereRadii;
    half4 weights = half4(distances2 < sphereDistSquare);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return half(4.0) - dot(weights, half4(4, 3, 2, 1));
}

float4 TransformWorldToShadowCoord(float3 positionWS, int cascadeIndex)
{
//#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
//    half cascadeIndex = ComputeCascadeIndex(positionWS);
//#else
//    half cascadeIndex = half(0.0);
//#endif

    float4 shadowCoord = mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0));

    return float4(shadowCoord.xyz, 0);
}

// visualization of culling spheres
half VarifyCascadeIdx(float3 positionWS)
{
    half cascadeIndex = ComputeCascadeIndex(positionWS); //0, 1, 2, 3
    return cascadeIndex * 0.2;
}



// ===================================================================================================================
//  PCSS & PCF
// ===================================================================================================================


#include "../ShaderLibrary/Lighting/Light.hlsl"

float GetLightTangent(float3 lightDir)
{
    return lightDir.y / sqrt(1 - lightDir.y * lightDir.y + 0.0001);
}


float ShadowEarlyTest(float shadowCoordZ, float2 shadowAtlasCoord, int cascadeIndex)
{
    float shadowDetector = 0;

    float texelSizeWS = _CascadeShadowSplitSpheres[cascadeIndex].w / 1024;

    float3 lightDir = normalize(_DirectionalLightsDirection[0].xyz);
    float t = GetLightTangent(lightDir);

    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            float2 bias = int2(i, j) / texelSizeWS;
            float2 biasedCoord = shadowAtlasCoord + bias;
            float deltaZ_WS = 0.2 / t;
            float deltaZ_LS = deltaZ_WS / _CascadeZDistance[cascadeIndex];
            float d = SAMPLE_TEXTURE2D(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture, float2(biasedCoord.x/2048, biasedCoord.y/2048));
            shadowDetector += (d < shadowCoordZ + deltaZ_LS);
        }
    }

    return shadowDetector;
}


float3 PCSS_BlockerDepthSamplingX64(float shadowCoordZ, float2 shadowAtlasCoord, float searchWidth, float random, float tangent)
{
    float blockerDepth = 0;
    float count = 0.0005;

    for(int i = 0; i < N_SAMPLE; i++)
    {
        float2 bias = poissonDisk[i];
        bias = RotateVector(bias, random);
        float2 uv_biased = shadowAtlasCoord + bias * searchWidth;
        float sampleDepth = BilinearSample(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), uv_biased);
        if (sampleDepth > shadowCoordZ)
        {
            blockerDepth += sampleDepth;
            count++;
        }
    }
    return float3(blockerDepth / count, count, tangent);
}


float3 PCSS_GetBlockerDepth(float3 positionWS, float shadowCoordZ, float2 shadowAtlasCoord, int cascadeIndex)
{
    float random = Random1D(positionWS.x + positionWS.y);

    float OrthHalfWidth0 = _CascadeShadowSplitSphereRadii[0];
    float OrthHalfWidth = _CascadeShadowSplitSphereRadii[cascadeIndex];
    
    float blockerSearchWidth = 60 / OrthHalfWidth;

    float3 lightDir = normalize(_DirectionalLightsDirection[0].xyz);
    //float4 offsetShadowCoord = TransformWorldToShadowCoord(positionWS + lightDir * OrthHalfWidth / 2048, cascadeIndex);
    float t = GetLightTangent(lightDir);
    float texelSize = OrthHalfWidth / 2048;
    float deltaZ_WS = blockerSearchWidth * texelSize / t;
    float deltaZ_LS = deltaZ_WS / _CascadeZDistance[cascadeIndex];

    return PCSS_BlockerDepthSamplingX64(shadowCoordZ + deltaZ_LS, shadowAtlasCoord, blockerSearchWidth, random, t);
}


float PCF_PoisonSamplingX64(float shadowCoordZ, float2 shadowAtlasCoord, float fadeWidth, float random)
{
    float softShadow = 0.0;
    float2 bias = 0.0;

    for(int i = 0; i < N_SAMPLE; i++)
    {
        bias = poissonDisk[i];
        bias = RotateVector(bias, random);
        float2 uv_biased = shadowAtlasCoord + bias * fadeWidth;
        float sampleDepth = BilinearSample(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), uv_biased);
        //softShadow += sampleDepth;
        if(sampleDepth < shadowCoordZ)
        {
            softShadow += 1.0;
        }
    }
    softShadow /= N_SAMPLE;

    return clamp(softShadow, 0.25, 1.0);
}


float GetPenumbra(float depth, float blockerDepth, int cascadeIndex)
{
    float penumbra = abs(depth - blockerDepth) * _CascadeZDistance[cascadeIndex] / _CascadeShadowSplitSphereRadii[cascadeIndex];
    return min(penumbra * 10, 100 / _CascadeShadowSplitSphereRadii[cascadeIndex]);
    //return penumbra;
}


float GetPenumbraRange(float penumbra, float tangent, int cascadeIndex)
{
    float texelSize = _CascadeShadowSplitSphereRadii[cascadeIndex] / 2048;
    float deltaZ_WS = penumbra * texelSize / tangent;
    return deltaZ_WS / _CascadeZDistance[cascadeIndex];
}


// screen-space shadows sampling
float SampleScreenSpaceShadowmap(float4 shadowCoord)
{
    shadowCoord.xy /= shadowCoord.w;

    return SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, shadowCoord.xy).x;
}

/*
float ShadowGaussianBlur(float2 uv, float blurSize)
{
    blurSize /= 2048;
    float color_V = 0;
    float color_H = 0;
    //float w[3] = {0.4026, 0.2442, 0.0545};
    float w[5] = {0.29675293, 0.19638062, 0.09442139, 0.01037598, 0.0002594};

    for (int i = -4; i < 4; i++)
    {
        color_V += SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, uv + float2(0, i * blurSize)).x * w[abs(i)];
    }
    
    for (int i = -4; i < 4; i++)
    {
        color_H += SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, uv + float2(i * blurSize, 0)).x * w[abs(i)];
    }

    return (color_H + color_V) / 2;
}*/


// ===================================================================================================================
//  built-in PCF sampling
// ===================================================================================================================

// DISPOSED. But you may choose this one to compute soft shadows.


/*

// ShadowParams
// x: ShadowStrength
// y: 1.0 if shadow is soft, 0.0 otherwise
// z: main light fade scale
// w: main light fade bias
half4 GetMainLightShadowParams()
{
    return _MainLightShadowParams;
}

ShadowSamplingData GetMainLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;

    shadowSamplingData.shadowOffset0 = _MainLightShadowOffset0;
    shadowSamplingData.shadowOffset1 = _MainLightShadowOffset1;
    shadowSamplingData.shadowOffset2 = _MainLightShadowOffset2;
    shadowSamplingData.shadowOffset3 = _MainLightShadowOffset3;

    // shadowmapSize is used in SampleShadowmapFiltered for other platforms
    shadowSamplingData.shadowmapSize = _MainLightShadowmapSize;

    return shadowSamplingData;
}

// PCF using built-in method
real SampleShadowmapFiltered(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
{
    real attenuation = 0;

    float weights[PCF_SAMPLES];
    float2 fliterUV[PCF_SAMPLES];
    PCF_FILTER_SETUP(samplingData.shadowmapSize, shadowCoord.xy, weights, fliterUV);

    for (int i = 0; i < PCF_SAMPLES; i++)
    {
        attenuation += weights[i] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fliterUV[i].xy, shadowCoord.z));
    }

    return clamp(attenuation, 0.25, 1.0);
}

real SampleShadowmap(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData, half4 shadowParams, bool isPerspectiveProjection = true)
{
    // Compiler will optimize this branch away as long as isPerspectiveProjection is known at compile time
    if (isPerspectiveProjection)
        shadowCoord.xyz /= shadowCoord.w;

    real attenuation;
    real shadowStrength = shadowParams.x;

//#ifdef _SHADOWS_SOFT
    if(shadowParams.y != 0)
    {
        attenuation = SampleShadowmapFiltered(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    }
    else
//#endif
    {
        // 1-tap hardware comparison
        attenuation = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz).x);
    }

    // 1 - shadowStrength + attenuation * shadowStrength
    attenuation = LerpWhiteTo(attenuation, shadowStrength);
    //attenuation = 1 - shadowStrength + attenuation * shadowStrength;

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    return BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}

*/


// ===================================================================================================================

float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
{
    float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
    float scale = invNdotL * _ShadowBias.y;

    // normal bias is negative since we want to apply an inset normal offset
    positionWS = lightDirection * _ShadowBias.xxx + positionWS;
    positionWS = normalWS * scale.xxx + positionWS;
    return positionWS;
}


half Alpha(half4 color, half cutoff)
{
    half alpha = color.a;

#if defined(_ALPHATEST_ON)
    clip(alpha - cutoff);
#endif

    return alpha;
}

#endif