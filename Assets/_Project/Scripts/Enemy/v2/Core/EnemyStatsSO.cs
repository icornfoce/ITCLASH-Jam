using UnityEngine;

namespace ITCLASH.Enemies
{
    /// All tunable enemy stats. One asset per archetype. Unused sections can be left at defaults.
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "ITCLASH/Enemy/Enemy Stats")]
    public sealed class EnemyStatsSO : ScriptableObject
    {
        // ─── General ──────────────────────────────────────────────
        [Header("General")]
        [Range(1f, 5000f)] public float maxHealth = 50f;
        [Range(0f, 20f)]   public float moveSpeed = 3.5f;
        [Range(0f, 1080f)] public float turnSpeedDeg = 540f;
        [Range(0f, 10f)]   public float deathFadeSeconds = 2f;

        // ─── Detection ────────────────────────────────────────────
        [Space, Header("Detection")]
        [Range(0f, 100f)] public float detectionRadius = 30f;

        // ─── Melee Combat ─────────────────────────────────────────
        [Space, Header("Melee Combat")]
        [Range(0f, 500f)]  public float meleeDamage   = 10f;
        [Range(0f, 10f)]   public float meleeRange    = 2.0f;
        [Range(0.1f, 10f)] public float meleeCooldown = 1.5f;

        // ─── Dash Attack (Heavy Dasher) ───────────────────────────
        [Space, Header("Dash Attack")]
        [Range(0f, 30f)]   public float dashTriggerRange = 8f;
        [Range(0f, 50f)]   public float dashSpeed        = 18f;
        [Range(0.05f, 2f)] public float dashDuration     = 0.45f;
        [Range(0.5f, 30f)] public float dashCooldown     = 6f;
        [Range(0f, 500f)]  public float dashDamage       = 25f;

        // ─── Kite (Ranged / Healer) ───────────────────────────────
        [Space, Header("Kite Behaviour")]
        [Range(0f, 50f)] public float preferredRange = 12f;
        [Range(0f, 30f)] public float tooCloseRange  = 7f;

        // ─── Ranged Attack (Mage) ─────────────────────────────────
        [Space, Header("Ranged Attack")]
        public GameObject projectilePrefab;
        [Range(0f, 80f)]   public float projectileSpeed  = 14f;
        [Range(0f, 500f)]  public float projectileDamage = 12f;
        [Range(0.2f, 30f)] public float rangedCooldown   = 2.5f;

        // ─── Healing (Support Mage) ───────────────────────────────
        [Space, Header("Healing")]
        public GameObject healingOrbPrefab;
        [Range(1, 6)]      public int   orbsPerCast   = 3;
        [Range(0f, 200f)]  public float healPerOrb    = 8f;
        [Range(0f, 80f)]   public float healCastRange = 20f;
        [Range(0.5f, 30f)] public float healCooldown  = 5f;
        [Range(0f, 40f)]   public float orbHomingSpeed = 8f;
    }
}
