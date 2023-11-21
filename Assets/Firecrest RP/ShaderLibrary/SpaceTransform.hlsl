#ifndef FIRECREST_SPACE_TRANSFORM_INCLUDED
#define FIRECREST_SPACE_TRANSFORM_INCLUDED


//float4      _ZBufferParams;

//float4x4    _CameraMatrixVPInv;


float3 TransformPositionCSToWS(float4 positionCS)
{
    //float4 positionWS = mul(_CameraMatrixVPInv, float4(positionCS, 1));
    float4 positionWS = mul(unity_MatrixInvVP, positionCS);
    
    return positionWS.xyz / positionWS.w;;
}

float3 ReconstructPositionWS(float2 uv, float depth)
{
    float4 positionCS = float4(uv * 2 - 1, depth, 1);
    float3 positionWS = TransformPositionCSToWS(positionCS);
	
    return positionWS;
}


#endif