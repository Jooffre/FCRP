// this script implements
// 1. Receive shadowmaps from CPU;
// 2. Receive the shadowmap transform matrix from CPU;
// 3. Sample shadowmaps

#ifndef FIRECREST_SHADOWS_INCLUDED
#define FIRECREST_SHADOWS_INCLUDED

// --------------------------------------------------
// the lib and Keywords regrading PCF filter

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif


// --------------------------------------------------
// define constant macros

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4    // 4 shadowed directional lights
#define MAX_CASCADE_COUNT 4    // Unity supports 4 cascades at most


// --------------------------------------------------
// shadow atlas sampler

// receive atlas from CPU
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
// SAMPLER_CMP samples a texture and compares a single component against the specified comparison value,
// useful when sampling depth
SAMPLER_CMP(SHADOW_SAMPLER);


// --------------------------------------------------
// constant buffer

CBUFFER_START(_CustomShadows)

	int         _CascadeCount;
	float4      _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4      _CascadeData[MAX_CASCADE_COUNT];
	float4x4    _TransformMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4      _ShadowAtlasSize;
    float4      _ShadowFadingData;

CBUFFER_END


// --------------------------------------------------

struct ShadowData
{
    float       strength;
    float       cascadeBlend;
	int         cascadeIndex;
};

struct DirectionalShadowData
{
	float       strength;   // the final strength of shadow that ranged from 0 to 1
    float       normalBias;
    int         tileIndex;
};


// --------------------------------------------------
// methods from here

// let shadow fade out when the distance from viewpoint increasing
// fade = (1-d*s)*f clamp to [0, 1]
// note that the s and f are the reciprocals of parameters inputed
float FadedShadowStrength (float distance, float scale, float fadeFactor)
{
	return saturate((1.0 - distance * scale) * fadeFactor);
}


// calculate shadowData, especially which level of cascade is the fragment in
// invoked in lighting.hlsl
ShadowData GetShadowData(Surface surfaceWS)
{
	ShadowData shadowData;

    // default cascade blend interpolation is 1.0, use current cascade completely
    shadowData.cascadeBlend = 1.0;
    shadowData.strength = FadedShadowStrength(surfaceWS.depth, _ShadowFadingData.x, _ShadowFadingData.y);

    int i;
	for (i = 0; i < _CascadeCount; i++)
    {
		float4 lightSource = _CascadeCullingSpheres[i];
		float distanceToLightSource = GetDistanceSquared(surfaceWS.position, lightSource.xyz);
		if (distanceToLightSource < lightSource.w)
		{
            float fade = FadedShadowStrength(distanceToLightSource, _CascadeData[i].x, _ShadowFadingData.z);
            
            // special process for the max cascade
            if (i == _CascadeCount - 1) { shadowData.strength *= fade; }
            else { shadowData.cascadeBlend = fade; }

            break;
        }
	}

    if (i == _CascadeCount) { shadowData.strength = 0.0; }

#if defined(_CASCADE_BLEND_DITHER)
    else if (shadowData.cascadeBlend < surfaceWS.dither)
    { i += 1; }
#endif

#if !defined(_CASCADE_BLEND_SOFT)
    shadowData.cascadeBlend = 1.0;
#endif

	shadowData.cascadeIndex = i;
	
    return shadowData;
}


// variant
ShadowData GetShadowData(float3 positionWS, float depth, float dither)
{
	ShadowData shadowData;

    // default cascade blend interpolation is 1.0, use current cascade completely
    shadowData.cascadeBlend = 1.0;
    shadowData.strength = FadedShadowStrength(depth, _ShadowFadingData.x, _ShadowFadingData.y);

    int i;
	for (i = 0; i < _CascadeCount; i++)
    {
		float4 lightSource = _CascadeCullingSpheres[i];
		float distanceToLightSource = GetDistanceSquared(positionWS, lightSource.xyz);
		if (distanceToLightSource < lightSource.w)
		{
            float fade = FadedShadowStrength(distanceToLightSource, _CascadeData[i].x, _ShadowFadingData.z);
            
            // special process for the max cascade
            if (i == _CascadeCount - 1) { shadowData.strength *= fade; }
            else { shadowData.cascadeBlend = fade; }

            break;
        }
	}

    if (i == _CascadeCount) { shadowData.strength = 0.0; }

#if defined(_CASCADE_BLEND_DITHER)
    else if (shadowData.cascadeBlend < dither)
    { i += 1; }
#endif

#if !defined(_CASCADE_BLEND_SOFT)
    shadowData.cascadeBlend = 1.0;
#endif

	shadowData.cascadeIndex = i;
	
    return shadowData;
}



// sample shadow map
// position in STS (Shadow Texture Space)
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}


// PCF Filtering
// when the keyword is defined it needs to sample multiple times, 
// otherwise it can suffice with invoking SampleDirectionalShadowAtlas() once.
float FilterDirectionalShadow(float3 positionSTS)
{
#if defined(DIRECTIONAL_FILTER_SETUP)
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

// ==============================================================================================================

// The following function calculates shadow attenuation, and invoked by Light.hlsl, as light.attenuation
// attenuation fatcor :
//   - if a fragment is fully shadowed then we get 0.0 and when it's not shadowed at all then we get 1.0.
//   - values in between indicate that the fragment is partially shadowed.

float GetDirectionalShadowAttenuation(DirectionalShadowData dirShadowData, ShadowData global, Surface surfaceWS)
{
    // don't receive shadows, return 1.0
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif

    // ignore the lights that disable shadow or shadow strength is 0.0
    if (dirShadowData.strength <= 0.0)
    {
		return 1.0;
	}

    float3 normalBias = surfaceWS.normalWS * (dirShadowData.normalBias * _CascadeData[global.cascadeIndex].y);
    // transformWorldToShadow
	float3 positionSTS = mul(_TransformMatrices[dirShadowData.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
	float shadow = FilterDirectionalShadow(positionSTS);

    if (global.cascadeBlend < 1.0)
    {
		normalBias = surfaceWS.normalWS * (dirShadowData.normalBias * _CascadeData[global.cascadeIndex + 1].y);
		positionSTS = mul(_TransformMatrices[dirShadowData.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
	}
	
    return lerp(1.0, shadow, dirShadowData.strength);
}


#endif