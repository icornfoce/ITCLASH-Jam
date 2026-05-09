using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Maintain preferred distance from player. Transition out via DecideTransition delegate.
    public sealed class KiteState : EnemyState
    {
        const float BACKOFF_SPEED_MUL = 1.1f;

        public Func<EnemyState> DecideTransition;

        public KiteState(EnemyController owner) : base(owner) { }

        public override void OnEnter()
        {
            owner.DebugState("Kite → Enter");
            ResumeAgent();
        }

        public override void Tick(float dt)
        {
            if (owner.PlayerTransform == null) return;

            float dist = owner.DistanceToPlayer();
            float preferred = owner.Stats.preferredRange;
            float tooClose  = owner.Stats.tooCloseRange;

            Vector3 self    = owner.transform.position;
            Vector3 toPlay  = owner.PlayerTransform.position - self;
            toPlay.y = 0f;
            Vector3 dir = toPlay.sqrMagnitude > 0.0001f ? toPlay.normalized : owner.transform.forward;

            if (dist < tooClose)
            {
                // Back away
                Vector3 awayPoint = self - dir * (preferred - dist);
                if (owner.Agent != null && owner.Agent.isOnNavMesh)
                {
                    owner.Agent.speed = owner.Stats.moveSpeed * BACKOFF_SPEED_MUL;
                    owner.Agent.SetDestination(awayPoint);
                }
                owner.Animation.SetWalking(true);
            }
            else if (dist > preferred)
            {
                // Close in
                if (owner.Agent != null && owner.Agent.isOnNavMesh)
                {
                    owner.Agent.speed = owner.Stats.moveSpeed;
                    owner.Agent.SetDestination(owner.PlayerTransform.position);
                }
                owner.Animation.SetWalking(true);
            }
            else
            {
                // Hold position
                if (owner.Agent != null && owner.Agent.isOnNavMesh)
                {
                    owner.Agent.SetDestination(self);
                    owner.Agent.velocity = Vector3.zero;
                }
                owner.Animation.SetWalking(false);
                owner.FacePlayer(dt);
            }

            var next = DecideTransition?.Invoke();
            if (next != null) owner.StateMachine.ChangeState(next);
        }

        public override void OnExit()
        {
            owner.Animation.SetWalking(false);
        }
    }
}
