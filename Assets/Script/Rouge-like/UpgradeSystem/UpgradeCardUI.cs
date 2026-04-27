using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private RawImage iconImage; // ใช้ RawImage เพราะปรับเป็น Texture2D แล้ว
    [SerializeField] private RawImage frameImage;   // กรอบการ์ดสำหรับเปลี่ยนรูปตามความหายาก (ใช้ RawImage)
    [SerializeField] private Button selectButton;

    [Header("Rarity Settings")]
    [SerializeField] private UpgradeRarityConfigSO rarityConfig;
    [SerializeField] private Transform prefabContainer; // สำหรับวาง Prefab พิเศษ (ถ้ามี)

    private UpgradeItemSO currentItem;
    private GameObject currentRarityPrefab;

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnCardClicked);
            Debug.Log($"[UpgradeCardUI] Initialized and Listener added on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[UpgradeCardUI] selectButton is not assigned on {gameObject.name}");
        }
    }

    public void Setup(UpgradeItemSO item, int currentLevel)
    {
        currentItem = item;

        if (nameText != null) nameText.text = item.itemName;
        if (descriptionText != null) descriptionText.text = item.description;
        
        if (levelText != null) 
        {
            // ถ้ามีเลเวลแล้วให้แสดงเลเวลต่อไป
            levelText.text = $"Lv. {currentLevel + 1}";
        }

        if (iconImage != null && item.icon != null)
        {
            iconImage.texture = item.icon;
        }

        if (rarityConfig != null)
        {
            UpdateRarityVisuals(item.rarity);
        }
    }

    private void UpdateRarityVisuals(ItemRarity rarity)
    {
        // เคลียร์ Prefab เก่าออกก่อน
        if (currentRarityPrefab != null)
        {
            Destroy(currentRarityPrefab);
        }

        RarityVisualData visualData = rarityConfig.GetVisualData(rarity);
        if (visualData == null) return;

        // 1. จัดการ Texture/Color บน Frame Image
        if (frameImage != null)
        {
            if (visualData.frameTexture != null)
            {
                frameImage.texture = visualData.frameTexture;
            }
            frameImage.color = visualData.frameColor;
        }

        // 2. จัดการ Prefab (ถ้ามี)
        if (visualData.framePrefab != null && prefabContainer != null)
        {
            currentRarityPrefab = Instantiate(visualData.framePrefab, prefabContainer);
            currentRarityPrefab.transform.localPosition = Vector3.zero;
            currentRarityPrefab.transform.localRotation = Quaternion.identity;
            currentRarityPrefab.transform.localScale = Vector3.one;
        }
    }

    public void OnCardClicked()
    {
        if (currentItem != null)
        {
            Debug.Log($"[UpgradeCardUI] Card Clicked: {currentItem.itemName}");
            if (LevelUpManager.Instance != null)
            {
                LevelUpManager.Instance.SelectItem(currentItem);
            }
        }
        else
        {
            Debug.LogWarning("[UpgradeCardUI] Card Clicked but currentItem is null!");
        }
    }
}
