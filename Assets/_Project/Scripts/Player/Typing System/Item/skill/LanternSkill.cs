using UnityEngine;
using ITCLASH.Enemies;

public class LanternSkill : BaseProjectileSkill
{
    [Header("Lantern Settings")]
    public float damage = 15f;
    public float slowDuration = 3f;
    public GameObject fireVFX;

    protected override void OnHit(Collision collision)
    {
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            
            // หมายเหตุ: ถ้าระบบศัตรูมีฟังก์ชันลดความเร็ว ให้เรียกใช้ตรงนี้
            // enemy.ApplySlow(0.5f, slowDuration);
            Debug.Log($"[LanternSkill] ศัตรูติดไฟและเดินช้าลง {slowDuration} วินาที");
            
            if (fireVFX != null)
            {
                Destroy(Instantiate(fireVFX, enemy.transform.position, Quaternion.identity, enemy.transform), slowDuration);
            }
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
