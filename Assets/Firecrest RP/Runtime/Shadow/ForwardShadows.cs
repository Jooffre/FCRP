using UnityEngine;
using UnityEngine.Rendering;


namespace Firecrest
{

// this class deals with processing logic of directional lighting shadows
// related functions being called in Lighting.cs

public class ForwardShadows
{
    // support 4 directional light with shadows
    const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;
    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer()
    { name = bufferName };

    // a counter, represents the number of directional light that has been configured
    int ShadowedDirectionalLightCount;

    // Ids
    static int 
    dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
    transformMatricesId = Shader.PropertyToID("_TransformMatrices"),
    cascadeCountId = Shader.PropertyToID("_CascadeCount"),
    cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
    cascadeBiasDataId = Shader.PropertyToID("_CascadeData"),
    shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),

    shadowFadingDataId = Shader.PropertyToID("_ShadowFadingData");


    static string[]
    cascadeBlendKeywords = {"_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER" };

    static string[] directionalFilterKeywords =
    {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};

    static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_DEFAULT",
        "_SHADOW_MASK_DISTANCE"
    };
    
    bool useShadowMask;

	static Vector4[]
    cascadeCullingSpheres = new Vector4[maxCascades],
    cascadeBiasData = new Vector4[maxCascades];


    // the matrix that transform coord from world to the pixel of shadowmap
    static Matrix4x4[]
    transformWorldToShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];


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


    // an array to store directional lights
	ShadowedDirectionalLight[] 
    ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    
    // set up fields
	public void Setup
    (
		ScriptableRenderContext context,
        CullingResults cullingResults,
		ShadowSettings settings
	){
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;
        ShadowedDirectionalLightCount = 0;
        useShadowMask = false;
    }


    /// <summary> 
    /// Render shadows.
    /// </summary> 
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
            RenderDirectionalShadows();
        else
        {
            // when there's no shadow to shade, create a 1x1 atlas
            buffer.GetTemporaryRT
            (
				dirShadowAtlasId,
                1,
                1,
				32,
                FilterMode.Bilinear,
                RenderTextureFormat.Shadowmap
			);
        }

        // shadow mask is not realtime, we have to set the keywords anyway.
        buffer.BeginSample(bufferName);

        SetKeywords(shadowMaskKeywords, 
                        /* mark: the following statement = (true, true) : (true, false) : (false, -) */
                        useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask? 
                            0 : 1 : -1);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }


    public void ExecuteBuffer()
    {
        this.context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }


    /// <summary>
    /// Shadow render logic.
    /// </summary>
    private void RenderDirectionalShadows()
    {
        int atlasSize = (int) settings.directional.atlasSize;
        
        buffer.GetTemporaryRT
        (
            dirShadowAtlasId,   // identification
            atlasSize,  // width
            atlasSize,  // height
            32, // depth
            FilterMode.Bilinear,    // filter mode
            RenderTextureFormat.Shadowmap   // RT format
        );
        buffer.SetRenderTarget  // tells GPU the operation target is that RT
        (
            dirShadowAtlasId,
            RenderBufferLoadAction.DontCare,    // ignore the exisiting contents of the render buffer
            RenderBufferStoreAction.Store   // store to RAM so that we could reuse it later
        );

        // clear the DepthBuffer with Color.clear, reverse the ColorBuffer
        buffer.ClearRenderTarget(true, false, Color.clear);
        
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderTilesToAtlas(i, split, tileSize);
        }

        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
		buffer.SetGlobalVectorArray
        (
			cascadeCullingSpheresId, cascadeCullingSpheres
		);

        buffer.SetGlobalVectorArray(cascadeBiasDataId, cascadeBiasData);

        // send the VP matrices array to the GPU
        buffer.SetGlobalMatrixArray(transformMatricesId, transformWorldToShadowMatrices);

        //buffer.SetGlobalFloat(shadowDistanceId, settings.maxDistance);
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector
        (
			shadowFadingDataId,
			new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f), 0)
		);

        SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);

        buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }


    /// <summary>
    /// Render a shadowmap for a single light to the ShadowAtlas.
    /// </summary>
    /// <param name="idx">The index of light.</param>
    ///<param name="split">The split size.</param>
    /// <param name="tileSize">The size of an assigned tile for a single light on the ShadowAtlas</param>
    private void RenderTilesToAtlas(int idx, int split, int tileSize)
    {
        // get the current light data
        ShadowedDirectionalLight light = ShadowedDirectionalLights[idx];
        
        // we need to create a ShadowDrawingSettings instance
        // it will be inputed as a reference parameter to DrawShadows()
        ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        int cascadeCount = settings.directional.cascadeCount;
		int tileOffset = idx * cascadeCount;
		Vector3 ratios = settings.directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);

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
            transformWorldToShadowMatrices[tileIndex] = ConvertToAtlasMatrix
            (
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), // offset
                split
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


    /// <summary>
    /// Set different sampling bias for shadows in each cascade.
    /// </summary>
    private void SetCascadeBiasData(int index, Vector4 cullingSphere, float tileSize)
    {
        
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)settings.directional.filter + 1f);
        cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;

        cascadeBiasData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
	}


    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
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
        float scale = 1f / split;
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


    /// <summary>
    /// <para>Execute per frame.</para>
    /// <para>To reserve space in the shadow atlas for the light's shadow map,
    /// and store the information needed to render them.</para>
    /// </summary>
    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        // record a light if
        //   - the configurated light < preseted max number
        //   - the light enables shadow casting and the shadow strength > 0
        //   - the light affects at least one shadow casting object in the Scene.
        if(
            ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount
            && light.shadows != LightShadows.None
            && light.shadowStrength > 0f
            //&& cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b) // also returns a bounding box
        ){
            // if use shadow mask
            float maskChannel = -1;
            ShadowMaskIfnot(light);
            if (useShadowMask)
                maskChannel = light.bakingOutput.occlusionMaskChannel;

            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);

            ShadowedDirectionalLights[ShadowedDirectionalLightCount]
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
                settings.directional.cascadeCount * ShadowedDirectionalLightCount++,

                light.shadowNormalBias,

                maskChannel
            );
        }
        else
            return new Vector4(0f, 0f, 0f, -1f);
    }


    /// <summary>
    /// <para>Execute per frame.</para>
    /// <para>Applying the shadow mask for point and spot lights.</para>
    /// </summary>
    /// <param name="light"></param>
    /// <param name="visibleLightIndex"></param>
    /// <returns></returns>
    public Vector4 ReserveOptionalLightShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
        {
            ShadowMaskIfnot(light);
            
            return new Vector4(light.shadowStrength, 0f, 0f, light.bakingOutput.occlusionMaskChannel);
        }

        return new Vector4(0f, 0f, 0f, -1f);
    }
    

    /// <summary>
    /// The function checks if using shadow mask, true if a light's baking output is set to Mixed 
    /// and the mixed lighting mode is set to "Shadowmask".
    /// </summary>
    /// <param name ="light">The "Light" parameter is an object of the Light class. It represents a light
    /// source in a scene, such as a directional light, point light, or spot light.</param>
    private void ShadowMaskIfnot(Light light)
    {
        LightBakingOutput lightBaking = light.bakingOutput;

        if (
            lightBaking.lightmapBakeType == LightmapBakeType.Mixed
            && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask
        ){
            useShadowMask = true;
        }
    }
    

    /// <summary>
    /// Release the temporary RT for shadow atlas.
    /// </summary>
    public void Cleanup()
    {
		buffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}
}

}