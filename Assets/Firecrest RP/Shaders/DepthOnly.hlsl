#ifndef CUSTOM_DEPTH_ONLY_INCLUDED
#define CUSTOM_DEPTH_ONLY_INCLUDED

struct Attributes
{
    float3  positionOS       : POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4  positionCS       : SV_POSITION;
    float2  depth            : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    
    output.depth = output.positionCS.zw;

    return output;
}

inline float4 EncodeFloatRGBA(float v)
{
	float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 160581375.0);
	float kEncodeBit = 1.0/255.0;
	float4 enc = kEncodeMul * v;
	enc = frac (enc);
	enc -= enc.yzww * kEncodeBit;
	return enc;
}

float4 DepthOnlyFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    float depth = input.depth.x / input.depth.y;

#if defined (UNITY_REVERSED_Z)
        depth = 1.0 - depth;
#endif

    float4 color = EncodeFloatRGBA(depth);

    return color;
}

#endif