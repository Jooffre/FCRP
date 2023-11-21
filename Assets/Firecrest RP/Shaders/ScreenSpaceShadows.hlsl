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

float SSSFragment(Varying input) : SV_Target
{
    float softShadows = 0.0;

    float cameraDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy);

    float3 positionWS = ComputeWorldSpacePosition(input.uv.xy, cameraDepth, unity_MatrixInvVP);
    // float3 positionWS = ReconstructPositionWS(uv, cameraDepth);

    int cascadeIndex = ComputeCascadeIndex(positionWS);

    float4 shadowCoords = TransformWorldToShadowCoord(positionWS, cascadeIndex);
    // half idx = VarifyCascadeIdx(positionWS);

    float shadow = SAMPLE_TEXTURE2D(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture, shadowCoords.xy).x;

    float random = Random1D(positionWS.x + positionWS.y);
    float2 shadowAtlasCoord = shadowCoords.xy * 2047;

    // float shadowTest = ShadowEarlyTest(shadowCoords.z, shadowAtlasCoord, cascadeIndex);
    
    // if (shadowTest == 9)
    // {
    //     return 1.0;
        
    // }else if (shadowTest == 0)
    // {
    //     softShadows = 0.25;
    //     return softShadows;
    // }else
    // {
        float3 blocker = PCSS_GetBlockerDepth(positionWS, shadowCoords.z, shadowAtlasCoord, cascadeIndex);

        float blockerDepthLS = blocker.x;
        float blockerCount = blocker.y;

        if (blockerCount < 1.0)
        {
            softShadows = 1.0;
        }else
        {
            float penumbra = GetPenumbra(shadowCoords.z, blockerDepthLS, cascadeIndex);
            float penumbraRange = GetPenumbraRange(penumbra, blocker.z, cascadeIndex);

            softShadows = PCF_PoisonSamplingX64(shadowCoords.z + penumbraRange, shadowAtlasCoord, penumbra * 5, random);
        }

        return softShadows;
    // }


    // ===================================================================================================================

    // built-in PCF sampling

    //ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
    //half4 shadowParams = GetMainLightShadowParams();
    //return SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowCoords, shadowSamplingData, shadowParams, false);
}

#endif