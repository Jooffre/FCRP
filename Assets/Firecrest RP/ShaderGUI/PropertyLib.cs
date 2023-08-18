using UnityEditor;

namespace Firecrest
{

public class PropertyLib : ShaderGUI
{
	public struct LitProperties
	{
		public MaterialProperty tint;
        public MaterialProperty tex;
        public MaterialProperty workflow;
        public MaterialProperty metallicMap;
        public MaterialProperty metallic;
        public MaterialProperty specularMap;
        public MaterialProperty specular;
        public MaterialProperty smoothness;
        public MaterialProperty smoothMap;
        public MaterialProperty emission;
        public MaterialProperty enableEmission;
        public MaterialProperty emissionMap;
        public MaterialProperty renderFace;
        public MaterialProperty premulAlpha;
        public MaterialProperty alphaClip;
        public MaterialProperty cutoff;
        public MaterialProperty src;
        public MaterialProperty dst;
        public MaterialProperty shadow;
        public MaterialProperty receiveShadow;
  

        // init
        public LitProperties(MaterialProperty[] properties)
        {
            tint = ShaderGUI.FindProperty("_BaseColor", properties, true);
            tex = ShaderGUI.FindProperty("_BaseMap", properties, true);
            workflow = ShaderGUI.FindProperty("_Workflow", properties, true);
            metallicMap = ShaderGUI.FindProperty("_WorkflowMap", properties, true);
            metallic = ShaderGUI.FindProperty("_Metallic", properties, true);
            specularMap = ShaderGUI.FindProperty("_WorkflowMap", properties, true);
            specular = ShaderGUI.FindProperty("_Specular", properties, true);
            smoothness = ShaderGUI.FindProperty("_Smoothness", properties, true);
            smoothMap = ShaderGUI.FindProperty("_SmoothMap", properties, true);
            emission = ShaderGUI.FindProperty("_Emission", properties, true);
            enableEmission = ShaderGUI.FindProperty("_EnableEmission", properties, true);
            emissionMap = ShaderGUI.FindProperty("_EmissionMap", properties, true);
            renderFace = ShaderGUI.FindProperty("_RenderFace", properties, true);
            premulAlpha = ShaderGUI.FindProperty("_PremulAlpha", properties, true);
            alphaClip = ShaderGUI.FindProperty("_Clipping", properties, true);
            cutoff = ShaderGUI.FindProperty("_Cutoff", properties, true);
            src = ShaderGUI.FindProperty("_SrcBlend", properties, true);
            dst = ShaderGUI.FindProperty("_DstBlend", properties, true);
            shadow = ShaderGUI.FindProperty("_Shadows", properties, true);
            receiveShadow = ShaderGUI.FindProperty("_ReceiveShadows", properties, true);
        }
	}

}

}