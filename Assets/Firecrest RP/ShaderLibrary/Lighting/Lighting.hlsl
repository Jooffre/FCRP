// Main Lighting

#ifndef FIRECREST_LIGHTING_INCLUDED
#define FIRECREST_LIGHTING_INCLUDED


float3 IncomingLight(Surface surface, Light light)
{
	return saturate(dot(surface.normalWS, light.direction) * light.attenuation) * light.color;
}


float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}


float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi)
{
    ShadowData shadowData = GetShadowData(surfaceWS);

    shadowData.shadowMask = gi.shadowMask;

    float3 color = gi.diffuse * brdf.diffuse;
    // float3 color = 0;
    // directional light + additional light

    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light dirLight = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, dirLight);
    }


#if defined(_LIGHTS_PER_OBJECT)

    for (int j = 0; j < min(8, unity_LightData.y); j++)
    {
        int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
        Light opLight = GetOptionalLight(lightIndex, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, opLight);
    }
#else
    for (int j = 0; j < _OptionalLightCount; j++)
    {
        Light opLight = GetOptionalLight(j, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, opLight);
    }

#endif

	return color;
}


// without GI
float3 GetLighting(Surface surfaceWS, BRDF brdf)
{
    ShadowData shadowData = GetShadowData(surfaceWS);

    float3 color = brdf.diffuse;

    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light dirLight = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, dirLight);
    }

#if defined(_LIGHTS_PER_OBJECT)

    for (int j = 0; j < min(8, unity_LightData.y); j++)
    {
        int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
        Light opLight = GetOptionalLight(lightIndex, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, opLight);
    }
#else
    for (int j = 0; j < _OptionalLightCount; j++)
    {
        Light opLight = GetOptionalLight(j, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, opLight);
    }

#endif

	return color;
}


#endif