using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;

public class Dictionary : MonoBehaviour
{
    [Header("Data Slot")]
    [Tooltip("Drag any ScriptableObject here. The script will try to find 'itemName', 'description', and 'icon' fields.")]
    [SerializeField] private ScriptableObject itemData;

    [Header("List Selection (If ItemData)")]
    [Tooltip("If the ScriptableObject is an ItemData container, use this index to pick which item to show.")]
    [SerializeField] private int itemIndex = 0;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RawImage iconImage;

    private bool _isManuallySetup = false;

    private void Start()
    {
        // Only auto-refresh if we haven't been manually set up by a manager
        if (!_isManuallySetup && itemData != null)
        {
            RefreshUI();
        }
    }

    /// <summary>
    /// Manually sets up the UI with ItemInfo data.
    /// </summary>
    public void Setup(ItemInfo info)
    {
        if (info == null) return;
        _isManuallySetup = true;

        Debug.Log($"[Dictionary] Setting up UI for: {info.itemName}");
        
        if (nameText != null) nameText.text = info.itemName;
        else Debug.LogWarning($"[Dictionary] NameText is missing on {gameObject.name}!");

        if (descriptionText != null) descriptionText.text = info.description;
        
        if (iconImage != null) iconImage.texture = info.icon;
    }



    /// <summary>
    /// Updates the UI elements by searching for matching fields in the ScriptableObject via reflection.
    /// </summary>
    public void RefreshUI()
    {
        if (itemData == null)
        {
            return;
        }

        // Special Case: ItemData (List container)
        if (itemData is ItemData container)
        {
            if (container.items != null && itemIndex >= 0 && itemIndex < container.items.Count)
            {
                ItemInfo item = container.items[itemIndex];
                if (nameText != null) nameText.text = item.itemName;
                if (descriptionText != null) descriptionText.text = item.description;
                if (iconImage != null) iconImage.texture = item.icon;
                return; // Handled specifically
            }
        }

        // Generic Case: Use Reflection
        Type type = itemData.GetType();

        // 1. Try to find the Name
        string foundName = GetFieldValue<string>(type, itemData, "itemName", "Name", "title", "Title");
        if (nameText != null)
        {
            nameText.text = !string.IsNullOrEmpty(foundName) ? foundName : itemData.name;
        }

        // 2. Try to find the Description
        string foundDesc = GetFieldValue<string>(type, itemData, "description", "Description", "desc", "Desc", "info", "Info");
        if (descriptionText != null)
        {
            descriptionText.text = foundDesc ?? "";
        }

        // 3. Try to find the Icon (Texture2D or Sprite)
        if (iconImage != null)
        {
            // Try Texture2D first
            Texture2D tex = GetFieldValue<Texture2D>(type, itemData, "icon", "Icon", "texture", "Texture", "image", "Image");
            if (tex != null)
            {
                iconImage.texture = tex;
            }
            else
            {
                // Try Sprite
                Sprite sprite = GetFieldValue<Sprite>(type, itemData, "icon", "Icon", "sprite", "Sprite");
                if (sprite != null)
                {
                    iconImage.texture = sprite.texture;
                }
            }
        }
    }

    /// <summary>
    /// Helper to find a field by multiple possible names and return its value.
    /// </summary>
    private T GetFieldValue<T>(Type type, object obj, params string[] names) where T : class
    {
        foreach (string name in names)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                object value = field.GetValue(obj);
                if (value is T result) return result;
                
                // Special case: if we want a string but got something else, try ToString()
                if (typeof(T) == typeof(string) && value != null) return value.ToString() as T;
            }

            // Also try properties
            PropertyInfo prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null)
            {
                object value = prop.GetValue(obj);
                if (value is T result) return result;
                if (typeof(T) == typeof(string) && value != null) return value.ToString() as T;
            }
        }
        return null;
    }

    private void OnValidate()
    {
        if (itemData != null)
        {
            RefreshUI();
        }
    }
}
