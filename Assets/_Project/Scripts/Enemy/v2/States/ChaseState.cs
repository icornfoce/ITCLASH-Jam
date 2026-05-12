using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Chase the player. Transition out via DecideTransition delegate.
    public sealed class ChaseState : EnemyState
    {
        /// Called every frame after nav update. Return non-null to switch state.
        public Func<EnemyState> DecideTransition;

        public ChaseState(EnemyController owner) : base(owner) { }

        public override void OnEnter()
        {
            owner.DebugState("Chase → Enter");
            ResumeAgent();
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.speed = owner.Stats.moveSpeed;
            owner.Animation.SetWalking(true);
        }

        public override void OnExit()
        {
            owner.Animation.SetWalking(false);
        }

        public override void Tick(float dt)
        {
            if (owner.PlayerTransform == null) return;

            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                if (owner.DistanceToPlayer() <= owner.Stats.meleeRange)
                {
                    // In range but on cooldown — hold position.
                    owner.Agent.isStopped = true;
                    if (owner.Anim != null) owner.Anim.SetBool("IsWalking", false);
                    owner.FacePlayer(dt);
                }
                else
                {
                    owner.Agent.isStopped = false;
                    owner.Agent.SetDestination(owner.PlayerTransform.position);
                    owner.Animation.SetWalking(true);
                }
            }

            var next = DecideTransition?.Invoke();
            if (next != null) owner.StateMachine.ChangeState(next);
        }
    }
}
