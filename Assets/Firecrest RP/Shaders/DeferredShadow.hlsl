// Shadows shading main


#ifndef FIRECREST_DEFEERD_SHADOW_INCLUDED
#define FIRECREST_DEFEERD_SHADOW_INCLUDED


#include "../ShaderLibrary/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/GeometryBuffer.hlsl" 


#ifdef _SOFTSHADOW_PCF7
	#define PCF_SAMPLES 16
	#define PCF_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#else
	#define PCF_SAMPLES 9
	#define PCF_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#endif

#define MAX_SHADOW_CASCADES 4
#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= 0.0 || shadowCoord.z >= 1.0


TEXTURE2D_SHADOW(_MainLightShadowmapTexture);
SAMPLER_CMP(sampler_MainLightShadowmapTexture);

#ifndef SHADER_API_GLES3
CBUFFER_START(MainLightShadows)
#endif

float4x4    _MainLightWorldToShadow[MAX_SHADOW_CASCADES + 1];
float4      _MainLightShadowParams;   // (x: shadowStrength, y: 1.0 if soft shadows, 0.0 otherwise, z: main light fade scale, w: main light fade bias)
float4      _CascadeShadowSplitSpheres[MAX_SHADOW_CASCADES];
float4      _CascadeShadowSplitSphereRadii;

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

// --------------------------------------------------------------------------------------------------------------------------------------

// ShadowParams
// x: ShadowStrength
// y: 1.0 if shadow is soft, 0.0 otherwise
// z: main light fade scale
// w: main light fade bias
half4 GetMainLightShadowParams()
{
    return _MainLightShadowParams;
}


half ComputeCascadeIndex(float3 positionWS)
{
    float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    half4 weights = half4(distances2 < _CascadeShadowSplitSphereRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return half(4.0) - dot(weights, half4(4, 3, 2, 1));
}


float4 TransformWorldToShadowCoord(float3 positionWS)
{
//#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    half cascadeIndex = ComputeCascadeIndex(positionWS);
//#else
//    half cascadeIndex = half(0.0);
//#endif

    float4 shadowCoord = mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0));

    return float4(shadowCoord.xyz, 0);
}


// to visualize culling spheres
half VarifyCascadeIdx(float3 positionWS)
{
    half cascadeIndex = ComputeCascadeIndex(positionWS); //0, 1, 2, 3
    return cascadeIndex * 0.2;
}


half SampleScreenSpaceShadowmap(float4 shadowCoord)
{
    shadowCoord.xy /= shadowCoord.w;

    // The stereo transform has to happen after the manual perspective divide
    //shadowCoord.xy = UnityStereoTransformScreenSpaceTex(shadowCoord.xy);

//#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
//    half attenuation = SAMPLE_TEXTURE2D_ARRAY(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, shadowCoord.xy, unity_StereoEyeIndex).x;
//#else
    half attenuation = half(SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, shadowCoord.xy).x);
//#endif

    return attenuation;
}


ShadowSamplingData GetMainLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;

    // shadowOffsets are used in SampleShadowmapFiltered #if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
    shadowSamplingData.shadowOffset0 = _MainLightShadowOffset0;
    shadowSamplingData.shadowOffset1 = _MainLightShadowOffset1;
    shadowSamplingData.shadowOffset2 = _MainLightShadowOffset2;
    shadowSamplingData.shadowOffset3 = _MainLightShadowOffset3;

    // shadowmapSize is used in SampleShadowmapFiltered for other platforms
    shadowSamplingData.shadowmapSize = _MainLightShadowmapSize;

    return shadowSamplingData;
}


real SampleShadowmapFiltered(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
{
    real attenuation = 0;

// #if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
//     // 4-tap hardware comparison
//     real4 attenuation4;
//     attenuation4.x = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset0.xyz));
//     attenuation4.y = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset1.xyz));
//     attenuation4.z = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset2.xyz));
//     attenuation4.w = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset3.xyz));
//     attenuation = dot(attenuation4, real(0.25));
// #else
    float weights[PCF_SAMPLES];
    float2 fliterUV[PCF_SAMPLES];
    PCF_FILTER_SETUP(samplingData.shadowmapSize, shadowCoord.xy, weights, fliterUV);

    for (int i = 0; i < PCF_SAMPLES; i++)
    {
        attenuation += weights[i] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fliterUV[i].xy, shadowCoord.z));
    }
//#endif

    return attenuation;
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