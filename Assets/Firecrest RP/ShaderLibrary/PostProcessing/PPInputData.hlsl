#ifndef FIRECREST_PP_RENDERTEXTURE_INCLUDED
#define FIRECREST_PP_RENDERTEXTURE_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

TEXTURE2D(_ScreenSoureImage); // the original texture
TEXTURE2D(_BloomTexture);
TEXTURE2D(_BloomCache);

SAMPLER(sampler_linear_clamp);

float4 _ScreenSoureImage_TexelSize;


float4 GetSource(float2 screenUV)
{
	return SAMPLE_TEXTURE2D_LOD(_ScreenSoureImage, sampler_linear_clamp, screenUV, 0);
}

float4 GetBloomTexture(float2 screenUV)
{
	return SAMPLE_TEXTURE2D_LOD(_BloomTexture, sampler_linear_clamp, screenUV, 0);
}

float4 GetBloomCache(float2 screenUV)
{
	return SAMPLE_TEXTURE2D_LOD(_BloomCache, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceTexelSize()
{
	return _ScreenSoureImage_TexelSize;
}

float4 GetSourceBicubic (float2 screenUV)
{
	return SampleTexture2DBicubic(
		TEXTURE2D_ARGS(_BloomCache, sampler_linear_clamp),
		screenUV,
		_ScreenSoureImage_TexelSize.zwxy,
		1.0,
		0.0
	);
}

#endif