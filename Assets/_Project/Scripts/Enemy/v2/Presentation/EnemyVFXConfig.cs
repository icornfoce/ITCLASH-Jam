using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// One VFX prefab slot. Spawns a copy at <see cref="attachPoint"/> (or the owner's
    /// position) and optionally auto-destroys it after a fixed delay.
    /// </summary>
    [Serializable]
    public class VFXSlot
    {
        [Tooltip("Prefab to spawn (ParticleSystem or any GameObject).")]
        public GameObject prefab;

        [Tooltip("Optional anchor; if null, the owner's transform is used.")]
        public Transform attachPoint;

        [Tooltip("Parent the spawned VFX to the anchor (good for trails / casts).")]
        public bool parentToAnchor;

        [Tooltip("Auto-destroy after this many seconds. 0 = let the VFX self-destroy.")]
        [Range(0f, 30f)] public float autoDestroyAfter = 5f;

        public GameObject Spawn(Transform owner, Vector3? overridePos = null, Quaternion? overrideRot = null)
        {
            if (prefab == null) return null;

            Transform anchor = attachPoint != null ? attachPoint : owner;
            Vector3 pos = overridePos ?? (anchor != null ? anchor.position : Vector3.zero);
            Quaternion rot = overrideRot ?? (anchor != null ? anchor.rotation : Quaternion.identity);

            GameObject go = parentToAnchor && anchor != null
                ? UnityEngine.Object.Instantiate(prefab, pos, rot, anchor)
                : UnityEngine.Object.Instantiate(prefab, pos, rot);

            if (autoDestroyAfter > 0f) UnityEngine.Object.Destroy(go, autoDestroyAfter);
            return go;
        }
    }

    /// <summary>
    /// All VFX slots used by an enemy. Inspector-organized so designers can
    /// drop in particle prefabs per-prefab without touching code.
    /// </summary>
    [Serializable]
    public class EnemyVFXConfig
    {
        [Header("VFX Slots")]
        public VFXSlot spawn        = new VFXSlot();
        public VFXSlot attackTrail  = new VFXSlot();
        public VFXSlot muzzleFlash  = new VFXSlot();
        public VFXSlot hitImpact    = new VFXSlot();
        public VFXSlot death        = new VFXSlot();

        public GameObject SpawnAt(VFXSlot slot, Transform owner, Vector3? overridePos = null, Quaternion? overrideRot = null)
            => slot != null ? slot.Spawn(owner, overridePos, overrideRot) : null;

        public GameObject SpawnSpawnFx(Transform owner)       => spawn.Spawn(owner);
        public GameObject SpawnAttackTrail(Transform owner)   => attackTrail.Spawn(owner);
        public GameObject SpawnMuzzleFlash(Transform owner)   => muzzleFlash.Spawn(owner);
        public GameObject SpawnHitImpact(Transform owner, Vector3? pos = null) => hitImpact.Spawn(owner, pos);
        public GameObject SpawnDeath(Transform owner)         => death.Spawn(owner);
    }
}
