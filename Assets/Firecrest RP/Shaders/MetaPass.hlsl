#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED


#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Lighting/ComputeLight.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"


// use for control the data produced by meta pass
bool4 unity_MetaFragmentControl;

float unity_OneOverOutputBoost;
float unity_MaxOutputValue;


struct Attributes
{
	float3 positionOS   : POSITION;
	float2 uv           : TEXCOORD0;
	float2 lightMapUV	: TEXCOORD1;
};

struct Varyings
{
	float4 positionCS   : SV_POSITION;
	float2 uv           : TEXCOORD0;
};


Varyings MetaVertex (Attributes input)
{
	Varyings output;
	
	// here position doesn't represent the vetex position in object/clip space,
	// it is in UV coord instead
	input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;

	output.positionCS = TransformWorldToHClip(input.positionOS);
	output.uv = TransformUV(input.uv);

	return output;
}


float4 MetaFragment (Varyings input) : SV_TARGET
{
	float4 base = GetBaseColor(input.uv);

	Surface surface;
	
    ZERO_INITIALIZE(Surface, surface);
	
    surface.color = base.rgb;
	surface.metallic = GetMetallic(input.uv);
	surface.smoothness = GetSmoothness(input.uv);
	BRDF brdf = GetBRDF(surface);
	float4 meta = 0.0;
	if (unity_MetaFragmentControl.x)
	{
		meta = float4(brdf.diffuse, 1.0);
		meta.rgb += brdf.specular * brdf.roughness * 0.5;
		meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
	}
	else if (unity_MetaFragmentControl.y)
	{
		meta = float4(GetEmission(input.uv), 1.0);
	}
	
    return meta;
}

#endif