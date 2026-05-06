using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Faces the player, plays the attack animation, and applies damage on the
    /// <c>Anim_AttackHit</c> animation event (or after the configured wind-up if no
    /// event is wired). Returns to <see cref="returnState"/> after recovery.
    /// </summary>
    public sealed class MeleeAttackState : EnemyState
    {
        readonly EnemyState returnState;
        float enterTime;
        float exitAt;
        bool didHit;

        public MeleeAttackState(EnemyController owner, EnemyState returnState) : base(owner)
        {
            this.returnState = returnState;
        }

        public override void OnEnter()
        {
            owner.DebugState("MeleeAttack → Enter");
            enterTime = Time.time;
            didHit = false;

            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = true;
                owner.Agent.velocity = Vector3.zero;
            }

            owner.Animation.TriggerAttack();
            owner.Audio.PlayAttackSwing();
            owner.VFX.SpawnAttackTrail(owner.transform);

            exitAt = Time.time + owner.Stats.meleeWindupSeconds + owner.Stats.meleeRecoverySeconds;
        }

        public override void OnExit()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.isStopped = false;
            owner.ConsumeMelee();
        }

        public override void Tick(float dt)
        {
            owner.FacePlayer(dt);

            // Fallback: if no animation event fires within wind-up + 0.1s, deal damage anyway.
            if (!didHit && Time.time >= enterTime + owner.Stats.meleeWindupSeconds + 0.1f)
            {
                ResolveHit();
            }

            if (Time.time >= exitAt) owner.StateMachine.ChangeState(returnState);
        }

        public override void OnAnimAttackHit() => ResolveHit();

        void ResolveHit()
        {
            if (didHit) return;
            didHit = true;

            if (owner.PlayerHealth == null || owner.PlayerTransform == null) return;
            if (owner.DistanceToPlayer() <= owner.Stats.meleeRange + 0.5f)
            {
                owner.PlayerHealth.TakeDamage(owner.Stats.meleeDamage);
                owner.Audio.PlayAttackHit();
                owner.VFX.SpawnHitImpact(owner.transform, owner.PlayerTransform.position);
            }
        }
    }
}
