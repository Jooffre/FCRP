using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using PID = Firecrest.ShaderPropertyID;


namespace Firecrest
{

// at most 4 directional lights, 64 alternative lights supported
public class Lighting
{
    private const string bufferName = "Lighting";

    private static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";


    private const int
    maxDirLightCount = 4, maxOtherLightCount = 64;


    private readonly Vector4[]
    dirLightsColor = new Vector4[maxDirLightCount],
    dirLightsDirection = new Vector4[maxDirLightCount],
    dirLightsShadowData = new Vector4[maxDirLightCount],

    otherLightColor = new Vector4[maxOtherLightCount],
    otherLightPosition = new Vector4[maxOtherLightCount],
    spotLightDirection = new Vector4[maxOtherLightCount],
    spotLightAngles = new Vector4[maxOtherLightCount],
    otherLightShadowData = new Vector4[maxOtherLightCount];


    private CullingResults cullingResults;


    private readonly CommandBuffer buffer = new CommandBuffer()
    {name = bufferName};


    private readonly ForwardShadows shadows = new ForwardShadows();


    public void Setup
    (
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings shadowSettings,
        bool useLightsPerObject
    ){
        this.cullingResults = cullingResults;
        
        buffer.BeginSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights(useLightsPerObject);
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
        shadows.CleanupShadowAtlasRT();
    }


    /// <summary>
    /// Initialize light data and send to GPU.
    /// </summary>
    private void SetupLights(bool useLightsPerObject)
    {
        // we just keep track of POINT & SPOT lights, use a array to store the
        // indices of them, and use "-1" representing all other lights.
        NativeArray<int> lightIndexMap = useLightsPerObject?
            cullingResults.GetLightIndexMap(Allocator.Temp) : default;

        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        // use a loop traversing light sequence and initialize the data
        int dirLightCount = 0, otherLightCount = 0;
        int i;
        for (i = 0; i < visibleLights.Length; i++)
        {
            int tempIdx = -1;

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
                        SetupDirectionalLight(dirLightCount++, i, ref visibleLight);
                break;

                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        tempIdx = otherLightCount;
                        SetupPointLight(otherLightCount++, i, ref visibleLight);
                    }
                break;

                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        tempIdx = otherLightCount;
                        SetupSpotLight(otherLightCount++, i, ref visibleLight);
                    }
                break;
            }

            if (useLightsPerObject)
                lightIndexMap[i] = tempIdx;
        }

        // mark the indices of light unseen back to -1
        if (useLightsPerObject)
        {
            for(; i < lightIndexMap.Length; i++)
            {
                lightIndexMap[i] = -1;
            }

            cullingResults.SetLightIndexMap(lightIndexMap);
			lightIndexMap.Dispose();

            Shader.EnableKeyword(lightsPerObjectKeyword);
        } else
        {
            Shader.DisableKeyword(lightsPerObjectKeyword);
        }



        // send light data to GPU
        buffer.SetGlobalInt(PID.dirLightCountID, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(PID.dirLightsColorID, dirLightsColor);
            buffer.SetGlobalVectorArray(PID.dirLightsDirectionID, dirLightsDirection);
            buffer.SetGlobalVectorArray(PID.dirLightsShadowDataID, dirLightsShadowData);
        }

        buffer.SetGlobalInt(PID.otherLightCountID, otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(PID.otherLightColorID, otherLightColor);
            buffer.SetGlobalVectorArray(PID.otherLightPositionID, otherLightPosition);
            buffer.SetGlobalVectorArray(PID.spotLightDirID, spotLightDirection);
            buffer.SetGlobalVectorArray(PID.spotLightAngleID, spotLightAngles);
            buffer.SetGlobalVectorArray(PID.otherLightShadowDataID, otherLightShadowData);
        }
    }


    #region Setup each light category

    // For "localToWorldMatrix" :
    // Unity transform matrix following the order scale -> rotate -> translate,
    // and this gives a 4x4 matrix. The first 3x3 submatrix represents the
    // rotation matrix, while the last column means the translation. That can
    // be thought as the position after translation and thus equivalents to the
    // world space position.

    private void SetupDirectionalLight(int idx, int visibleLightIdx, ref VisibleLight visibleLight)
    {
        dirLightsColor[idx] = visibleLight.finalColor;
        
        // note that the light space is in the right-hand coordinates, therefore
        // its "forward" direction is +z axis (0, 0, 1), and multiplied with the
        // OS -> WS transform matrix, it returns the 3rd column which is idexed 2.
        dirLightsDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        // Vec4 (shadow strength, start index per light, normal bias, light index)
        dirLightsShadowData[idx] = shadows.RecordDirectionalShadowData(visibleLight.light, visibleLightIdx);
    }


    private void SetupPointLight(int idx, int visibleLightIdx, ref VisibleLight visibleLight)
    {
        Light light = visibleLight.light;

        otherLightColor[idx] = visibleLight.finalColor;

        // see the comments before for why we choose the last column
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);

        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPosition[idx] = position;

        spotLightAngles[idx] = new Vector4(0f, 1f);

        otherLightShadowData[idx] = shadows.RecordOtherLightShadowData(light, visibleLightIdx);
    }


    private void SetupSpotLight(int idx, int visibleLightIdx, ref VisibleLight visibleLight)
    {
        Light light = visibleLight.light;

        otherLightColor[idx] = visibleLight.finalColor;

        spotLightDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);

        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPosition[idx] = position;

        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
		float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
		float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
		spotLightAngles[idx] = new Vector4(angleRangeInv, -outerCos * angleRangeInv, 0);

        otherLightShadowData[idx] = shadows.RecordOtherLightShadowData(light, visibleLightIdx);
    }


    #endregion
}

}