using UnityEngine;
using UnityEngine.AI;
using ITCLASH.Enemies;

public class LanternSkill : BaseProjectileSkill
{
    [Header("Lantern Settings")]
    public float damage = 15f;
    public float slowDuration = 3f;
    [Range(0f, 1f)] public float slowPercent = 0.5f;
    public GameObject fireVFX;

    protected override void OnHit(Collision collision)
    {
        NavMeshAgent agent = collision.gameObject.GetComponentInParent<NavMeshAgent>();
        if (agent != null)
        {
            SlowEffect existingSlow = agent.gameObject.GetComponent<SlowEffect>();
            if (existingSlow != null)
            {
                existingSlow.RefreshSlow(slowPercent, slowDuration);
            }
            else
            {
                SlowEffect slow = agent.gameObject.AddComponent<SlowEffect>();
                slow.Setup(agent, slowPercent, slowDuration);
            }
            Debug.Log($"[LanternSkill] ศัตรู {collision.gameObject.name} ติดไฟและเดินช้าลง {slowPercent * 100}% เป็นเวลา {slowDuration} วินาที");
        }

        IDamageable damageable = collision.gameObject.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
        }

        if (agent != null || damageable != null)
        {
            if (fireVFX != null)
            {
                // Spawn VFX on the enemy itself to follow it around
                Transform enemyTransform = agent != null ? agent.transform : collision.transform;
                Destroy(Instantiate(fireVFX, enemyTransform.position, Quaternion.identity, enemyTransform), slowDuration);
            }
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
