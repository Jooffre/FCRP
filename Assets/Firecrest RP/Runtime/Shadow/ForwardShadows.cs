using UnityEngine;
using UnityEngine.Rendering;


namespace Firecrest
{

// this class deals with processing logic of directional lighting shadows
// related functions being called in Lighting.cs

public class ForwardShadows
{
    // support 4 directional light with shadows
    const int maxShadowedDirLightCount = 4, maxShadowedOtherLightCount = 16;
    const int maxCascades = 4;

    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer()
    { name = bufferName };


    private int shadowedDirLightCount, shadowedOtherLightCount;


    static string[]
    cascadeBlendKeywords = {"_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER" };

    static string[] directionalFilterKeywords =
    {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7"
	};

    static string[] otherFilterKeywords =
    {
		"_OTHER_PCF3",
		"_OTHER_PCF5",
		"_OTHER_PCF7",
	};

    static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_DEFAULT",
        "_SHADOW_MASK_DISTANCE"
    };

    static int 
    otherShadowAtlasID = Shader.PropertyToID("_OtherShadowAtlas"),
    otherShadowMatricesID = Shader.PropertyToID("_OtherShadowMatrices"),
    otherShadowTilesID = Shader.PropertyToID("_OtherShadowTiles");

    private bool useShadowMask;

    private Vector4 atlasSizes;

	static Vector4[]
    cascadeCullingSpheres = new Vector4[maxCascades],
    cascadeBiasData = new Vector4[maxCascades],
    otherShadowTiles = new Vector4[maxShadowedOtherLightCount];


    // the matrix that transform coord from world to the pixel of shadowmap
    static Matrix4x4[]
    dirShadowTransformMatrices = new Matrix4x4[maxShadowedDirLightCount * maxCascades],
    otherShadowTransformMatrices  = new Matrix4x4[maxShadowedOtherLightCount];


    ScriptableRenderContext context;
	CullingResults cullingResults;
	ShadowSettings settings;


    struct ShadowedDirectionalLight
    {
        // the index of the current light source
		public int visibleLightIndex;
        public float slopeScaleBias;
        public float nearPlaneOffset;
	}
    struct ShadowedOtherLight
    {
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float normalBias;
	}

