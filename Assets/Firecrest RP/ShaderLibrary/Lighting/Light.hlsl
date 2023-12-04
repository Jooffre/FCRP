// Defining "Light" for this RP and necessary parameters connecting with c# scripts.

#ifndef FIRECREST_LIGHTDATA_INCLUDED
#define FIRECREST_LIGHTDATA_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OPTIONAL_LIGHT_COUNT 64


CBUFFER_START(_CustomLight)

    int         _DirectionalLightCount;
    float4      _DirectionalLightsColor[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4      _DirectionalLightsDirection[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4      _DirectionalLightsShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

    int         _OptionalLightCount;
    float4      _OptionalLightColor[MAX_OPTIONAL_LIGHT_COUNT];
    float4      _SpotLightDirection[MAX_OPTIONAL_LIGHT_COUNT];
    float4      _OptionalLightPosition[MAX_OPTIONAL_LIGHT_COUNT];
    float4      _OptionalSpotLightAngle[MAX_OPTIONAL_LIGHT_COUNT];
    float4      _OptionalLightShadowData[MAX_OPTIONAL_LIGHT_COUNT];

CBUFFER_END


struct Light
{
    float3      color;
    float3      direction;
    float       attenuation;
};

#endif