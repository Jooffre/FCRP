#ifndef FIRECREST_UNITY_INPUT_INCLUDED
#define FIRECREST_UNITY_INPUT_INCLUDED

// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt
float4 _TimeParameters; // t, sin(t), cos(t)

CBUFFER_START(UnityPerDraw)

  float4x4 unity_ObjectToWorld;
  float4x4 unity_WorldToObject;
  float4 unity_LODFade;
  
  real4 unity_WorldTransformParams;

  real4 unity_LightData; // contains the amount of lights in its Y component
	real4 unity_LightIndices[2]; // 


  float4 unity_LightmapST;
  float4 unity_DynamicLightmapST;

  float4 unity_ProbesOcclusion;

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
float4x4 unity_MatrixInvP;
float4x4 unity_MatrixVP; // World --> HClip
float4x4 unity_MatrixInvVP;
float4x4 glstate_matrix_projection;

float3 _WorldSpaceCameraPos;

// for checking texture V coordinate start at the top or the bottom
float4 _ProjectionParams;

#define UNITY_LIGHTMODEL_AMBIENT float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w)

#endif