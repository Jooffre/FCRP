#ifndef FIRECREST_PP_TONEMAPPING_INCLUDED
#define FIRECREST_PP_TONEMAPPING_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

float3 ToneMapping_Natural(float3 color)
{
	return NeutralTonemap(color);
}

float3 ToneMapping_ACES(float3 color)
{
	return AcesTonemap(unity_to_ACES(color));
}

float3 ToneMapping_Reinhard(float3 color)
{
	color.rgb /= color.rgb + 1.0;
	return color;
}

#endif