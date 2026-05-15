using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Stop, face player, cast animation, spawn projectile at muzzle. Returns to returnState after recovery.
    public sealed class RangedCastState : EnemyState
    {
        const float RECOVER  = 0.8f;
        const float LIFETIME = 6f;

        readonly EnemyState returnState;
        float enterTime;
        float exitAt;
        bool didFire;

        public RangedCastState(EnemyController owner, EnemyState returnState) : base(owner)
        {
            this.returnState = returnState;
        }

        public override void OnEnter()
        {
            owner.DebugState("RangedCast → Enter");
            enterTime = Time.time;
            didFire = false;

            StopAgent();
            owner.Animation.TriggerCast();
            owner.Audio.PlayCast();

            exitAt = Time.time + RECOVER + 0.1f;
        }

        public override void OnExit()
        {
            ResumeAgent();
            owner.ConsumeRanged();
        }

        public override void Tick(float dt)
        {
            owner.FacePlayer(dt);

            // Fallback firing if no animation event arrives.
            if (!didFire && Time.time >= enterTime + RECOVER * 0.4f)
                Fire();

            if (Time.time >= exitAt) owner.StateMachine.ChangeState(returnState);
        }

        public override void OnAnimRangedFire() => Fire();

        void Fire()
        {
            if (didFire) return;
            didFire = true;

            var prefab = owner.Stats.projectilePrefab;
            var target = owner.GetCombatTarget();
            if (prefab == null || target == null) return;

            Vector3 muzzlePos = owner.OrbSpawnPoint.position;
            Vector3 toTarget  = target.position - muzzlePos;
            Quaternion rot = Quaternion.LookRotation(toTarget.normalized);

            var go = Object.Instantiate(prefab, muzzlePos, rot);
            if (go.TryGetComponent(out EnemyProjectile proj))
            {
                proj.Launch(
                    direction: toTarget.normalized,
                    speed: owner.Stats.projectileSpeed,
                    damage: owner.Stats.projectileDamage,
                    lifetime: LIFETIME,
                    ownerCollider: owner.GetComponent<Collider>());
            }

            owner.VFX.SpawnMuzzleFlash(owner.transform);
        }
    }
}
