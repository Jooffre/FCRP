using UnityEngine;
using UnityEngine.Rendering;
using static Firecrest.Utils;


namespace Firecrest
{

public class WriteGeometryBuffer
{
    private const string bufferName = "Write Geometry Buffer";
    private CommandBuffer buffer = new CommandBuffer()
    {name = bufferName};
    private ScriptableRenderContext context;

    private Camera camera;

    public RenderTexture cameraDepthTexture;
    public RenderTargetIdentifier cameraDepthID;
    public RenderTargetIdentifier[] GBufferLayers = new RenderTargetIdentifier[4];


    public WriteGeometryBuffer(){}


    public void Setup(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;
    }

    public void RenderGBbuffer
    (RenderTargetIdentifier[] geometryBufferHandles, RTHandle cameraDepthHandle, ShaderTagId gBufferPassID, CullingResults cullingResults, bool useDynamicBatching, bool useGPUInstancing)
    {
        buffer.SetRenderTarget(geometryBufferHandles, cameraDepthHandle);

        buffer.ClearRenderTarget(true, true, Color.clear);

        buffer.BeginSample(bufferName);
        Executebuffer(context, buffer);

        SortingSettings sortingSettings = new SortingSettings(camera)
        {criteria = SortingCriteria.CommonOpaque};

        DrawingSettings drawingSettings = new DrawingSettings(gBufferPassID, sortingSettings)
        { enableDynamicBatching = useDynamicBatching, enableInstancing = useGPUInstancing };

        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        buffer.EndSample(bufferName);
        Executebuffer(context, buffer);
    }
}

}