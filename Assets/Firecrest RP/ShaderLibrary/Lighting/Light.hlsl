// Defining "Light" for this RP and necessary parameters connecting with c# scripts.

#ifndef FIRECREST_LIGHTDATA_INCLUDED
#define FIRECREST_LIGHTDATA_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64


CBUFFER_START(_CustomLight)

    int         _DirectionalLightCount;
    float4      _DirectionalLightsColor[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4      _DirectionalLightsDirection[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4      _DirectionalLightsShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

    int         _OtherLightCount;
    float4      _OtherLightColor[MAX_OTHER_LIGHT_COUNT];
    float4      _OtherLightPosition[MAX_OTHER_LIGHT_COUNT];
    float4      _SpotLightDirection[MAX_OTHER_LIGHT_COUNT];
    float4      _SpotLightAngle[MAX_OTHER_LIGHT_COUNT];
    float4      _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];

CBUFFER_END


struct Light
{
    float3      color;
    float3      direction;
    float       attenuation;
};

#endif