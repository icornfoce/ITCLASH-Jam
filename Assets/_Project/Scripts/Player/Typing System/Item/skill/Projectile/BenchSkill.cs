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
        IDamageable damageable = collision.gameObject.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
        }

        IKnockbackable knockable = collision.gameObject.GetComponentInParent<IKnockbackable>();
        if (knockable != null)
        {
            Transform enemyTransform = ((MonoBehaviour)knockable).transform;
            Vector3 knockDir = (enemyTransform.position - transform.position).normalized;
            knockable.ApplyKnockback(knockDir * knockback, 0.3f);
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
