using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Heavy melee elite. Closes distance with a telegraphed dash, then transitions
    /// into a normal melee swing. From close range the dash is skipped.
    /// </summary>
    [AddComponentMenu("ITCLASH/Enemy/Heavy Dasher (Boss)")]
    public sealed class HeavyDasherEnemy : EnemyController
    {
        [Header("Heavy Dasher Tuning")]
        [Tooltip("Chance per frame, while in dash range and off-cooldown, to commit to a dash.")]
        [Range(0f, 1f)] [SerializeField] float dashTriggerProbability = 0.7f;

        [Tooltip("Minimum distance from the player to consider a dash. Below this we just melee.")]
        [Range(0f, 20f)] [SerializeField] float dashMinDistance = 3.0f;

        protected override void BuildStateMachine()
        {
            ChaseState chase = null;
            chase = new ChaseState(this);
            var melee = new MeleeAttackState(this, returnState: chase);
            var dash  = new DashAttackState(this, returnState: chase);

            chase.DecideTransition = () =>
            {
                if (PlayerTransform == null) return null;
                float d = DistanceToPlayer();

                // Prefer melee when already point-blank.
                if (d <= Stats.meleeRange && MeleeReady) return melee;

                // Dash to close mid-range gaps.
                if (DashReady && d > dashMinDistance && d <= Stats.dashTriggerRange)
                {
                    if (Random.value <= dashTriggerProbability * Time.deltaTime * 10f)
                        return dash;
                }
                return null;
            };

            StateMachine.ChangeState(chase);
        }
    }
}
