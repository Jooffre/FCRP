Shader "Hidden/Firecrest RP/Copy With Depth"
{
    SubShader
    {
        HLSLINCLUDE

        #include "../ShaderLibrary/Common.hlsl"
        //#include "../ShaderLibrary/PostProcessing/FullScreen.hlsl"
        
        #pragma target 3.5

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        TEXTURE2D(_ScreenSoureImage);
        SAMPLER(sampler_ScreenSoureImage);


        struct Attributes
        {
        //#if _USE_DRAW_PROCEDURAL
        //    uint vertexID     : SV_VertexID;
        //#else
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
        //#endif
        //    UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
        //    UNITY_VERTEX_OUTPUT_STEREO
        };

        ENDHLSL
        
        Pass
        {
            Name "Copy Screen Texture"

            ZTest Always
            ZWrite On
            Cull Back

            HLSLPROGRAM

            Varyings CopyVert(Attributes input)
            {
                Varyings output;
            //    UNITY_SETUP_INSTANCE_ID(input);
                //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            //#if _USE_DRAW_PROCEDURAL
            //    output.positionCS = GetQuadVertexPosition(input.vertexID);
            //    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            //    output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
            //#else
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            //#if UNITY_UV_STARTS_AT_TOP 
            //    output.uv = float2(input.uv.x, 1 - input.uv.y);
            //#else
                output.uv = input.uv;
            //#endif
            //#endif

                return output;
            }


            float4 CopyFrag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 source = SAMPLE_TEXTURE2D(_ScreenSoureImage, sampler_ScreenSoureImage, uv).rgb;
                //depthOut = SAMPLE_TEXTURE2D(_QuadDepth, sampler_QuadDepth, input.uv).r;

                return float4(source, 1);
            }


            #pragma vertex CopyVert
            #pragma fragment CopyFrag

            ENDHLSL
        }

        Pass
        {
            Name "Copy Screen"

            ZTest Always
            ZWrite On
            Cull Back

            HLSLPROGRAM

            Varyings CopyVert(Attributes input)
            {
                Varyings output;
            //    UNITY_SETUP_INSTANCE_ID(input);
                //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            //#if _USE_DRAW_PROCEDURAL
            //    output.positionCS = GetQuadVertexPosition(input.vertexID);
            //    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            //    output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
            //#else
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            #if UNITY_UV_STARTS_AT_TOP 
                output.uv = float2(input.uv.x, 1 - input.uv.y);
            #else
                output.uv = input.uv;
            #endif
            //#endif

                return output;
            }


            float4 CopyFrag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                //depthOut = SAMPLE_TEXTURE2D(_QuadDepth, sampler_QuadDepth, input.uv).r;

                return float4(source, 1);
            }


            #pragma vertex CopyVert
            #pragma fragment CopyFrag

            ENDHLSL
        }
    }
}
