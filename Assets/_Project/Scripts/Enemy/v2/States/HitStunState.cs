using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Brief stagger — used for stun procs from typing skills if you want enemies to flinch.
    /// Optional: not wired into any of the default 5 enemy types, but available to extend.
    /// </summary>
    public sealed class HitStunState : EnemyState
    {
        readonly EnemyState returnState;
        readonly float duration;
        float exitAt;

        public HitStunState(EnemyController owner, EnemyState returnState, float duration) : base(owner)
        {
            this.returnState = returnState;
            this.duration = duration;
        }

        public override void OnEnter()
        {
            owner.DebugState("HitStun → Enter");
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = true;
                owner.Agent.velocity = Vector3.zero;
            }
            owner.Animation.TriggerHit();
            exitAt = Time.time + Mathf.Max(0.05f, duration);
        }

        public override void OnExit()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.isStopped = false;
        }

        public override void Tick(float dt)
        {
            if (Time.time >= exitAt) owner.StateMachine.ChangeState(returnState);
        }
    }
}
