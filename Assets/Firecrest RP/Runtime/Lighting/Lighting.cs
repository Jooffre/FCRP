using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;


namespace Firecrest
{

public class Lighting
{
    private const string bufferName = "Lighting";
    private const int maxDirLightCount = 4;

    private readonly Vector4[]
    dirLightsColor = new Vector4[maxDirLightCount],
    dirLightsDirection = new Vector4[maxDirLightCount],
    dirLightsShadowData = new Vector4[maxDirLightCount];

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


    private void SetupLights()
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

        buffer.SetGlobalInt(ShaderPropertyID.dirLightCountID, visibleLights.Length);
        buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsColorID, dirLightsColor);
        buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsDirectionID, dirLightsDirection);
        buffer.SetGlobalVectorArray(ShaderPropertyID.dirLightsShadowDataID, dirLightsShadowData);
    }


    private void SetupDirectionalLight(int idx, ref VisibleLight visibleLight)
    {
        dirLightsColor[idx] = visibleLight.finalColor;
        //Debug.Log("visibleLight.finalColor");
        dirLightsDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        // Vec4 (shadow strength, start index per light, normal bias, light index)
        dirLightsShadowData[idx] = shadows.ReserveDirectionalShadows(visibleLight.light, idx);
    }
}

}