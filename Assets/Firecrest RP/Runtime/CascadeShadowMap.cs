using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CascadeShadowMap
{
    // 4 sub-bounding boxes
    Vector3[] bb_0, bb_1, bb_2, bb_3;

    public float[] splits = {0.07f, 0.13f, 0.25f, 0.55f};

    Vector3[] farPlane = new Vector3[4];
    Vector3[] nearPlane = new Vector3[4];

    Vector3[] nearPlane_0 = new Vector3[4], farPlane_0 = new Vector3[4];
    Vector3[] nearPlane_1 = new Vector3[4], farPlane_1 = new Vector3[4];
    Vector3[] nearPlane_2 = new Vector3[4], farPlane_2 = new Vector3[4];
    Vector3[] nearPlane_3 = new Vector3[4], farPlane_3 = new Vector3[4];


    // we have to reserve the original Main camera settings,
    // because we will modify its properties to render shadows
    struct OriginalCameraSettings
    {
        public Vector3 position;
        public Quaternion rotation;
        public float nearClipPlane;
        public float farClipPlane;
        public float aspect;
    };

    OriginalCameraSettings originalCameraSettings;


    private Vector3 HomogeneousCoordTransform(Vector3 inputCoord, float coord_w, Matrix4x4 transformMat)
    {
        Vector4 coordTemp = new Vector4(inputCoord.x, inputCoord.y, inputCoord.z, coord_w);
        coordTemp = transformMat * coordTemp;

        return new Vector3(coordTemp.x, coordTemp.y, coordTemp.z); // the last component is 1
    }


    // returns AABB box coords in light space
    private Vector3[] TransformFrustrumBoundingBoxToLightSpace
    (
        Vector3[] nearPlane,
        Vector3[] farPlane,
        Vector3 lightDir
    ){
        Matrix4x4 toLightSpaceMat = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        Matrix4x4 toLightSpaceMatInv = toLightSpaceMat.inverse;

        // to get the frustrum coords in original space after transformation
        for (int i = 0; i < 4; i++)
        {
            nearPlane[i] = HomogeneousCoordTransform(nearPlane[i], 1.0f, toLightSpaceMatInv);
            farPlane[i] = HomogeneousCoordTransform(farPlane[i], 1.0f, toLightSpaceMatInv);
        }

        // AABB bounding box
        float[] bb_x_coords_set = new float[8];
        float[] bb_y_coords_set = new float[8];
        float[] bb_z_coords_set = new float[8];

        for (int i = 0; i < 4; i++)
        {
            bb_x_coords_set[i] = nearPlane[i].x; bb_x_coords_set[i + 4] = farPlane[i].x;
            bb_y_coords_set[i] = nearPlane[i].y; bb_y_coords_set[i + 4] = farPlane[i].y;
            bb_z_coords_set[i] = nearPlane[i].z; bb_z_coords_set[i + 4] = farPlane[i].z;
        }

        float
        bb_x_coords_min = Mathf.Min(bb_x_coords_set), bb_x_coords_max = Mathf.Max(bb_x_coords_set),
        bb_y_coords_min = Mathf.Min(bb_y_coords_set), bb_y_coords_max = Mathf.Max(bb_y_coords_set),
        bb_z_coords_min = Mathf.Min(bb_z_coords_set), bb_z_coords_max = Mathf.Max(bb_z_coords_set);

        // build coords of the bounding box in world space after transformation
        Vector3[] bb_vertices_posWS =
        {
            new Vector3(bb_x_coords_min, bb_y_coords_min, bb_z_coords_min), new Vector3(bb_x_coords_min, bb_y_coords_min, bb_z_coords_max),
            new Vector3(bb_x_coords_min, bb_y_coords_max, bb_z_coords_min), new Vector3(bb_x_coords_min, bb_y_coords_max, bb_z_coords_max),
            new Vector3(bb_x_coords_max, bb_y_coords_min, bb_z_coords_min), new Vector3(bb_x_coords_max, bb_y_coords_min, bb_z_coords_max),
            new Vector3(bb_x_coords_max, bb_y_coords_max, bb_z_coords_min), new Vector3(bb_x_coords_max, bb_y_coords_max, bb_z_coords_max)
        };
        for(int i = 0; i < 8; i++)
        {
            bb_vertices_posWS[i] = HomogeneousCoordTransform(bb_vertices_posWS[i], 1.0f, toLightSpaceMat);
        }
        
        // reset the original frustrum coords
        for(int i = 0; i < 4; i++)
        {
            farPlane[i] = HomogeneousCoordTransform(farPlane[i], 1.0f, toLightSpaceMat);
            nearPlane[i] = HomogeneousCoordTransform(nearPlane[i], 1.0f, toLightSpaceMat);
        }

        return bb_vertices_posWS;
    }


    // configurate shadow bounding boxes
    public void UpdateCascadeShadowBox(Camera camera, Vector3 lightDir)
    {
        // get camera's frustrum
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearPlane);
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farPlane);

        // transform frustrum to world space
        for (int i = 0; i < 4; i++)
        {
            nearPlane[i] = camera.transform.TransformVector(nearPlane[i]) + camera.transform.position;
            farPlane[i] = camera.transform.TransformVector(farPlane[i]) + camera.transform.position;
        }


        // divide frustrum according to the preseted ratio
        for(int i = 0; i < 4; i++)
        {
            Vector3 dir = farPlane[i] - nearPlane[i];

            nearPlane_0[i] = nearPlane[i];
            farPlane_0[i] = nearPlane_0[i] + dir * splits[0];

            nearPlane_1[i] = farPlane_0[i];
            farPlane_1[i] = nearPlane_1[i] + dir * splits[1];

            nearPlane_2[i] = farPlane_1[i];
            farPlane_2[i] = nearPlane_2[i] + dir * splits[2];
            
            nearPlane_3[i] = farPlane_2[i];
            farPlane_3[i] = nearPlane_3[i] + dir * splits[3];
        }


        // calculate the light space coords for each sub-bounding box
        bb_0 = TransformFrustrumBoundingBoxToLightSpace(nearPlane_0, farPlane_0, lightDir);
        bb_1 = TransformFrustrumBoundingBoxToLightSpace(nearPlane_1, farPlane_1, lightDir);
        bb_2 = TransformFrustrumBoundingBoxToLightSpace(nearPlane_2, farPlane_2, lightDir);
        bb_3 = TransformFrustrumBoundingBoxToLightSpace(nearPlane_3, farPlane_3, lightDir);
    }


    // set camera properties with the corresponding level of shadow cascade
    // it will be used for rendering shadow map
    public void PrepareCameraForShadowsAtlas(ref Camera camera, Vector3 lightDir, int level, float distance)
    {
        // determine the level of cascade
        Vector3[] box = new Vector3[8];
        
        if (level == 0) {box = bb_0;}
        if (level == 1) {box = bb_1;}
        if (level == 2) {box = bb_2;}
        if (level == 3) {box = bb_3;}

        // calculate the center point, width and height of the box
        Vector3 centerPoint = (box[3] - box[4]) / 2;
        float width = Vector3.Magnitude(box[0] - box[4]);
        float height = Vector3.Magnitude(box[0] - box[2]);

        // set camera
        camera.transform.rotation = Quaternion.LookRotation(lightDir);
        camera.transform.position = centerPoint; 
        camera.nearClipPlane = -1 * distance;
        camera.farClipPlane = distance;
        camera.aspect = width / height;
        camera.orthographicSize = height * 0.5f;
    }


    public void BackupOriginalCameraSettings(ref Camera camera)
    {
        originalCameraSettings.position = camera.transform.position;
        originalCameraSettings.rotation = camera.transform.rotation;
        originalCameraSettings.nearClipPlane =camera.nearClipPlane;
        originalCameraSettings.farClipPlane = camera.farClipPlane;
        originalCameraSettings.aspect = camera.aspect;

        // set it being orthogonal
        camera.orthographic = true;
    }


    public void RevertOriginalCameraSettings(ref Camera camera)
    {
        camera.transform.position = originalCameraSettings.position;
        camera.transform.rotation = originalCameraSettings.rotation;
        camera.nearClipPlane = originalCameraSettings.nearClipPlane;
        camera.farClipPlane = originalCameraSettings.farClipPlane;
        camera.aspect = originalCameraSettings.aspect;
        
        camera.orthographic = false;
    }


