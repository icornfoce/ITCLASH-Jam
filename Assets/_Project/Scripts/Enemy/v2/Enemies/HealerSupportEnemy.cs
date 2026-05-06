using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Support mage. Kites the player like the Ranged Mage, but instead of attacking
    /// it fires healing orbs that home onto the lowest-HP ally within
    /// <see cref="EnemyStatsSO.healCastRange"/>. If no wounded allies exist,
    /// the healer just keeps kiting.
    /// </summary>
    [AddComponentMenu("ITCLASH/Enemy/Healer Support")]
    public sealed class HealerSupportEnemy : EnemyController
    {
        [Header("Healer Tuning")]
        [Tooltip("If true, each orb re-evaluates the lowest-HP ally every frame mid-flight.")]
        [SerializeField] bool retargetOrbsEachFrame = false;

        [Tooltip("If true, the healer will only cast when at least one ally is below this HP%.")]
        [Range(0f, 1f)] [SerializeField] float castThresholdPercent = 0.95f;

        protected override void BuildStateMachine()
        {
            KiteState kite = null;
            kite = new KiteState(this);
            var heal = new HealCastState(this, returnState: kite, retargetEachFrame: retargetOrbsEachFrame);

            kite.DecideTransition = () =>
            {
                if (!HealReady) return null;
                var lowest = EnemyRegistry.FindLowestHpInRange(transform.position, Stats.healCastRange, exclude: this);
                if (lowest == null) return null;
                if (lowest.HealthPercent >= castThresholdPercent) return null;
                return heal;
            };

            StateMachine.ChangeState(kite);
        }
    }
}
