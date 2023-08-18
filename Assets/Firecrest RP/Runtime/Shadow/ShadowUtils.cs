using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace Firecrest
{

public struct ShadowSliceData
{
    public Matrix4x4 viewMatrix;
    public Matrix4x4 projectionMatrix;
    public Matrix4x4 shadowTransform;
    public int offsetX;
    public int offsetY;
    public int resolution;
    public ShadowSplitData splitData; // splitData contains culling information

    public void Clear()
    {
        viewMatrix = Matrix4x4.identity;
        projectionMatrix = Matrix4x4.identity;
        shadowTransform = Matrix4x4.identity;
        offsetX = offsetY = 0;
        resolution = 1024;
    }
}

public class ShadowUtils
{

    public static int GetMaxTileResolutionInAtlas(int atlasWidth, int atlasHeight, int tileCount)
    {
        int resolution = Mathf.Min(atlasWidth, atlasHeight);
        int currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
        while (currentTileCount < tileCount)
        {
            resolution = resolution >> 1;
            currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
        }
        return resolution;
    }

    public static RenderTexture GetTemporaryShadowTexture(int width, int height, int bits)
    {
        RenderTextureDescriptor rtd = new RenderTextureDescriptor(width, height, RenderTextureFormat.Depth, bits);
        rtd.shadowSamplingMode = ShadowSamplingMode.CompareDepths;
        var shadowTexture = RenderTexture.GetTemporary(rtd);
        shadowTexture.filterMode = FilterMode.Bilinear;
        shadowTexture.wrapMode = TextureWrapMode.Clamp;
        
        return shadowTexture;
    }

    public static void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, float border, out float scale, out float bias)
    {
        // To avoid division from zero
        // This values ensure that fade within cascade will be 0 and outside 1
        if (border < 0.0001f)
        {
            float multiplier = 1000f; // To avoid blending if difference is in fractions
            scale = multiplier;
            bias = -fadeDistance * multiplier;
            return;
        }

        border = 1 - border;
        border *= border;

        // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
        float distanceFadeNear = border * fadeDistance;
        scale = 1.0f / (fadeDistance - distanceFadeNear);
        bias = -distanceFadeNear / (fadeDistance - distanceFadeNear);
    }

    public static bool ExtractDirectionalLightMatrix
    (ref CullingResults cullResults, int dirightIndex, int cascadeIndex, int shadowCascadesCount, Vector3 cascadesSplitRatio, int shadowResolution, float shadowNearPlane,
    int shadowmapWidth, int shadowmapHeight, out Vector4 cascadeSplitDistance, out ShadowSliceData shadowSliceData)
    {
        bool success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
        (dirightIndex, cascadeIndex, shadowCascadesCount, cascadesSplitRatio, shadowResolution, shadowNearPlane,
        out shadowSliceData.viewMatrix, out shadowSliceData.projectionMatrix, out shadowSliceData.splitData);

        cascadeSplitDistance = shadowSliceData.splitData.cullingSphere;
        shadowSliceData.offsetX = (cascadeIndex % 2) * shadowResolution;
        shadowSliceData.offsetY = (cascadeIndex / 2) * shadowResolution;
        shadowSliceData.resolution = shadowResolution;
        shadowSliceData.shadowTransform = GetShadowTransform(shadowSliceData.projectionMatrix, shadowSliceData.viewMatrix);

        // It is the culling sphere radius multiplier for shadow cascade blending
        // If this is less than 1.0, then it will begin to cull castors across cascades
        shadowSliceData.splitData.shadowCascadeBlendCullingFactor = 1.0f;

        // If we have shadow cascades baked into the atlas we bake cascade transform
        // in each shadow matrix to save shader ALU and L/S
        if (shadowCascadesCount > 1)
            ApplySliceTransform(ref shadowSliceData, shadowmapWidth, shadowmapHeight);

        return success;
    }

    public static void ApplySliceTransform(ref ShadowSliceData shadowSliceData, int atlasWidth, int atlasHeight)
    {
        Matrix4x4 sliceTransform = Matrix4x4.identity;
        float oneOverAtlasWidth = 1.0f / atlasWidth;
        float oneOverAtlasHeight = 1.0f / atlasHeight;
        sliceTransform.m00 = shadowSliceData.resolution * oneOverAtlasWidth;
        sliceTransform.m11 = shadowSliceData.resolution * oneOverAtlasHeight;
        sliceTransform.m03 = shadowSliceData.offsetX * oneOverAtlasWidth;
        sliceTransform.m13 = shadowSliceData.offsetY * oneOverAtlasHeight;

        // Apply shadow slice scale and offset
        shadowSliceData.shadowTransform = sliceTransform * shadowSliceData.shadowTransform;
    }


    static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
    {
        // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
        // apply z reversal to projection matrix. We need to do it manually here.
        if (SystemInfo.usesReversedZBuffer)
        {
            proj.m20 = -proj.m20;
            proj.m21 = -proj.m21;
            proj.m22 = -proj.m22;
            proj.m23 = -proj.m23;
        }

        Matrix4x4 worldToShadow = proj * view;

        var textureScaleAndBias = Matrix4x4.identity;
        textureScaleAndBias.m00 = 0.5f;
        textureScaleAndBias.m11 = 0.5f;
        textureScaleAndBias.m22 = 0.5f;
        textureScaleAndBias.m03 = 0.5f;
        textureScaleAndBias.m23 = 0.5f;
        textureScaleAndBias.m13 = 0.5f;
        // textureScaleAndBias maps texture space coordinates from [-1, 1] to [0, 1]

        // Apply texture scale and offset to save a MAD in shader.
        return textureScaleAndBias * worldToShadow;
    }

    public static Vector4 GetShadowBias(ref VisibleLight shadowLight, int shadowLightIndex, Vector4[] bias, Matrix4x4 lightProjectionMatrix, float shadowResolution, bool softShadows)
    {
        if (shadowLightIndex < 0)
        {
            //Debug.LogWarning(string.Format("{0} is not a valid light index.", shadowLightIndex));
            return Vector4.zero;
        }

        float frustumSize;
        if (shadowLight.lightType == LightType.Directional)
        {
            // Frustum size is guaranteed to be a cube as we wrap shadow frustum around a sphere
            frustumSize = 2.0f / lightProjectionMatrix.m00;
        }else
        {
            //Debug.LogWarning("Only point, spot and directional shadow casters are supported in universal pipeline");
            frustumSize = 0.0f;
        }

        // depth and normal bias scale is in shadowmap texel size in world space
        float texelSize = frustumSize / shadowResolution;
        float depthBias = -bias[shadowLightIndex].x * texelSize;
        float normalBias = -bias[shadowLightIndex].y * texelSize;

        if (softShadows)
        {
            // TODO: depth and normal bias assume sample is no more than 1 texel away from shadowmap
            // This is not true with PCF. Ideally we need to do either
            // cone base bias (based on distance to center sample)
            // or receiver place bias based on derivatives.
            // For now we scale it by the PCF kernel size of non-mobile platforms (5x5)
            const float kernelRadius = 2.5f;
            depthBias *= kernelRadius;
            normalBias *= kernelRadius;
        }

        return new Vector4(depthBias, normalBias, 0f, 0f);
    }

    public static void SetupShadowCasterConstantBuffer(CommandBuffer buffer, ref VisibleLight shadowLight, Vector4 shadowBias)
    {
        buffer.SetGlobalVector(ShaderPropertyID.shadowBiasID, shadowBias);

        // Light direction is currently used in shadow caster pass to apply shadow normal offset (normal bias).
        Vector3 lightDirection = -shadowLight.localToWorldMatrix.GetColumn(2);
        buffer.SetGlobalVector(ShaderPropertyID.lightDirectionID, new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0.0f));

        // For punctual lights, computing light direction at each vertex position provides more consistent results (shadow shape does not change when "rotating the point light" for example)
        //Vector3 lightPosition = shadowLight.localToWorldMatrix.GetColumn(3);
        //buffer.SetGlobalVector("_LightPosition", new Vector4(lightPosition.x, lightPosition.y, lightPosition.z, 1.0f));
    }


    public static Vector4 SetBiasDataForEachCascade(Vector4 cullingSphere, float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        //float filterSize = texelSize * ((float)settings.directional.filter + 1f);
        float filterSize = texelSize * (1f + 1f);
        cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;

        return new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f, 0f, 0f);
	}


    public static void RenderShadowSlice
    (CommandBuffer buffer, ref ScriptableRenderContext context, ref ShadowSliceData shadowSliceData,
    ref ShadowDrawingSettings settings, Matrix4x4 proj, Matrix4x4 view)
    {
        buffer.SetGlobalDepthBias(1.0f, 2.5f); // these values match HDRP defaults (see https://github.com/Unity-Technologies/Graphics/blob/9544b8ed2f98c62803d285096c91b44e9d8cbc47/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAtlas.cs#L197 )

        buffer.SetViewport(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
        buffer.SetViewProjectionMatrices(view, proj);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        context.DrawShadows(ref settings);
        buffer.DisableScissorRect();
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        buffer.SetGlobalDepthBias(0.0f, 0.0f); // Restore previous depth bias values
    }
}
}