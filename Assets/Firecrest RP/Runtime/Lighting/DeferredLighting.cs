using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;


namespace Firecrest
{

public class DeferredLighting
{
    private readonly CommandBuffer m_buffer = new CommandBuffer();
    private const int maxDirLightCount = 4;

    private Vector4[]
    dirLightsColor = new Vector4[maxDirLightCount],
    dirLightsDirection = new Vector4[maxDirLightCount],
    dirLightsShadowData = new Vector4[maxDirLightCount];
    

    /// <summary>
    /// Deliver Unity light component's settings to shader properties.
    /// </summary>
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
    
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if(visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                if (dirLightCount >= maxDirLightCount)
                    break;
            }
        }

        m_buffer.SetGlobalInt(ShaderPropertyID.dirLightCountID, visibleLights.Length);
        m_buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsColorID, dirLightsColor);
        m_buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsDirectionID, dirLightsDirection);
        m_buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsShadowDataID, dirLightsShadowData);

        context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
    }


    private void SetupDirectionalLight(int idx, ref VisibleLight visibleLight)
    {
        dirLightsColor[idx] = visibleLight.finalColor;
        dirLightsDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        // Vec4 (shadow strength, bias, normal bias, near plane)
        dirLightsShadowData[idx] = new Vector4
        (visibleLight.light.shadowStrength, visibleLight.light.shadowBias, visibleLight.light.shadowNormalBias, visibleLight.light.shadowNearPlane);
    }
} 

}