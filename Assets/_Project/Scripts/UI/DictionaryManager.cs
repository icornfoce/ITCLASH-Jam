using UnityEngine;
using System.Collections.Generic;

public class DictionaryManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private ItemData itemDataSO;

    [Header("UI Spawning")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform container;

    [Tooltip("Panel หลักที่มี Header 'List' (ถ้าไม่ใส่จะหาจาก Container อัตโนมัติ)")]
    [SerializeField] private GameObject listPanel;

    [Header("Settings")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private float itemHeight = 100f;

    // ─── Global Instance ───
    public static DictionaryManager Instance { get; private set; }

    private bool _isDestroying = false;

    private void Awake()
    {
        // Setup Singleton — เก็บเฉพาะตัวเดียว, ซ่อน UI ของตัวที่ซ้ำให้หมด
        if (Instance != null && Instance != this)
        {
            // ถ้าตัวเดิมไม่มี container แต่ตัวนี้มี → เปลี่ยนตัว
            if (Instance.container == null && this.container != null)
            {
                Instance.HideAndDestroy();
                Instance = this;
                return;
            }

            // ตัวนี้เป็น duplicate → ทำลายตัวเอง
            HideAndDestroy();
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// ซ่อน UI ทั้งหมดของ DictionaryManager นี้แล้วทำลาย
    /// </summary>
    private void HideAndDestroy()
    {
        _isDestroying = true;

        // 1. ซ่อน List Panel (ถ้ามี)
        GameObject panel = GetListPanel();
        if (panel != null) panel.SetActive(false);

        // 2. ซ่อน Container (กรณี panel ไม่ได้ถูก assign แต่ container อยู่คนละ GO)
        if (container != null) container.gameObject.SetActive(false);

        // 3. ซ่อนตัวเอง
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// หา List Panel: ใช้ listPanel ที่ assign ไว้ ถ้าไม่มีให้ไล่ขึ้นจาก container
    /// จะหยุดก่อนถึง Canvas (เพราะ Canvas = UI ทั้งระบบ ห้ามปิด)
    /// </summary>
    private GameObject GetListPanel()
    {
        if (listPanel != null) return listPanel;
        if (container == null) return null;

        // ไล่ขึ้นจาก container → หา parent ที่อยู่ใต้ Canvas พอดี
        Transform current = container;
        while (current.parent != null)
        {
            // ถ้า parent เป็น Canvas แล้ว → current คือ List panel
            if (current.parent.GetComponent<Canvas>() != null)
                return current.gameObject;
            current = current.parent;
        }

        // ถ้าหา Canvas ไม่เจอ → ใช้ parent ตรงๆ ของ container
        if (container.parent != null) return container.parent.gameObject;

        return null;
    }

    private void OnEnable()
    {
        // ป้องกัน duplicate ที่กำลังจะถูกทำลายไม่ให้ Refresh
        if (_isDestroying) return;

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
