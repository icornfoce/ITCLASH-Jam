using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Ranged caster. Maintains preferred distance and fires homing-less magic orbs
    /// at the player on a cooldown. Set <see cref="EnemyController.MuzzlePoint"/>
    /// to the staff/wand tip transform on the prefab.
    /// </summary>
    [AddComponentMenu("ITCLASH/Enemy/Ranged Mage")]
    public sealed class RangedMageEnemy : EnemyController
    {
        protected override void BuildStateMachine()
        {
            KiteState kite = null;
            kite = new KiteState(this);
            var cast = new RangedCastState(this, returnState: kite);

            kite.DecideTransition = () =>
            {
                if (GetCombatTarget() == null) return null;
                if (!RangedReady) return null;
 
                float d = DistanceToCombatTarget();
                // Only cast if target is within roughly the kite range — don't fire blind.
                if (d <= Stats.preferredRange + 4f && d <= Stats.detectionRadius)
                    return cast;
                return null;
            };

            StateMachine.ChangeState(kite);
        }
    }
}
