namespace ITCLASH.Enemies
{
    /// Base class for AI states. Override lifecycle hooks as needed.
    public abstract class EnemyState
    {
        protected readonly EnemyController owner;
        protected EnemyController Owner => owner;

        protected EnemyState(EnemyController owner) { this.owner = owner; }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Tick(float dt) { }

        // ── Animation-event hooks ──
        public virtual void OnAnimAttackHit() { }
        public virtual void OnAnimDashImpact() { }
        public virtual void OnAnimRangedFire() { }
        public virtual void OnAnimHealOrbFire() { }
        public virtual void OnAnimFootstep() { }

        // ── Agent helpers (reduces null-check boilerplate) ──
        protected void StopAgent()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = true;
                owner.Agent.velocity = UnityEngine.Vector3.zero;
            }
        }

        protected void ResumeAgent()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.isStopped = false;
        }
    }
}
