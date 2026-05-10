using UnityEngine;
using UnityEngine.AI;
using ITCLASH.Enemies;

public class RomanticSkill : BaseProjectileSkill
{
    [Header("Romantic Settings")]
    public float stunDuration = 3f;
    public float dpsDamage = 5f;

    protected override void OnHit(Collision collision)
    {
        NavMeshAgent agent = collision.gameObject.GetComponentInParent<NavMeshAgent>();
        if (agent != null)
        {
            // Attach a stun effect to handle stopping the agent
            StunEffect stun = agent.gameObject.GetComponent<StunEffect>();
            if (stun == null)
            {
                stun = agent.gameObject.AddComponent<StunEffect>();
            }
            stun.Initialize(agent, stunDuration);
        }

        // Also check for IDamageable just in case they still use it for health
        IDamageable damageable = collision.gameObject.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(dpsDamage);
        }

        if (agent != null || damageable != null)
        {
            Debug.Log($"[RomanticSkill] ศัตรู {collision.gameObject.name} โดน Stun {stunDuration} วินาที และโดนดาเมจ DPS!");
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}

public class StunEffect : MonoBehaviour
{
    private NavMeshAgent agent;
    private float timer;
    private float originalSpeed;

    public void Initialize(NavMeshAgent agent, float duration)
    {
        this.agent = agent;
        this.timer = duration;

        if (agent != null && agent.isOnNavMesh)
        {
            this.originalSpeed = agent.speed;
            agent.speed = 0f;
            agent.velocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = originalSpeed;
                }
                Destroy(this);
            }
        }
    }
}
