#ifndef CUSTOM_LIT_FORWARD_INCLUDED
#define CUSTOM_LIT_FORWARD_INCLUDED


#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"


struct Attributes
{
    float3 positionOS       : POSITION;
    float3 normalOS         : NORMAL;
    float2 uv               : TEXCOORD0;
    
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS       : SV_POSITION;
    float3 normalWS         : NORMAL;
    float2 uv               : TEXCOORD0;
    float3 positionWS       : TEXCOORD1;
    
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings ForwardVertex(Attributes input)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);

    output.uv = TransformUV(input.uv);

    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);

    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    return output;
}


half4 ForwardFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseColor = GetBaseColor(input.uv);

#ifdef _CLIPPING
    clip(baseColor.a - GetCutoff(input.uv));
#endif

    // initialize surface data
    Surface surface;
    surface.position = input.positionWS;
	surface.normalWS = normalize(input.normalWS);
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.color = baseColor.rgb;
	surface.alpha = baseColor.a;
    surface.metallic = GetMetallic(input.uv);
    surface.smoothness = GetSmoothness(input.uv);
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

#ifdef _PREMULTIPLY_ALPHA
		BRDF brdf = GetBRDF(surface, true);
#else
		BRDF brdf = GetBRDF(surface);
#endif

    // global illumination
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface);

    float3 color = GetLighting(surface, brdf, gi);
    
#ifdef _EMISSION
     color += GetEmission(input.uv);
#endif

    return float4(color, surface.alpha);
}

#endif