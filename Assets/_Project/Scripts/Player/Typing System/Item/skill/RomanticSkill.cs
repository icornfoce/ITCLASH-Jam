using UnityEngine;
using ITCLASH.Enemies;

public class RomanticSkill : BaseProjectileSkill
{
    [Header("Romantic Settings")]
    public float stunDuration = 3f;
    public float dpsDamage = 5f;

    protected override void OnHit(Collision collision)
    {
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            // enemy.ApplyStun(stunDuration);
            // ให้ดาเมจเล็กน้อย
            enemy.ApplyDamage(dpsDamage);
            Debug.Log($"[RomanticSkill] ศัตรู {enemy.name} โดน Stun {stunDuration} วินาที และโดนดาเมจ DPS!");
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
