using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Terminal state. Disables AI and colliders, plays death anim/SFX/VFX, then destroys
    /// the GameObject after <c>stats.deathFadeSeconds</c>. The Destroy is critical: the
    /// existing Spawner.cs tracks live monsters via null-check cleanup.
    /// </summary>
    public sealed class DeadState : EnemyState
    {
        public DeadState(EnemyController owner) : base(owner) { }

        public override void OnEnter()
        {
            owner.DebugState("Dead → Enter");

            if (owner.Agent != null)
            {
                if (owner.Agent.isOnNavMesh)
                {
                    owner.Agent.isStopped = true;
                    owner.Agent.velocity = Vector3.zero;
                }
                owner.Agent.enabled = false;
            }

            // Stop further damage / typing-system raycasts from registering hits on a corpse.
            foreach (var c in owner.GetComponentsInChildren<Collider>())
                c.enabled = false;

            owner.Animation.TriggerDie();
            owner.Audio.PlayDeath();
            owner.VFX.SpawnDeath(owner.transform);

            float lifetime = owner.Stats != null ? owner.Stats.deathFadeSeconds : 2f;
            Object.Destroy(owner.gameObject, lifetime);
        }

        public override void Tick(float dt) { /* corpse is inert */ }
    }
}
