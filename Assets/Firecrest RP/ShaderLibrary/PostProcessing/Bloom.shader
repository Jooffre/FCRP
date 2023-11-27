Shader "Hidden/Firecrest RP/Bloom"
{
	// a template shader for PP
	// just returns the original screen frame
	// like copy and paste
	SubShader
    {
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE

		#include "../../ShaderLibrary/Core/Common.hlsl"
		#include "Bloom.hlsl"
		
        ENDHLSL

		Pass // 0
        {
			Name "BlurHorizontal"
			
			HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment DownSampleHorizontalPassFragment

			ENDHLSL
		}

		Pass // 1
        {
			Name "BlurVertical"
			
			HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment DownSampleVerticalPassFragment

			ENDHLSL
		}

		Pass // 2
        {
			Name "AdditiveaBlend"
			
			HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomAdditiveCombinePassFragment

			ENDHLSL
		}

		Pass // 3
        {
			Name "ScatterBlend"
			
			HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterCombinePassFragment

			ENDHLSL
		}

		Pass // 4
        {
			Name "ThresholdFilter"
			
			HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomThresholdFilterPassFragment

			ENDHLSL
		}

		Pass // 5
        {
			Name "FadeFireflies"
			
			HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterFirefliesPassFragment

			ENDHLSL
		}
	}
}