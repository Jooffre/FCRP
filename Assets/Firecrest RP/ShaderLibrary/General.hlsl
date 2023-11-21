// This file contains necessary methods used for computations in shader.

#ifndef FIRECREST_GENERAL_INCLUDED
#define FIRECREST_GENERAL_INCLUDED


float Square(float v)
{
	return v * v;
}


float GetDistanceSquared(float3 A, float3 B)
{
	return dot(A - B, A - B);
}


float rand(float2 coord)
{
    return saturate(frac(sin(dot(coord, float2(12.9898, 78.223))) * 43758.5453));
}


float Random1D(float x)
{
	return frac(sin(x + 0.546) * 143758.5964);
}

#endif