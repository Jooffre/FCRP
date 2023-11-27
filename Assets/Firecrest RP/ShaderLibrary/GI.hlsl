#ifndef FIRECREST_GI_INCLUDED
#define FIRECREST_GI_INCLUDED


// --------------------------------------------------
// use the lib to retrieve the light data
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"


// --------------------------------------------------
// define macros for GI

#ifdef LIGHTMAP_ON
	#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
	#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
	#define TRANSFER_GI_DATA(input, output) \
        output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input, output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif

 
// --------------------------------------------------
// define sampling of the light map and LPPV

TEXTURE2D(unity_Lightmap);    // the light map texture is named as "unity_Lightmap"
SAMPLER(samplerunity_Lightmap);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

// --------------------------------------------------
// shadow mask can be also considered as a part of the baked lighting data of the scene
// so we add this field to the struct GI
struct GI
{
    float3      diffuse;
    ShadowMask  shadowMask;
};


// --------------------------------------------------


// sample the light map
float3 SampleLightMap(float2 lightMapUV)
{
#ifdef LIGHTMAP_ON
    // the method from EntityLighting.hlsl
    return SampleSingleLightmap(
        TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
        lightMapUV,
        float4(1.0, 1.0, 0.0, 0.0),
#ifdef UNITY_LIGHTMAP_FULL_HDR
        false,
#else
        true,
#endif
        float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
        );
#else
    return 0.0;
#endif
}


float4 SampleBakedShadows(float2 lightMapUV, Surface surfaceWS)
{
# if defined(LIGHTMAP_ON)
    return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, lightMapUV);
# else
    if (unity_ProbeVolumeParams.x)
    {
        return SampleProbeOcclusion
        (
            TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
            surfaceWS.position,
            unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y,
            unity_ProbeVolumeParams.z,
            unity_ProbeVolumeMin.xyz,
            unity_ProbeVolumeSizeInv.xyz
        );
    }
    else
        return unity_ProbesOcclusion;
# endif
}


float3 SampleLightProbe (Surface surfaceWS)
{
#if defined(LIGHTMAP_ON)
    return 0.0;
#else
    if (unity_ProbeVolumeParams.x)
    {
        return SampleProbeVolumeSH4(
            TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
            surfaceWS.position,
            surfaceWS.normalWS,
            unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y,
            unity_ProbeVolumeParams.z,
            unity_ProbeVolumeMin.xyz,
            unity_ProbeVolumeSizeInv.xyz
			);
	}
    else
    {
        float4 coefficients[7];
        coefficients[0] = unity_SHAr;
        coefficients[1] = unity_SHAg;
        coefficients[2] = unity_SHAb;
        coefficients[3] = unity_SHBr;
        coefficients[4] = unity_SHBg;
        coefficients[5] = unity_SHBb;
        coefficients[6] = unity_SHC;    
        
        return max(0.0, SampleSH9(coefficients, surfaceWS.normalWS));
    }
    
#endif
}


GI GetGI(float2 lightMapUV, Surface surfaceWS)
{
    GI gi;

    gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
    gi.shadowMask.always = false;
    gi.shadowMask.distance = false;
    gi.shadowMask.maskValue = 1.0;

# if defined(_SHADOW_MASK_DEFAULT)
    gi.shadowMask.always = true;
    gi.shadowMask.maskValue = SampleBakedShadows(lightMapUV, surfaceWS);
#elif defined(_SHADOW_MASK_DISTANCE)
    gi.shadowMask.distance = true;
    gi.shadowMask.maskValue = SampleBakedShadows(lightMapUV, surfaceWS);
# endif

    return gi;
}
#endif