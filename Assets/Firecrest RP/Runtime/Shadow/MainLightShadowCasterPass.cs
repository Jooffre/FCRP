using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace Firecrest
{

public class MainLightShadowCasterPass
{
    private CullingResults cullingResults;
    private CommandBuffer buffer = new CommandBuffer();

    private static class ShadowSettings
    {
        public static int mainLightShadowmapWidth = 2048;
        public static int mainLightShadowmapHeight = 2048;
        public static int maxShadowDistance = 50;
        public static float mainLightShadowCascadeFadeBorder = 0.1f;
        public static Vector4[] shadowBias = new Vector4[4];
    };

    private const int k_MaxCascades = 4;
    private const int k_ShadowmapBufferBits = 32;

    // In this RP, soft shadows are enabled by default.
    private const bool m_useSoftshadows = true;

    private float m_MaxShadowDistanceSquared;
    private float m_CascadeBorder;
    
    private int renderTargetWidth;
    private int renderTargetHeight;
    private int tileResolution;

    private Vector3 cascadesSplitRatio;
    private Matrix4x4[] m_MainLightShadowMatrices;
    private ShadowSliceData[] m_CascadeSlices;
    private Vector4[] m_cascadeBias;
    private Vector4[] m_CascadeSplitDistances;

    int shadowLightIndex;
    int m_ShadowCasterCascadesCount;
    RenderTexture m_MainLightShadowmapTexture = null;

    bool m_CreateEmptyShadowmap;

    public MainLightShadowCasterPass()
    {
        m_MainLightShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
        m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
        m_cascadeBias = new Vector4[k_MaxCascades];
        m_CascadeSplitDistances = new Vector4[k_MaxCascades];
    }

    public void Setup(CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;

        Clear();

        shadowLightIndex = 0;

        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        VisibleLight visibleLight = visibleLights[shadowLightIndex];
        
        // cases that no shadows
        if( visibleLights.Length == 0 || visibleLight.light.shadows == LightShadows.None 
            || visibleLight.lightType != LightType.Directional || !cullingResults.GetShadowCasterBounds(shadowLightIndex, out Bounds bounds))
        {
            SetupForEmptyRendering();
            return;
        }

        ShadowSettings.shadowBias[0] = new Vector4(visibleLight.light.shadowBias, visibleLight.light.shadowNormalBias, 0, 0);

        m_ShadowCasterCascadesCount = 4;
        cascadesSplitRatio = new Vector3(0.12f, 0.3f, 0.5f);

        this.tileResolution =
        ShadowUtils.GetMaxTileResolutionInAtlas(ShadowSettings.mainLightShadowmapWidth, ShadowSettings.mainLightShadowmapHeight, m_ShadowCasterCascadesCount);

        renderTargetWidth = ShadowSettings.mainLightShadowmapWidth;
        renderTargetHeight = ShadowSettings.mainLightShadowmapHeight;
        //renderTargetHeight = (m_ShadowCasterCascadesCount == 2) ? ShadowSettings.mainLightShadowmapHeight >> 1 : ShadowSettings.mainLightShadowmapHeight;

        for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
        {
            bool success = ShadowUtils.ExtractDirectionalLightMatrix
            (ref cullingResults, shadowLightIndex, cascadeIndex, m_ShadowCasterCascadesCount, cascadesSplitRatio, tileResolution, visibleLight.light.shadowNearPlane, 
            renderTargetWidth, renderTargetHeight, out m_CascadeSplitDistances[cascadeIndex], out m_CascadeSlices[cascadeIndex]);
            
            if (!success)
            {
                SetupForEmptyRendering();
                return;
            }
        }

        m_MainLightShadowmapTexture = ShadowUtils.GetTemporaryShadowTexture(renderTargetWidth, renderTargetHeight, k_ShadowmapBufferBits);
        m_MaxShadowDistanceSquared = ShadowSettings.maxShadowDistance * ShadowSettings.maxShadowDistance;
        m_CascadeBorder = ShadowSettings.mainLightShadowCascadeFadeBorder;
        m_CreateEmptyShadowmap = false;
    }

    void SetupForEmptyRendering()
    {
        m_CreateEmptyShadowmap = true;
        m_MainLightShadowmapTexture = ShadowUtils.GetTemporaryShadowTexture(1, 1, k_ShadowmapBufferBits);
    }


    public void Render(ScriptableRenderContext context)
    {
        if (m_CreateEmptyShadowmap)
        {
            return;
        }

        RenderMainLightCascadeShadowmap(ref context, ref cullingResults);
    }


    void RenderMainLightCascadeShadowmap(ref ScriptableRenderContext context, ref CullingResults cullingResults)
    {
        shadowLightIndex = 0;
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        VisibleLight shadowLight = visibleLights[shadowLightIndex];

        //buffer = CommandBufferPool.Get();

        buffer.SetRenderTarget(m_MainLightShadowmapTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        buffer.BeginSample("Draw Cascade Shadow Atlas");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        buffer.ClearRenderTarget(true, false, Color.clear);

        var settings = new ShadowDrawingSettings(cullingResults, shadowLightIndex);
        //settings.useRenderingLayerMaskTest = UniversalRenderPipeline.asset.supportsLightLayers;

        SetupMainLightShadowReceiverConstants(shadowLight, m_useSoftshadows);
        
        for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
        {
            settings.splitData = m_CascadeSlices[cascadeIndex].splitData;

            Vector4 shadowBias = ShadowUtils.GetShadowBias
            (ref shadowLight, shadowLightIndex, ShadowSettings.shadowBias, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].resolution, m_useSoftshadows);
            ShadowUtils.SetupShadowCasterConstantBuffer(buffer, ref shadowLight, shadowBias);

            //m_cascadeBias[cascadeIndex] = ShadowUtils.SetBiasDataForEachCascade(m_CascadeSplitDistances[cascadeIndex], tileResolution);

            //CoreUtils.SetKeyword(buffer, ShaderKeywordStrings.CastingPunctualLightShadow, false);
            ShadowUtils.RenderShadowSlice(buffer, ref context, ref m_CascadeSlices[cascadeIndex],
                ref settings, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].viewMatrix);

        }
        //buffer.SetGlobalVectorArray(ShaderPropertyID.shadowCascadeBiasID, m_cascadeBias);

        RenderTexture.ReleaseTemporary(m_MainLightShadowmapTexture);

        // shadowData.isKeywordSoftShadowsEnabled = shadowLight.light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;
        // CoreUtils.SetKeyword(buffer, ShaderKeywordStrings.MainLightShadows, shadowData.mainLightShadowCascadesCount == 1);
        // CoreUtils.SetKeyword(buffer, ShaderKeywordStrings.MainLightShadowCascades, shadowData.mainLightShadowCascadesCount > 1);
        // CoreUtils.SetKeyword(buffer, ShaderKeywordStrings.SoftShadows, shadowData.isKeywordSoftShadowsEnabled);

        buffer.EndSample("Draw Cascade Shadow Atlas");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        //CommandBufferPool.Release(buffer);
    }


    void SetupMainLightShadowReceiverConstants(VisibleLight shadowLight, bool supportsSoftShadows)
    {
        Light light = shadowLight.light;

        buffer.SetGlobalInt(ShaderPropertyID.cascadeCountID, k_MaxCascades);

        int cascadeCount = m_ShadowCasterCascadesCount;

        for (int i = 0; i < cascadeCount; ++i)
        {
            m_MainLightShadowMatrices[i] = m_CascadeSlices[i].shadowTransform;
        }
        // We setup and additional a no-op WorldToShadow matrix in the last index
        // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
        // out of bounds. (position not inside any cascade) and we want to avoid branching
        Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
        noOpShadowMatrix.m22 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
        for (int i = cascadeCount; i <= k_MaxCascades; ++i)
            m_MainLightShadowMatrices[i] = noOpShadowMatrix;

        float invShadowAtlasWidth = 1.0f / renderTargetWidth;
        float invShadowAtlasHeight = 1.0f / renderTargetHeight;
        float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
        float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
        float softShadowsProp = m_useSoftshadows ? 1.0f : 0.0f;

        ShadowUtils.GetScaleAndBiasForLinearDistanceFade(m_MaxShadowDistanceSquared, m_CascadeBorder, out float shadowFadeScale, out float shadowFadeBias);

        buffer.SetGlobalTexture(ShaderPropertyID.mainLightShadowmapID, m_MainLightShadowmapTexture);

        buffer.SetGlobalMatrixArray(ShaderPropertyID.worldToShadowMatID, m_MainLightShadowMatrices);

        buffer.SetGlobalVector(ShaderPropertyID.shadowParamsID, new Vector4(light.shadowStrength, softShadowsProp, shadowFadeScale, shadowFadeBias));

        float f = 1f - 0.1f; //1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(ShaderPropertyID.shadowFadingDataID, new Vector4(1f / 50f, 1f / 0.1f, 1f / (1f - f * f), 0));

        if (m_ShadowCasterCascadesCount > 1)
        {
            buffer.SetGlobalVectorArray(ShaderPropertyID.cascadeShadowSplitSpheresID, m_CascadeSplitDistances);

            buffer.SetGlobalVector
            (
                ShaderPropertyID.cascadeShadowSplitSphereRadiiID,
                new Vector4
                (
                    m_CascadeSplitDistances[0].w * m_CascadeSplitDistances[0].w,
                    m_CascadeSplitDistances[1].w * m_CascadeSplitDistances[1].w,
                    m_CascadeSplitDistances[2].w * m_CascadeSplitDistances[2].w,
                    m_CascadeSplitDistances[3].w * m_CascadeSplitDistances[3].w
                )
            );
        }

        if (supportsSoftShadows)
        {
            buffer.SetGlobalVector(ShaderPropertyID.shadowOffset0ID, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            buffer.SetGlobalVector(ShaderPropertyID.shadowOffset1ID, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            buffer.SetGlobalVector(ShaderPropertyID.shadowOffset2ID, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            buffer.SetGlobalVector(ShaderPropertyID.shadowOffset3ID, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));

            // Currently only used when !SHADER_API_MOBILE but risky to not set them as it's generic
            // enough so custom shaders might use it.
            buffer.SetGlobalVector(ShaderPropertyID.shadowmapSizeID, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight, renderTargetWidth, renderTargetHeight));
        }
    }

    private void Clear()
    {
        RenderTexture.ReleaseTemporary(m_MainLightShadowmapTexture);
        m_MainLightShadowmapTexture = null;
        

        for (int i = 0; i < m_MainLightShadowMatrices.Length; ++i)
            m_MainLightShadowMatrices[i] = Matrix4x4.identity;

        for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
            m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

        for (int i = 0; i < m_CascadeSlices.Length; ++i)
            m_CascadeSlices[i].Clear();
    }
}

}