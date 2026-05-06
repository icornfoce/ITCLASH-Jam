using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Maintains a preferred distance from the player:
    ///   • Closer than <c>tooCloseRange</c> → back away.
    ///   • Farther than <c>preferredRange</c> → close in.
    ///   • Otherwise → hold position and face the player.
    ///
    /// Each Tick the owner is given a chance to transition (e.g. into RangedCast or
    /// HealCast) via <see cref="DecideTransition"/>.
    /// </summary>
    public sealed class KiteState : EnemyState
    {
        public Func<EnemyState> DecideTransition;

        public KiteState(EnemyController owner) : base(owner) { }

        public override void OnEnter()
        {
            owner.DebugState("Kite → Enter");
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.isStopped = false;
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
                    owner.Agent.speed = owner.Stats.moveSpeed * owner.Stats.kiteBackoffSpeedMul;
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
                // Hold
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
