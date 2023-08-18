#ifndef FIRECREST_SCREENSPACE_SHADOWS_INCLUDED
#define FIRECREST_SCREENSPACE_SHADOWS_INCLUDED


struct Attribute
{
//#if _USE_DRAW_PROCEDURAL
//    uint vertexID     : SV_VertexID;
//#else
    float4 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
//#endif
    //UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varying
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;

    //UNITY_VERTEX_OUTPUT_STEREO
};



Varying SSSVertex(Attribute input)
{
    Varying output;
    //UNITY_SETUP_INSTANCE_ID(input);
    //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

//#if _USE_DRAW_PROCEDURAL
//    output.positionCS = GetQuadVertexPosition(input.vertexID);
//    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
//    output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
//#else
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    //output.uv = TRANSFORM_TEX(input.uv, _CameraDepthTexture);
    output.uv = input.uv;
//#endif

    return output;
}

float4 SSSFragment(Varying input) : SV_Target
{
    float cameraDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy);

    float3 positionWS = ComputeWorldSpacePosition(input.uv.xy, cameraDepth, unity_MatrixInvVP);
    //float3 positionWS = ReconstructPositionWS(uv, cameraDepth);

    float4 shadowCoords = TransformWorldToShadowCoord(positionWS);
    //half idx = VarifyCascadeIdx(positionWS);

    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
    half4 shadowParams = GetMainLightShadowParams();

    float shadow = SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture, shadowCoords.xyz).x;
    
    return float4(SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowCoords, shadowSamplingData, shadowParams, false).xxx, 1);
    //return float4(shadow.xxx,1);
    //return float4(idx.xxx,1);
}

#endif