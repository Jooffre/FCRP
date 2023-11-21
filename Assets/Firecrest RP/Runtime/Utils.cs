using UnityEngine;
using UnityEngine.Rendering;


namespace Firecrest
{

public class Utils
{
    public static void Draw
    (
        CommandBuffer buffer, int tempTexName, RenderTargetIdentifier src, 
        RenderTargetIdentifier dst, Material material, int pass
    )
    {
        buffer.SetGlobalTexture(tempTexName, src);
		buffer.SetRenderTarget(dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural
        (Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
    }


    public static Mesh MakeCanvasMesh()
    {
        Vector3[] positions =
        { new Vector3(-1.0f, -1.0f, 0.0f), new Vector3(1.0f, -1.0f, 0.0f), new Vector3(-1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f) };

        Vector2[] uvs =
        { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };

        int[] indices = {0, 2, 1, 1, 2, 3};

        Mesh mesh = new Mesh();
        
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.vertices = positions;
        mesh.triangles = indices;
        mesh.uv = uvs;
        
        return mesh;
    }


    public static void Executebuffer(ScriptableRenderContext context, CommandBuffer buffer)
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }


    public static LocalKeyword TryGetLocalKeyword(Shader shader, string name)
    {
        return shader.keywordSpace.FindKeyword(name);
    }

    /// <summary>
    /// Naming command buffer automatically.
    /// </summary>
    public static void PrepareBufferName(CommandBuffer buffer, string cameraName, ref string SampleName, string suffix)
    {
        // Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = cameraName + suffix;
        // Profiler.EndSample();
    }

    /// <summary>
    /// Render UI on the scene window. 
    /// Executed before culling because it may add geometry to the Scene.
    /// </summary>
    public static void PrepareWorldUIForSceneWindow(Camera camera)
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }


    // Caution: such a call should not be use interlaced with command buffer command, as it is immediate
    /// <summary>
    /// Set a keyword immediatly on a Material.
    /// </summary>
    /// <param name="material">Material on which to set the keyword.</param>
    /// <param name="keyword">Keyword to set on the material.</param>
    /// <param name="state">Value of the keyword to set on the material.</param>
    public static void SetKeyword(Material material, string keyword, bool state)
    {
        if (state)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }
}

}