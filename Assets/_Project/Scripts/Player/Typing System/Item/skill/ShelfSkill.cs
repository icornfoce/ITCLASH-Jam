using UnityEngine;

public class ShelfSkill : BaseProjectileSkill
{
    protected override void OnHit(Collision collision)
    {
        // เมื่อชั้นหนังสือตกกระทบพื้นหรือกระแทกศัตรู
        
        // 1. หยุดชะงักและกลายเป็นสิ่งกีดขวางนิ่งๆ
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;

        // 2. ปล่อยเอฟเฟกต์
        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);

        // หมายเหตุ: ตัวมันเองจะถูกทำลายทิ้งตามเวลา `lifetime` ที่ตั้งไว้ใน Inspector อัตโนมัติ (อยู่ใน BaseProjectileSkill)
        Debug.Log("[ShelfSkill] ชั้นหนังสือกระแทกพื้นแล้ว กลายเป็นกำแพงชั่วคราว!");
    }
}
