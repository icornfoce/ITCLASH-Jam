using UnityEngine;
using System.Collections;

public class BishopSkill : BaseBuffSkill
{
    [Header("Bishop Settings")]
    public float healAmount = 50f;

    [Header("VFX Settings")]
    public GameObject healVFXPrefab;
    [Tooltip("ระยะเวลาที่ VFX จะแสดงผลก่อนถูกทำลาย (วินาที)")]
    public float vfxDuration = 3f;

    protected override void ApplyBuff(Transform playerTransform)
    {
        // 1. ฮีลผู้เล่น
        PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(healAmount);
            Debug.Log($"[BishopSkill] ฮีลผู้เล่น {healAmount} หน่วย!");
        }
        else
        {
            Debug.LogWarning("[BishopSkill] ไม่พบสคริปต์ PlayerHealth บนตัวผู้เล่น!");
        }

        // 2. สร้าง VFX ที่เท้าผู้เล่น
        if (healVFXPrefab != null)
        {
            Vector3 feetPos = playerTransform.position;

            // พยายามคำนวณตำแหน่งเท้าจาก CharacterController (ถ้ามี)
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null)
            {
                // ตำแหน่งเท้า = จุดศูนย์กลาง - (ครึ่งหนึ่งของความสูง)
                feetPos = playerTransform.position + Vector3.up * (cc.center.y - (cc.height / 2f));
            }

            GameObject vfx = Instantiate(healVFXPrefab, feetPos, Quaternion.identity, playerTransform);

            // ทำลาย VFX หลังจากเวลาที่กำหนด
            if (vfx != null)
            {
                Destroy(vfx, vfxDuration);
            }
        }
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        // ไม่ต้องลดเลือดคืน เพราะฮีลคือการเพิ่มถาวร
    }
}

