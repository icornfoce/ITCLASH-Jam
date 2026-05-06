using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Three-phase dash:
    ///   1. Wind-up — face the player while telegraphing.
    ///   2. Dash    — NavMeshAgent disabled; transform translates forward at dashSpeed.
    ///   3. Recover — agent re-enabled, brief delay before returning to <see cref="returnState"/>.
    ///
    /// Damage is dealt either by an <c>Anim_DashImpact</c> event or by an automatic
    /// proximity check at the end of the dash phase, whichever fires first.
    /// </summary>
    public sealed class DashAttackState : EnemyState
    {
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
            phaseEndTime = Time.time + owner.Stats.dashWindupSeconds;

            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = true;
                owner.Agent.velocity = Vector3.zero;
            }
            owner.Animation.TriggerDash();
            owner.Audio.PlayDash();
        }

        public override void OnExit()
        {
            if (owner.Agent != null)
            {
                owner.Agent.enabled = true;
                if (owner.Agent.isOnNavMesh) owner.Agent.isStopped = false;
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
                        if (!didDamage) CheckDashHit(); // last-frame chance
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

            // Lock direction at dash start so we don't track-and-stick to the player.
            Vector3 to = owner.PlayerTransform != null
                ? owner.PlayerTransform.position - owner.transform.position
                : owner.transform.forward;
            to.y = 0f;
            dashDirection = to.sqrMagnitude > 0.0001f ? to.normalized : owner.transform.forward;

            if (owner.Agent != null) owner.Agent.enabled = false; // free transform during dash
        }

        void EnterRecover()
        {
            phase = Phase.Recover;
            phaseEndTime = Time.time + owner.Stats.dashRecoverySeconds;

            if (owner.Agent != null)
            {
                owner.Agent.enabled = true;
                if (owner.Agent.isOnNavMesh)
                {
                    owner.Agent.isStopped = true;
                    owner.Agent.velocity = Vector3.zero;
                }
            }
        }

        void CheckDashHit()
        {
            if (didDamage || owner.PlayerHealth == null || owner.PlayerTransform == null) return;

            Vector3 center = owner.DashImpactPoint.position;
            float dist = Vector3.Distance(center, owner.PlayerTransform.position);
            if (dist <= owner.Stats.dashHitRadius + 0.5f)
            {
                didDamage = true;
                owner.PlayerHealth.TakeDamage(owner.Stats.dashDamage);
                owner.Audio.PlayAttackHit();
                owner.VFX.SpawnHitImpact(owner.transform, owner.PlayerTransform.position);
            }
        }
    }
}
