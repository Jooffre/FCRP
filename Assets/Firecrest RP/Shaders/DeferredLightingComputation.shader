Shader "Firecrest RP/Deferred Lighting Computation"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name "Deferred Lighting Computation"
            
            ZTest Always
            ZWrite On
            Cull Back
            
            HLSLPROGRAM

            #pragma target 3.5

            #pragma shader_feature _USE_DRAW_PROCEDURAL
            #pragma shader_feature _RECEIVE_SHADOWS
        
            #pragma vertex DeferredLightingVertex
            //#pragma vertex FullscreenVert
            #pragma fragment DeferredLightingFragment

            #include "DeferredLightingComputation.hlsl"

            ENDHLSL
        }
    }
}
