using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RarityVisualData
{
    public ItemRarity rarity;
    public Texture2D frameTexture;
    public Color frameColor = Color.white;
    public GameObject framePrefab; // For complex visual effects or specialized UI
}

[CreateAssetMenu(fileName = "UpgradeRarityConfig", menuName = "Rouge-like/Rarity Config")]
public class UpgradeRarityConfigSO : ScriptableObject
{
    public List<RarityVisualData> rarityVisuals = new List<RarityVisualData>();

    public RarityVisualData GetVisualData(ItemRarity rarity)
    {
        return rarityVisuals.Find(v => v.rarity == rarity);
    }
}
