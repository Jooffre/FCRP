#ifndef CUSTOM_PP_DEFAULT_VERTEX_INCLUDED
#define CUSTOM_PP_DEFAULT_VERTEX_INCLUDED


struct Varyings
{
	float4 positionCS   : SV_POSITION;
	float2 screenUV     : TEXCOORD0;
};


Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
	Varyings output;
	output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
	output.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);

    // in case the texture turns upside down
    if (_ProjectionParams.x < 0.0)
    {
		output.screenUV.y = 1.0 - output.screenUV.y;
	}
	return output;
}

#endif