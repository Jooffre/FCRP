#ifndef FIRECREST_LIT_INPUT_INCLUDED
#define FIRECREST_LIT_INPUT_INCLUDED


TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);
TEXTURE3D(_DitherMaskLOD);  SAMPLER(sampler_DitherMaskLOD);

//CBUFFER_START(UnityPerMaterial)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

    //float4 _BaseColor;
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float3, _Specular)
    UNITY_DEFINE_INSTANCED_PROP(float4, _CameraDepthTexture_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Emission)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
//CBUFFER_END


// --------------------------------------------------

float2 TransformUV(float2 uv)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    return uv * baseST.xy + baseST.zw;
}

float4 GetBaseColor(float2 uv)
{
    float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
	float4 tint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	return tex * tint;
}

float3 GetSpecularColor()
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Specular);
}

float GetCutoff (float2 uv) 
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic (float2 uv)
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
}

float GetSmoothness (float2 uv)
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
}

float3 GetEmission(float2 uv)
{
    float4 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, uv);
    float4 emissionColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Emission);
    return emissionMap.rgb * emissionColor.rgb;
}

#endif