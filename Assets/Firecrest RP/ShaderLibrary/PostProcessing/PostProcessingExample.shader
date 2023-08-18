Shader "Hidden/Custom RP/Post Processing Example"
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

		#include "../../ShaderLibrary/Common.hlsl"
		#include "PostProcessingExample.hlsl"
		 
        ENDHLSL

		Pass // pass 0
        {
			Name "Copy"
			
			HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment

			ENDHLSL
		}

		// pass 1, 2, etc...
	}
}