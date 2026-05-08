using UnityEngine;
using ITCLASH.Enemies;

public class ShelfterSkill : BaseProjectileSkill
{
    [Header("Shelfter Settings")]
    public float extremeDamage = 150f;

    protected override void OnHit(Collision collision)
    {
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(extremeDamage);
            Debug.Log($"[ShelfterSkill] ทำดาเมจมหาศาล {extremeDamage} ใส่ {enemy.name}!");
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
