using UnityEngine;
using UnityEngine.Rendering;

namespace Firecrest
{

    internal static class ShaderPropertyID
    {
        // Space Transform
        public static readonly int
        viewMatrixID = Shader.PropertyToID("unity_MatrixV"),
        invViewMatrixID = Shader.PropertyToID("unity_MatrixInvV"),
        viewAndProjectionMatrixID = Shader.PropertyToID("unity_MatrixVP"),
        inverseViewAndProjectionMatrixID = Shader.PropertyToID("unity_MatrixInvVP"),
    
        CameraMatrixVPInv = Shader.PropertyToID("_CameraMatrixVPInv");


        // Lighting
        public static readonly int
        dirLightCountID = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightsColorID = Shader.PropertyToID("_DirectionalLightsColor"),
        dirLightsDirectionID = Shader.PropertyToID("_DirectionalLightsDirection"),
        dirLightsShadowDataID = Shader.PropertyToID("_DirectionalLightsShadowData"),
        otherLightCountID = Shader.PropertyToID("_OtherLightCount"),
        otherLightColorID = Shader.PropertyToID("_OtherLightColor"),
        otherLightPositionID = Shader.PropertyToID("_OtherLightPosition"),
        otherLightShadowDataID = Shader.PropertyToID("_OtherLightShadowData"),
        spotLightDirID = Shader.PropertyToID("_SpotLightDirection"),
        spotLightAngleID = Shader.PropertyToID("_SpotLightAngle");


        // Shadows
        public static readonly int
        shadowAtlasSizeID = Shader.PropertyToID("_ShadowAtlasSize"),
        dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas"),
        otherShadowAtlasID = Shader.PropertyToID("_OtherShadowAtlas"),
        dirTransformMatricesID = Shader.PropertyToID("_DirShadowTransformMatrices"),
        otherShadowTransformMatricesID = Shader.PropertyToID("_OtherShadowTransformMatrices"),
        otherShadowTilesID = Shader.PropertyToID("_OtherShadowTiles"),
        cascadeCountID = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres"),
        cascadeBiasDataID = Shader.PropertyToID("_CascadeData"),
        shadowFadingDataID = Shader.PropertyToID("_ShadowFadingData"),
        shadowPancakingID = Shader.PropertyToID("_ShadowPancaking"),

        cameraDepthID = Shader.PropertyToID("_CameraDepthTexture"),
        mainLightShadowmapID = Shader.PropertyToID("_MainLightShadowmapTexture"),
        worldToShadowMatID = Shader.PropertyToID("_MainLightWorldToShadow"),

        shadowParamsID = Shader.PropertyToID("_MainLightShadowParams"),
        shadowBiasID = Shader.PropertyToID("_ShadowBias"),
        lightDirectionID = Shader.PropertyToID("_LightDirection"),

        shadowRampMapID = Shader.PropertyToID("_ShadowRampMap"),

        
        cascadeShadowSplitSpheresID = Shader.PropertyToID("_CascadeShadowSplitSpheres"),
        cascadeShadowSplitSphereRadiiID = Shader.PropertyToID("_CascadeShadowSplitSphereRadii"),
        cascadeZDistanceID = Shader.PropertyToID("_CascadeZDistance");
    

        // shadowOffset0ID = Shader.PropertyToID("_MainLightShadowOffset0"),
        // shadowOffset1ID = Shader.PropertyToID("_MainLightShadowOffset1"),
        // shadowOffset2ID = Shader.PropertyToID("_MainLightShadowOffset2"),
        // shadowOffset3ID = Shader.PropertyToID("_MainLightShadowOffset3"),
        // shadowmapSizeID = Shader.PropertyToID("_MainLightShadowmapSize");


        // Post Processing
        public static readonly int
        screenSourceID = Shader.PropertyToID("_ScreenSoureImage"),
        sourceSizeID = Shader.PropertyToID("_SourceSize"),

        bloomTextureID = Shader.PropertyToID("_BloomTexture"),
        bloomCacheID = Shader.PropertyToID("_BloomCache"),
        bloomBicubicUpsamplingID = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomPrefilterID = Shader.PropertyToID("_BloomPrefilter"),

        bloomTintID = Shader.PropertyToID("_BloomTint"),
        bloomThresholdID = Shader.PropertyToID("_BloomThreshold"),
        bloomIntensityID = Shader.PropertyToID("_BloomIntensity"),
        bloomScatterID = Shader.PropertyToID("_BloomScatter"),

        colorAdjustmentsDataID = Shader.PropertyToID("_ColorAdjustmentVector"),
        colorFilterID = Shader.PropertyToID("_ColorFilter");

    }

    internal static class ShaderKeywordStrings
    {
        // post processing
        public static readonly string
        colorAdjustment = "_COLORADJUSTMENT",
        toneMapping_Natural = "_TOMAPPING_NATURAL",
        toneMapping_ACES = "_TOMAPPING_ACES",
        toneMapping_Reinhard = "_TOMAPPING_REINHARD";
    }
}