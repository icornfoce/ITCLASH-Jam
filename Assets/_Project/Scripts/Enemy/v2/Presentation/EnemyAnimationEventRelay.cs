using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Place this on the rigged GameObject that owns the Animator (often a child of the
    /// enemy root). Add Animation Events on attack/cast/dash clips that call the
    /// <c>Anim_*</c> methods below — they're forwarded up to the <see cref="EnemyController"/>
    /// so damage/projectiles/heals land at the exact animation frame.
    /// </summary>
    public sealed class EnemyAnimationEventRelay : MonoBehaviour
    {
        [Tooltip("Optional explicit reference. If null, found in parent at Awake.")]
        [SerializeField] EnemyController controller;

        void Awake()
        {
            if (controller == null) controller = GetComponentInParent<EnemyController>();
        }

        // Method names mirror the convention "Anim_*" so they're easy to find in the
        // Animation Event picker. Never rename without updating the AnimationClip events.
        public void Anim_AttackHit()    => controller?.OnAnimAttackHit();
        public void Anim_DashImpact()   => controller?.OnAnimDashImpact();
        public void Anim_RangedFire()   => controller?.OnAnimRangedFire();
        public void Anim_HealOrbFire()  => controller?.OnAnimHealOrbFire();
        public void Anim_Footstep()     => controller?.OnAnimFootstep();
    }
}
