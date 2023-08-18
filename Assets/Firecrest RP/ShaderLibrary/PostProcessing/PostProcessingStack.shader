Shader "Hidden/Firecrest RP/Post Processing Stack"
{
    HLSLINCLUDE
    
    #pragma multi_compile _ _USE_DRAW_PROCEDURAL
    #pragma multi_compile_local_fragment _ _COLORADJUSTMENT
    #pragma multi_compile_local_fragment _ _TOMAPPING_NATURAL _TOMAPPING_ACES _TOMAPPING_REINHARD
    
    #include "../../ShaderLibrary/Common.hlsl"
    #include "PPInputData.hlsl"
    #include "ColorAdjustment.hlsl"
    #include "ToneMapping.hlsl"
    #include "FullScreen.hlsl"


    float4 PostProcessingStackFrag(Varyings input) : SV_Target
    {
        float2 screen_uv = input.uv;

        float3 ScreenImage = GetBloomTexture(screen_uv).rgb;        

//#if defined(_POSTPROCECESSING)
        // color adjust
        #if defined(_COLORADJUSTMENT)
            ScreenImage = ColorGrading(ScreenImage);
        #endif
        // tone mapping
        #if defined(_TOMAPPING_NATURAL)
            ScreenImage = ToneMapping_Natural(ScreenImage);
        #elif defined(_TOMAPPING_ACES)
            ScreenImage = ToneMapping_ACES(ScreenImage);
        #elif defined(_TOMAPPING_REINHARD)
            ScreenImage = ToneMapping_Reinhard(ScreenImage);
        #else
            ScreenImage = ScreenImage;
        #endif
//#endif

        return float4(ScreenImage, 1);
    }

    ENDHLSL
    
    SubShader
    {
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Firecrest Post Processing"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment PostProcessingStackFrag
            
            ENDHLSL
        }
    }
}
