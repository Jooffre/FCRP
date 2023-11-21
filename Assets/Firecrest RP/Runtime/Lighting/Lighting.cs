using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;


namespace Firecrest
{

// at most 4 directional lights, 64 alternative lights supported
public class Lighting
{
    private const string bufferName = "Lighting";

    private const int
    maxDirLightCount = 4,
    maxOptionalLightCount = 64;

    private readonly Vector4[]
    dirLightsColor = new Vector4[maxDirLightCount],
    dirLightsDirection = new Vector4[maxDirLightCount],
    dirLightsShadowData = new Vector4[maxDirLightCount],

    optionalLightColor = new Vector4[maxOptionalLightCount],
    optionalLightDirection = new Vector4[maxOptionalLightCount],
    optionalLightPosition = new Vector4[maxOptionalLightCount],
    optionalSpotLightAngle = new Vector4[maxOptionalLightCount];

    private CullingResults cullingResults;

    private readonly CommandBuffer buffer = new CommandBuffer()
    {name = bufferName};

    private readonly ForwardShadows shadows = new ForwardShadows();


    public void Setup
    (ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        
        buffer.BeginSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        shadows.Render();

        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        Cleanup();
    }

    
    /// <summary> 
    /// Release the shadow atlas RT.
    /// </summary> 
    public void Cleanup()
    {
        shadows.Cleanup();
    }


    /// <summary>
    /// Initialize light data and send to GPU.
    /// </summary>
    private void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        // use a loop traversing light sequence and initialize the data
        int dirLightCount = 0, optionalLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            
            // if(visibleLight.lightType == LightType.Directional)
            // {
            //    SetupDirectionalLight(dirLightCount++, ref visibleLight);
            //    if (dirLightCount >= maxDirLightCount)
            //        break;
            // }

            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                        SetupDirectionalLight(dirLightCount++, ref visibleLight);
                break;

                case LightType.Point:
                    if (optionalLightCount < maxOptionalLightCount)
                        SetupPointLight(optionalLightCount++, ref visibleLight);
                break;

                case LightType.Spot:
                    if (optionalLightCount < maxOptionalLightCount)
                        SetupSpotLight(optionalLightCount++, ref visibleLight);
                break;
            }
        }

        // send light data to GPU
        buffer.SetGlobalInt(ShaderPropertyID.dirLightCountID, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsColorID, dirLightsColor);
            buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsDirectionID, dirLightsDirection);
            buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsShadowDataID, dirLightsShadowData);
        }

        buffer.SetGlobalInt(ShaderPropertyID.optionalLightCountID, optionalLightCount);
        if (optionalLightCount > 0)
        {
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalLightColorID, optionalLightColor);
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalLightDirectionID, optionalLightDirection);
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalLightPositionID, optionalLightPosition);
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalSpotLightAngleID, optionalSpotLightAngle);
        }
    }


    #region Set up each light category
    private void SetupDirectionalLight(int idx, ref VisibleLight visibleLight)
    {
        dirLightsColor[idx] = visibleLight.finalColor;

        dirLightsDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        // Vec4 (shadow strength, start index per light, normal bias, light index)
        dirLightsShadowData[idx] = shadows.ReserveDirectionalShadows(visibleLight.light, idx);
    }


    private void SetupPointLight(int idx, ref VisibleLight visibleLight)
    {
        optionalLightColor[idx] = visibleLight.finalColor;
        
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        optionalLightPosition[idx] = position;

        optionalSpotLightAngle[idx] = new Vector4(0f, 1f);
    }


    private void SetupSpotLight(int idx, ref VisibleLight visibleLight)
    {
        optionalLightColor[idx] = visibleLight.finalColor;

        optionalLightDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        optionalLightPosition[idx] = position;

        optionalSpotLightAngle[idx] = ConfigSpotLightAngle(ref visibleLight);
    }

    private Vector4 ConfigSpotLightAngle(ref VisibleLight visibleLight)
    {
        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);

        return new Vector4(angleRangeInv, -outerCos * angleRangeInv);
    }

    #endregion
}

}