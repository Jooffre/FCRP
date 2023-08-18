using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace Firecrest
{
public class CustomShaderGUI : ShaderGUI
{
    MaterialEditor materialEditor;
	MaterialProperty[] materialProperties;
	Object[] materials;

	private bool foldOutBasic = true;
    private bool showPresets;
	
	private PropertyLib.LitProperties Property;

	enum ShadowMode {On, Clip, Dither, Off}


    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
    {
		this.materialEditor = materialEditor;
		this.materialProperties = materialProperties;
		this.materials = materialEditor.targets;
		
		FindProperties(materialProperties);
		
		//EditorGUI.BeginChangeCheck();		

		if (GUILayout.Button("Fundamental Settings", GUILayout.Height(20)))
			foldOutBasic = !foldOutBasic;

		if(foldOutBasic)
			DrawShaderGUI();

    }

	private void FindProperties(MaterialProperty[] materialProperties)
	{
		Property = new PropertyLib.LitProperties(materialProperties);
	}


	private void ResetGUIWidth()
	{
		EditorGUIUtility.fieldWidth = 0; // 0 means resetting to default width
		EditorGUIUtility.labelWidth = 0;
	}


# region Draw ShaderGUI

	private void DrawShaderGUI()
	{
		// don't use base.OnGUI(materialEditor, properties);
		// it will introduce all properties in order which is not what we want,
		// we hope to customize our own panel
		
		EditorGUI.BeginChangeCheck();

		DrawSurface();
		DrawSplitLine();
		DrawTransparencyAndBlend();
		DrawSplitLine();
		DrawShadow();
		DrawSplitLine();
		DrawOtherOptions();
		
		if (EditorGUI.EndChangeCheck())
		{
			SetShadowCasterPass();
			BakeEmission();
			CopyLightMappingProperties();
		}
	}

	private void DrawSurface()
	{	
		GUILayout.Label("Surface", EditorStyles.boldLabel); // bold label
		EditorGUILayout.Space(); // small space
		EditorGUI.indentLevel ++;

		GUIContent content = new GUIContent(Property.tex.displayName, Property.tex.textureValue, "Setting surface color and texture.");
		materialEditor.SetDefaultGUIWidths(); // tight layout, to make UI short
		materialEditor.TexturePropertySingleLine(content, Property.tex, Property.tint);
		ResetGUIWidth();
		materialEditor.TextureScaleOffsetProperty(Property.tex);

		materialEditor.SetDefaultGUIWidths();
		materialEditor.ShaderProperty(Property.renderFace, "Render Face");
		switch(Property.renderFace.floatValue)
		{
			case 0.0f:
				SetProperty("_Cull", 0.0f);
				break;
			case 1.0f:
				SetProperty("_Cull", 1.0f);
				break;
			case 2.0f:
				SetProperty("_Cull", 2.0f);
				break;
		}
		materialEditor.ShaderProperty(Property.workflow, "Workflow");
		ResetGUIWidth();

		if (Property.workflow.floatValue == 0)
		{
			SetProperty("_WORKFLOW", "_SPECULAR", false);
			DrawMetallicPanel();
		}
		else
		{
			SetProperty("_WORKFLOW", "_SPECULAR", true);
			DrawSpecularPanel();
		}

		materialEditor.SetDefaultGUIWidths();
		materialEditor.ShaderProperty(Property.enableEmission, "Enable Emission");
		if (Property.enableEmission.floatValue == 1)
			DrawEmissionPanel();
		//ResetGUIWidth();

		EditorGUI.indentLevel --;
		EditorGUILayout.Space();
		//GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight); // large space
	}


	private void DrawTransparencyAndBlend()
	{	
		GUILayout.Label("Transparecy & Blend Mode ", EditorStyles.boldLabel);
		EditorGUILayout.Space();
		EditorGUI.indentLevel ++;
		
		DrawBlendPresets();
		
		materialEditor.SetDefaultGUIWidths();
		
		materialEditor.ShaderProperty(Property.premulAlpha, "Premultiply Alpha");
		materialEditor.ShaderProperty(Property.alphaClip, "Alpha Clipping");
		materialEditor.ShaderProperty(Property.cutoff, "Alpha Cutoff");
		
		materialEditor.ShaderProperty(Property.src, "Source");
		materialEditor.ShaderProperty(Property.dst, "Destination");
		materialEditor.RenderQueueField();
		ResetGUIWidth();

		EditorGUI.indentLevel --;
		EditorGUILayout.Space();
	}


	private void DrawShadow()
	{	
		GUILayout.Label("Shadow", EditorStyles.boldLabel);
		EditorGUILayout.Space();
		EditorGUI.indentLevel ++;

		materialEditor.SetDefaultGUIWidths();
		materialEditor.ShaderProperty(Property.shadow, "Shadow Mode");
		materialEditor.ShaderProperty(Property.receiveShadow, "Receive Shadows");
		ResetGUIWidth();

		EditorGUI.indentLevel --;
		EditorGUILayout.Space();
	}

	private void DrawOtherOptions()
	{
		GUILayout.Label("Others", EditorStyles.boldLabel);
		EditorGUILayout.Space();
		EditorGUI.indentLevel ++;

		materialEditor.SetDefaultGUIWidths();
		materialEditor.EnableInstancingField();
		materialEditor.DoubleSidedGIField();
		materialEditor.LightmapEmissionProperty();
		
		
		ResetGUIWidth();

		EditorGUI.indentLevel --;
	}

	private void DrawEmissionPanel()
	{
		EditorGUI.indentLevel ++;

		materialEditor.TexturePropertySingleLine(MakeGUIContent("Emission", "Set emssion map."), Property.emissionMap, Property.emission);
		ResetGUIWidth();
		materialEditor.TextureScaleOffsetProperty(Property.emissionMap);

		EditorGUI.indentLevel --;
	}


	private void BakeEmission()
	{
		foreach (Material material in materialEditor.targets)
		{
			material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
		}
	}

	private void DrawMetallicPanel()
	{
		EditorGUI.indentLevel ++;

		materialEditor.TexturePropertySingleLine(MakeGUIContent("Metallic", "Set metallic map."), Property.metallicMap, Property.metallic);
		materialEditor.TextureScaleOffsetProperty(Property.metallicMap);
		materialEditor.TexturePropertySingleLine(MakeGUIContent("Smoothness", "Set smoothness map."), Property.smoothMap, Property.smoothness);
		materialEditor.TextureScaleOffsetProperty(Property.smoothMap);
		EditorGUI.indentLevel --;
	}


	private void DrawSpecularPanel()
	{
		EditorGUI.indentLevel ++;

		materialEditor.SetDefaultGUIWidths();
		materialEditor.TexturePropertySingleLine(MakeGUIContent("Specular", "Set Specular map."), Property.specularMap, Property.specular);
		ResetGUIWidth();
		materialEditor.TextureScaleOffsetProperty(Property.specularMap);
		materialEditor.TexturePropertySingleLine(MakeGUIContent("Smoothness", "Set smoothness map."), Property.smoothMap, Property.smoothness);
		materialEditor.TextureScaleOffsetProperty(Property.smoothMap);

		EditorGUI.indentLevel --;
	}


	private GUIContent MakeGUIContent(string text, string tooltip = null)
	{
		GUIContent content = new GUIContent(text, tooltip);
		return content;
	}


	private void DrawSplitLine()
	{
		EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("—————————————————————");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
	}

# endregion


	// Unity applys hard coding to inner properties _MainTex & _Color
	// to bake transparent or clipped object correctly,
	// we have to assgin the same values from exsited properties to them
	/// <summary>
	/// Copy properties for baking transparent or clipped object.
	/// </summary>
	private void CopyLightMappingProperties()
	{
		MaterialProperty mainTex = FindProperty("_MainTex", materialProperties, false);
		MaterialProperty baseMap = FindProperty("_BaseMap", materialProperties, false);
		if (mainTex != null && baseMap != null)
		{
			mainTex.textureValue = baseMap.textureValue;
			mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
		}
		MaterialProperty color = FindProperty("_Color", materialProperties, false);
		MaterialProperty baseColor = FindProperty("_BaseColor", materialProperties, false);
		if (color != null && baseColor != null)
		{
			color.colorValue = baseColor.colorValue;
		}
	}


    private bool HasProperty(string name) => FindProperty(name, materialProperties, false) != null;
    private bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");


    private bool SetProperty (string name, float value)
    {
		MaterialProperty property = FindProperty(name, materialProperties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }
        else
            return false;
	}

	
    private void SetProperty (string name, string keyword, bool value)
    {
		if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyword(keyword, value);
        }
	}


    private void SetKeyword (string keyword, bool enabled)
    {
		if (enabled)
        {
			foreach (Material m in materials)
            {
				m.EnableKeyword(keyword);
			}
		}
		else
        {
			foreach (Material m in materials)
            {
				m.DisableKeyword(keyword);
			}
		}
	}


	private void SetShadowCasterPass()
	{
		MaterialProperty shadows = FindProperty("_Shadows", materialProperties, true);
		//if (shadows == null || shadows.hasMixedValue) return;
		
		bool enabled = shadows.floatValue < (float)ShadowMode.Off;
		
		foreach (Material m in materials)
		{
			m.SetShaderPassEnabled("ShadowCaster", enabled);
		}
	}
    

# region Presets Settings

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }
    
	private bool PremultiplyAlpha
    {
		set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
	}

	private BlendMode SrcBlend
    {
		set => SetProperty("_SrcBlend", (float)value);
	}

	private BlendMode DstBlend
    {
		set => SetProperty("_DstBlend", (float)value);
	}

	private bool ZWrite
    {
		set => SetProperty("_ZWrite", value ? 1f : 0f);
	}

	/*
	ShadowMode Shadows
	{
		set
		{
			if (SetProperty("_Shadows", (float)value))
			{
				SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
				SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
			}
		}
	}
	*/

    private RenderQueue RenderQueue
    {
		set
        {
			foreach (Material m in materials)
            {
				m.renderQueue = (int)value;
			}
		}
	}


    bool PresetButton (string name)
    {
		if (GUILayout.Button(name))
        {
			materialEditor.RegisterPropertyChangeUndo(name);
			return true;
		}
		return false;
	}
# endregion


# region Render Presets

    void OpaquePreset()
    {
		if (PresetButton("Opaque"))
        {
			Clipping = false;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.Zero;
			ZWrite = true;
			RenderQueue = RenderQueue.Geometry;
		}
	}

    void ClipPreset()
    {
		if (PresetButton("Clip")) {
			Clipping = true;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.Zero;
			ZWrite = true;
			RenderQueue = RenderQueue.AlphaTest;
		}
	}

	void FadePreset()
    {
		if (PresetButton("Fade"))
        {
			Clipping = false;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.SrcAlpha;
			DstBlend = BlendMode.OneMinusSrcAlpha;
			ZWrite = false;
			RenderQueue = RenderQueue.Transparent;
		}
	}

    void TransparentPreset()
    {
		if (HasPremultiplyAlpha && PresetButton("Transparent"))
        {
			Clipping = false;
			PremultiplyAlpha = true;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.OneMinusSrcAlpha;
			ZWrite = false;
			RenderQueue = RenderQueue.Transparent;
		}
	}

	void DrawBlendPresets()
	{
		showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
		if (showPresets)
        {
			GUILayout.BeginHorizontal();
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
			GUILayout.EndHorizontal();
        }
	}

# endregion

}

}