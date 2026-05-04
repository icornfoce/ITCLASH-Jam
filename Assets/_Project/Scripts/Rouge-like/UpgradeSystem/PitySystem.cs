using System.Collections.Generic;
using UnityEngine;

public class PitySystem : MonoBehaviour
{
    private Dictionary<ItemRarity, int> missCount = new Dictionary<ItemRarity, int>();

    [Header("Pity Bonus Config (Percentage Multiplier)")]
    public float bonusPerMissRare = 0.15f;    // พลาด 1 ครั้ง +15%
    public float bonusPerMissEpic = 0.25f;    // พลาด 1 ครั้ง +25%
    public float bonusPerMissLegendary = 0.5f;// พลาด 1 ครั้ง +50%

    public void Init()
    {
        missCount[ItemRarity.Common] = 0;
        missCount[ItemRarity.Uncommon] = 0;
        missCount[ItemRarity.Rare] = 0;
        missCount[ItemRarity.Epic] = 0;
        missCount[ItemRarity.Legendary] = 0;
    }

    public float GetBonusWeight(ItemRarity rarity)
    {
        if (!missCount.ContainsKey(rarity)) return 0f;

        int misses = missCount[rarity];
        
        switch(rarity)
        {
            case ItemRarity.Rare: return misses * bonusPerMissRare;
            case ItemRarity.Epic: return misses * bonusPerMissEpic;
            case ItemRarity.Legendary: return misses * bonusPerMissLegendary;
            default: return 0f;
        }
    }

    public void RegisterMiss(ItemRarity rarity)
    {
        if (missCount.ContainsKey(rarity))
        {
            missCount[rarity]++;
        }
    }

    public void ResetMiss(ItemRarity rarity)
    {
        if (missCount.ContainsKey(rarity))
        {
            missCount[rarity] = 0;
        }
    }
}
