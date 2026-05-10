using UnityEngine;
using ITCLASH.Enemies;

public class ShelfSkill : BaseProjectileSkill
{
    [Header("Shelf Settings")]
    public float damage = 20f;

    protected override void OnHit(Collision collision)
    {
        // 1. ตรวจสอบว่าโดนศัตรูหรือไม่ แล้วทำดาเมจ
        IDamageable damageable = collision.gameObject.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
            Debug.Log($"[ShelfSkill] ชั้นหนังสือกระแทกศัตรู โดนดาเมจ {damage}!");
        }

        // 2. ปล่อยเอฟเฟกต์
        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);

        // 3. ทำลายตัวเองทันทีเมื่อชนสิ่งใดก็ตาม
        Destroy(gameObject);
    }
}
