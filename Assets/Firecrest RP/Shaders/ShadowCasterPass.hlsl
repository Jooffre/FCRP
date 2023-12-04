#ifndef FIRECREST_SHADOW_CASTER_PASS_INCLUDED
#define FIRECREST_SHADOW_CASTER_PASS_INCLUDED


struct Attributes
{
    float3  positionOS      : POSITION;
    float2  uv              : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4  positionCS      : SV_POSITION;
    float2  uv              : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

bool _ShadowPancaking;

Varyings ShadowCasterVertex(Attributes input)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    if (_ShadowPancaking)
    {
        #if UNITY_REVERSED_Z
            output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
        #else
            output.positionCS.z =max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
        #endif
    }

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.uv = input.uv * baseST.xy + baseST.zw;

    return output;
}


void ShadowCasterFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);

    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 baseColor =  UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseMap * baseColor;

#ifdef _SHADOWS_CLIP

    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));

#elif defined(_SHADOWS_DITHER)

    // generate random noise carefully
    // the quality of semi-transparent shadow depends on your choice of noise

    // the following two methods still looked bad under PCF 7x7
    // 1. - float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    // 2. - float dither = rand(input.positionCS.xy);
    // clip(base.a - dither);

    float alphaRef = SAMPLE_TEXTURE3D(_DitherMaskLOD, sampler_DitherMaskLOD, float3(input.positionCS.xy * 0.25, base.a * 0.9375 ) ).a;
    clip( alphaRef - 0.01 );

#endif

}

#endif