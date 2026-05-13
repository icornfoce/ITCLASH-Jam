using UnityEngine;
using ITCLASH.Enemies;

/// <summary>
/// BlankSkill — โยนภาพวาดเปล่า (ไร้สี) ใส่ศัตรู
/// ชนแล้วทำให้ศัตรูมองไม่เห็น (Blind) ชั่วคราว ไม่มีดาเมจ ทำเฉพาะ CC
/// </summary>
public class BlankSkill : BaseProjectileSkill
{
    [Header("─── Blank Settings ───")]
    [Tooltip("ระยะเวลาที่ศัตรูมองไม่เห็น (วินาที)")]
    public float blindDuration = 4f;

    protected override void OnHit(Collision collision)
    {
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            BlindEffect existing = enemy.GetComponent<BlindEffect>();
            if (existing != null)
            {
                existing.Refresh(blindDuration);
            }
            else
            {
                enemy.gameObject.AddComponent<BlindEffect>().Setup(enemy, blindDuration);
            }

            Debug.Log($"[BlankSkill] {collision.gameObject.name} โดน Blank! มองไม่เห็น {blindDuration} วิ");
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
