using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Stops, faces the player, plays the cast animation. Spawns the projectile at the
    /// muzzle on the <c>Anim_RangedFire</c> event (or after a fallback delay if no event
    /// is wired). Returns to <see cref="returnState"/> after recovery.
    /// </summary>
    public sealed class RangedCastState : EnemyState
    {
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

            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = true;
                owner.Agent.velocity = Vector3.zero;
            }
            owner.Animation.TriggerCast();
            owner.Audio.PlayCast();

            exitAt = Time.time + owner.Stats.rangedRecoverySeconds + 0.1f;
        }

        public override void OnExit()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.isStopped = false;
            owner.ConsumeRanged();
        }

        public override void Tick(float dt)
        {
            owner.FacePlayer(dt);

            // Fallback firing if no animation event arrives.
            if (!didFire && Time.time >= enterTime + owner.Stats.rangedRecoverySeconds * 0.4f)
                Fire();

            if (Time.time >= exitAt) owner.StateMachine.ChangeState(returnState);
        }

        public override void OnAnimRangedFire() => Fire();

        void Fire()
        {
            if (didFire) return;
            didFire = true;

            var prefab = owner.Stats.projectilePrefab;
            if (prefab == null || owner.PlayerTransform == null) return;

            Vector3 muzzlePos = owner.MuzzlePoint.position;
            Vector3 toPlayer  = owner.PlayerTransform.position - muzzlePos;
            // Aim slightly toward upper torso so flat ground shots don't undershoot.
            toPlayer.y += 1.0f;
            Quaternion rot = Quaternion.LookRotation(toPlayer.normalized);

            var go = Object.Instantiate(prefab, muzzlePos, rot);
            if (go.TryGetComponent(out EnemyProjectile proj))
            {
                proj.Launch(
                    direction: toPlayer.normalized,
                    speed: owner.Stats.projectileSpeed,
                    damage: owner.Stats.projectileDamage,
                    lifetime: owner.Stats.projectileLifetime);
            }

            owner.VFX.SpawnMuzzleFlash(owner.transform);
        }
    }
}
