using UnityEngine;
using UnityEngine.Rendering;
using static Firecrest.Utils;
using static Firecrest.PostProcessingSettings;


namespace Firecrest
{

public partial class PostProcessingStack
{
    private const string m_bufferName = "Post Processing";
    private CommandBuffer m_buffer = new CommandBuffer()
    {name = m_bufferName};

    private Camera camera;
    private PostProcessingSettings settings;
    private BloomSettings bloomSettings;
    private ColorAdjustmentsSettings adjustmentsSettings;
    private ToneMappingSettings toneMappingSettings;
    
    private LocalKeyword
    m_colorAdjustment, m_toneMapping_Natural, m_toneMapping_ACES, m_toneMapping_Reinhard;

    private Mesh m_canvas;

    private Material m_postProcessingMaterial;
    private Shader m_postProcessingShader;
    private Material bloomMaterial;

    private const int maxDownSampleLevels = 16;
    private int downSamplePyramidID;


    public PostProcessingStack()
    {}


    public void Setup(Camera camera, PostProcessingSettings settings)
    {
        RegisterDownSamplePyramidID();

		this.camera = camera;
		this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;

        this.bloomSettings = settings.Bloom;
        this.adjustmentsSettings = settings.colorAdjustmentsSettings;
        this.toneMappingSettings = settings.ToneMapping;

        this.m_postProcessingShader = Shader.Find("Hidden/Firecrest RP/Post Processing Stack");
        this.m_postProcessingMaterial = new Material(this.m_postProcessingShader);
        m_postProcessingMaterial.hideFlags = HideFlags.HideAndDontSave;
        
        this.bloomMaterial = new Material(Shader.Find("Hidden/Firecrest RP/Bloom"));
        bloomMaterial.hideFlags = HideFlags.HideAndDontSave;

        this.m_canvas = MakeCanvasMesh();

        ApplySceneViewState();
	}
    
    public bool IsActive => settings != null;

    public void Render(ScriptableRenderContext context, int srcHandle, RenderTargetIdentifier bloomTexture)
    {
        m_buffer.SetGlobalTexture(ShaderPropertyID.screenSourceID, srcHandle);

        SetupPostProcessing_Bloom(camera, srcHandle, bloomTexture);

        SetupPostProcessing_ColorAdjustmentAndToneMapping();

        ReleasPPTextures(srcHandle);

        m_buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_postProcessingMaterial, 0);

        context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
	}

    public void Render_Deferred(ScriptableRenderContext context, RenderTargetIdentifier srcHandle, RenderTargetIdentifier bloomTexture)
    {
        m_buffer.SetGlobalTexture(ShaderPropertyID.screenSourceID, srcHandle);

        SetupPostProcessing_Bloom(camera, srcHandle, bloomTexture);

        SetupPostProcessing_ColorAdjustmentAndToneMapping();

        m_buffer.SetRenderTarget(srcHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_postProcessingMaterial, 0, 0);

        //m_buffer.Blit(bloomTexture, BuiltinRenderTextureType.CameraTarget);

        context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();

        m_buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_postProcessingMaterial, 0, 1);

        context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
	}


// =========================================================================================================

