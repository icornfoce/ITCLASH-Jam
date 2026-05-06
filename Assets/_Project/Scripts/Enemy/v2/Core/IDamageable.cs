using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Anything that can receive damage and heal — used by HealingOrb to query allies,
    /// and by gameplay systems to interact with enemies generically.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>World transform — used for distance/targeting queries.</summary>
        Transform Transform { get; }

        /// <summary>Current HP / Max HP, in [0,1]. Used by healer AI to find lowest-HP allies.</summary>
        float HealthPercent { get; }

        /// <summary>False once the entity has reached 0 HP and entered its death sequence.</summary>
        bool IsAlive { get; }

        /// <summary>Apply damage with float precision (for DoTs, fractional damage, etc.).</summary>
        void ApplyDamage(float amount);

        /// <summary>Restore HP up to MaxHealth.</summary>
        void Heal(float amount);
    }
}
