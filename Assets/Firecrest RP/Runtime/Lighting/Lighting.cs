using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;


namespace Firecrest
{

// at most 4 directional lights, 64 alternative lights supported
public class Lighting
{
    private const string bufferName = "Lighting";

    private static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";


    private const int
    maxDirLightCount = 4, maxOptionalLightCount = 64;


    private readonly Vector4[]
    dirLightsColor = new Vector4[maxDirLightCount],
    dirLightsDirection = new Vector4[maxDirLightCount],
    dirLightsShadowData = new Vector4[maxDirLightCount],

    optionalLightColor = new Vector4[maxOptionalLightCount],
    optionalLightPosition = new Vector4[maxOptionalLightCount],
    optionalLightDirection = new Vector4[maxOptionalLightCount],
    optionalSpotLightAngle = new Vector4[maxOptionalLightCount],
    optionalLightShadowData = new Vector4[maxOptionalLightCount];


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
        shadows.Cleanup();
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
        int dirLightCount = 0, optionalLightCount = 0,
        i;
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
                        SetupDirectionalLight(dirLightCount++, ref visibleLight);
                break;

                case LightType.Point:
                    if (optionalLightCount < maxOptionalLightCount)
                    {
                        tempIdx = optionalLightCount;
                        SetupPointLight(optionalLightCount++, ref visibleLight);
                    }
                break;

                case LightType.Spot:
                    if (optionalLightCount < maxOptionalLightCount)
                    {
                        tempIdx = optionalLightCount;
                        SetupSpotLight(optionalLightCount++, ref visibleLight);
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
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalLightPositionID, optionalLightPosition);
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalLightDirectionID, optionalLightDirection);
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalSpotLightAngleID, optionalSpotLightAngle);
            buffer.SetGlobalVectorArray(ShaderPropertyID.optionalLightShadowDataID, optionalLightShadowData);
        }
    }


    #region Set up each light category

    // For "localToWorldMatrix" :
    // Unity transform matrix following the order scale -> rotate -> translate,
    // and this gives a 4x4 matrix. The first 3x3 submatrix represents the
    // rotation matrix, while the last column means the translation. That can
    // be thought as the position after translation and thus equivalents to the
    // world space position.

    private void SetupDirectionalLight(int idx, ref VisibleLight visibleLight)
    {
        dirLightsColor[idx] = visibleLight.finalColor;
        
        // note that the light space is in the right-hand coordinates, therefore
        // its "forward" direction is +z axis (0, 0, 1), and multiplied with the
        // OS -> WS transform matrix, it returns the 3rd column which is idexed 2.
        dirLightsDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        // Vec4 (shadow strength, start index per light, normal bias, light index)
        dirLightsShadowData[idx] = shadows.ReserveDirectionalShadows(visibleLight.light, idx);
    }


    private void SetupPointLight(int idx, ref VisibleLight visibleLight)
    {
        Light light = visibleLight.light;

        optionalLightColor[idx] = visibleLight.finalColor;

        // see the comments before for why we choose the last column
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);

        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        optionalLightPosition[idx] = position;

        optionalSpotLightAngle[idx] = new Vector4(0f, 1f);

        optionalLightShadowData[idx] = shadows.ReserveOptionalLightShadows(light, idx);
    }


    private void SetupSpotLight(int idx, ref VisibleLight visibleLight)
    {
        Light light = visibleLight.light;

        optionalLightColor[idx] = visibleLight.finalColor;

        optionalLightDirection[idx] = -visibleLight.localToWorldMatrix.GetColumn(2);

        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);

        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        optionalLightPosition[idx] = position;

        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);

        optionalSpotLightAngle[idx] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);

        optionalLightShadowData[idx] = shadows.ReserveOptionalLightShadows(light, idx);
    }


    #endregion
}

}