namespace ITCLASH.Enemies
{
    /// <summary>
    /// Minimal state machine: holds the current state, swaps via <see cref="ChangeState"/>,
    /// ticks each frame, and forwards animation events.
    /// </summary>
    public sealed class EnemyStateMachine
    {
        public EnemyState Current { get; private set; }

        public void ChangeState(EnemyState next)
        {
            if (next == null || ReferenceEquals(next, Current)) return;
            Current?.OnExit();
            Current = next;
            Current.OnEnter();
        }

        public void Tick(float dt) => Current?.Tick(dt);

        public void RaiseAttackHit()    => Current?.OnAnimAttackHit();
        public void RaiseDashImpact()   => Current?.OnAnimDashImpact();
        public void RaiseRangedFire()   => Current?.OnAnimRangedFire();
        public void RaiseHealOrbFire()  => Current?.OnAnimHealOrbFire();
        public void RaiseFootstep()     => Current?.OnAnimFootstep();
    }
}
