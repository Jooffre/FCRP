using UnityEngine;
using UnityEngine.Rendering;

namespace Firecrest
{
    public class CameraPropertiesSetting
    {
        public static void SetProperties(CommandBuffer buffer, Camera camera)
        {
            var viewMatrix = camera.worldToCameraMatrix;
            var projectMatrixDirect = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            var matrixVPDirect = projectMatrixDirect * viewMatrix;
            var invMatrixVPDirect = matrixVPDirect.inverse;

            buffer.SetGlobalMatrix(CameraShaderProperties.CameraMatrixVPInv, invMatrixVPDirect);
        }
    }


    public static class CameraShaderProperties
    {
        public static readonly int CameraMatrixVPInv = Shader.PropertyToID("_CameraMatrixVPInv");
    }
}

