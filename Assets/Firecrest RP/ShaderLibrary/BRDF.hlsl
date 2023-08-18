#ifndef FIRECREST_BRDF_INCLUDED
#define FIRECREST_BRDF_INCLUDED

#include "Light.hlsl"

#define MIN_REFLECTIVITY 0.04
#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)


struct BRDF
{
	float3	diffuse;
	float3	specular;
	float	roughness;
};

struct BRDFData
{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half reflectivity;
    half perceptualRoughness;
    half roughness;
    half roughness2;
    half grazingTerm;

    // We save some light invariant BRDF terms so we don't have to recompute
    // them in the light loop. Take a look at DirectBRDF function for detailed explaination.
    half normalizationTerm;     // roughness * 4.0 + 2.0
    half roughness2MinusOne;    // roughness^2 - 1.0
};



float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return (1 - metallic) * range;
}


BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
	BRDF brdf;
	
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    
    // premultiplied alpha
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }

    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);

    // P-Smoothness --> P-Roughness --> Roughness
	float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

	return brdf;
}


float ComputeSpecularStrength(Surface surface, BRDF brdf, Light light)
{
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normalWS, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}


float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
	return ComputeSpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}


float brdfdebug(Surface surface, BRDF brdf, Light light)
{
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normalWS, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / d2;
}


// ================


inline void InitializeBRDFDataDirect(half3 albedo, half3 diffuse, half3 specular, half reflectivity, half oneMinusReflectivity, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
    outBRDFData = (BRDFData) 0;
    outBRDFData.albedo = albedo;
    outBRDFData.diffuse = diffuse;
    outBRDFData.specular = specular;
    outBRDFData.reflectivity = reflectivity;

    outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
    outBRDFData.roughness           = max(PerceptualRoughnessToRoughness(outBRDFData.perceptualRoughness), HALF_MIN_SQRT);
    outBRDFData.roughness2          = max(outBRDFData.roughness * outBRDFData.roughness, HALF_MIN);
    outBRDFData.grazingTerm         = saturate(smoothness + reflectivity);
    outBRDFData.normalizationTerm   = outBRDFData.roughness * half(4.0) + half(2.0);
    outBRDFData.roughness2MinusOne  = outBRDFData.roughness2 - half(1.0);

#ifdef _ALPHAPREMULTIPLY_ON
    outBRDFData.diffuse *= alpha;
    alpha = alpha * oneMinusReflectivity + reflectivity; // NOTE: alpha modified and propagated up.
#endif
}


half MetallicFromReflectivity(half reflectivity)
{
    half oneMinusDielectricSpec = kDielectricSpec.a;
    return (reflectivity - kDielectricSpec.r) / oneMinusDielectricSpec;
}


BRDFData BRDFDataFromGbuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2)
{
    half3 albedo = gbuffer0.rgb;
    half3 specular = gbuffer2.rgb;
    //uint materialFlags = UnpackMaterialFlags(gbuffer0.a);
    uint materialFlags = 1;
    half smoothness = gbuffer2.a;

    BRDFData brdfData = (BRDFData)0;
    half alpha = half(1.0); // NOTE: alpha can get modfied, forward writes it out (_ALPHAPREMULTIPLY_ON).

    half3 brdfDiffuse;
    half3 brdfSpecular;
    half reflectivity;
    half oneMinusReflectivity;

    // if ((materialFlags & kMaterialFlagSpecularSetup) != 0)
    // {
    //     // Specular setup
    //     reflectivity = ReflectivitySpecular(specular);
    //     oneMinusReflectivity = half(1.0) - reflectivity;
    //     brdfDiffuse = albedo * (half3(1.0h, 1.0h, 1.0h) - specular);
    //     brdfSpecular = specular;
    // }
    // else
    // {
        // Metallic setup
        reflectivity = specular.r;
        oneMinusReflectivity = 1.0 - 0.2;
        half metallic = MetallicFromReflectivity(reflectivity);
        brdfDiffuse = albedo * oneMinusReflectivity;
        brdfSpecular = lerp(kDielectricSpec.rgb, albedo, metallic);
    // }
    InitializeBRDFDataDirect(albedo, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, smoothness, alpha, brdfData);

    return brdfData;
}


struct InputData
{
    float3  positionWS;
    half3   normalWS;
    half3   viewDirectionWS;
};


float3 GetCurrentViewPosition()
{
    // This is a generic solution.
    // However, for the primary camera, using '_WorldSpaceCameraPos' is better for cache locality,
    // and in case we enable camera-relative rendering, we can statically set the position is 0.
    return UNITY_MATRIX_I_V._14_24_34;
}

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
    return UNITY_MATRIX_P[3][3] == 0;
}

float3 GetViewForwardDir()
{
    float4x4 viewMat = UNITY_MATRIX_V;
    return -viewMat[3].xyz; // [2]
}

float3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        float3 V = GetCurrentViewPosition() - positionWS;
        return normalize(V);
    }
    else
    {
        // Orthographic
        return -GetViewForwardDir();
    }
}


InputData InputDataFromGbufferAndWorldPosition(half4 gbuffer, float3 positionWS)
{
    InputData inputData = (InputData)0;

    inputData.positionWS = positionWS;
    inputData.normalWS = normalize(gbuffer.xyz);

    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS.xyz);

    return inputData;
}

#endif