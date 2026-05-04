using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "NewUpgradeItem", menuName = "Rouge-like/Upgrade Item")]
public class UpgradeItemSO : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Texture2D icon;
    
    public ItemRarity rarity = ItemRarity.Common;
    
    [Tooltip("น้ำหนักพื้นฐาน (Base Weight) ยิ่งเยอะยิ่งออกง่าย")]
    public float baseWeight = 100f;
    
    [Tooltip("เลเวลตันที่จำกัดไว้ (Max Level)")]
    public int maxLevel = 1;

    [Header("Buff Logic")]
    [Tooltip("รายการ Script หรือ Code ที่จะทำงานเมื่อเลือกบัพนี้")]
    public System.Collections.Generic.List<UpgradeEffect> codeForBuff;

    // สำหรับใช้คำนวณแบบ Dynamic ตอนเลือกบัพ ห้ามแก้ไขใน Editor ถาวร
    [HideInInspector] public float runtimeWeight; 
}
