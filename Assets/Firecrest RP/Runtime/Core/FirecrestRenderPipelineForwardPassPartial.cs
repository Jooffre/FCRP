using UnityEngine;
using Unity.Collections;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

namespace Firecrest
{

public partial class FirecrestRenderPipelineForwardPass
{
    partial void InitDifferentLightFallOffForEditor();

#if UNITY_EDITOR

    partial void InitDifferentLightFallOffForEditor()
    {
        Lightmapping.SetDelegate(lightsDelegate);
    }

    protected void ResetDelegate()
    {
        Lightmapping.ResetDelegate();
    }


    static Lightmapping.RequestLightsDelegate lightsDelegate
    = (Light[] lights, NativeArray<LightDataGI> output) =>
    {
        var lightData = new LightDataGI();
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            switch (light.type)
            {
                case LightType.Directional:
                    var directionalLight = new DirectionalLight();
                    LightmapperUtils.Extract(light, ref directionalLight);
                    lightData.Init(ref directionalLight);
                    break;

                case LightType.Point:
                    var pointLight = new PointLight();
                    LightmapperUtils.Extract(light, ref pointLight);
                    lightData.Init(ref pointLight);
                    break;

                case LightType.Spot:
                    var spotLight = new SpotLight();
                    LightmapperUtils.Extract(light, ref spotLight);

                    // For Unity >= 2022 Only
                    // spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
					// spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;

                    lightData.Init(ref spotLight);
                    break;

                case LightType.Area:
                    var rectangleLight = new RectangleLight();
                    LightmapperUtils.Extract(light, ref rectangleLight);
                    rectangleLight.mode = LightMode.Baked;
                    lightData.Init(ref rectangleLight);
                    break;

                default:
                    lightData.InitNoBake(light.GetInstanceID());
                    break;
            }

            lightData.falloff = FalloffType.InverseSquared;
            output[i] = lightData;
        }
    };

    # endif
}

}