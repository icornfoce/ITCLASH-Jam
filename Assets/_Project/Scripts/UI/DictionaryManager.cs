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
            if (item.isUnlocked)
            {
                unlockedCount++;
                GameObject newObj = Instantiate(itemPrefab, container);
                
                // FORCE the UI to be visible and correctly scaled
                newObj.SetActive(true);
                RectTransform rt = newObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Force the item to stretch horizontally and align to the top
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    
                    rt.localScale = Vector3.one;
                    rt.localPosition = Vector3.zero;
                    rt.anchoredPosition3D = Vector3.zero;
                    
                    // Force a default height since width is now handled by anchors
                    rt.sizeDelta = new Vector2(0, 100); 
                }
                
                Dictionary display = newObj.GetComponent<Dictionary>();
                if (display != null)
                {
                    display.Setup(item);
                    Debug.Log($"[DictionaryManager] Spawned and Displayed: {item.itemName}");
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
