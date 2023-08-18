#ifndef FIRECREST_UNITY_INPUT_INCLUDED
#define FIRECREST_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)

  float4x4 unity_ObjectToWorld;
  float4x4 unity_WorldToObject;
  float4 unity_LODFade;

  real4 unity_WorldTransformParams;

  float4 unity_LightmapST;
  float4 unity_DynamicLightmapST;

  // sampling light probes
  float4 unity_SHAr;
  float4 unity_SHAg;
  float4 unity_SHAb;
  float4 unity_SHBr;
  float4 unity_SHBg;
  float4 unity_SHBb;
  float4 unity_SHC;

  // sampling LPPV
  float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;

CBUFFER_END

float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixVP; // World --> HClip
float4x4 unity_MatrixInvVP;
float4x4 glstate_matrix_projection;

float3 _WorldSpaceCameraPos;

// for checking texture V coordinate start at the top or the bottom
float4 _ProjectionParams;

#endif