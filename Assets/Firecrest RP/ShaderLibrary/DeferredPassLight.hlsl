#ifndef FIRECREST_DEFERRED_LIGHT_INCLUDED
#define FIRECREST_DEFERRED_LIGHT_INCLUDED


#define MAX_DIRECTIONAL_LIGHT_COUNT 4


CBUFFER_START(_CustomLight)

    int         _DirectionalLightCount;
    float4      _DirectionalLightsColor[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4      _DirectionalLightsDirection[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4      _DirectionalLightsShadowData[MAX_DIRECTIONAL_LIGHT_COUNT]; //Vec4 (shadow strength, bias, normal bias, near plane)

CBUFFER_END


struct Light
{
    float3      color;
    float3      direction;
    float       attenuation;
};

struct DirectionalShadowData_Deferred
{
    float       strength;
    float       normalBias;
};

int GetDirectionalLightCount()
{
	return _DirectionalLightCount;
}


DirectionalShadowData_Deferred GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData_Deferred data;

	data.strength = _DirectionalLightsShadowData[lightIndex].x;
    data.normalBias = _DirectionalLightsShadowData[lightIndex].z;
	
    return data;
}


// Deferred mode
Light GetDirectionalLight(int idx)
{
    Light light;

    light.color = _DirectionalLightsColor[idx].rgb;
    light.direction = _DirectionalLightsDirection[idx].xyz;
    light.attenuation = 0.5;

    return light;
}

#endif