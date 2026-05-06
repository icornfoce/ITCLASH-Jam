namespace ITCLASH.Enemies
{
    /// <summary>
    /// Base class for any AI state. Subclasses override only the lifecycle hooks they need.
    /// Animation-event hooks are routed here from <see cref="EnemyAnimationEventRelay"/>
    /// via <see cref="EnemyController"/>, so attack/cast frames land at the exact animation moment.
    /// </summary>
    public abstract class EnemyState
    {
        protected readonly EnemyController owner;
        protected EnemyController Owner => owner;

        protected EnemyState(EnemyController owner) { this.owner = owner; }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Tick(float dt) { }

        // ── Animation-event hooks (Unity → AnimationEventRelay → EnemyController → here) ──
        public virtual void OnAnimAttackHit() { }
        public virtual void OnAnimDashImpact() { }
        public virtual void OnAnimRangedFire() { }
        public virtual void OnAnimHealOrbFire() { }
        public virtual void OnAnimFootstep() { }
    }
}
