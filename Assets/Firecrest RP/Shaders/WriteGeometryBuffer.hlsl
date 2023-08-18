#ifndef FIRECREST_WRITE_GBUFFER_INCLUDED
#define FIRECREST_WRITE_GBUFFER_INCLUDED


#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/GeometryBuffer.hlsl"

struct Attributes
{
    float3  positionOS       : POSITION;
    float3  normalOS         : NORMAL;
    float2  uv               : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4  positionCS       : SV_POSITION;
    float3  normalWS         : NORMAL;
    float2  uv               : TEXCOORD0;
    float3  positionWS       : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings GBufferVertex(Attributes input)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);

    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    output.uv = TransformUV(input.uv);

    return output;
}


GBuffer SurfaceToGBuffer(Varyings input)
{
    GBuffer output;

    UNITY_SETUP_INSTANCE_ID(input);

    float4 baseColor = GetBaseColor(input.uv);
    //float3 packedNormalWS = PackNormal(inputData.normalWS);

    // Surface surface;
    // surface.color = baseColor.rgb;
    // surface.position = input.positionWS;
    // surface.metallic = GetMetallic(input.uv);
    // surface.smoothness = GetSmoothness(input.uv);
    // surface.normalWS = input.normalWS;
    // surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    // surface.depth = -TransformWorldToView(input.positionWS).z;
    // surface.alpha = baseColor.a;
    // surface.dither = 0;

    SurfaceData surface;

    surface.color = baseColor.rgb;
    surface.specular = GetSpecularColor().rgb;
    surface.metallic = 0.0;
    surface.smoothness = GetSmoothness(input.uv);
    surface.normalWS = input.normalWS; // can be packed later
    surface.alpha = 1.0;

    return EncodeSurfaceToGBuffer(surface);
}


#endif