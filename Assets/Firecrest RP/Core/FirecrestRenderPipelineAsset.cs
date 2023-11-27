using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Firecrest
{

[CreateAssetMenu(menuName = "Custom Rendering/Firecrest RP/Firecrest Render Pipeline")]
public class FirecrestRenderPipelineAsset : RenderPipelineAsset
{
    [Serializable] public struct Rendering
    {
        public enum Path
        { forward, deferred }
        public Path renderingPath;
    }
    [SerializeField] private Rendering rendering = default;

    // SRP Setting
    [SerializeField] private bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    // Light Per Object
    [SerializeField] private bool useLightPerObject = true;

    // Shadow Setting
    [SerializeField] private ShadowSettings shadowSettings = default;

    // Post Processing
    [SerializeField] private PostProcessingSettings postProcessingSettings = default;


    // Being invoked before rendering the first frame
    protected override RenderPipeline CreatePipeline()
    {
        if(rendering.renderingPath == Rendering.Path.forward)
        {
            Debug.Log("Switch to the Forward Pass");
            return new FirecrestRenderPipelineForwardPass
            (new bool[] {useDynamicBatching, useGPUInstancing, useSRPBatcher}, useLightPerObject, shadowSettings, postProcessingSettings);
        }
        else
        {
            Debug.Log("Switch to the Deferred Pass");
            return new FirecrestRenderPipelineDeferredPass
            (new bool[] {useDynamicBatching, useGPUInstancing, useSRPBatcher}, shadowSettings, postProcessingSettings);
        }
    }
}

}