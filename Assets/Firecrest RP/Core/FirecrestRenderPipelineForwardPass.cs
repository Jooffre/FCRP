using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using static Firecrest.Utils;
using UnityEngine.Experimental.Rendering;


namespace Firecrest
{

public class FirecrestRenderPipelineForwardPass : RenderPipeline
{
    private CommandBuffer m_buffer;
    private CullingResults cullingResults;


    // [useDynamicBatching, useGPUInstancing, useSRPBatcher]
    private bool[] m_batchingSettings;
    private ShadowSettings m_shadowSettings;
    private PostProcessingSettings m_postProcessingSettings;


    private Lighting lighting;
    private PostProcessingStack postProcessingStack;


    private RTHandle m_bloomTexture;
    private RTHandle m_screenSoureHandle;
    private const string m_bloomTextureName = "Blomm Texture";


    private int currentWidth, currentHeight;


    private string sampleName;
    private Material errorMaterial;
    private int frameBufferID;


    private static ShaderTagId
    unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
    litShaderTagId = new ShaderTagId("ForwardLit");


    private static ShaderTagId[] legacyShaderTagIds = // the pass that we don't want to render
    {
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("Always"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };


    public FirecrestRenderPipelineForwardPass(bool[] batchingSettings, ShadowSettings shadowSettings, PostProcessingSettings postProcessingSettings)
    {
        this.m_buffer = new CommandBuffer();
        this.m_batchingSettings = new bool[] {batchingSettings[0], batchingSettings[1]};
        this.m_shadowSettings = shadowSettings;
        this.m_postProcessingSettings = postProcessingSettings;

        this.lighting = new Lighting();
        this.postProcessingStack = new PostProcessingStack();

        this.frameBufferID = Shader.PropertyToID("_CameraFrameBuffer");

        this.currentWidth = Screen.width;
        this.currentHeight = Screen.height;

        RTHandles.Initialize(Screen.width, Screen.height);
        InitRenderTextures();

        GraphicsSettings.useScriptableRenderPipelineBatching = batchingSettings[2];
        GraphicsSettings.lightsUseLinearIntensity = true;
    }


    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            ForwardPassMain(context, camera, m_batchingSettings, m_shadowSettings);
        }
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        this.ClearRenderTextures();

        //Debug.Log("Deferred pass resources disposed.");
    }


    private void ForwardPassMain(ScriptableRenderContext context, Camera camera, bool[] batchingSettings, ShadowSettings shadowSettings)
    {
        PrepareBufferName(m_buffer, camera.name, ref sampleName, " - Forward");
        PrepareWorldUIForSceneWindow(camera);

        if(!Cull(context, camera, shadowSettings.maxDistance)) return;

        m_buffer.BeginSample(sampleName);
        Executebuffer(context, m_buffer);

        // draw shadow map
        lighting.Setup(context, cullingResults, shadowSettings);

        // prepare PP
        postProcessingStack.Setup(camera, m_postProcessingSettings);

        // rendering in order

        // SetupCameraProperties() will set a render target simultaneously,
        // if you clear a render target before it, ClearRenderTarget() will
        // render a full-screen quad repeatedly, that is unnecessary.
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        if (flags > CameraClearFlags.Color)
            flags = CameraClearFlags.Color;
        //if (postProcessingStack.IsActive)
        //{
        m_buffer.GetTemporaryRT(frameBufferID, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        m_buffer.SetRenderTarget(frameBufferID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		//}
        //m_buffer.SetRenderTarget(m_screenSoureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        // clear after SetupCameraProperties() just clear the RT info, it's faster
        // than create a new quad. So put this operation here.
        m_buffer.ClearRenderTarget
        (
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
        );

        Executebuffer(context, m_buffer);

         // draw opaque
        SortingSettings sortingSettings = new SortingSettings(camera)
        {criteria = SortingCriteria.CommonOpaque};
        // draw unlit
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = batchingSettings[0], enableInstancing = batchingSettings[1],
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

        DrawUnsupportedShaders(context, camera);

        // draw Gizmos

        DrawGizmosBeforePP(context, camera);

        if (postProcessingStack.IsActive)
			postProcessingStack.Render(context, frameBufferID, m_bloomTexture);

        DrawGizmosAfterPP(context, camera);

        m_buffer.EndSample(sampleName);
        Executebuffer(context, m_buffer);

        context.Submit();
    }

    private void DrawUnsupportedShaders(ScriptableRenderContext context, Camera camera)
    {
        if (errorMaterial == null)
            errorMaterial = new Material(Shader.Find("Custom RP/Hidden/Error"));

        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };

        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private bool Cull(ScriptableRenderContext context, Camera camera, float maxShadowDistance)
    {
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
        {
            cullingParams.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            this.cullingResults = context.Cull(ref cullingParams);
			return true;
        }
        return false;
	}


    private void InitRenderTextures()
    {
        // bloom temp texture
        m_bloomTexture = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
        filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: m_bloomTextureName);
        m_buffer.SetGlobalTexture(ShaderPropertyID.bloomTextureID, m_bloomTexture);

        // screen source image (as the final texture that rendered to camera)
        // m_screenSoureHandle = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
        // filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: m_bloomTextureName);
        // m_buffer.SetGlobalTexture(ShaderPropertyID.screenSourceID, m_screenSoureHandle);
    }


    private void ClearRenderTextures()
    {
        m_bloomTexture?.Release();

        //m_screenSoureHandle?.Release();
    }


    private void DrawGizmosBeforePP(ScriptableRenderContext context, Camera camera)
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        }
    }

    private void DrawGizmosAfterPP(ScriptableRenderContext context, Camera camera)
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }
}

}