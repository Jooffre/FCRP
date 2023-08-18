/*using UnityEngine;
using UnityEngine.Rendering;

namespace Firecrest
{
public class FirecrestRenderPipeline : RenderPipeline
{
    int renderPath;
    bool useDynamicBatching, useGPUInstancing, HDR;
    ShadowSettings shadowSettings;
    PostProcessingSettings postProcessingSettings;

    public FirecrestRenderPipeline
    (
        int renderPath,
        bool useDynamicBatching,
        bool useGPUInstancing,
        bool useSRPBatcher,
        ShadowSettings shadowSettings,
        PostProcessingSettings postProcessingSettings,
        bool HDR
    ){
        this.renderPath = renderPath;
        this.useDynamicBatching = useDynamicBatching; // enable SRP batcher
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
        this.postProcessingSettings = postProcessingSettings;
        this.HDR = HDR;

        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }

    CameraRenderer renderer = new CameraRenderer();

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        switch (renderPath)
        {
            // forward
            case 0 :

                renderer.PipelineSetup(0);

                foreach (Camera camera in cameras)
                {
                    renderer.ForwardPath
                    (
                        context, camera,
                        useDynamicBatching,
                        useGPUInstancing,
                        shadowSettings,
                        postProcessingSettings,
                        HDR
                    );
                }
                break;
            
            // deferred
            case 1 :

                renderer.PipelineSetup(1);

                foreach (Camera camera in cameras)
                {
                    renderer.DeferredPath
                    (
                        context, camera,
                        useDynamicBatching,
                        useGPUInstancing,
                        shadowSettings,
                        HDR
                    );
                }
                break;
        }

    }

}

}*/