using UnityEngine;
using UnityEngine.Rendering;
using static Firecrest.Utils;


namespace Firecrest
{

public class ScreenSpaceShadows
{
    private const string m_bufferName = "Screen Space Shadows";
    private CommandBuffer m_buffer = new CommandBuffer()
    { name = m_bufferName };

    private Mesh m_canvas;

    private Material m_screenSpaceShadowMaterial;
    private Material m_GaussianBlurMaterial;

    
    public ScreenSpaceShadows()
    {
        m_screenSpaceShadowMaterial = new Material(Shader.Find("Firecrest RP/ScreenSpaceShadows"));
        m_screenSpaceShadowMaterial.hideFlags = HideFlags.HideAndDontSave;

        m_GaussianBlurMaterial = new Material(Shader.Find("Hidden/Firecrest RP/Gaussian Blur"));
        m_GaussianBlurMaterial.hideFlags = HideFlags.HideAndDontSave;

        m_canvas = MakeCanvasMesh();
    }


    public void RenderScreenSpaceShadows(ScriptableRenderContext context, RenderTargetIdentifier screenSpaceHandle)
    {
        //m_buffer.BeginSample(m_bufferName);
        //Executebuffer(context, m_buffer);

        m_buffer.SetRenderTarget(screenSpaceHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //m_buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_screenSpaceShadowMaterial);

        //m_buffer.Blit(null, screenSpaceHandle, m_screenSpaceShadowMaterial);

        //m_buffer.EndSample(m_bufferName);
        Executebuffer(context, m_buffer);
    }

    public void GaussianBlur(ScriptableRenderContext context, Camera camera, RenderTargetIdentifier screenSpaceHandle)
    {
        int blurCache = Shader.PropertyToID("blurCache");
        //int blurCache2 = Shader.PropertyToID("blurCache2");
        m_buffer.GetTemporaryRT(blurCache, camera.pixelWidth, camera.pixelHeight);
        m_buffer.Blit(screenSpaceHandle, blurCache, m_GaussianBlurMaterial, 0);

        //m_buffer.GetTemporaryRT(blurCache2, camera.pixelWidth, camera.pixelHeight);
        m_buffer.Blit(blurCache, screenSpaceHandle, m_GaussianBlurMaterial, 1);
        m_buffer.ReleaseTemporaryRT(blurCache);
        context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
    }
}

}