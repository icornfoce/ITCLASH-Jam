using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Robot-like heavy melee. Shares the Chase ↔ MeleeAttack loop with WeakMeleeEnemy
    /// but a separate class lets designers attach distinct stats / VFX / animator setups
    /// without prefab variant gymnastics.
    /// </summary>
    [AddComponentMenu("ITCLASH/Enemy/Strong Melee Enemy")]
    public sealed class StrongMeleeEnemy : EnemyController
    {
        protected override void BuildStateMachine()
        {
            ChaseState chase = null;
            chase = new ChaseState(this);
            var melee = new MeleeAttackState(this, returnState: chase);

            chase.DecideTransition = () =>
            {
                if (PlayerTransform == null) return null;
                if (DistanceToPlayer() <= Stats.meleeRange && MeleeReady) return melee;
                return null;
            };

            StateMachine.ChangeState(chase);
        }
    }
}
