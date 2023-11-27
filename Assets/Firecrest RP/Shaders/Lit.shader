Shader "Firecrest RP/Lit"
{
    Properties
    {
        // basic
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
        _BaseMap("Texture", 2D) = "white" {}
        
        [KeywordEnum(Metallic, Specular)] _Workflow ("Workflow", Float) = 0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Specular("Specular", Color) = (1.0, 1.0, 1.0)
		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothMap("Smoothness Map", 2D) = "white" {}
        _WorkflowMap("Workflow Map", 2D) = "white" {}
        [Toggle(_EMISSION)] _EnableEmission("Enable Emission", Float) = 0
        [HDR] _Emission("Emission", Color) = (0, 0, 0, 0)
        _EmissionMap("Emission Map", 2D) = "white" {}

        // alpha
        [Enum(Both, 0, Back, 1, Front, 2)] _RenderFace("Render Face", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // blend
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend Source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend Destination", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
        
        // shadow
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1

        // hidden
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)

    }

    SubShader
    {
        HLSLINCLUDE

		#include "../ShaderLibrary/Core/Common.hlsl"
		#include "../ShaderLibrary/LitInput.hlsl"
		
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags {"LightMode" = "ForwardLit"}

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM

            #pragma target 3.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            #pragma multi_compile _ _SHADOW_MASK_DEFAULT _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7

            #pragma shader_feature _ _WORKFLOW_SPECULAR
            #pragma shader_feature _EMISSION
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _RECEIVE_SHADOWS

            #pragma vertex ForwardVertex
            #pragma fragment ForwardFragment

            #include "LitForward.hlsl"

            ENDHLSL
        }


        Pass
        {
            Name "ShadowCaster"
            Tags
            { "LightMode" = "ShadowCaster" }

            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5

            //#pragma shader_feature _CLIPPING
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER

            #pragma multi_compile_instancing

            #pragma vertex ShadowCasterVertex
            #pragma fragment ShadowCasterFragment

            #include "ShadowCasterPass.hlsl"

            ENDHLSL
        }


        Pass
        {
            Name "Meta"
			Tags {"LightMode" = "Meta"}

			Cull Off

			HLSLPROGRAM
			
            #pragma target 3.5
			#pragma vertex MetaVertex
			#pragma fragment MetaFragment

			#include "MetaPass.hlsl"
			
            ENDHLSL
		}
    }
    CustomEditor "Firecrest.CustomShaderGUI"
}