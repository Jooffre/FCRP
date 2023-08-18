#ifndef FIRECREST_PP_COLOR_ADJUSTMENT_INCLUDED
#define FIRECREST_PP_COLOR_ADJUSTMENT_INCLUDED


//#include "PPInputData.hlsl"
//#include "PPDefaultPassVertex.hlsl"

// x : post exposure
// y : contrast
// z : hue shift
// w : saturation
float4	_ColorAdjustmentVector;
float4	_ColorFilter;


// clamp
float3 LimitBrightness(float3 color)
{
	return color = min(color, 60.0);
}

// post exposure
float3 ApplyPostExposure(float3 color)
{
	return color * _ColorAdjustmentVector.x;
}

// contrast
float3 ApplyContrast(float3 color)
{
	color = LinearToLogC(color);
	color = (color - ACEScc_MIDGRAY) * _ColorAdjustmentVector.y + ACEScc_MIDGRAY;

	return LogCToLinear(color);
}

// color filter
float3 ApplyColorFilter(float3 color)
{
	return color * _ColorFilter.rgb;
}

// hue shift
float3 ApplyHueShift(float3 color)
{
	color = RgbToHsv(color);
	float hue = color.x + _ColorAdjustmentVector.z;
	color.x = RotateHue(hue, 0.0, 1.0);

	return HsvToRgb(color);
}

// saturation
float3 ApplySaturation(float3 color)
{
	float luminance = Luminance(color);

	return (color - luminance) * _ColorAdjustmentVector.w + luminance;
}

// combine together
float3 ColorGrading(float3 color)
{
	color = LimitBrightness(color);
	color = ApplyPostExposure(color);
	color = max(ApplyContrast(color), 0.0); // eliminate negative
	color = ApplyColorFilter(color);
	color = ApplyHueShift(color);
	color = ApplySaturation(color);

	return max(color, 0.0);
}

#endif