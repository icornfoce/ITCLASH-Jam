using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Terminal state. Disables AI/colliders, plays death anim, then destroys the GameObject.
    public sealed class DeadState : EnemyState
    {
        public DeadState(EnemyController owner) : base(owner) { }

        public override void OnEnter()
        {
            owner.DebugState("Dead → Enter");

            if (owner.Agent != null)
            {
                StopAgent();
                owner.Agent.enabled = false;
            }

            foreach (var c in owner.GetComponentsInChildren<Collider>())
                c.enabled = false;

            owner.Animation.TriggerDie();
            owner.Audio.PlayDeath();
            owner.VFX.SpawnDeath(owner.transform);

            float lifetime = owner.Stats != null ? owner.Stats.deathFadeSeconds : 2f;
            Object.Destroy(owner.gameObject, lifetime);
        }

        public override void Tick(float dt) { }
    }
}
