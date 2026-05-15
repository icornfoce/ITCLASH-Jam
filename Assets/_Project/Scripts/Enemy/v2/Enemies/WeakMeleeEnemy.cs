using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Scorpion-like fodder. Walks straight at the player and bites in melee range.
    /// Tune entirely via the assigned <see cref="EnemyStatsSO"/>.
    /// </summary>
    [AddComponentMenu("ITCLASH/Enemy/Weak Melee Enemy")]
    public sealed class WeakMeleeEnemy : EnemyController
    {
        protected override void BuildStateMachine()
        {
            ChaseState chase = null;
            chase = new ChaseState(this);
            var melee = new MeleeAttackState(this, returnState: chase);

            chase.DecideTransition = () =>
            {
                if (GetCombatTarget() == null) return null;
                if (DistanceToCombatTarget() <= Stats.meleeRange && MeleeReady) return melee;
                return null;
            };

            StateMachine.ChangeState(chase);
        }
    }
}
