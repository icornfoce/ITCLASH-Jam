using System.Collections.Generic;
using UnityEngine;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance { get; private set; }

    [Header("Pool Settings")]
    public ExpGem commonGemPrefab;
    public ExpGem uncommonGemPrefab;
    public ExpGem rareGemPrefab;

    [Header("Merge Settings")]
    public float mergeCheckInterval = 5f; // เช็คเพื่อรวมร่างทุกๆ 5 วินาที
    public float mergeRadius = 10f; // รัศมีการดึงดูดมารวมร่าง
    public int mergeThreshold = 10; // ต้องมี Gem อย่างน้อย 10 อันบนพื้น ถึงจะรวมร่าง

    private List<ExpGem> _activeGems = new List<ExpGem>();
    public List<ExpGem> ActiveGems => _activeGems;
    private float _mergeTimer = 0f;

    // Simple Pool (สามารถปรับปรุงเป็น ObjectPool ของ Unity แนะนำภายหลังได้)
    private Queue<ExpGem> _commonPool = new Queue<ExpGem>();
    private Queue<ExpGem> _uncommonPool = new Queue<ExpGem>();
    private Queue<ExpGem> _rarePool = new Queue<ExpGem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        _mergeTimer += Time.deltaTime;
        if (_mergeTimer >= mergeCheckInterval)
        {
            _mergeTimer = 0f;
            CheckAndMergeGems();
        }
    }

    public void RegisterGem(ExpGem gem)
    {
        if (!_activeGems.Contains(gem))
        {
            _activeGems.Add(gem);
        }
    }

    public void UnregisterGem(ExpGem gem)
    {
        if (_activeGems.Contains(gem))
        {
            _activeGems.Remove(gem);
        }
    }

    public ExpGem SpawnGem(GemType type, Vector3 position, float baseExpOverride = -1f)
    {
        ExpGem newGem = GetFromPool(type);
        
        // แก้ไขบัค: ถ้า instantiation สร้าง object ที่กำลัง active อยู่
        // OnEnable จะถูกเรียกก่อนที่ position จะถูกตั้งค่า ทำให้จุด start ของ ExpGem ผิดเพี้ยน (เด้งหายไปจุดอื่น)
        // ต้องปิด Active ก่อนตั้งค่าตำแหน่ง แล้วค่อยเปิดใหม่
        newGem.gameObject.SetActive(false);
        newGem.transform.position = position;
        newGem.gameObject.SetActive(true);

        // สามารถ Override ค่า EXP สำหรับ Rare Gem ที่เกิดจากการยุบรวมได้
        if (baseExpOverride > 0)
        {
            newGem.baseExpValue = baseExpOverride;
        }

        return newGem;
    }

    public void ReturnToPool(ExpGem gem)
    {
        gem.gameObject.SetActive(false);
        switch (gem.gemType)
        {
            case GemType.Common:
                _commonPool.Enqueue(gem);
                break;
            case GemType.Uncommon:
                _uncommonPool.Enqueue(gem);
                break;
            case GemType.Rare:
                _rarePool.Enqueue(gem);
                break;
        }
    }

    private ExpGem GetFromPool(GemType type)
    {
        Queue<ExpGem> pool = type switch
        {
            GemType.Common => _commonPool,
            GemType.Uncommon => _uncommonPool,
            GemType.Rare => _rarePool,
            _ => _commonPool
        };

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        else
        {
            // Instantiate new if pool is empty
            ExpGem prefab = type switch
            {
                GemType.Common => commonGemPrefab,
                GemType.Uncommon => uncommonGemPrefab,
                GemType.Rare => rareGemPrefab,
                _ => commonGemPrefab
            };
            ExpGem inst = Instantiate(prefab, transform);
            inst.gameObject.SetActive(false); // ปิดแอกทีฟไว้ก่อน เพื่อให้ OnEnable ทำงานเมื่อสั่ง Spawn
            return inst;
        }
    }

    private void CheckAndMergeGems()
    {
        // 1. หาเพชรที่พร้อมรวมร่าง
        List<ExpGem> readyGems = new List<ExpGem>();
        foreach (var gem in _activeGems)
        {
            if (gem.IsReadyToMerge && gem.gemType != GemType.Rare)
            {
                readyGems.Add(gem);
            }
        }

        // 2. ถ้ามีไม่ถึงเกณฑ์ขั้นต่ำ ก็ข้ามไป
        if (readyGems.Count < mergeThreshold) return;

        // 3. จับกลุ่มตามบริเวณ (ลดความซับซ้อนด้วยการเอาระยะห่างจาก Gem ตัวแรกที่พร้อมเป็นตัวตั้ง)
        List<ExpGem> gemsToMerge = new List<ExpGem>();
        Vector3 clusterCenter = readyGems[0].transform.position;
        float totalExp = 0f;

        foreach (var gem in readyGems)
        {
            if (Vector3.Distance(gem.transform.position, clusterCenter) <= mergeRadius)
            {
                gemsToMerge.Add(gem);
                totalExp += gem.baseExpValue;
            }
        }

        // 4. ถ้าจับกลุ่มได้ถึงเวลาที่กำหนด ให้ทำการรวมร่าง
        if (gemsToMerge.Count >= mergeThreshold)
        {
            foreach (var gem in gemsToMerge)
            {
                ReturnToPool(gem);
            }

            // สปอน Rare Gem ออกมาแทนที่
            SpawnGem(GemType.Rare, clusterCenter, totalExp);
            Debug.Log($"Merged {gemsToMerge.Count} gems into 1 Rare Gem with {totalExp} EXP.");
        }
    }

    // ฟังก์ชันสำหรับ Dev Panel เรียกใช้ เพื่อรวมร่างอัญมณีแบบบังคับ
    public void ForceMergeGems(Vector3 centerPosition, float searchRadius)
    {
        List<ExpGem> gemsToMerge = new List<ExpGem>();
        float totalExp = 0f;

        foreach (var gem in _activeGems)
        {
            if (gem.gemType != GemType.Rare)
            {
                if (Vector3.Distance(gem.transform.position, centerPosition) <= searchRadius)
                {
                    gemsToMerge.Add(gem);
                    totalExp += gem.baseExpValue;
                }
            }
        }

        if (gemsToMerge.Count > 1)
        {
            Vector3 averagePos = Vector3.zero;
            foreach (var gem in gemsToMerge)
            {
                averagePos += gem.transform.position;
                ReturnToPool(gem);
            }
            averagePos /= gemsToMerge.Count;

            SpawnGem(GemType.Rare, averagePos, totalExp);
            Debug.Log($"[DEV] Forced Merge: {gemsToMerge.Count} gems into 1 Rare Gem with {totalExp} EXP.");
        }
        else
        {
            Debug.Log("[DEV] Forced Merge: Not enough gems found in radius to merge (need at least 2).");
        }
    }
}
