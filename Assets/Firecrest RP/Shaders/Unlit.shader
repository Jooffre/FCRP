Shader "Custom RP/Unlit"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _BaseMap("Texture", 2D) = "white" {}
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend Source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend Destination", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }

     SubShader
    {
        HLSLINCLUDE
		
        #include "../ShaderLibrary/Common.hlsl"
		#include "../ShaderLibrary/UnlitInput.hlsl"
		
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags {"LightMode" = "SRPDefaultUnlit"}

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM

            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma shader_feature _CLIPPING

            #pragma vertex vert
            #pragma fragment frag

            #include "UnlitForward.hlsl"

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
    }
    //CustomEditor "CustomShaderGUI"
}