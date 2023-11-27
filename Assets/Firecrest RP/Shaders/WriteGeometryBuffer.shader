Shader "Firecrest RP/Write Geometry Buffer"
{
    Properties
    {
        // basic
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
        _BaseMap("Texture", 2D) = "white" {}

        [KeywordEnum(Metallic, Specular)] _Workflow ("Workflow", Float) = 0
        _Metallic("Metaliic", Range(0.0, 1.0)) = 0.0
        _Specular("Specular", Color) = (1.0, 1.0, 1.0)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothMap("Smoothness Map", 2D) = "white" {}
        _WorkflowMap("Workflow Map", 2D) = "white" {}
        [Toggle(_EMISSION)] _EnableEmission("Enable Emission", Float) = 0
        [HDR] _Emission("Emission", Color) = (0, 0, 0, 0)
        _EmissionMap("Emission Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType" = "sphere"}

        HLSLINCLUDE

        #include "../ShaderLibrary/Core/Common.hlsl"
        #include "../ShaderLibrary/LitInput.hlsl"

        ENDHLSL

        Pass
        {
            Name "Write G-Buffer"
            Tags {"LightMode" = "GBuffer"}

            HLSLPROGRAM

            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma vertex GBufferVertex
            #pragma fragment SurfaceToGBuffer

            #include "../ShaderLibrary/LitInput.hlsl"
            #include "WriteGeometryBuffer.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Draw Shadow Atlas"
            Tags {"LightMode" = "ShadowCaster"}

            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5

            //#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            //#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER

            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "ShadowCasterPassDeferred.hlsl"

            ENDHLSL
        }
    }
}