using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// All tunable enemy stats. One asset per enemy archetype (WeakMelee, HeavyDasher, …).
    /// Drives gameplay numbers without recompiling — designers tweak in Inspector.
    /// Unused sections (e.g. Dash on a non-dasher) can be left at their defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "ITCLASH/Enemy/Enemy Stats")]
    public sealed class EnemyStatsSO : ScriptableObject
    {
        // ─── General ──────────────────────────────────────────────
        [Header("General")]
        [Tooltip("Maximum HP. Damage subtracts from this; reaching 0 triggers death.")]
        [Range(1f, 5000f)] public float maxHealth = 50f;

        [Tooltip("NavMeshAgent base move speed (m/s).")]
        [Range(0f, 20f)] public float moveSpeed = 3.5f;

        [Tooltip("How fast the enemy turns to face the player (deg/s).")]
        [Range(0f, 1080f)] public float turnSpeedDeg = 540f;

        [Tooltip("How long the corpse lingers before being destroyed (lets death anim/SFX/VFX finish).")]
        [Range(0f, 10f)] public float deathFadeSeconds = 2f;

        // ─── Detection ────────────────────────────────────────────
        [Space, Header("Detection")]
        [Tooltip("Inside this radius the enemy is fully aggressive.")]
        [Range(0f, 100f)] public float detectionRadius = 30f;

        [Tooltip("Outside this radius the enemy stops chasing (only used by some states).")]
        [Range(0f, 200f)] public float loseSightRadius = 60f;

        // ─── Melee Combat ─────────────────────────────────────────
        [Space, Header("Melee Combat")]
        [Tooltip("Damage dealt per melee hit.")]
        [Range(0f, 500f)] public float meleeDamage = 10f;

        [Tooltip("Distance at which a melee swing connects.")]
        [Range(0f, 10f)] public float meleeRange = 2.0f;

        [Tooltip("Seconds between melee attacks.")]
        [Range(0.1f, 10f)] public float meleeCooldown = 1.5f;

        [Tooltip("Wind-up before the attack animation triggers (lets the enemy face the player first).")]
        [Range(0f, 2f)] public float meleeWindupSeconds = 0.15f;

        [Tooltip("Total seconds the enemy is locked in the attack animation before returning to chase.")]
        [Range(0.1f, 5f)] public float meleeRecoverySeconds = 0.6f;

        // ─── Dash Attack (Heavy Dasher) ───────────────────────────
        [Space, Header("Dash Attack (Heavy Dasher)")]
        [Tooltip("Maximum range from which the enemy will start a dash attack.")]
        [Range(0f, 30f)] public float dashTriggerRange = 8f;

        [Tooltip("Movement speed during the dash (m/s).")]
        [Range(0f, 50f)] public float dashSpeed = 18f;

        [Tooltip("How long the dash lasts.")]
        [Range(0.05f, 2f)] public float dashDuration = 0.45f;

        [Tooltip("Wind-up before the dash starts (telegraph window).")]
        [Range(0f, 2f)] public float dashWindupSeconds = 0.4f;

        [Tooltip("Recovery time after the dash before resuming AI.")]
        [Range(0f, 3f)] public float dashRecoverySeconds = 0.5f;

        [Tooltip("Seconds between dash attacks.")]
        [Range(0.5f, 30f)] public float dashCooldown = 6f;

        [Tooltip("Damage dealt by a dash impact.")]
        [Range(0f, 500f)] public float dashDamage = 25f;

        [Tooltip("Radius around the enemy that counts as a dash hit.")]
        [Range(0f, 5f)] public float dashHitRadius = 1.5f;

        // ─── Kite (Ranged / Healer) ───────────────────────────────
        [Space, Header("Kite Behaviour (Ranged / Healer)")]
        [Tooltip("Distance the enemy tries to maintain from the player.")]
        [Range(0f, 50f)] public float preferredRange = 12f;

        [Tooltip("If the player gets closer than this, the enemy backs away.")]
        [Range(0f, 30f)] public float tooCloseRange = 7f;

        [Tooltip("Speed multiplier applied while backing away from the player.")]
        [Range(0.1f, 3f)] public float kiteBackoffSpeedMul = 1.1f;

        // ─── Ranged Attack (Mage) ─────────────────────────────────
        [Space, Header("Ranged Attack (Mage)")]
        [Tooltip("Projectile prefab — must have an EnemyProjectile component.")]
        public GameObject projectilePrefab;

        [Tooltip("Projectile travel speed (m/s).")]
        [Range(0f, 80f)] public float projectileSpeed = 14f;

        [Tooltip("Damage dealt to the player on projectile impact.")]
        [Range(0f, 500f)] public float projectileDamage = 12f;

        [Tooltip("Seconds before the projectile self-destructs if it never hits anything.")]
        [Range(0.5f, 30f)] public float projectileLifetime = 6f;

        [Tooltip("Seconds between ranged casts.")]
        [Range(0.2f, 30f)] public float rangedCooldown = 2.5f;

        [Tooltip("Total seconds the enemy is locked in the cast animation before resuming AI.")]
        [Range(0.1f, 5f)] public float rangedRecoverySeconds = 0.8f;

        // ─── Healing (Support Mage) ───────────────────────────────
        [Space, Header("Healing (Support Mage)")]
        [Tooltip("Healing orb prefab — must have a HealingOrb component.")]
        public GameObject healingOrbPrefab;

        [Tooltip("How many orbs are released per cast.")]
        [Range(1, 6)] public int orbsPerCast = 3;

        [Tooltip("HP restored to the targeted ally per orb.")]
        [Range(0f, 200f)] public float healPerOrb = 8f;

        [Tooltip("Maximum range for picking heal targets.")]
        [Range(0f, 80f)] public float healCastRange = 20f;

        [Tooltip("Seconds between heal casts.")]
        [Range(0.5f, 30f)] public float healCooldown = 5f;

        [Tooltip("Recovery after firing the orbs before resuming AI.")]
        [Range(0.1f, 5f)] public float healRecoverySeconds = 0.8f;

        [Tooltip("Healing orb travel speed toward its target.")]
        [Range(0f, 40f)] public float orbHomingSpeed = 8f;

        [Tooltip("Healing orb self-destruct timeout (safety).")]
        [Range(0.5f, 30f)] public float orbMaxLifetime = 8f;
    }
}
