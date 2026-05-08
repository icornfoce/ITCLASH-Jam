using UnityEngine;
using ITCLASH.Enemies;

public class BenchSkill : BaseProjectileSkill
{
    [Header("Bench Settings")]
    public float damage = 25f;
    public float knockback = 10f;

    protected override void OnHit(Collision collision)
    {
        // สร้างความเสียหาย
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            
            IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
                knockable.ApplyKnockback(knockDir * knockback, 0.3f);
            }
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
