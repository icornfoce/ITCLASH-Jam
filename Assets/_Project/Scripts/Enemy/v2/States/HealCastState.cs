using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Pick lowest-HP ally, cast heal animation, spawn homing orbs.
    /// If no wounded ally, exits immediately without consuming cooldown.
    public sealed class HealCastState : EnemyState
    {
        const float RECOVER      = 0.8f;
        const float ORB_LIFETIME = 8f;

        readonly EnemyState returnState;
        readonly bool retargetEachFrame;

        EnemyController target;
        float enterTime;
        float exitAt;
        bool didFire;
        bool aborted;

        public HealCastState(EnemyController owner, EnemyState returnState, bool retargetEachFrame = false) : base(owner)
        {
            this.returnState = returnState;
            this.retargetEachFrame = retargetEachFrame;
        }

        public override void OnEnter()
        {
            owner.DebugState("HealCast → Enter");
            enterTime = Time.time;
            didFire = false;
            aborted = false;

            target = EnemyRegistry.FindLowestHpInRange(
                owner.transform.position, owner.Stats.healCastRange, exclude: owner);

            if (target == null)
            {
                aborted = true;
                exitAt = Time.time;
                return;
            }

            StopAgent();
            owner.Animation.TriggerHealCast();
            owner.Audio.PlayCast();

            exitAt = Time.time + RECOVER + 0.1f;
        }

        public override void OnExit()
        {
            ResumeAgent();
            if (didFire) owner.ConsumeHeal();
        }

        public override void Tick(float dt)
        {
            if (aborted) { owner.StateMachine.ChangeState(returnState); return; }

            // Face the ally.
            if (target != null && target.IsAlive)
            {
                Vector3 to = target.transform.position - owner.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                {
                    Quaternion want = Quaternion.LookRotation(to.normalized);
                    owner.transform.rotation = Quaternion.RotateTowards(
                        owner.transform.rotation, want, owner.Stats.turnSpeedDeg * dt);
                }
            }

            // Fallback firing if no animation event arrives.
            if (!didFire && Time.time >= enterTime + RECOVER * 0.4f)
                Fire();

            if (Time.time >= exitAt) owner.StateMachine.ChangeState(returnState);
        }

        public override void OnAnimHealOrbFire() => Fire();

        void Fire()
        {
            if (didFire) return;
            didFire = true;

            var prefab = owner.Stats.healingOrbPrefab;
            if (prefab == null) return;

            // Re-pick if the original target is gone.
            if (target == null || !target.IsAlive)
            {
                target = EnemyRegistry.FindLowestHpInRange(
                    owner.transform.position, owner.Stats.healCastRange, exclude: owner);
                if (target == null) return;
            }

            int n = Mathf.Max(1, owner.Stats.orbsPerCast);
            Vector3 origin = owner.OrbSpawnPoint.position;
            for (int i = 0; i < n; i++)
            {
                Quaternion spread = Quaternion.AngleAxis(
                    UnityEngine.Random.Range(-25f, 25f), Vector3.up);
                Vector3 launchDir = spread * owner.transform.forward;

                Vector3 spawnPos = origin + launchDir * 0.3f + Vector3.up * (i * 0.05f);
                var go = Object.Instantiate(prefab, spawnPos, Quaternion.LookRotation(launchDir));

                if (go.TryGetComponent(out HealingOrb orb))
                {
                    orb.Initialize(
                        target: target,
                        sourceCenter: owner.transform.position,
                        searchRange: owner.Stats.healCastRange,
                        homingSpeed: owner.Stats.orbHomingSpeed,
                        healAmount: owner.Stats.healPerOrb,
                        maxLifetime: ORB_LIFETIME,
                        retargetEachFrame: retargetEachFrame,
                        caster: owner);
                }
            }

            owner.Audio.PlayHeal();
            owner.VFX.SpawnMuzzleFlash(owner.transform);
        }
    }
}
