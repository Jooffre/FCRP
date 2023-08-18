using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    public bool enableBlockSettings = false;
    
    static int
    baseColorID = Shader.PropertyToID("_BaseColor"),
    cutoffID = Shader.PropertyToID("_Cutoff"),
    metallicId = Shader.PropertyToID("_Metallic"),
	smoothnessId = Shader.PropertyToID("_Smoothness"),
    emissionColorId = Shader.PropertyToID("_Emission");
    static MaterialPropertyBlock block;

    [SerializeField] public Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)] float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;
    [SerializeField, ColorUsage(false, true)] Color emissionColor = Color.black;

    void Awake()
    {
        OnValidate();
    }

    void OnValidate()
    {
        if (enableBlockSettings)
        {
            if (block == null)
                block = new MaterialPropertyBlock();
            
            block.SetColor(baseColorID, baseColor);
            block.SetFloat(cutoffID, cutoff);
            block.SetFloat(metallicId, metallic);
    		block.SetFloat(smoothnessId, smoothness);
            block.SetColor(emissionColorId, emissionColor);

            GetComponent<Renderer>().SetPropertyBlock(block);
        }
        else
        {
            if (block != null)
            {
                block.Clear();
            
                GetComponent<Renderer>().SetPropertyBlock(block);
            
                block = null;
            }
            else
                return;
        }
    }

}