    // arrays to store lights
	ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirLightCount];
    ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];


	public void Setup
    (
		ScriptableRenderContext context,
        CullingResults cullingResults,
		ShadowSettings settings
	){
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;
        this.shadowedDirLightCount = this.shadowedOtherLightCount = 0;
        this.useShadowMask = false;
    }





    // =======================================================================================================


    # region Render Shadow Atlas


    /// <summary> 
    /// Render shadows.
    /// </summary> 
    public void Render()
    {
        if (shadowedDirLightCount > 0)
            RenderDirectionalShadows();
        else
        {
            // when there's no shadow to shade, create a 1x1 atlas
            buffer.GetTemporaryRT
            (
				ShaderPropertyID.dirShadowAtlasID,
                1, 1, depthBuffer: 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
			);
        }

        if (shadowedOtherLightCount > 0)
			RenderOtherShadows();
		else
			buffer.SetGlobalTexture(otherShadowAtlasID, ShaderPropertyID.dirShadowAtlasID);


        // shadow mask is not realtime, we have to set the keywords anyway.
        buffer.BeginSample(bufferName);

        SetKeywords(shadowMaskKeywords, 
                        /* mark: the following statement = (true, true) : (true, false) : (false, -) */
                        useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 
                            0 : 1 : -1);

        buffer.SetGlobalInt(ShaderPropertyID.cascadeCountID,
            shadowedDirLightCount > 0 ? settings.dirLightShadowAtlasSettings.cascadeCount : 0);

        float f = 1f - settings.dirLightShadowAtlasSettings.cascadeFade;
        buffer.SetGlobalVector(ShaderPropertyID.shadowFadingDataID,
            new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f), 0));

        buffer.SetGlobalVector(ShaderPropertyID.shadowAtlasSizeID, atlasSizes);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }



    private void RenderDirectionalShadows()
    {
        int atlasSize = (int) settings.dirLightShadowAtlasSettings.atlasSize;

        atlasSizes.x = atlasSize;
		atlasSizes.y = 1f / atlasSize;
        
        buffer.GetTemporaryRT
        (
            ShaderPropertyID.dirShadowAtlasID,
            atlasSize, atlasSize, depthBuffer: 32,
            FilterMode.Bilinear, RenderTextureFormat.Shadowmap
        );
        buffer.SetRenderTarget  // tells GPU the operation target is that RT
        (
            ShaderPropertyID.dirShadowAtlasID,
            RenderBufferLoadAction.DontCare,    // ignore the exisiting contents of the render buffer
            RenderBufferStoreAction.Store   // store to RAM so that we could reuse it later
        );

        // clear the DepthBuffer with Color.clear, reverse the ColorBuffer
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.SetGlobalFloat(ShaderPropertyID.shadowPancakingID, 1f);
        
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = shadowedDirLightCount * settings.dirLightShadowAtlasSettings.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < shadowedDirLightCount; i++)
        {
            RenderDirectionalShadow(i, split, tileSize);
        }

		buffer.SetGlobalVectorArray(ShaderPropertyID.cascadeCullingSpheresID, cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(ShaderPropertyID.cascadeBiasDataID, cascadeBiasData);

        // send the VP matrices array to the GPU
        buffer.SetGlobalMatrixArray(ShaderPropertyID.transformMatricesID, dirShadowTransformMatrices);

        //buffer.SetGlobalFloat(shadowDistanceId, settings.maxDistance);

        SetKeywords(directionalFilterKeywords, (int)settings.dirLightShadowAtlasSettings.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)settings.dirLightShadowAtlasSettings.cascadeBlend - 1);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }


    /// <summary>
    /// Render a shadowmap for a single light to the ShadowAtlas.
    /// </summary>
    /// <param name="idx">The index of light.</param>
    ///<param name="split">The split size.</param>
    /// <param name="tileSize">The size of an assigned tile for a single light on the ShadowAtlas</param>
    private void RenderDirectionalShadow(int idx, int split, int tileSize)
    {
        // get the current light data
        ShadowedDirectionalLight light = shadowedDirectionalLights[idx];

        // we need to create a ShadowDrawingSettings instance
        // it will be inputed as a reference parameter to DrawShadows()
        ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        int cascadeCount = settings.dirLightShadowAtlasSettings.cascadeCount;
		int tileOffset = idx * cascadeCount;
		Vector3 ratios = settings.dirLightShadowAtlasSettings.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.dirLightShadowAtlasSettings.cascadeFade);

        float tileScale = 1f / split;

        for (int i = 0; i < cascadeCount; i++)
        {
            // what we need to do is calculate VP matrices that match the light's orientation 
            // and gives us a clip space cube that overlaps the area visible to the camera 
            // that can contain the light's shadows.

            // therefore, use this method to compute VP matrices and splitData
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
            (
                light.visibleLightIndex,
                i,
                cascadeCount,
                ratios,
                tileSize,
                light.nearPlaneOffset,
                out Matrix4x4 viewMatrix,
                out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );

            splitData.shadowCascadeBlendCullingFactor = cullingFactor;

            // splitData includes information that how should we clip the objects castering shadows
            shadowSettings.splitData = splitData;
            
            // we only set 4 culling spheres for the first light
            // because it is in light space and same for all lights
            if (idx == 0)
            {
                SetCascadeBiasData(i, splitData.cullingSphere, tileSize);
            }

            int tileIndex = tileOffset + i;

            // world --> light space VP Matrices array
            dirShadowTransformMatrices[tileIndex] = ConvertToAtlasMatrix
            (
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), // offset
                tileScale
            );
            
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            // buffer.SetGlobalDepthBias(0f, 3f);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);

            ExecuteBuffer();

            // render shadow map tile to Atlas
            context.DrawShadows(ref shadowSettings);

            // buffer.SetGlobalDepthBias(0f, 0f);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }


    /// <summary>
    /// Set different sampling bias for shadows in each cascade.
    /// </summary>
    private void SetCascadeBiasData(int index, Vector4 cullingSphere, float tileSize)
    {
        
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)settings.dirLightShadowAtlasSettings.filter + 1f);
        cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;

        cascadeBiasData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
	}


    private void RenderOtherShadows()
    {
        int atlasSize = (int) settings.dirLightShadowAtlasSettings.atlasSize;

        atlasSizes.z = atlasSize;
		atlasSizes.w = 1f / atlasSize;
        
        buffer.GetTemporaryRT
        (
            otherShadowAtlasID,
            atlasSize, atlasSize, depthBuffer: 32,
            FilterMode.Bilinear, RenderTextureFormat.Shadowmap
        );
        buffer.SetRenderTarget
        (
            otherShadowAtlasID,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store
        );

        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.SetGlobalFloat(ShaderPropertyID.shadowPancakingID, 0f);
        
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < shadowedOtherLightCount; i++)
        {
            RenderSpotShadows(i, split, tileSize);
        }

        buffer.SetGlobalMatrixArray(otherShadowMatricesID, otherShadowTransformMatrices);
        buffer.SetGlobalVectorArray(otherShadowTilesID, otherShadowTiles);

        SetKeywords(otherFilterKeywords, (int)settings.otherLightShadowSettings.filter - 1);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }


    private void RenderSpotShadows(int index, int split, int tileSize)
    {
		ShadowedOtherLight light = shadowedOtherLights[index];

		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives
        (
			light.visibleLightIndex,
            out Matrix4x4 viewMatrix,
			out Matrix4x4 projectionMatrix,
            out ShadowSplitData splitData
		);

		shadowSettings.splitData = splitData;

        float texelSize = 2f / (tileSize * projectionMatrix.m00);
		float filterSize = texelSize * ((float)settings.otherLightShadowSettings.filter + 1f);
		float bias = light.normalBias * filterSize * 1.4142136f;
		Vector2 offset = SetTileViewport(index, split, tileSize);
		float tileScale = 1f / split;
        SetOtherTileData(index, offset, tileScale, bias);

        otherShadowTransformMatrices[index] = ConvertToAtlasMatrix(
			projectionMatrix * viewMatrix, offset, tileScale
		);
		
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
		buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
		ExecuteBuffer();
        
		context.DrawShadows(ref shadowSettings);
		buffer.SetGlobalDepthBias(0f, 0f);
	}



    /// <summary>
    /// Compute world to shadow space transform matrix.
    /// </summary>
	private Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, float scale)
    {
		if (SystemInfo.usesReversedZBuffer)
        {
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}

        // the range of coord of the light clip space is [-1, 1], which is [0, 1] in shadowmap coord however
        // thus remapping the coord, and apply the offset and scale corresponding to each tile

		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);

		return m;
	}

    /// <summary>
    /// Set the tile region that to be rendered.
    /// </summary>
    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
		Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport
        (
            new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize)
        );
        return offset;
	}


    private void SetOtherTileData (int index, Vector2 offset, float scale, float bias)
    {
		float border = atlasSizes.w * 0.5f;
		Vector4 data;
		data.x = offset.x * scale + border;
		data.y = offset.y * scale + border;
		data.z = scale - border - border;
		data.w = bias;
		otherShadowTiles[index] = data;
	}

    # endregion





    // =======================================================================================================


    # region Recording Shadow Data

    /// <summary>
    /// <para>Execute per frame.</para>
    /// <para>Recording shadow data of directional lights if necessary.</para>
    /// </summary>
    public Vector4 RecordDirectionalShadowData(Light light, int visibleLightIndex)
    {
        // record a light if
        //   - the configurated light < preseted max number
        //   - the light enables shadow casting and the shadow strength > 0
        //   - the light affects at least one shadow casting object in the Scene.
        if(
            shadowedDirLightCount < maxShadowedDirLightCount
            && light.shadows != LightShadows.None
            && light.shadowStrength > 0f
            //&& cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b) // also returns a bounding box
        ){
            // determine if use shadow mask
            float maskChannel = -1f;

            LightBakingOutput lightBakingOutput = light.bakingOutput;
            if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                    lightBakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBakingOutput.occlusionMaskChannel;
            }


            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);


            shadowedDirectionalLights[shadowedDirLightCount]
            = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };

            return new Vector4
            (
                light.shadowStrength,

                // record the start index for each light
                // e.g., 0, 4, 8, 12
                settings.dirLightShadowAtlasSettings.cascadeCount * shadowedDirLightCount++,

                light.shadowNormalBias,

                maskChannel
            );
        }
        else
            return new Vector4(0f, 0f, 0f, -1f);
    }


    /// <summary>
    /// <para>Execute per frame.</para>
    /// <para>Recording shadow data of optional lights if necessary. 
    /// And applying the shadow mask for point and spot lights.</para>
    /// </summary>
    public Vector4 RecordOtherLightShadowData(Light light, int visibleLightIdx)
    {
        // if any lights don't satisfy the conditions, just return and no shadow casted
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
			return new Vector4(0f, 0f, 0f, -1f);


        float maskChannel = -1f;

        LightBakingOutput lightBakingOutput = light.bakingOutput;

        if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
            lightBakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBakingOutput.occlusionMaskChannel;
        }

        if (shadowedOtherLightCount >= maxShadowedOtherLightCount ||
			!cullingResults.GetShadowCasterBounds(visibleLightIdx, out Bounds b))
        {
			return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
		}

        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
        {
			visibleLightIndex = visibleLightIdx,
			slopeScaleBias = light.shadowBias,
			normalBias = light.shadowNormalBias
		};

        return new Vector4(light.shadowStrength, shadowedOtherLightCount++, 0f, maskChannel);
    }

    # endregion





    // =======================================================================================================


    # region Common

    private void SetKeywords(string[] keywords, int enabledIndex)
    {
		// int enabledIndex = (int)settings.directional.filter - 1;
		for (int i = 0; i < keywords.Length; i++)
        {
			if (i == enabledIndex)
				buffer.EnableShaderKeyword(keywords[i]);
			else
				buffer.DisableShaderKeyword(keywords[i]);
		}
	}


    public void ExecuteBuffer()
    {
        this.context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }


    /// <summary>
    /// Release the temporary RT for shadow atlas.
    /// </summary>
    public void Cleanup()
    {
		buffer.ReleaseTemporaryRT(ShaderPropertyID.dirShadowAtlasID);

        if (shadowedOtherLightCount > 0)
        {
			buffer.ReleaseTemporaryRT(otherShadowAtlasID);
		}

		ExecuteBuffer();
	}

    # endregion
}

}