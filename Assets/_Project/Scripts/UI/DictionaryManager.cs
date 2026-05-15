using UnityEngine;
using System.Collections.Generic;

public class DictionaryManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private ItemData itemDataSO;

    [Header("UI Spawning")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform container;

    [Header("Settings")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private float itemHeight = 100f;

    // ─── Global Instance ───
    public static DictionaryManager Instance { get; private set; }

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
        {
            RefreshDictionary();
        }
    }

    /// <summary>
    /// Clears the container and spawns a new UI element for every unlocked item in the ItemData ScriptableObject.
    /// </summary>
    public void RefreshDictionary()
    {
        if (itemDataSO == null || itemPrefab == null || container == null)
        {
            Debug.LogWarning("[DictionaryManager] Missing references! Please assign ItemData, Prefab, and Container in the Inspector.");
            return;
        }

        Debug.Log($"[DictionaryManager] Starting Refresh. Total items in SO: {itemDataSO.items.Count}");

        // 1. Clear existing items
        int childCount = container.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            // ใช้ DestroyImmediate เพื่อให้ Hierarchy เคลียร์ทันที ไม่ต้องรอกดจบเฟรม
            DestroyImmediate(container.GetChild(i).gameObject);
        }

        // บังคับให้ Container ยึดด้านบนสุดเสมอ
        RectTransform containerRT = container.GetComponent<RectTransform>();
        if (containerRT != null)
        {
            containerRT.anchorMin = new Vector2(0, 1);
            containerRT.anchorMax = new Vector2(1, 1);
            containerRT.pivot = new Vector2(0.5f, 1);
        }

        // 2. Spawn unlocked items
        int unlockedCount = 0;
        foreach (var item in itemDataSO.items)
        {
            if (item.isUnlocked)
            {
                unlockedCount++;
                GameObject newObj = Instantiate(itemPrefab, container);
                newObj.name = "Word_" + item.itemName;
                newObj.transform.SetAsLastSibling(); // มั่นใจว่าตัวใหม่จะอยู่ล่างสุดใน Hierarchy
                
                newObj.SetActive(true);
                
                RectTransform rt = newObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    
                    // บังคับให้ไอเทมแต่ละชิ้นยึดด้านบนของ Container
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);

                    // คำนวณตำแหน่ง Y ให้ไล่จาก 0 ลงไปเรื่อยๆ (ติดลบ)
                    float yPos = -(unlockedCount - 1) * itemHeight;
                    rt.anchoredPosition = new Vector2(0, yPos);
                    rt.sizeDelta = new Vector2(0, itemHeight);

                    Debug.Log($"[DictionaryManager] Spawned {item.itemName} at Y: {yPos}");
                }
                
                Dictionary display = newObj.GetComponent<Dictionary>();
                if (display != null)
                {
                    display.Setup(item);
                }
                else
                {
                    Debug.LogWarning($"[DictionaryManager] Prefab {itemPrefab.name} is missing the Dictionary script!");
                }
            }
        }

        Debug.Log($"[DictionaryManager] Refresh Complete. Spawned {unlockedCount} unlocked items.");
    }
}
