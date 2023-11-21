Shader "Hidden/Firecrest RP/Post Processing Stack"
{
    HLSLINCLUDE
    
    #include "../../ShaderLibrary/Common.hlsl"
    #include "PPInputData.hlsl"
    #include "ColorAdjustment.hlsl"
    #include "ToneMapping.hlsl"
    #include "FullScreen.hlsl"
    #include "SimpleFXAA.hlsl"

    float4 _SourceSize;

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

        return float4(ScreenImage, 1.0);
    }


    float4 FXAAFrag(Varyings input) : SV_Target
    {
        float2 screen_uv = input.uv;
        int2 positionSS  = screen_uv * _SourceSize.xy;

        float3 ScreenImage = GetSource(screen_uv).rgb;

        return float4(ApplyFXAA(ScreenImage, screen_uv, positionSS, _SourceSize, _ScreenSoureImage), 1.0);
    }

    ENDHLSL
    
    SubShader
    {
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Firecrest Post Processing"

            HLSLPROGRAM
                
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile_local_fragment _ _COLORADJUSTMENT
            #pragma multi_compile_local_fragment _ _TOMAPPING_NATURAL _TOMAPPING_ACES _TOMAPPING_REINHARD

            #pragma vertex FullscreenVert
            #pragma fragment PostProcessingStackFrag
            
            ENDHLSL
        }

        Pass
        {
            Name "Simple FXAA"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment FXAAFrag
            
            ENDHLSL
        }
    }
}
