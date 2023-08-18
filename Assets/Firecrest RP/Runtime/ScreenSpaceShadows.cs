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

    private Material m_Material;

    
    public ScreenSpaceShadows()
    {}


    public void RenderScreenSpaceShadows(ScriptableRenderContext context, RenderTargetIdentifier screenSpaceHandle)
    {
        if (!m_Material)
        {
            m_Material = new Material(Shader.Find("Firecrest RP/ScreenSpaceShadows"));
            m_Material.hideFlags = HideFlags.HideAndDontSave;
        }
        if (!m_canvas)
            m_canvas = MakeCanvasMesh();

        //m_buffer.BeginSample(m_bufferName);
        //Executebuffer(context, m_buffer);

        m_buffer.SetRenderTarget(screenSpaceHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //m_buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_Material);

        //m_buffer.Blit(null, screenSpaceHandle, m_Material);

        //m_buffer.EndSample(m_bufferName);
        Executebuffer(context, m_buffer);
    }
}

}