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

    float3 color = gi.diffuse * brdf.diffuse;
    
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }

	return color;
}


float3 GetLighting(Surface surfaceWS, BRDF brdf)
{
    ShadowData shadowData = GetShadowData(surfaceWS);

    float3 color = brdf.diffuse;

    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }

	return color;
}


#endif