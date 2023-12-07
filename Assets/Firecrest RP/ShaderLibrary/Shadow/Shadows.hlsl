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

#if defined(_OTHER_PCF3)
    #define OTHER_FILTER_SAMPLES 4
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_OTHER_PCF5)
    #define OTHER_FILTER_SAMPLES 9
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_OTHER_PCF7)
    #define OTHER_FILTER_SAMPLES 16
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

// --------------------------------------------------
// define constant macros

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4    // 4 shadowed directional lights
#define MAX_SHADOWED_OTHER_LIGHT_COUNT 16
#define MAX_CASCADE_COUNT 4    // Unity supports 4 cascades at most


// --------------------------------------------------
// shadow atlas sampler

// receive atlas from CPU
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_OtherShadowAtlas);

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
	float4x4    _DirShadowTransformMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4x4    _OtherShadowTransformMatrices[MAX_SHADOWED_OTHER_LIGHT_COUNT];
    float4      _OtherShadowTiles[MAX_SHADOWED_OTHER_LIGHT_COUNT];
    float4      _ShadowAtlasSize;
    float4      _ShadowFadingData;

CBUFFER_END


// --------------------------------------------------

struct ShadowMask
{
    bool        always;     // default shadowmask
    bool        distance;   // distance shadowmask
    float4      maskValue;
};

struct ShadowData
{
    float       strength;
    float       cascadeBlend;
	int         cascadeIndex;
    ShadowMask  shadowMask;
};

struct DirectionalShadowData
{
	float       strength;   // the final strength of shadow that ranged from 0 to 1
    float       normalBias;
    int         tileIndex;
    int         shadowMaskChannel;
};

struct OtherLightShadowData
{
    bool        isPointLight;
    float       strength;
    int         tileIndex;
    int         shadowMaskChannel;
    float3      lightPositionWS;
    float3      lightDirectionWS;
    float3      spotDirectionWS;
};

// --------------------------------------------------
// methods from here

// let shadow fade out when the distance from viewpoint increasing
// fade = (1-d*s)*f clamp to [0, 1]
// note that the s and f are the reciprocals of parameters inputed
float FadedShadowStrength(float distance, float scale, float fadeFactor)
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

    if (i == _CascadeCount && _CascadeCount > 0)
        shadowData.strength = 0.0;

#if defined(_CASCADE_BLEND_DITHER)
    else if (shadowData.cascadeBlend < surfaceWS.dither)
    { i += 1; }
#endif

#if !defined(_CASCADE_BLEND_SOFT)
    shadowData.cascadeBlend = 1.0;
#endif

	shadowData.cascadeIndex = i;

    // shadow mask

    shadowData.shadowMask.always = false;
    shadowData.shadowMask.distance = false;
    shadowData.shadowMask.maskValue = 1.0;
	
    return shadowData;
}


// variant
/*
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

        if (i == _CascadeCount && _CascadeCount > 0)
        { shadowData.strength = 0.0; }

    #if defined(_CASCADE_BLEND_DITHER)
        else if (shadowData.cascadeBlend < dither)
        { i += 1; }
    #endif

    #if !defined(_CASCADE_BLEND_SOFT)
        shadowData.cascadeBlend = 1.0;
    #endif

        shadowData.cascadeIndex = i;

        // shadow mask

        shadowData.shadowMask.always = false;
        shadowData.shadowMask.distance = false;
        shadowData.shadowMask.maskValue = 1.0;
        
        return shadowData;
    }
*/


// sample shadow map
// position in STS : Shadow Texture Space
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float SampleOtherShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, positionSTS);
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

