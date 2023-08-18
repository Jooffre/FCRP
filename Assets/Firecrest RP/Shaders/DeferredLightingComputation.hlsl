#ifndef FIRECREST_DEFERRED_LIGHTING_COMPUTATION_INCLUDED
#define FIRECREST_DEFERRED_LIGHTING_COMPUTATION_INCLUDED


#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/SpaceTransform.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/GeometryBuffer.hlsl"
#include "../ShaderLibrary/FormalLighting.hlsl"

//#include "../ShaderLibrary/Postprocessing/FullScreen.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    //uint vertexID       : SV_VertexID;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 screenUV     : TEXCOORD0;
};


Varyings DeferredLightingVertex(Attributes input)
{
    Varyings output;

    float3 positionOS = input.positionOS.xyz;
    
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    //output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.screenUV = input.uv;
//    output.screenUV = output.positionCS.xyw;

//#if UNITY_UV_STARTS_AT_TOP
//    output.screenUV.xy = output.screenUV.xy * float2(0.5, -0.5) + 0.5 * output.screenUV.z;
//    output.screenUV = float2(input.uv.x, 1 - input.uv.y);
//#else
//    output.screenUV.xy = output.screenUV.xy * 0.5 + 0.5 * output.screenUV.z;
//#endif

    return output;
}


float4 DeferredLightingFragment(Varyings input, out float depthOut : SV_Depth) : SV_Target
{
    float2 screen_uv = input.screenUV;

    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screen_uv);
    depthOut = depth;

    float4 layer_0 =  _GBLayer_0.Sample(sampler_pointer_clamp, screen_uv);
    float4 layer_1 =  _GBLayer_1.Sample(sampler_pointer_clamp, screen_uv);
    float4 layer_2 =  _GBLayer_2.Sample(sampler_pointer_clamp, screen_uv);
    float4 layer_3 =  _GBLayer_3.Sample(sampler_pointer_clamp, screen_uv);
    
    float4 shadowCoord = float4(screen_uv, 0.0, 1.0);
    float screenSpaceShadow = SampleScreenSpaceShadowmap(shadowCoord);

    // build position from depth
    float3 positionWS = ReconstructPositionWS(screen_uv, depth);

    SurfaceData surface;

    DecodeGBufferToSurface(surface, layer_0, layer_1, layer_2);

    InputData inputData = InputDataFromGbufferAndWorldPosition(layer_1, positionWS);

    AdvLight advlight = GetStencilLight(screen_uv);
    BRDFData brdfData = BRDFDataFromGbuffer(layer_0, layer_1, layer_2);
    float3 advColor = LightingPhysicallyBased(brdfData, advlight, inputData.normalWS, inputData.viewDirectionWS, false);

    return float4(advColor, 1);
}

#endif