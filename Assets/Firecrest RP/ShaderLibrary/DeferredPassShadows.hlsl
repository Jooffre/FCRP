#ifndef FIRECREST_DEFERRED_SHADOWS_INCLUDED
#define FIRECREST_DEFERRED_SHADOWS_INCLUDED


struct ShadowData
{
    float       strength;
    float       cascadeBlend;
	int         cascadeIndex;
};


float GetDeferredLightAttenuation(DirectionalShadowData dirShadowData, ShadowData global, Surface surfaceWS)
{
    // don't receive shadows, return 1.0
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif

    if (dirShadowData.strength <= 0.0)
    {
		return 1.0;
	}

    /*float3 normalBias = surfaceWS.normalWS * (dirShadowData.normalBias * _CascadeData[global.cascadeIndex].y);

	float3 positionSTS = mul(_TransformMatrices[dirShadowData.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
	float shadow = FilterDirectionalShadow(positionSTS);

    if (global.cascadeBlend < 1.0)
    {
		normalBias = surfaceWS.normalWS * (dirShadowData.normalBias * _CascadeData[global.cascadeIndex + 1].y);
		positionSTS = mul(_TransformMatrices[dirShadowData.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
	}*/
	
    //return lerp(1.0, shadow, dirShadowData.strength);
    return 0.5;
}

#endif