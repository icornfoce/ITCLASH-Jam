using UnityEngine;
using System.Collections;

public abstract class BaseBuffSkill : BaseItemSkill
{
    [Header("─── Buff Settings ───")]
    [Tooltip("ระยะเวลาที่บัฟส่งผล (วินาที)")]
    public float buffDuration = 5f;
    
    [Tooltip("VFX ออร่าที่จะติดอยู่ที่ตัวผู้เล่นตอนแสดงผล")]
    public GameObject buffAuraVFX;

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(playerTransform.position);
        
        // เราไม่ต้องการให้ไอเทมชิ้นนี้ถูกยิงหรือตกลงพื้น เพราะมันคือสถานะ (Buff)
        // ดังนั้นเราจะซ่อนโมเดลมันทิ้ง
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        // ปิด Collider ถ้ามี จะได้ไม่เกะกะ
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        // เริ่มต้นการทำงานของบัฟ
        StartCoroutine(BuffRoutine(playerTransform));
    }

    private IEnumerator BuffRoutine(Transform playerTransform)
    {
        // 1. สร้างออร่า (ถ้ามี) ให้วิ่งตามตัวผู้เล่น
        GameObject aura = null;
        if (buffAuraVFX != null)
        {
            aura = Instantiate(buffAuraVFX, playerTransform);
        }

        // 2. ให้คลาสลูกทำงาน (เช่น เพิ่มความเร็ว, ฮีล, บัฟดาเมจ)
        ApplyBuff(playerTransform);
        
        // 3. รอจนหมดเวลาบัฟ
        yield return new WaitForSecondsRealtime(buffDuration);
        
        // 4. ลบบัฟออก (คืนค่าเดิมให้ตัวละคร)
        RemoveBuff(playerTransform);
        
        // 5. ทำลายออร่าและตัวสกิลทิ้ง
        if (aura != null) Destroy(aura);
        Destroy(gameObject);
    }

    /// <summary>
    /// ทำงานเมื่อบัฟเริ่มต้น (คลาสลูกนำไปจัดการเพิ่มสเตตัสต่างๆ)
    /// </summary>
    protected abstract void ApplyBuff(Transform playerTransform);

    /// <summary>
    /// ทำงานเมื่อบัฟหมดเวลา (คลาสลูกนำไปคืนค่าสเตตัสกลับสู่ปกติ)
    /// </summary>
    protected abstract void RemoveBuff(Transform playerTransform);
}
