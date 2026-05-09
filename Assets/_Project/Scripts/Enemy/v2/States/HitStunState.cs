using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Brief stagger/stun. Returns to returnState after duration.
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
            StopAgent();
            owner.Animation.TriggerHit();
            exitAt = Time.time + Mathf.Max(0.05f, duration);
        }

        public override void OnExit()
        {
            ResumeAgent();
        }

        public override void Tick(float dt)
        {
            if (Time.time >= exitAt) owner.StateMachine.ChangeState(returnState);
        }
    }
}
