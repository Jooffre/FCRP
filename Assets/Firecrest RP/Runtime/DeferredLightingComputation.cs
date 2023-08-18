using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static Firecrest.Utils;


namespace Firecrest
{

public class DeferredLightingComputation
{    
    private const string bufferName = "Deferred Lighting Computation";
    private const string subBufferName = "Deferred Shading";
    private CommandBuffer m_buffer = new CommandBuffer()
    { name = bufferName };

    // private const string tempTexName = "_Temp_DeferredLighting";
    // private int m_tempRTID = Shader.PropertyToID(tempTexName);
    
    private Mesh m_canvas;
    private Material m_Material;
    private Material m_copyMaterial;
    //private RTHandle m_depth;

    public DeferredLightingComputation()
    {
        m_Material = new Material(Shader.Find("Firecrest RP/Deferred Lighting Computation"));
        m_Material.hideFlags = HideFlags.HideAndDontSave;
        m_copyMaterial = new Material(Shader.Find("Hidden/Firecrest RP/Copy With Depth"));
        m_copyMaterial.hideFlags = HideFlags.HideAndDontSave;

        m_canvas = MakeCanvasMesh();
    }


    public void LightingComputation(ScriptableRenderContext context, Camera camera, RenderTargetIdentifier screenSource, RenderTargetIdentifier cameraDepth)
    {        

        //m_buffer.BeginSample(bufferName);
        //Executebuffer(context, m_buffer);

        //RenderTargetIdentifier depthID = new RenderTargetIdentifier(quadDepth);

        //m_buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //m_buffer.GetTemporaryRT(Shader.PropertyToID("quadDepth"), camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear);
        m_buffer.SetRenderTarget(screenSource, cameraDepth);

        //m_buffer.ClearRenderTarget(true, true, Color.clear);
        
        m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_Material);

        //m_buffer.Blit(null, BuiltinRenderTextureType.CameraTarget, m_Material);
        Executebuffer(context, m_buffer);
        context.SetupCameraProperties(camera);
        m_buffer.SetRenderTarget(screenSource, cameraDepth);
        Executebuffer(context, m_buffer);
        context.DrawSkybox(camera);

        //m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        //m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_copyMaterial); 
    }

    /*public void copyCameraToTexture(ScriptableRenderContext context, Camera camera)
    {
        m_buffer.GetTemporaryRT(ShaderPropertyID.screenSourceID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear);
        //m_buffer.SetGlobalTexture(ShaderPropertyID.screenSourceID, ShaderPropertyID.screenSourceID);
        m_buffer.Blit(BuiltinRenderTextureType.CameraTarget, ShaderPropertyID.screenSourceID);
        
        //m_buffer.SetRenderTarget(ShaderPropertyID.screenSourceID);

        //m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        //m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_copyMaterial);

        Executebuffer(context, m_buffer);
    }*/

    public void copyToCamera(ScriptableRenderContext context, RenderTargetIdentifier screenSource)
    { 
        // m_buffer.SetGlobalTexture(screenSourceID, BuiltinRenderTextureType.CameraTarget);
        // m_buffer.SetRenderTarget(screenSourceID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        // m_buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        // m_buffer.DrawMesh(m_canvas, Matrix4x4.identity, m_copyMaterial);

        //m_buffer.GetTemporaryRT(ShaderPropertyID.screenSourceID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear);

        m_buffer.Blit(screenSource, BuiltinRenderTextureType.CameraTarget);

        //m_buffer.ReleaseTemporaryRT(ShaderPropertyID.screenSourceID);
        Executebuffer(context, m_buffer);
    }
}

}