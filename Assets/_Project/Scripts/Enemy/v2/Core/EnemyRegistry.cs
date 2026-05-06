using System.Collections.Generic;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Static registry of every live enemy. Used by HealCastState / HealingOrb to
    /// find the lowest-HP ally in range. Enemies register on enable, unregister on disable.
    /// </summary>
    public static class EnemyRegistry
    {
        static readonly List<EnemyController> all = new List<EnemyController>(64);

        public static IReadOnlyList<EnemyController> All => all;

        public static void Register(EnemyController e)
        {
            if (e != null && !all.Contains(e)) all.Add(e);
        }

        public static void Unregister(EnemyController e)
        {
            if (e != null) all.Remove(e);
        }

        /// <summary>
        /// Returns the alive enemy with the lowest HP percent within <paramref name="radius"/>
        /// of <paramref name="origin"/>. Returns null if no candidate is below 100% HP, or
        /// if no candidate exists. <paramref name="exclude"/> is skipped (typically the caster itself).
        /// </summary>
        public static EnemyController FindLowestHpInRange(Vector3 origin, float radius, EnemyController exclude = null)
        {
            EnemyController best = null;
            float bestPercent = 1f; // ignore full-HP allies
            float radiusSq = radius * radius;

            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || !e.IsAlive || ReferenceEquals(e, exclude)) continue;

                float distSq = (e.transform.position - origin).sqrMagnitude;
                if (distSq > radiusSq) continue;

                float pct = e.HealthPercent;
                if (pct < bestPercent)
                {
                    bestPercent = pct;
                    best = e;
                }
            }
            return best;
        }
    }
}
