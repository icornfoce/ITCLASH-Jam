using UnityEngine;
using System.Collections;

public class BishopSkill : BaseBuffSkill
{
    [Header("Bishop Settings")]
    public float healAmount = 50f;

    protected override void ApplyBuff(Transform playerTransform)
    {
        // 1. เพิ่มเลือดให้ผู้เล่น (ต้องมีสคริปต์ PlayerHealth มารองรับ)
        // PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
        // if (health != null) health.Heal(healAmount);
        
        Debug.Log($"[BishopSkill] ฮีลผู้เล่น {healAmount} หน่วย! (เปิดใช้งาน VFX ฮีล)");
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        // ไม่ต้องลดเลือดคืน เพราะฮีลคือการเพิ่มถาวร
    }
}
