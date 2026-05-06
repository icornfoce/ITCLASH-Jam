using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Inspector-configurable mapping from gameplay actions to Animator parameter names.
    /// Each enemy prefab can map its own Animator's parameter naming. Empty names are
    /// silently skipped, so partially-rigged enemies don't error.
    /// </summary>
    [Serializable]
    public class EnemyAnimationConfig
    {
        [Header("Animator Parameter Names")]
        [Tooltip("Bool — true while moving (walk / run loop).")]
        [SerializeField] string walkBoolParam = "IsWalking";

        [Tooltip("Trigger — fired at the start of a melee attack.")]
        [SerializeField] string attackTriggerParam = "Attack";

        [Tooltip("Trigger — fired at the start of a dash attack.")]
        [SerializeField] string dashTriggerParam = "Dash";

        [Tooltip("Trigger — fired at the start of a ranged cast.")]
        [SerializeField] string castTriggerParam = "Cast";

        [Tooltip("Trigger — fired at the start of a heal cast.")]
        [SerializeField] string healCastTriggerParam = "HealCast";

        [Tooltip("Trigger — fired when taking damage (hit reaction).")]
        [SerializeField] string hitTriggerParam = "Hit";

        [Tooltip("Trigger — fired on death.")]
        [SerializeField] string dieTriggerParam = "Die";

        // ── Cached state ──────────────────────────────────────────
        Animator animator;
        bool initialized;

        int walkHash, attackHash, dashHash, castHash, healHash, hitHash, dieHash;
        bool hasWalk, hasAttack, hasDash, hasCast, hasHeal, hasHit, hasDie;

        /// <summary>Cache parameter hashes once; safe to call from EnemyController.Awake.</summary>
        public void Initialize(Animator anim)
        {
            animator    = anim;
            hasWalk     = !string.IsNullOrWhiteSpace(walkBoolParam);
            hasAttack   = !string.IsNullOrWhiteSpace(attackTriggerParam);
            hasDash     = !string.IsNullOrWhiteSpace(dashTriggerParam);
            hasCast     = !string.IsNullOrWhiteSpace(castTriggerParam);
            hasHeal     = !string.IsNullOrWhiteSpace(healCastTriggerParam);
            hasHit      = !string.IsNullOrWhiteSpace(hitTriggerParam);
            hasDie      = !string.IsNullOrWhiteSpace(dieTriggerParam);

            if (hasWalk)   walkHash    = Animator.StringToHash(walkBoolParam);
            if (hasAttack) attackHash  = Animator.StringToHash(attackTriggerParam);
            if (hasDash)   dashHash    = Animator.StringToHash(dashTriggerParam);
            if (hasCast)   castHash    = Animator.StringToHash(castTriggerParam);
            if (hasHeal)   healHash    = Animator.StringToHash(healCastTriggerParam);
            if (hasHit)    hitHash     = Animator.StringToHash(hitTriggerParam);
            if (hasDie)    dieHash     = Animator.StringToHash(dieTriggerParam);

            initialized = animator != null;
        }

        public void SetWalking(bool walking)
        {
            if (initialized && hasWalk) animator.SetBool(walkHash, walking);
        }

        public void TriggerAttack()    { if (initialized && hasAttack) animator.SetTrigger(attackHash); }
        public void TriggerDash()      { if (initialized && hasDash)   animator.SetTrigger(dashHash); }
        public void TriggerCast()      { if (initialized && hasCast)   animator.SetTrigger(castHash); }
        public void TriggerHealCast()  { if (initialized && hasHeal)   animator.SetTrigger(healHash); }
        public void TriggerHit()       { if (initialized && hasHit)    animator.SetTrigger(hitHash); }
        public void TriggerDie()       { if (initialized && hasDie)    animator.SetTrigger(dieHash); }
    }
}
