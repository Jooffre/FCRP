#ifndef FIRECREST_GEOMETRY_BUFFER_INCLUDED
#define FIRECREST_GEOMETRY_BUFFER_INCLUDED


struct GBuffer
{
    float4  GBLayer_0       : SV_Target0;
    float4  GBLayer_1       : SV_Target1;
    float4  GBLayer_2       : SV_Target2;
    float4  GBLayer_3       : SV_Target3;
};


TEXTURE2D(_ScreenSpaceShadowmapTexture);
SAMPLER(sampler_ScreenSpaceShadowmapTexture);

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

Texture2D   _GBLayer_0;
Texture2D   _GBLayer_1;
Texture2D   _GBLayer_2;
Texture2D   _GBLayer_3;

SamplerState sampler_pointer_clamp;


GBuffer EncodeSurfaceToGBuffer(SurfaceData surface)
{
    GBuffer output;
    
    output.GBLayer_0 = float4(surface.color, 1.0);
    output.GBLayer_1 = float4(surface.normalWS, surface.smoothness);
    output.GBLayer_2 = float4(surface.specular, 1.0); // specular
    output.GBLayer_3 = float4(surface.emission, 1.0);

    return output;
}


void DecodeGBufferToSurface(inout SurfaceData surface, float4 layer_0, float4 layer_1, float4 layer_2, float4 layer_3)
{
    surface.color = layer_0.rgb;
    surface.specular = layer_2.rgb;
    surface.metallic = 0;
    surface.smoothness = layer_1.w;
    surface.normalWS = layer_1.xyz;
    surface.emission = layer_3.xyz;
    surface.alpha = 1;
}

#endif