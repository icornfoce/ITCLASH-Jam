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
    [SerializeField] private bool forceStretchWidth = true;

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
            Destroy(container.GetChild(i).gameObject);
        }

        // 2. Spawn unlocked items
        int unlockedCount = 0;
        foreach (var item in itemDataSO.items)
        {
            Debug.Log($"[DictionaryManager] Checking item: {item.itemName} | Unlocked: {item.isUnlocked}");
            if (item.isUnlocked)
            {
                unlockedCount++;
                GameObject newObj = Instantiate(itemPrefab, container);
                newObj.name = "Word_" + item.itemName;
                
                // FORCE the UI to be visible and correctly scaled
                newObj.SetActive(true);
                
                // Basic UI reset - allowing the Prefab and Layout Groups to control the look
                RectTransform rt = newObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    
                    // AUTO-SPACING: Offset each item vertically based on its index
                    // If you add a VerticalLayoutGroup to the container, it will automatically override this!
                    float yOffset = -(unlockedCount - 1) * itemHeight;
                    rt.anchoredPosition = new Vector2(0, yOffset);

                    // Force the height and width stretching logic
                    if (forceStretchWidth)
                    {
                        rt.anchorMin = new Vector2(0, 1);
                        rt.anchorMax = new Vector2(1, 1);
                        rt.pivot = new Vector2(0.5f, 1);
                        // In stretch mode, sizeDelta.x = 0 means "match parent width"
                        rt.sizeDelta = new Vector2(0, itemHeight);
                    }
                    else
                    {
                        // If not stretching, just ensure the height is set
                        rt.sizeDelta = new Vector2(rt.sizeDelta.x, itemHeight);
                    }
                    
                    Debug.Log($"[DictionaryManager] Successfully Spawned: {item.itemName} at {rt.anchoredPosition} with size {rt.sizeDelta}");
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
