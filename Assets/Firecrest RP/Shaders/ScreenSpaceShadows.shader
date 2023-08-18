Shader "Firecrest RP/ScreenSpaceShadows"
{
    Properties
    {}
    SubShader
    {
        Tags{ "RenderType"="Opaque" "IgnoreProjector" = "True"}

        HLSLINCLUDE

        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/SpaceTransform.hlsl"
        #include "../ShaderLibrary/LitInput.hlsl"
        #include "DeferredShadow.hlsl"
        #include "ScreenSpaceShadows.hlsl"

        ENDHLSL

        Pass
        {
            Name "ScreenSpaceShadows"
            Tags {"LightMode" = "ScreenSpaceShadows"}

            ZTest Always
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            
            // the following keywords are enabled by default in this RP
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            //#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile _ _SOFTSHADOW_PCF7

            #pragma vertex   SSSVertex
            #pragma fragment SSSFragment

            ENDHLSL
        }
    }
}
