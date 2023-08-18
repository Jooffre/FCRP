/*using UnityEngine;
using UnityEngine.Rendering;
//using static Firecrest.Utils;

namespace Firecrest
{

public partial class CameraRenderer
{
    private CommandBuffer buffer = new CommandBuffer();
    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private Camera camera;
    
    
    private Lighting lighting;
    private PostProcessingStack postProcessingStack;


    private GeometryBuffer geometryBuffer;
    private DeferredLightingComputation deferredLightingComputation;
    private MainLightShadowCasterPass mainLightShadowCasterPass;
    private ScreenSpaceShadows screenSpaceShadows;


    private static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer"); // camera target
    private bool useHDR;

    private static ShaderTagId
    unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
    litShaderTagId = new ShaderTagId("ForwardLit"),
    gbufferTagId = new ShaderTagId("GBuffer");
    //depthOnlyTagId = new ShaderTagId("depthOnly");


    /// <summary>
    /// Instancing components for the selected render passes.
    /// </summary>
    /// <param name="renderPass"> 0 - forward, 1 - deferred. </param>
    public void PipelineSetup(int renderPass)
    {
        // initialize forward pass
        if(renderPass == 0)
        {
            this.lighting = new Lighting();
            this.postProcessingStack = new PostProcessingStack();
        }
        // initialize deferred pass
        else
        {
            this.lighting = new Lighting();
            this.geometryBuffer = new GeometryBuffer();
            this.deferredLightingComputation = new DeferredLightingComputation();
            this.screenSpaceShadows = new ScreenSpaceShadows();
            
        }
    }




# region Render Paths

    // ================================================================
    // ||                       Forward Path                         ||
    // ================================================================
    public void ForwardPath
    (
        ScriptableRenderContext context,
        Camera camera,
        bool useDynamicBatching,
        bool useGPUInstancing,
        ShadowSettings shadowSettings,
        PostProcessingSettings postProcessingSettings,
        bool HDR
    ){
        this.context = context;
        this.camera = camera;
        this.useHDR = HDR && camera.allowHDR; // enable HDR when both the camera and the RP allow it.

        PrepareBufferName("Forward");

        PrepareWorldGeometryForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
            return;

        // Forward Rendering Main
        // --------------------- shadow ---------------------
        // to nest shadow operations under camera
        // e.g., [camera name] Forward
        SampleBegin(SampleName);

        // set light & shadows (atlas) first
        lighting.Setup(context, cullingResults, shadowSettings);
        postProcessingStack.Setup(context, camera, postProcessingSettings, useHDR);
        
        SampleEnd(SampleName);
        
        // -------------------- forward ---------------------
        // reset the target to camera
        ForwardSetup(context);

        SampleBegin(SampleName);

        DrawForwardPathContents(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();

        // ---------------- post processing ------------------

        DrawGizmosAndPostProcessing();

        Cleanup();

        SampleEnd(SampleName);

        context.Submit();
    }



    // ================================================================
    // ||                       Deferred Path                        ||
    // ================================================================

    public void DeferredPath
    (
        ScriptableRenderContext context,
        Camera camera,
        bool useDynamicBatching,
        bool useGPUInstancing,
        ShadowSettings shadowSettings,
        //PostProcessingSettings postProcessingSettings,
        bool HDR
    ){
        this.context = context;
        this.camera = camera;
        this.useHDR = HDR && camera.allowHDR;

        // for(int i = 0; i < 4; i++)
        // {
        //     Shader.SetGlobalTexture("_shadowtex" + i, shadowTextures[i]);
        //     Shader.SetGlobalFloat("_split" + i, cascadeShadowMap.splits[i]);
        // }

        PrepareBufferName("Deferred");

        PrepareWorldGeometryForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
            return;

        SampleBegin(SampleName);

        lighting.SetupWithoutRenderingShadows(context, cullingResults, shadowSettings);
        // Deferred Rendering Main
        // DeferredShadowCasting(context, camera);
        DrawDeferredShadow(context, cullingResults);

        // ------------------- G Buffer -------------------
        WriteToGBuffer(useDynamicBatching, useGPUInstancing);

        // ----------------- Deferred Lit ------------------
        DeferredSetup();
        
        DrawDeferredPathContents(context, camera);

        ScreenSpaceShadow(context, camera, cullingResults);

        //geometryBuffer.CleanRT();

        DrawGizmosBeforePP();
        DrawGizmosAfterPP();

        SampleEnd(SampleName);

        context.Submit();
    }

# endregion



# region Render Logics

    private void ForwardSetup(ScriptableRenderContext context)
    {
        // SetupCameraProperties() will set a render target simultaneously,
        // if you clear a render target before it, ClearRenderTarget() will
        // render a full-screen quad repeatedly, that is unnecessary.
        context.SetupCameraProperties(camera);

        CameraClearFlags flags = camera.clearFlags;

        if (postProcessingStack.IsActive)
        {
            if (flags > CameraClearFlags.Color)
            {
				flags = CameraClearFlags.Color;
			}
			buffer.GetTemporaryRT
            (
				frameBufferId, camera.pixelWidth, camera.pixelHeight,
				32, FilterMode.Bilinear, 
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
			);
			buffer.SetRenderTarget
            (
				frameBufferId,
				RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
			);
		}

        // clear after SetupCameraProperties() just clear the RT info, it's faster
        // than create a new quad. So put this operation here.
        buffer.ClearRenderTarget
        (
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
        );

        //Executebuffer(context, buffer);
        ExecuteCMD();
    }


    /// <summary>
    /// Draw visible objects with the forward lit pass, and other necessary components.
    /// </summary>
    private void DrawForwardPathContents(bool useDynamicBatching, bool useGPUInstancing)
    {
        // draw opaque
        SortingSettings sortingSettings = new SortingSettings(camera)
        {criteria = SortingCriteria.CommonOpaque};
        
        // draw unlit
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            // to pass the lightmap and lightprobes (LPPV) data to GPU
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume
        };

        // draw forward lit
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // draw skybox
        context.DrawSkybox(camera);

        // draw transparent
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }


    private void DrawDeferredShadow(ScriptableRenderContext context, CullingResults cullingResults)
    {
        SampleBegin(SampleName);
        
        mainLightShadowCasterPass = new MainLightShadowCasterPass();
        mainLightShadowCasterPass.Setup(cullingResults);
        mainLightShadowCasterPass.Render(context);

        SampleEnd(SampleName);
    }


    private void WriteToGBuffer(bool useDynamicBatching, bool useGPUInstancing)
    {
        context.SetupCameraProperties(camera);

        CameraPropertiesSetting.SetProperties(buffer, camera);

        geometryBuffer.Setup(context, camera);

        SampleBegin(SampleName);
        
        //geometryBuffer.InitBuffers();
        geometryBuffer.RenderGBbuffer(gbufferTagId, cullingResults, useDynamicBatching, useGPUInstancing);
        geometryBuffer.Cleanup();

        SampleEnd(SampleName);
    }


    private void DeferredSetup()
    {
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        CameraClearFlags flags = camera.clearFlags;

        buffer.ClearRenderTarget
        (
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
        );

        //Executebuffer(context, buffer);
        ExecuteCMD();
    }


    private void DrawDeferredPathContents(ScriptableRenderContext context, Camera camera)
    {
        
        SampleBegin(SampleName);

        deferredLightingComputation.Computation(context, camera);

        // deliver camera parameters again before drawing skybox
        context.SetupCameraProperties(camera);
        context.DrawSkybox(camera);
        
        SampleEnd(SampleName);
    }


    private void ScreenSpaceShadow(ScriptableRenderContext context, Camera camera, CullingResults cullingResults)
    {
        screenSpaceShadows.Setup(context, camera);

        SampleBegin(SampleName);

        screenSpaceShadows.RenderScreenSpaceShadows(cullingResults);
        //screenSpaceShadows.Cleanup();

        SampleEnd(SampleName);
    }

# endregion



# region Methods

    // for the sake of simplification
    private void ExecuteCMD()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void SampleBegin(string name)
    {
        buffer.BeginSample(name);
        ExecuteCMD();
    }

    private void SampleEnd(string name)
    {
        buffer.EndSample(name);
        ExecuteCMD();
    }


    private void GBufferCleanup()
    {
        geometryBuffer.Cleanup();
    }
    
    
    /// <summary>
    /// If false, stop rendering; else, execute culling.
    /// </summary>
    private bool Cull(float maxShadowDistance)
    {
        // get culling parameters for a camera.
        // returns FALSE if camera is invalid to render, like :
        //   - empty viewport rectangle, invalid clip plane setup etc..
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
        {
            // the actual culling distance is the smaller one
            cullingParams.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref cullingParams);
			return true;
        }
        return false;
	}
    
    
    /// <summary>
    /// Draw post processing and Gizmos before / after that.
    /// </summary>
    private void DrawGizmosAndPostProcessing()
    {
        DrawGizmosBeforePP();

        if (postProcessingStack.IsActive)
        {
            // post processing
			postProcessingStack.Render(frameBufferId);
		}

        DrawGizmosAfterPP();
    }


    /// <summary>
    /// Release cached temporary render textures.
    /// </summary>
    private void Cleanup()
    {
		lighting.Cleanup();

		if (postProcessingStack.IsActive)
        {
			buffer.ReleaseTemporaryRT(frameBufferId);
		}
	}

# endregion
}

}*/