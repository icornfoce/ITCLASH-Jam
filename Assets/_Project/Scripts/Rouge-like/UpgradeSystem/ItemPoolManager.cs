using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemPoolManager : MonoBehaviour
{
    // --- Dependencies (Inject ผ่าน Inspector หรือ Awake) ---
    [SerializeField] private SeededRNG rng;
    [SerializeField] private PitySystem pitySystem;

    // --- Master Pool ---
    // ไอเทมทั้งหมดในเกม อย่าแก้ตรงนี้ตอน Runtime
    [SerializeField] private List<UpgradeItemSO> masterPool;

    // --- Runtime State ---
    // ไอเทมที่ถูก Banish ในรันนี้ (เคลียร์เมื่อจบด่าน)
    private HashSet<UpgradeItemSO> banishedItems = new HashSet<UpgradeItemSO>();

    private void Start()
    {
        // สร้าง Seed เริ่มต้นหากยังไม่มี (ระบบจริงอาจโหลด Seed จาก GameSession)
        if (rng != null && rng.seed == 0)
        {
            uint newSeed = (uint)System.DateTime.Now.Ticks;
            rng.Init(newSeed);
            Debug.Log($"[ItemPoolManager] Initialized Seeded RNG: {newSeed}");
        }
        if (pitySystem != null)
        {
            pitySystem.Init();
        }
    }

    // --- Public API ---
    public void ClearBanishList()
    {
        banishedItems.Clear();
    }

    public void BanishItem(UpgradeItemSO item)
    {
        if (!banishedItems.Contains(item))
        {
            banishedItems.Add(item);
        }
    }

    // playerInventory: เก็บ Item ปัจจุบันที่เพลเยอร์มีเป็น Key และบอก Level เป็น Value
    public List<UpgradeItemSO> DrawOptions(Dictionary<UpgradeItemSO, int> playerInventory, int count = 3)
    {
        // 1. FILTER
        List<UpgradeItemSO> activePool = new List<UpgradeItemSO>();
        foreach (var item in masterPool)
        {
            if (banishedItems.Contains(item)) continue;
            
            int currentLvl = 0;
            if (playerInventory != null && playerInventory.ContainsKey(item))
            {
                currentLvl = playerInventory[item];
            }
            
            if (currentLvl >= item.maxLevel) continue;
            
            activePool.Add(item);
        }

        if (activePool.Count == 0)
        {
            Debug.LogWarning("[ItemPoolManager] Active pool is empty!");
            return new List<UpgradeItemSO>();
        }

        // 2. WEIGHT CALCULATION
        foreach (var item in activePool)
        {
            item.runtimeWeight = item.baseWeight;
            item.runtimeWeight *= GetDynamicMultiplier(item, playerInventory);
            
            if (pitySystem != null)
            {
                // บวกโบนัส Pity เป็นเปอร์เซ็นต์ของเบสน้ำหนัก เช่น base 100 + (100 * bonus)
                float pityPercentage = pitySystem.GetBonusWeight(item.rarity);
                item.runtimeWeight += (item.baseWeight * pityPercentage);
            }
        }

        // 3. DRAW (Weighted Pick ไม่ซ้ำ)
        List<UpgradeItemSO> results = new List<UpgradeItemSO>();
        List<UpgradeItemSO> tempPool = new List<UpgradeItemSO>(activePool);

        int actualCount = Mathf.Min(count, tempPool.Count); // ไม่ Crash ถ้าน้อยกว่า count
        
        for (int i = 0; i < actualCount; i++)
        {
            UpgradeItemSO picked = WeightedPick(tempPool);
            if (picked != null)
            {
                results.Add(picked);
                tempPool.Remove(picked); // ตัดออก ป้องกันซ้ำ
            }
        }

        // 4. NOTIFY PITY
        if (pitySystem != null)
        {
            // Reset Miss ให้ของที่เราโชคดีจับได้
            foreach (var r in results)
            {
                pitySystem.ResetMiss(r.rarity);
            }
            
            // หา Pity ว่าเราพลาดอะไรบ้าง จากตัวเลือกที่ activePool มี แต่เราจับไม่ได้ลงผลลัพธ์
            HashSet<ItemRarity> drawnRarities = new HashSet<ItemRarity>(results.Select(x => x.rarity));
            HashSet<ItemRarity> availableRarities = new HashSet<ItemRarity>(activePool.Select(x => x.rarity));
            
            foreach (ItemRarity rarity in availableRarities)
            {
                if (!drawnRarities.Contains(rarity))
                {
                    pitySystem.RegisterMiss(rarity);
                }
            }
        }

        // 5. RETURN
        return results;
    }

    private UpgradeItemSO WeightedPick(List<UpgradeItemSO> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var item in pool)
        {
            if (item.runtimeWeight < 0) item.runtimeWeight = 0;
            totalWeight += item.runtimeWeight;
        }

        if (totalWeight <= 0f)
        {
            // Fallback: Uniform random (ถ้า weight 0 หมด หรือไม่มีของ)
            int randomIndex = rng != null ? rng.NextInt(0, pool.Count) : Random.Range(0, pool.Count);
            return pool[randomIndex];
        }

        float randomValue = rng != null ? rng.NextFloat() * totalWeight : Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var item in pool)
        {
            currentSum += item.runtimeWeight;
            if (randomValue <= currentSum)
            {
                return item;
            }
        }

        return pool[pool.Count - 1]; // ป้องกัน Edge Case ของ float precision
    }

    // Dynamic Weight Multiplier คำนวณความสอดคล้องกับของที่ถือ
    private float GetDynamicMultiplier(UpgradeItemSO item, Dictionary<UpgradeItemSO, int> inventory)
    {
        if (inventory == null || inventory.Count == 0) return 1.0f;

        // ไอเทมเป็น upgrade ของอาวุธที่ถืออยู่ (มีอยู่แล้วจะออกง่ายขึ้น)
        if (inventory.ContainsKey(item))
        {
            return 2.5f; 
        }

        // // ไอเทมเป็น synergy กับระบบอื่น (อนาคต)
        // if (item.HasTag("Fire") && HasWeaponTag(inventory, "Fire")) return 1.5f;
        
        return 1.0f; // ไม่เกี่ยวกับ build เลย
    }
}
