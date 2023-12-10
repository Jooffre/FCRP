using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using static Firecrest.Utils;
using UnityEngine.Experimental.Rendering;


namespace Firecrest
{

public class FirecrestRenderPipelineDeferredPass : RenderPipeline
{
    private CommandBuffer m_buffer;
    private CullingResults cullingResults;


    // [useDynamicBatching, useGPUInstancing, useSRPBatcher]
    private bool[] m_batchingSettings;
    
    private ShadowSettings m_shadowSettings;
    private PostProcessingSettings m_postProcessingSettings;

    private DeferredLighting m_lighting;
    private WriteGeometryBuffer geometryBuffer;
    private DeferredLightingComputation deferredLightingComputation;
    private DeferredShadows deferredShadows;
    private ScreenSpaceShadows screenSpaceShadows;
    private PostProcessingStack postProcessingStack;


    private ShaderTagId
    gBufferPassID = new ShaderTagId("GBuffer");


    //private RTHandleSystem m_RTHandleSystem ;
    private RTHandle m_cameraDepthHandle;
    private RTHandle[] m_geometryBufferHandles = new RTHandle[4];
    private RenderTargetIdentifier[] m_geometryBufferTexIDs = new RenderTargetIdentifier[4];
    private RTHandle m_screenSpaceShaodwHandle;
    private RTHandle m_bloomTexture;
    private RTHandle m_ScreenSource;
    private RTHandle m_quadDepth;

    private string geometryBufferNamePrefix = "_GBLayer_";
    private int geometryBufferNameID;
    private const string m_screenSpaceShadowTex = "_ScreenSpaceShadowmapTexture";
    private const string m_bloomTextureName = "Bloom Texture";
    private const string m_screenSourceName = "Screen Source";


    private Matrix4x4 inv_ViewMatrix, inv_ProjectionMatrix, VP_Matrix, inv_VPMatrix;


    private string sampleName;


    private int currentWidth, currentHeight;

    public FirecrestRenderPipelineDeferredPass(bool[] batchingSettings, ShadowSettings shadowSettings, PostProcessingSettings postProcessingSettings)
    {
        this.m_buffer = new CommandBuffer();
        this.m_batchingSettings = new bool[] {batchingSettings[0], batchingSettings[1]};
        this.m_shadowSettings = shadowSettings;
        this.m_postProcessingSettings = postProcessingSettings;

        this.m_lighting = new DeferredLighting();
        this.geometryBuffer = new WriteGeometryBuffer();
        this.deferredLightingComputation = new DeferredLightingComputation();
        this.deferredShadows = new DeferredShadows();
        this.screenSpaceShadows = new ScreenSpaceShadows();
        this.postProcessingStack = new PostProcessingStack();

        this.currentWidth = Screen.width;
        this.currentHeight = Screen.height;

        RTHandles.Initialize(Screen.width, Screen.height);

        InitRenderTextures(currentWidth, currentHeight);

        GraphicsSettings.useScriptableRenderPipelineBatching = batchingSettings[2];
        GraphicsSettings.lightsUseLinearIntensity = true;
    }


    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            InitMatricesForShader(camera);

