Shader "Hidden/Firecrest RP/Gaussian Blur"
{
    Properties 
    {
        _MainTex ("Base", 2D) = "white" {}
    }
    SubShader 
    {
        ZTest Always
        ZWrite Off
        Cull Off

        HLSLINCLUDE

        #include "Core/Common.hlsl"

        #define blurSize 1
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);  
        half4 _MainTex_TexelSize;

        struct Attributes
        {
            float4 positionOS       : POSITION;
            float2 uv               : TEXCOORD0;
        };
        struct Varyings 
        {
            float4 positionCS       : SV_POSITION;
            float2 uv[5]            : TEXCOORD0;
        };
        
        Varyings vertBlurVertical(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            
            half2 uv = input.uv;
            
            output.uv[0] = uv;
            output.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * blurSize;
            output.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * blurSize;
            output.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * blurSize;
            output.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * blurSize;

            return output;
        }
        
        Varyings vertBlurHorizontal(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            
            half2 uv = input.uv;
            
            output.uv[0] = uv;
            output.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * blurSize;
            output.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * blurSize;
            output.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * blurSize;
            output.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * blurSize;
                     
            return output;
        }
        

        half4 fragBlur(Varyings input) : SV_Target
        {
            float weight[3] = {0.4026, 0.2442, 0.0545};
            
            half3 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv[0]).rgb * weight[0];
            
            for (int i = 1; i < 3; i++)
            {
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv[i * 2 - 1]).rgb * weight[i];
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv[i * 2]).rgb * weight[i];
            }
            
            return half4(sum, 1.0);
        }
            
        ENDHLSL
        
        Pass
        {
            HLSLPROGRAM
              
            #pragma vertex vertBlurVertical  
            #pragma fragment fragBlur
              
            ENDHLSL  
        }
        
        Pass
        {
            HLSLPROGRAM  
            
            #pragma vertex vertBlurHorizontal  
            #pragma fragment fragBlur
            
            ENDHLSL
        }
    } 
}