float FilterOtherShadow(float3 positionSTS, float3 bounds)
{
#if defined(OTHER_FILTER_SETUP)
    real weights[OTHER_FILTER_SAMPLES];
    real2 positions[OTHER_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.wwzz;
    OTHER_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;

    for (int i = 0; i < OTHER_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleOtherShadowAtlas(float3(positions[i].xy, positionSTS.z), bounds);
    }
    return shadow;
#else
    return SampleOtherShadowAtlas(positionSTS, bounds);
#endif
}

// ==============================================================================================================

// The following function calculates shadow attenuation, and invoked by Light.hlsl, as light.attenuation
// attenuation fatcor :
//   - if a fragment is fully shadowed then we get 0.0 and when it's not shadowed at all then we get 1.0.
//   - values in between indicate that the fragment is partially shadowed.

float GetRealtimeShadowAttenuation(DirectionalShadowData dirShadowData, ShadowData global, Surface surfaceWS)
{
    float3 normalBias = surfaceWS.normalWS * (dirShadowData.normalBias * _CascadeData[global.cascadeIndex].y);
    // transform WorldToShadow
	float3 positionSTS = mul(_DirShadowTransformMatrices[dirShadowData.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
	
    float shadow = FilterDirectionalShadow(positionSTS);

    if (global.cascadeBlend < 1.0)
    {
		normalBias = surfaceWS.normalWS * (dirShadowData.normalBias * _CascadeData[global.cascadeIndex + 1].y);
		positionSTS = mul(_DirShadowTransformMatrices[dirShadowData.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
	}

    return shadow;
}

// returning the corresponding attenuation given the shadow mask
float GetBakedShadowAttenuation(ShadowMask mask, int channel)
{
    float shadow = 1.0;
    if (mask.always || mask.distance)
    {
        if (channel >= 0)
        {
            shadow = mask.maskValue[channel];
        }
    }

    return shadow;
}

// a variant that controls baked shadow by shadow strength
float GetBakedShadowAttenuation(ShadowMask mask, int channel, float strength)
{
    if (mask.always || mask.distance)
    {
        return lerp(1.0, GetBakedShadowAttenuation(mask, channel), strength);
    }

    return 1.0;
}

float MixBakedAndRealtimeShadows(ShadowData shadowData, float shadow, int shadowMaskChannel, float strength)
{
    float bakedShadow = GetBakedShadowAttenuation(shadowData.shadowMask, shadowMaskChannel);
    
    if (shadowData.shadowMask.always)
    {
		shadow = lerp(1.0, shadow, shadowData.strength);
		shadow = min(bakedShadow, shadow);

		return lerp(1.0, shadow, strength);
    }

    if (shadowData.shadowMask.distance)
    {
        shadow = lerp(bakedShadow, shadow, shadowData.strength);
        
        return lerp(1.0, shadow, strength);
    }

    return lerp(1.0, shadow, strength * shadowData.strength);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData dirShadowData, ShadowData shadowData, Surface surfaceWS)
{
    // don't receive shadows, return 1.0
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif

    float shadow;

    // ignore the lights that disable shadow or shadow strength is 0.0
    if (dirShadowData.strength * shadowData.strength <= 0.0)
    {
		shadow = GetBakedShadowAttenuation(shadowData.shadowMask, dirShadowData.shadowMaskChannel, abs(dirShadowData.strength));
	}
    else
    {
        shadow = GetRealtimeShadowAttenuation(dirShadowData, shadowData, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(shadowData, shadow, dirShadowData.shadowMaskChannel, dirShadowData.strength);
    }

    return shadow;
}


// shadows of other lights

static const float3 pointShadowPlanes[6] =
{
	float3(-1.0, 0.0, 0.0),
	float3(1.0, 0.0, 0.0),
	float3(0.0, -1.0, 0.0),
	float3(0.0, 1.0, 0.0),
	float3(0.0, 0.0, -1.0),
	float3(0.0, 0.0, 1.0)
};

float GetOtherShadow(OtherLightShadowData other, ShadowData global, Surface surfaceWS)
{
    float tileIndex = other.tileIndex;
    float3 lightPlane = other.spotDirectionWS;

    if (other.isPointLight)
    {
        // the order of the cubemap faces is
        // +X, −X, +Y, −Y, +Z, −Z,
        // which matches how we rendered them
        float faceOffset = CubeMapFaceID(-other.lightDirectionWS);
        tileIndex += faceOffset;
        lightPlane = pointShadowPlanes[faceOffset];
    }

    float4 tileData = _OtherShadowTiles[tileIndex];
    float3 surfaceToLight = other.lightPositionWS - surfaceWS.position;
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);
    float3 normalBias = surfaceWS.normalWS * (distanceToLightPlane * tileData.w);
    float4 positionSTS = mul(_OtherShadowTransformMatrices[tileIndex], float4(surfaceWS.position + normalBias, 1.0));

	return FilterOtherShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

float GetOtherLightShadowAttenuation(OtherLightShadowData opShadowData, ShadowData shadowData, Surface surfaceWS)
{

# if !defined(_RECEIVE_SHADOWS)
    return 1.0;
# endif

    float shadow;
    
    if (opShadowData.strength * shadowData.strength <= 0)
    {
        shadow = GetBakedShadowAttenuation(shadowData.shadowMask, opShadowData.shadowMaskChannel, abs(opShadowData.strength));
    }
    else
    {
        shadow = GetOtherShadow(opShadowData, shadowData, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(shadowData, shadow, opShadowData.shadowMaskChannel, opShadowData.strength);
    }

    return shadow;
}

#endif