# region visualization bounding boxes
    private void DrawFrustum(Vector3[] nearCorners, Vector3[] farCorners, Color color)
    {
        for (int i = 0; i < 4; i++)
            Debug.DrawLine(nearCorners[i], farCorners[i], color);

        Debug.DrawLine(farCorners[0], farCorners[1], color);
        Debug.DrawLine(farCorners[0], farCorners[3], color);
        Debug.DrawLine(farCorners[2], farCorners[1], color);
        Debug.DrawLine(farCorners[2], farCorners[3], color);
        Debug.DrawLine(nearCorners[0], nearCorners[1], color);
        Debug.DrawLine(nearCorners[0], nearCorners[3], color);
        Debug.DrawLine(nearCorners[2], nearCorners[1], color);
        Debug.DrawLine(nearCorners[2], nearCorners[3], color);
    }


    void DrawAABB(Vector3[] points, Color color)
    {
        // draw auxiliary lines
        Debug.DrawLine(points[0], points[1], color);
        Debug.DrawLine(points[0], points[2], color);
        Debug.DrawLine(points[0], points[4], color);

        Debug.DrawLine(points[6], points[2], color);
        Debug.DrawLine(points[6], points[7], color);
        Debug.DrawLine(points[6], points[4], color);

        Debug.DrawLine(points[5], points[1], color);
        Debug.DrawLine(points[5], points[7], color);
        Debug.DrawLine(points[5], points[4], color);

        Debug.DrawLine(points[3], points[1], color);
        Debug.DrawLine(points[3], points[2], color);
        Debug.DrawLine(points[3], points[7], color);
    }


    public void DebugDraw()
    {
        DrawFrustum(nearPlane, farPlane, Color.white);
        DrawAABB(bb_0, Color.yellow);  
        DrawAABB(bb_1, Color.magenta);
        DrawAABB(bb_2, Color.green);
        DrawAABB(bb_3, Color.cyan);
    }
# endregion

}