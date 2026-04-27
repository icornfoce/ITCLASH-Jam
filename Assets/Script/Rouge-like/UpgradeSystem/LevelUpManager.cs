using UnityEngine;
using System.Collections.Generic;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    [SerializeField] private ItemPoolManager poolManager;
    
    [Header("UI References")]
    [Tooltip("Panel UI แสดงหน้าต่างเลือกสกิลอัพเวล")]
    [SerializeField] private GameObject levelUpPanel; 
    
    [Tooltip("Prefab ของการ์ดที่ต้องการสร้าง")]
    [SerializeField] private UpgradeCardUI cardPrefab;
    
    [Tooltip("ตำแหน่ง (เช่น HorizontalLayoutGroup) ที่จะใช้สร้างการ์ด")]
    [SerializeField] private Transform cardContainer;
    
    [Header("Settings")]
    [SerializeField] private int optionsCount = 3;
    
    [Header("Layout Settings (Manual)")]
    [Tooltip("จุดเริ่มต้นของการ์ดใบแรก")]
    [SerializeField] private Vector2 startPosition = Vector2.zero;
    [Tooltip("ระยะห่างระหว่างการ์ด (เช่น x=300 สำหรับแนวนอน หรือ y=-250 สำหรับแนวตั้ง)")]
    [SerializeField] private Vector2 spacing = new Vector2(0, -250);
    
    private List<UpgradeCardUI> spawnedCards = new List<UpgradeCardUI>();
    private bool isPanelActive = false;
    private int rerollCount = 0;
    
    // Inventory จำลองสำหรับใช้พัฒนาและเก็บของ (สามารถแทนที่ด้วย Inventory ระบบจริงของเกมได้)
    public Dictionary<UpgradeItemSO, int> playerInventory = new Dictionary<UpgradeItemSO, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    public bool IsPanelActive() => isPanelActive;

    public void ShowLevelUpPanel()
    {
        if (levelUpPanel == null) 
        {
            Debug.LogError("[LevelUpManager] Level Up Panel reference is MISSING! Please assign it in the Inspector.");
            return;
        }

        isPanelActive = true;
        levelUpPanel.SetActive(true);
        Debug.Log($"[LevelUpManager] Showing Level Up Panel: {levelUpPanel.name}");
        
        // ตรึงเวลาในเกม (Time Pause) เพื่อให้สิ่งต่างๆ หยุดชะงัก
        Time.timeScale = 0f; 

        // ปลดล็อคเมาส์เพื่อให้กดเลือกการ์ดได้
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reroll count ควรจำกัดเฉพาะในแต่ละ Level Up session
        rerollCount = 0;
        
        DrawAndDisplay();
    }

    public void HideLevelUpPanel()
    {
        isPanelActive = false;
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        
        // คืนเวลาให้กับเกม
        Time.timeScale = 1f;

        // ล็อคเมาส์กลับไปเป็นโหมดเล่นเกม
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // หลังจากปิดหน้าจอ ให้ส่งสัญญาณไปบอก PlayerExperience ว่าทำรายการเสร็จแล้ว
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.ProcessLevelUpQueue();
        }
    }

    private void DrawAndDisplay()
    {
        if (poolManager == null) 
        {
            Debug.LogError("[LevelUpManager] ItemPoolManager is missing!");
            return;
        }

        List<UpgradeItemSO> options = poolManager.DrawOptions(playerInventory, optionsCount);
        
        Debug.Log($"[LevelUpManager] Drawn {options.Count} items to display.");

        if (options.Count == 0)
        {
            Debug.LogWarning("[LevelUpManager] No items found in pool to draw! Closing panel.");
            HideLevelUpPanel();
            return;
        }

        // ลบการ์ดเก่าทิ้งก่อน (ถ้ามี)
        foreach (var card in spawnedCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        spawnedCards.Clear();

        // ล้าง Object ทุกตัวที่หลงเหลืออยู่ใน Container (ป้องกันการ์ดที่วางค้างไว้ใน Editor)
        if (cardContainer != null && cardPrefab != null)
        {
            foreach (Transform child in cardContainer)
            {
                // ป้องกันกรณีที่ผู้เล่นลาก Object ใน Hierarchy ที่เป็นลูกของ Container มาเป็น Prefab
                if (child.gameObject != cardPrefab.gameObject)
                {
                    // ป้องกัน Error ใน Editor หากกำลังส่องวัตถุที่จะถูกทำลาย
                    #if UNITY_EDITOR
                    if (UnityEditor.Selection.activeGameObject == child.gameObject)
                        UnityEditor.Selection.activeGameObject = null;
                    #endif

                    Destroy(child.gameObject);
                }
                else
                {
                    // ซ่อนตัวต้นแบบไว้ ไม่ให้โชว์ซ้อนกับของที่จะสุ่มใหม่
                    child.gameObject.SetActive(false);
                }
            }
        }

        // สร้างการ์ดใหม่ตามจำนวนตัวเลือก
        if (cardPrefab != null && cardContainer != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                UpgradeCardUI newCard = Instantiate(cardPrefab, cardContainer);
                newCard.gameObject.SetActive(true); // มั่นใจว่าการ์ดที่สร้างใหม่จะมองเห็นได้
                
                // รีเซ็ตค่า Transform สำหรับ UI ให้ถูกต้องที่สุด
                RectTransform rt = newCard.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    
                    // คำนวณตำแหน่งตาม Index (ใบที่ 1, 2, 3...)
                    rt.anchoredPosition = startPosition + (spacing * i);
                }

                spawnedCards.Add(newCard);
                
                int currentLevel = 0;
                if (playerInventory.ContainsKey(options[i]))
                {
                    currentLevel = playerInventory[options[i]];
                }
                
                newCard.Setup(options[i], currentLevel);
            }

            // บังคับให้ Unity คำนวณ Layout ใหม่ทันที เพื่อป้องกันปัญหาตำแหน่งหรือขนาดเพี้ยน (NaN)
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            Debug.LogWarning("[LevelUpManager] Card Prefab or Container is not assigned!");
        }
    }

    // เรียกตอนผู้เล่นกดปุ่ม Reroll
    public void RerollOptions()
    {
        rerollCount++;
        Debug.Log($"[LevelUpManager] Player Rerolled! Total rerolls this session: {rerollCount}");
        DrawAndDisplay();
    }

    // เรียกตอนผู้เล่นกดเลือกช่องไอเทม
    public void SelectItem(UpgradeItemSO selectedItem)
    {
        if (selectedItem == null)
        {
            Debug.LogWarning("[LevelUpManager] SelectItem called with null item!");
            return;
        }

        Debug.Log($"[LevelUpManager] Player Selected: {selectedItem.itemName}");
        
        // 1. เพิ่มของเข้า Inventory 
        if (playerInventory.ContainsKey(selectedItem))
        {
            playerInventory[selectedItem]++;
        }
        else
        {
            playerInventory.Add(selectedItem, 1);
        }

        // 2. รัน Script/Effects ทั้งหมดที่อยู่ในบัพนี้
        if (selectedItem.codeForBuff != null && PlayerExperience.Instance != null)
        {
            foreach (var effect in selectedItem.codeForBuff)
            {
                if (effect != null)
                {
                    effect.Apply(PlayerExperience.Instance.gameObject);
                }
            }
        }

        // 3. ปิดหน้าจอ และสั่งรันคิวต่อไป (ถ้ามี)
        HideLevelUpPanel();
    }
}
