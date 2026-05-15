using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Three-phase dash: Windup → Dash (transform translate) → Recover.
    /// Damage via Anim_DashImpact event or auto proximity check.
    public sealed class DashAttackState : EnemyState
    {
        const float WINDUP     = 0.4f;
        const float RECOVER    = 0.5f;
        const float HIT_RADIUS = 1.5f;

        enum Phase { Windup, Dashing, Recover }

        readonly EnemyState returnState;
        Phase phase;
        float phaseEndTime;
        Vector3 dashDirection;
        bool didDamage;

        public DashAttackState(EnemyController owner, EnemyState returnState) : base(owner)
        {
            this.returnState = returnState;
        }

        public override void OnEnter()
        {
            owner.DebugState("DashAttack → Enter (Windup)");
            phase = Phase.Windup;
            didDamage = false;
            phaseEndTime = Time.time + WINDUP;

            StopAgent();
            owner.Animation.TriggerDash();
            owner.Audio.PlayDash();
        }

        public override void OnExit()
        {
            if (owner.Agent != null)
            {
                owner.Agent.enabled = true;
                ResumeAgent();
            }
            owner.ConsumeDash();
        }

        public override void Tick(float dt)
        {
            switch (phase)
            {
                case Phase.Windup:
                    owner.FacePlayer(dt);
                    if (Time.time >= phaseEndTime) StartDash();
                    break;

                case Phase.Dashing:
                    owner.transform.position += dashDirection * owner.Stats.dashSpeed * dt;
                    if (!didDamage) CheckDashHit();
                    if (Time.time >= phaseEndTime)
                    {
                        if (!didDamage) CheckDashHit();
                        EnterRecover();
                    }
                    break;

                case Phase.Recover:
                    if (Time.time >= phaseEndTime) owner.StateMachine.ChangeState(returnState);
                    break;
            }
        }

        public override void OnAnimDashImpact()
        {
            if (phase == Phase.Dashing) CheckDashHit();
        }

        void StartDash()
        {
            phase = Phase.Dashing;
            phaseEndTime = Time.time + owner.Stats.dashDuration;

            // Lock direction toward the current combat target at dash start.
            var target = owner.GetCombatTarget();
            Vector3 to = target != null
                ? target.position - owner.transform.position
                : owner.transform.forward;
            to.y = 0f;
            dashDirection = to.sqrMagnitude > 0.0001f ? to.normalized : owner.transform.forward;

            if (owner.Agent != null) owner.Agent.enabled = false;
        }

        void EnterRecover()
        {
            phase = Phase.Recover;
            phaseEndTime = Time.time + RECOVER;

            if (owner.Agent != null)
            {
                owner.Agent.enabled = true;
                StopAgent();
            }
        }

        void CheckDashHit()
        {
            var target     = owner.GetCombatTarget();
            var damageable = owner.GetCombatTargetDamageable();
            if (didDamage || target == null || damageable == null) return;

            Vector3 center = owner.DashImpactPoint.position;
            float dist = Vector3.Distance(center, target.position);
            if (dist <= HIT_RADIUS + 0.5f)
            {
                didDamage = true;
                damageable.ApplyDamage(owner.Stats.dashDamage);
                owner.Audio.PlayAttackHit();
                owner.VFX.SpawnHitImpact(owner.transform, target.position);
            }
        }
    }
}
