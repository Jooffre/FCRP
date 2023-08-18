#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED


TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);

//CBUFFER_START(UnityPerMaterial)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
//CBUFFER_END

#endif