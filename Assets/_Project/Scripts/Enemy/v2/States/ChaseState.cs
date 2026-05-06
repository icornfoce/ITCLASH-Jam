using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Pursues the player via NavMeshAgent. Each Tick the owning enemy is given a chance
    /// to break out via <see cref="DecideTransition"/> — typically into Melee, Dash,
    /// Ranged, or HealCast — based on cooldowns/ranges its archetype cares about.
    /// </summary>
    public sealed class ChaseState : EnemyState
    {
        /// <summary>
        /// Called every frame after destination is set. Return a non-null state to switch.
        /// Set this in the enemy subclass's BuildStateMachine.
        /// </summary>
        public Func<EnemyState> DecideTransition;

        public ChaseState(EnemyController owner) : base(owner) { }

        public override void OnEnter()
        {
            owner.DebugState("Chase → Enter");
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = false;
                owner.Agent.speed = owner.Stats.moveSpeed;
            }
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
                    // Already in range but on cooldown. Hold position and face player.
                    owner.Agent.isStopped = true;
                    owner.Animation.SetWalking(false);
                    owner.FacePlayer(dt);
                }
                else
                {
                    // Out of range, keep chasing.
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
