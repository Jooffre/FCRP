#ifndef FIRECREST_SURFACE_INCLUDED
#define FIRECREST_SURFACE_INCLUDED

struct Surface
{
	float3		color;
	float3		position;
	float		metallic;
	float		smoothness;
	float3		normalWS;
	float3		viewDirection;
	float		depth;
	float		alpha;
	float		dither;
};


struct SurfaceData
{
    float3      color;
    float3      specular;
    float       metallic;
    float       smoothness;
    float3      normalWS;
    //float3      emission;
    //float       occlusion;
    float       alpha;
    //float       clearCoatMask;
    //float       clearCoatSmoothness;
};

#endif