            DeferredPassMain(context, camera, m_batchingSettings, m_shadowSettings);
        }
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        this.ClearRenderTextures();
    }


    private void DeferredPassMain(ScriptableRenderContext context, Camera camera, bool[] batchingSettings, ShadowSettings shadowSettings)
    {
        PrepareBufferName(m_buffer, camera.name, ref sampleName, " - Deferred");
        PrepareWorldUIForSceneWindow(camera);

        if(!Cull(context, camera, 50)) return;

        m_lighting.Setup(context, cullingResults);

        // initialize post processing
        postProcessingStack.Setup(camera, m_postProcessingSettings);

        m_buffer.BeginSample(sampleName);
        Executebuffer(context, m_buffer);

        // draw shadow map
        m_buffer.SetGlobalTexture(ShaderPropertyID.shadowRampMapID, shadowSettings.shadowRampMap);
        deferredShadows.Setup(cullingResults);
        deferredShadows.Render(context);

        // write G-buffers
        context.SetupCameraProperties(camera);
        CameraPropertiesSetting.SetProperties(m_buffer, camera);
        geometryBuffer.Setup(context, camera);
        geometryBuffer.RenderGBbuffer(m_geometryBufferTexIDs, m_cameraDepthHandle, gBufferPassID, cullingResults, batchingSettings[0], batchingSettings[1]);
    
        // screen space shadows
        screenSpaceShadows.RenderScreenSpaceShadows(context, m_screenSpaceShaodwHandle);
        screenSpaceShadows.GaussianBlur(context, camera, m_screenSpaceShaodwHandle);

        // lighting computation & skybox
        context.SetupCameraProperties(camera);
        deferredLightingComputation.LightingComputation(context, camera, m_ScreenSource, m_quadDepth);

        // draw Gizmos
        DrawGizmosBeforePP(context, camera);
        DrawGizmosAfterPP(context, camera);
        
        if (postProcessingStack.IsActive)
        {
            postProcessingStack.Render_Deferred(context, m_ScreenSource, m_bloomTexture);
        }

        m_buffer.EndSample(sampleName);
        Executebuffer(context, m_buffer);

        context.Submit();
    }


    private void InitMatricesForShader(Camera camera)
    {
        inv_ViewMatrix = Matrix4x4.Inverse(camera.worldToCameraMatrix);

        inv_ProjectionMatrix = Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(camera.projectionMatrix, true));
        
        VP_Matrix = camera.worldToCameraMatrix * GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        
        inv_VPMatrix = inv_ViewMatrix * inv_ProjectionMatrix;

        m_buffer.SetGlobalMatrix(ShaderPropertyID.viewMatrixID, camera.worldToCameraMatrix);
        m_buffer.SetGlobalMatrix(ShaderPropertyID.invViewMatrixID, inv_ViewMatrix);
        m_buffer.SetGlobalMatrix(ShaderPropertyID.viewAndProjectionMatrixID, VP_Matrix);
        m_buffer.SetGlobalMatrix(ShaderPropertyID.inverseViewAndProjectionMatrixID, inv_VPMatrix);
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


    private void InitRenderTextures(int width, int height)
    {
        if (width == 0 || height == 0)
            width = height = 1;
        
        // global camera depth texture
        RTHandles.SetReferenceSize(width, height);
        m_cameraDepthHandle = RTHandles.Alloc(Vector2.one, colorFormat: GraphicsFormat.R32_SFloat, depthBufferBits: DepthBits.Depth32,
        dimension: TextureDimension.Tex2D, name: "Camera Depth Texture ");
        /*
        RenderTextureDescriptor depthDescriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.Depth, 24);
        this.cameraDepthTexture = new RenderTexture(depthDescriptor);
        this.cameraDepthTexture.filterMode = FilterMode.Point;
        cameraDepthTexID = new RenderTargetIdentifier(this.cameraDepthTexture);
        */
        this.m_buffer.SetGlobalTexture(ShaderPropertyID.cameraDepthID, m_cameraDepthHandle);

        // global geometry m_buffer textures :
        // gbuffer 0 - color + metallic
        m_geometryBufferHandles[0] = RTHandles.Alloc
        (Vector2.one, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, 
        filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: geometryBufferNamePrefix + "0");
        m_geometryBufferTexIDs[0] = m_geometryBufferHandles[0];

        // gbuffer 1 - normal + smoothness
        m_geometryBufferHandles[1] = RTHandles.Alloc
        (Vector2.one, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R8G8B8A8_SNorm, 
        filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: geometryBufferNamePrefix + "1");
        m_geometryBufferTexIDs[1] = m_geometryBufferHandles[1];

        // gbuffer2 - view dir
        m_geometryBufferHandles[2] = RTHandles.Alloc
        (Vector2.one, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, 
        filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: geometryBufferNamePrefix + "2");
        m_geometryBufferTexIDs[2] = m_geometryBufferHandles[2];

        // gbuffer3 - void
        m_geometryBufferHandles[3] = RTHandles.Alloc
        (Vector2.one, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, 
        filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: geometryBufferNamePrefix + "3");
        m_geometryBufferTexIDs[3] = m_geometryBufferHandles[3];

        for (int i = 0; i < 4; i++)
        {
            geometryBufferNameID = Shader.PropertyToID(geometryBufferNamePrefix + i);
            this.m_buffer.SetGlobalTexture(geometryBufferNameID, m_geometryBufferTexIDs[i]);
        }

        InitScreenSpaceStuffs();
    }


    private void InitScreenSpaceStuffs()
    {
        m_ScreenSource = RTHandles.Alloc
        (Vector2.one, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: m_screenSourceName);
        m_buffer.SetGlobalTexture(ShaderPropertyID.screenSourceID, BuiltinRenderTextureType.CameraTarget);

        // screen spece shadows
        m_screenSpaceShaodwHandle = RTHandles.Alloc
        (Vector2.one, colorFormat: GraphicsFormat.R8_UNorm, dimension: TextureDimension.Tex2D, name: m_screenSpaceShadowTex);
        m_buffer.SetGlobalTexture(m_screenSpaceShadowTex, m_screenSpaceShaodwHandle);

        // bloom temp texture
        m_bloomTexture = RTHandles.Alloc
        (Vector2.one, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, filterMode: FilterMode.Bilinear, dimension: TextureDimension.Tex2D, name: m_bloomTextureName);
        m_buffer.SetGlobalTexture(ShaderPropertyID.bloomTextureID, m_bloomTexture);

        m_quadDepth = RTHandles.Alloc(Vector2.one, colorFormat: GraphicsFormat.R32_SFloat, depthBufferBits: DepthBits.Depth32, dimension: TextureDimension.Tex2D, name: "Quad Depth");
        m_buffer.SetGlobalTexture("_QuadDepth", m_quadDepth);

        float width = m_ScreenSource.rt.width;
        float height = m_ScreenSource.rt.height;
        m_buffer.SetGlobalVector(ShaderPropertyID.sourceSizeID, new Vector4(width, height, 1.0f / width, 1.0f / height));
    }


    private void ClearRenderTextures()
    {
        m_cameraDepthHandle?.Release();

        for (int i = 0; i < 4; i++)
        {
            m_geometryBufferHandles[i]?.Release();
        }

        m_ScreenSource?.Release();
        m_screenSpaceShaodwHandle?.Release();

        m_bloomTexture?.Release();

        m_quadDepth?.Release();
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