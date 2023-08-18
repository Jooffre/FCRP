using UnityEngine;


// visualizing the bounding boxes of generated cascade shadows
// add this script to the camera you want to visualized
[ExecuteAlways]
public class VisualizedCSM : MonoBehaviour
{
    CascadeShadowMap cascadeShadowMap;

    void Update()
    {
        Camera mainCam = Camera.main;

        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;


        if(cascadeShadowMap == null)
        {
            cascadeShadowMap = new CascadeShadowMap();
        }
           
        cascadeShadowMap.UpdateCascadeShadowBox(mainCam, lightDir);

        cascadeShadowMap.DebugDraw();
    }
}