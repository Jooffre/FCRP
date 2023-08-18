#ifndef FIRECREST_PP_BLOOM_INCLUDED
#define FIRECREST_PP_BLOOM_INCLUDED


#include "PPInputData.hlsl"
#include "PPDefaultPassVertex.hlsl"


// extra data needed
float4		_BloomThreshold;
float		_BloomIntensity;
float		_BloomScatter;
float4		_BloomTint;
bool		_BloomBicubicUpsampling;


float3 ApplyBloomThreshold(float3 color)
{
	float brightness = Max3(color.r, color.g, color.b);
	float soft = brightness + _BloomThreshold.y;
	soft = clamp(soft, 0.0, _BloomThreshold.z);
	soft = soft * soft * _BloomThreshold.w;
	float contribution = max(soft, brightness - _BloomThreshold.x);
	contribution /= max(brightness, 0.00001);
	return color * contribution;
}

float4 DownSampleHorizontalPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	
	float offsets[] =
	{
		-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
	};
	float weights[] =
	{
		0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
		0.19459459, 0.12162162, 0.05405405, 0.01621622
	};
	
	for (int i = 0; i < 9; i++)
	{
		float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
		color += GetBloomCache(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
	}
	color *= _BloomTint.rgb;

	return float4(color, 1.0);
}

float4 DownSampleVerticalPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	
	float offsets[] =
	{
		-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
	};
	float weights[] =
	{
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	
	for (int i = 0; i < 5; i++)
	{
		float offset = offsets[i] * 2.0 * GetSourceTexelSize().y;
		color += GetBloomCache(input.screenUV + float2(0.0, offset)).rgb * weights[i];
	}
	color *= _BloomTint.rgb;
	
	return float4(color, 1.0);
}

float4 BloomAdditiveCombinePassFragment(Varyings input) : SV_TARGET
{
	float3 blurred;
	if (_BloomBicubicUpsampling)
	{
		blurred = GetSourceBicubic(input.screenUV).rgb;
	}
	else
	{
		blurred = GetBloomCache(input.screenUV).rgb;
	}

	float3 source = GetSource(input.screenUV).rgb;

	return float4(blurred * _BloomIntensity + source, 1.0);
}

float4 BloomScatterCombinePassFragment(Varyings input) : SV_TARGET
{
	float3 blurred;
	if (_BloomBicubicUpsampling)
	{
		blurred = GetSourceBicubic(input.screenUV).rgb;
	}
	else
	{
		blurred = GetBloomCache(input.screenUV).rgb;
	}

	float3 source = GetSource(input.screenUV).rgb;
	blurred += source - ApplyBloomThreshold(source);

	return float4(lerp(source, blurred , _BloomScatter), 1.0);
}

float4 BloomThresholdFilterPassFragment(Varyings input) : SV_TARGET
{
	float3 color = ApplyBloomThreshold(GetBloomCache(input.screenUV).rgb);

	return float4(color, 1.0);
}

float4 BloomPrefilterFirefliesPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	float weightSum = 0.0;

	float2 offsets[] = 
	{
		float2(0.0, 0.0),float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)//
		//float2(-1.0, 0.0), float2(1.0, 0.0), float2(0.0, -1.0), float2(0.0, 1.0)
	};
	for (int i = 0; i < 5; i++)
	{
		float3 c = GetBloomCache(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
		c = ApplyBloomThreshold(c);
		float w = 1.0 / (Luminance(c) + 1.0);
		color += c * w;
		weightSum += w;
	}
	color /= weightSum;

	return float4(color, 1.0);
}

#endif