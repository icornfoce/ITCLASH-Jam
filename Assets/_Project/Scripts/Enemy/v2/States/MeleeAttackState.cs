using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Face player, play attack anim, deal damage on hit event. Returns to returnState after recovery.
    public sealed class MeleeAttackState : EnemyState
    {
        const float WINDUP  = 0.15f;
        const float RECOVER = 0.6f;

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

            StopAgent();
            owner.Animation.TriggerAttack();
            owner.Audio.PlayAttackSwing();
            owner.VFX.SpawnAttackTrail(owner.transform);

            exitAt = Time.time + WINDUP + RECOVER;
        }

        public override void OnExit()
        {
            ResumeAgent();
            owner.ConsumeMelee();
        }

        public override void Tick(float dt)
        {
            owner.FacePlayer(dt);

            // Fallback: if no animation event fires within wind-up, deal damage anyway.
            if (!didHit && Time.time >= enterTime + WINDUP + 0.1f)
                ResolveHit();

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
