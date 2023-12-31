using System;
using UnityEngine;


namespace Firecrest
{

// this class provides preseted params of global shadow and exposes them to the inspector

[Serializable]
public class ShadowSettings
{
    // max distance for shadow rendering
    [Min(0.001f)] public float maxDistance = 100f;
	[Range(0.001f, 1f)]public float distanceFade = 0.1f;
	public Texture2D shadowRampMap;


    // size of shadow map
    public enum TextureSize
    {
		_256 = 256, _512 = 512, _1024 = 1024,
		_2048 = 2048, _4096 = 4096, _8192 = 8192
	}


	// PCF filtering
	public enum FilterMode
	{
		PCF2x2, PCF3x3, PCF5x5, PCF7x7
	}


	// atlas params for directional light
	[Serializable]public struct Directional
    {
		public TextureSize atlasSize;

		public FilterMode filter;

		[Range(1, 4)] public int cascadeCount;
		[Range(0f, 1f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
		public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
		[Range(0.001f, 1f)] public float cascadeFade;

		public enum CascadeBlendMode
		{
			Hard, Soft, Dither
		}

		public CascadeBlendMode cascadeBlend;

	}


	// create an atlas setting instance
	public Directional dirLightShadowAtlasSettings = new Directional
	{
		atlasSize = TextureSize._1024,
		filter = FilterMode.PCF5x5,
		cascadeCount = 4,
		cascadeRatio1 = 0.1f,
		cascadeRatio2 = 0.25f,
		cascadeRatio3 = 0.5f,
		cascadeFade = 0.1f,
		cascadeBlend = Directional.CascadeBlendMode.Soft
	};

	
	[Serializable] public struct OtherLightShadowSettings
	{
		public TextureSize atlasSize;
		public FilterMode filter; 
	}


	public OtherLightShadowSettings otherLightShadowSettings = new OtherLightShadowSettings
	{
		atlasSize = TextureSize._1024,
		filter = FilterMode.PCF5x5
	};
}

}