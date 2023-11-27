// Computing properties of the instance "Light"

#ifndef FIRECREST_LIGHT_INCLUDED
#define FIRECREST_LIGHT_INCLUDED


#include "Light.hlsl"

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData data;

	data.strength = _DirectionalLightsShadowData[lightIndex].x;

    // tile index = light start index + cascade order index
	data.tileIndex = _DirectionalLightsShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightsShadowData[lightIndex].z;

    data.shadowMaskChannel = _DirectionalLightsShadowData[lightIndex].w;
	
    return data;
}


// Forward mode
Light GetDirectionalLight(int idx, Surface surfaceWS, ShadowData shadowData)
{
    Light light;

    light.color = _DirectionalLightsColor[idx].rgb;
    light.direction = _DirectionalLightsDirection[idx].xyz;

    DirectionalShadowData dirShadowData = GetDirectionalShadowData(idx, shadowData);
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);

    return light;
}

Light GetDirectionalLight(int idx)
{
    Light light;

    light.color = _DirectionalLightsColor[idx].rgb;
    light.direction = _DirectionalLightsDirection[idx].xyz;
    light.attenuation = 0.5;

    return light;
}


OptionalLightShadowData GetOptionalLightShadowData(int idx)
{
    OptionalLightShadowData data;

    data.strength = _OptionalLightShadowData[idx].x;
    data.shadowMaskChannel = _OptionalLightShadowData[idx].w;

    return data;
}


Light GetOptionalLight(int idx, Surface surfaceWS, ShadowData shadowData)
{
    Light light;

    light.color = _OptionalLightColor[idx].rgb;

    float3 rayDir = _OptionalLightPosition[idx].xyz - surfaceWS.position;
    light.direction = normalize(rayDir);

    float distanceSqr = max(dot(rayDir, rayDir), 0.00001);
    float pointLightAttenuation = Square(saturate(1.0 - Square(distanceSqr *_OptionalLightPosition[idx].w)));

    float4 spotLightAngle = _OptionalSpotLightAngle[idx];
    float spotLightAttenuation = 
        Square(saturate(dot(_OptionalLightDirection[idx].xyz, light.direction)) * spotLightAngle.x + spotLightAngle.y);

    OptionalLightShadowData optionalLightShadowData = GetOptionalLightShadowData(idx);

    light.attenuation = GetOptionalLightShadowAttenuation(optionalLightShadowData, shadowData, surfaceWS)
        * spotLightAttenuation * pointLightAttenuation / distanceSqr;

    return light;
}


// ======================================

#include "../Shaders/DeferredShadow.hlsl"

struct AdvLight
{
    half3   direction;
    half3   color;
    float   distanceAttenuation;
    half    shadowAttenuation;
    //uint    layerMask;
};

AdvLight GetAdvLight(int idx)
{
    AdvLight light;

    light.direction =_DirectionalLightsDirection[idx].xyz;

    //light.distanceAttenuation = unity_LightData.z; // unity_LightData.z is 1 when not culled by the culling mask, otherwise 0.
    light.distanceAttenuation = 1.0; // unity_LightData.z is set per mesh for forward renderer, we cannot cull lights in this fashion with deferred renderer.

    light.shadowAttenuation = 1.0;
    light.color = _DirectionalLightsColor[idx].rgb;

//#ifdef _LIGHT_LAYERS
//    light.layerMask = _MainLightLayerMask;
//#else
//    light.layerMask = DEFAULT_LIGHT_LAYERS;
//#endif

    return light;
}

AdvLight GetStencilLight(float2 screen_uv)
{
    AdvLight light;

//#ifdef _LIGHT_LAYERS
//    uint lightLayerMask =_LightLayerMask;
//#else
//    uint lightLayerMask = DEFAULT_LIGHT_LAYERS;
//#endif

    light = GetAdvLight(0);
    //light.distanceAttenuation = 1.0;

//#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
    float4 shadowCoord = float4(screen_uv, 0.0, 1.0);
//#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    //float4 shadowCoord = TransformWorldToShadowCoord(posWS.xyz);
//#else
    //float4 shadowCoord = float4(0, 0, 0, 0);
//#endif
    light.shadowAttenuation = SampleScreenSpaceShadowmap(shadowCoord);
    //light.shadowAttenuation = ShadowGaussianBlur(shadowCoord.xy, 15);
// #if defined(_LIGHT_COOKIES)
//     real3 cookieColor = SampleMainLightCookie(posWS);
//     light.color *= float4(cookieColor, 1);
// #endif

    return light;
}
#endif