/*using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Profiling;


namespace Firecrest
{

public partial class CameraRenderer
{
    partial void DrawUnsupportedShaders();
    partial void DrawGizmosBeforePP();
    partial void DrawGizmosAfterPP();
    partial void PrepareWorldGeometryForSceneWindow();
    partial void PrepareBufferName(string renderPath);
    

#if UNITY_EDITOR
    private static ShaderTagId[] legacyShaderTagIds = // the pass that we don't want to render
    {
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("Always"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    private static Material errorMaterial;

    partial void DrawUnsupportedShaders()
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

    /// <summary>
    /// Draw Gizmos before the post processing.
    /// </summary>
    partial void DrawGizmosBeforePP()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        }
    }

    /// <summary>
    /// Draw Gizmos after the post processing.
    /// </summary>
    partial void DrawGizmosAfterPP()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    /// <summary>
    /// Render UI on the scene window. 
    /// Executed before culling because it may add geometry to the Scene.
    /// </summary>
    partial void PrepareWorldGeometryForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    string SampleName { get; set; }
    /// <summary>
    /// For multiple cameras in the Scene. 
    /// This makes the buffer's name equal to the camera's.
    /// </summary>
    partial void PrepareBufferName(string renderPath)
    {
        // Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name + " - " + renderPath;
        // Profiler.EndSample();
    }

#else

    const string SampleName = bufferName;

#endif
}

}*/