#ifndef FIRECREST_FORMAL_LIGHT_INCLUDED
#define FIRECREST_FORMAL_LIGHT_INCLUDED


#include "BRDF.hlsl"

TEXTURE2D(_ShadowRampMap);
SAMPLER(sampler_ShadowRampMap);

// Computes the scalar specular term for Minimalist CookTorrance BRDF
// NOTE: needs to be multiplied with reflectance f0, i.e. specular color to complete
half DirectBRDFSpecular(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
    float3 lightDirectionWSFloat3 = float3(lightDirectionWS);
    float3 halfDir = SafeNormalize(lightDirectionWSFloat3 + float3(viewDirectionWS));

    float NoH = saturate(dot(float3(normalWS), halfDir));
    half LoH = half(saturate(dot(lightDirectionWSFloat3, halfDir)));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // BRDFspec = (D * V * F) / 4.0
    // D = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2
    // V * F = 1.0 / ( LoH^2 * (roughness + 0.5) )
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155

    // Final BRDFspec = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2 * (LoH^2 * (roughness + 0.5) * 4.0)
    // We further optimize a few light invariant terms
    // brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

    // On platforms where half actually means something, the denominator has a risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
// #if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
//     specularTerm = specularTerm - HALF_MIN;
//     specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
// #endif

    return specularTerm;
}


float GetRampValue(float attenuation)
{
    if (attenuation < 0.4) return 1.0;
    else if (attenuation > 0.96) return 1.0;
    else
    {
        float forwardSS = smoothstep(0.4, 0.96, attenuation);
        float reversedSS = 1 - forwardSS;
        return forwardSS * reversedSS;
    }
}


half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, half lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff)
{
    float rampUV_U = clamp(lightAttenuation, 0.25, 0.99);
    float rampUV_V = 0.5;
    float3 shadowRampColor = SAMPLE_TEXTURE2D(_ShadowRampMap, sampler_ShadowRampMap, float2(rampUV_U, rampUV_V)).rgb;

    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = lightColor * (NdotL * lightAttenuation);
    radiance += UNITY_LIGHTMODEL_AMBIENT;

    half3 brdf = brdfData.diffuse;
#ifndef _SPECULARHIGHLIGHTS_OFF
    [branch] if (!specularHighlightsOff)
    {
        brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
    }
#endif

    return brdf * radiance;
}


half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat, AdvLight light, half3 normalWS, half3 viewDirectionWS, half clearCoatMask, bool specularHighlightsOff)
{
    return LightingPhysicallyBased(brdfData, brdfDataClearCoat, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, clearCoatMask, specularHighlightsOff);
}


half3 LightingPhysicallyBased(BRDFData brdfData, AdvLight light, half3 normalWS, half3 viewDirectionWS, bool specularHighlightsOff)
{
    const BRDFData noClearCoat = (BRDFData)0;
    return LightingPhysicallyBased(brdfData, noClearCoat, light, normalWS, viewDirectionWS, 0.0, specularHighlightsOff);
}

#endif