#region Post Processing Main

    /// <summary>
    /// Post processing : Bloom.
    /// </summary>
    public void SetupPostProcessing_Bloom(Camera camera, RenderTargetIdentifier screenSourceImage, RenderTargetIdentifier bloomTexture)
    {
        // Bloom.shader passes
        // pass 0 - horizontal
        // pass 1 - vertical
        // pass 2 - additive blend
        // pass 3 - scatter blend
        // pass 4 - threshold  
        // pass 5 - threshold + fade fireflies process

		int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;

        // early stopping
        // if we end up skipping bloomSettings entirely, we just perform a copy operation instead.
        if(bloomSettings.enableBloom == false || height < bloomSettings.minDownscalePixels * 2 || width < bloomSettings.minDownscalePixels * 2)//bloomSettings.intensity <= 0f // zero intensity
        {
            m_buffer.Blit(screenSourceImage, bloomTexture);
            return;
        }

        m_buffer.SetGlobalVector(ShaderPropertyID.bloomTintID, bloomSettings.tint);

        Vector4 threshold;
		threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
		threshold.y = threshold.x * bloomSettings.scatter;
		threshold.z = 2f * threshold.y;
		threshold.w = 0.25f / (threshold.y + 0.00001f);
		threshold.y -= threshold.x;
		m_buffer.SetGlobalVector(ShaderPropertyID.bloomThresholdID, threshold);

		RenderTextureFormat format = RenderTextureFormat.DefaultHDR;
        m_buffer.GetTemporaryRT(ShaderPropertyID.bloomPrefilterID, width, height, 0, FilterMode.Bilinear, format);
       
        Draw(m_buffer, ShaderPropertyID.bloomCacheID, screenSourceImage, ShaderPropertyID.bloomPrefilterID, bloomMaterial, bloomSettings.fadeFireflies? 5 : 4);

		width /= 2;
		height /= 2;

		int filterId = ShaderPropertyID.bloomPrefilterID, pyramidId = downSamplePyramidID + 1;

        int i;
		for (i = 0; i < bloomSettings.maxIterations * 2; i++)
        {
			if (height < bloomSettings.minDownscalePixels || width < bloomSettings.minDownscalePixels)
				break;
            
            int midId = pyramidId - 1;
			m_buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
			m_buffer.GetTemporaryRT(pyramidId, width, height, 0, FilterMode.Bilinear, format);
			Draw(m_buffer, ShaderPropertyID.bloomCacheID, filterId, midId, bloomMaterial, 0);
            Draw(m_buffer, ShaderPropertyID.bloomCacheID, midId, pyramidId, bloomMaterial, 1);
			filterId = pyramidId;
			pyramidId += 2;
			width /= 2; height /= 2;
		}

        m_buffer.ReleaseTemporaryRT(ShaderPropertyID.bloomPrefilterID);

        // if use bi-cubic up sampling
        m_buffer.SetGlobalFloat(ShaderPropertyID.bloomBicubicUpsamplingID, bloomSettings.highQuailty ? 1f : 0f);

        int combinePass; // additive or scatter
		if (bloomSettings.mode == PostProcessingSettings.BloomSettings.Mode.Additive)
        {
			combinePass = 2;
			m_buffer.SetGlobalFloat(ShaderPropertyID.bloomIntensityID, 1f);
		}
		else
        {
			combinePass = 3;
			m_buffer.SetGlobalFloat(ShaderPropertyID.bloomScatterID, bloomSettings.scatter);
		}

        if (i > 1)
        {
            m_buffer.ReleaseTemporaryRT(filterId - 1);
            pyramidId -= 5;

            for (i -= 1; i > 0; i--)
            {
                //m_buffer.SetGlobalTexture(BloomCache2Id, pyramidId + 1);
                Draw(m_buffer, ShaderPropertyID.bloomCacheID, filterId, pyramidId, bloomMaterial, combinePass);
                m_buffer.ReleaseTemporaryRT(filterId);
                m_buffer.ReleaseTemporaryRT(pyramidId + 1);
                filterId = pyramidId;
                pyramidId -= 2;
            }
        }
        else
        {
            m_buffer.ReleaseTemporaryRT(downSamplePyramidID);
        }

        m_buffer.SetGlobalFloat(ShaderPropertyID.bloomIntensityID, bloomSettings.intensity);

		//Draw(m_buffer, BloomCacheId, filterId, outputRTID, bloomMaterial, combinePass);
        m_buffer.SetRenderTarget(bloomTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, bloomMaterial, 0, combinePass);

        m_buffer.ReleaseTemporaryRT(filterId);
    }


    /// <summary>
    /// Post processing : ColorAdjustment and ToneMapping.
    /// </summary>
    public void SetupPostProcessing_ColorAdjustmentAndToneMapping()
    {
        // color adjust settings
        m_colorAdjustment = TryGetLocalKeyword(m_postProcessingShader, ShaderKeywordStrings.colorAdjustment);

        m_buffer.SetGlobalVector
        (
            ShaderPropertyID.colorAdjustmentsDataID,
            new Vector4
            (
                Mathf.Pow(2f, adjustmentsSettings.postExposure),
                adjustmentsSettings.contrast * 0.01f + 1f,
                adjustmentsSettings.hueShift * (1f / 360f),
                adjustmentsSettings.saturation * 0.01f + 1f
		    )
        );

        m_buffer.SetGlobalColor(ShaderPropertyID.colorFilterID, adjustmentsSettings.colorFilter.linear);

        if(adjustmentsSettings.enableColorAdjustments == false)
            m_postProcessingMaterial.SetKeyword(m_colorAdjustment, false);
        else
            m_postProcessingMaterial.SetKeyword(m_colorAdjustment, true);

        // tone mapping settings
        m_toneMapping_Natural = TryGetLocalKeyword(m_postProcessingShader, ShaderKeywordStrings.toneMapping_Natural);
        m_toneMapping_ACES = TryGetLocalKeyword(m_postProcessingShader, ShaderKeywordStrings.toneMapping_ACES);
        m_toneMapping_Reinhard = TryGetLocalKeyword(m_postProcessingShader, ShaderKeywordStrings.toneMapping_Reinhard);

        if (toneMappingSettings.enableToneMapping == false)
        {
            m_postProcessingMaterial.SetKeyword(m_toneMapping_Natural, false);
            m_postProcessingMaterial.SetKeyword(m_toneMapping_ACES, false);
            m_postProcessingMaterial.SetKeyword(m_toneMapping_Reinhard, false);
        }
        else
        {
            switch(toneMappingSettings.mode)
            {
                case ToneMappingSettings.Mode.Clamp:
                    // m_postProcessingMaterial.SetKeyword(m_toneMapping_Natural, false);
                    // m_postProcessingMaterial.SetKeyword(m_toneMapping_ACES, false);
                    // m_postProcessingMaterial.SetKeyword(m_toneMapping_Reinhard, false);
                    break;
                case ToneMappingSettings.Mode.Neutral:
                    m_postProcessingMaterial.SetKeyword(m_toneMapping_Natural, true);

                    break;
                case ToneMappingSettings.Mode.ACES:
                    m_postProcessingMaterial.SetKeyword(m_toneMapping_ACES, true);
                    break;
                case ToneMappingSettings.Mode.Reinhard:
                    m_postProcessingMaterial.SetKeyword(m_toneMapping_Reinhard, true);
                    break;
            }
        }
    }

#endregion


// =========================================================================================================

    /// <summary>
    /// Generate pyramid-texture ID for down sampling.
    /// </summary>
    public void RegisterDownSamplePyramidID()
    {
        downSamplePyramidID = Shader.PropertyToID("_DownSamplePyramid_0");
		for (int i = 1; i < maxDownSampleLevels; i++)
        {
			Shader.PropertyToID("_DownSamplePyramid_" + i);
		}
    }


    /// <summary>
    /// Release related render textures.
    /// </summary>
    public void ReleasPPTextures(int srcID)
    {
        m_buffer.ReleaseTemporaryRT(srcID);
    }

}

}