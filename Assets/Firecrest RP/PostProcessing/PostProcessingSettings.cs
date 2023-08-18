using System;
using UnityEngine;
using UnityEngine.Rendering;


namespace Firecrest
{
[CreateAssetMenu(menuName = "Custom Rendering/Firecrest RP/Post Processing Settings")]
public class PostProcessingSettings : ScriptableObject
{
    //[SerializeField] Shader shader = default;
    //[NonSerialized] Material material;


    [Serializable] public struct BloomSettings
    {
		public bool enableBloom;
        [Min(0f)] public float threshold;
        [Min(0f)] public float intensity;
		[Range(0f, 1f)] public float kneeFactor; // knee threshold factor
        [ColorUsage(false)] public Color tint;
        public bool highQuailty;
        public bool fadeFireflies;
        public enum Mode { Additive, Scattering }
		public Mode mode;
		[Range(0.05f, 0.95f)] public float scatter;
		[Range(1f, 16f)] public int maxIterations;

		[Min(1f)] public int minDownscalePixels;
	}

	[Serializable] public struct ColorAdjustmentsSettings
    {

		public bool enableColorAdjustments;
		public float postExposure;
		[Range(-100f, 100f)] public float contrast;
		[ColorUsage(false, true)] public Color colorFilter;
		[Range(-180f, 180f)] public float hueShift;
		[Range(-100f, 100f)] public float saturation;
	}

    [Serializable] public struct ToneMappingSettings
    {
		public bool enableToneMapping;
		public enum Mode
        {Clamp, Neutral, ACES, Reinhard }
		public Mode mode;
	}

	[SerializeField] BloomSettings bloom = new BloomSettings
    {
		enableBloom = false,
		tint = Color.white,
		fadeFireflies = true,
		scatter = 0.5f
	};

	public BloomSettings Bloom => bloom;

	[SerializeField] ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings
	{
		enableColorAdjustments = false,
		colorFilter = Color.white
	};
	public ColorAdjustmentsSettings colorAdjustmentsSettings => colorAdjustments;

	[SerializeField] ToneMappingSettings toneMapping = new ToneMappingSettings
	{ enableToneMapping = false };

	public ToneMappingSettings ToneMapping => toneMapping;
}

}