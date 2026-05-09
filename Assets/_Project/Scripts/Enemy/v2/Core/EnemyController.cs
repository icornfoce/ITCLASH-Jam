using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace ITCLASH.Enemies
{
    /// Base for every enemy archetype. Owns HP, nav, anim/audio/VFX, player tracking,
    /// state machine, and cooldowns. Subclasses implement BuildStateMachine().
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public abstract class EnemyController : MonoBehaviour, IDamageable, IKnockbackable
    {
        // ── Stats ────────────────────────────────────────────────
        [Header("Data")]
        [SerializeField] protected EnemyStatsSO stats;

        // ── Presentation ─────────────────────────────────────────
        [Header("Presentation")]
        [SerializeField] EnemyAnimationConfig animConfig  = new EnemyAnimationConfig();
        [SerializeField] EnemyAudioConfig     audioConfig = new EnemyAudioConfig();
        [SerializeField] EnemyVFXConfig       vfxConfig   = new EnemyVFXConfig();

        // ── Optional anchors ─────────────────────────────────────
        [Header("Anchors (optional)")]
        [SerializeField] Transform muzzlePoint;
        [SerializeField] Transform orbSpawnPoint;
        [SerializeField] Transform dashImpactPoint;

        // ── Events ───────────────────────────────────────────────
        [Header("Events")]
        public UnityEvent<float> OnDamaged = new UnityEvent<float>();
        public UnityEvent<float> OnHealed  = new UnityEvent<float>();
        public UnityEvent OnDeath          = new UnityEvent();

        // ── Debug ────────────────────────────────────────────────
        [Header("Debug")]
        [SerializeField] bool debugLogStates;

        // ── Cached refs (public for state classes) ───────────────
        public EnemyStatsSO Stats               => stats;
        public NavMeshAgent Agent               { get; private set; }
        public Animator Anim                    { get; private set; }
        public EnemyAnimationConfig Animation   => animConfig;
        public EnemyAudioConfig Audio           => audioConfig;
        public EnemyVFXConfig VFX               => vfxConfig;
        public EnemyStateMachine StateMachine   { get; private set; }

        public Transform PlayerTransform        { get; private set; }
        public PlayerHealth PlayerHealth        { get; private set; }

        public Transform MuzzlePoint        => muzzlePoint != null ? muzzlePoint : transform;
        public Transform OrbSpawnPoint      => orbSpawnPoint != null ? orbSpawnPoint : transform;
        public Transform DashImpactPoint    => dashImpactPoint != null ? dashImpactPoint : transform;

        // ── HP / IDamageable ─────────────────────────────────────
        public float CurrentHealth { get; private set; }
        public float HealthPercent => stats != null && stats.maxHealth > 0f
            ? Mathf.Clamp01(CurrentHealth / stats.maxHealth) : 0f;
        public bool IsAlive => !isDead;
        public Transform Transform => transform;

        bool isDead;

        // ── Knockback ─────────────────────────────────────────────
        Vector3 knockbackVelocity = Vector3.zero;
        float   knockbackTimer    = 0f;

        // ── Cooldowns (shared between states) ────────────────────
        public bool MeleeReady  => Time.time >= nextMeleeReady;
        public bool DashReady   => Time.time >= nextDashReady;
        public bool RangedReady => Time.time >= nextRangedReady;
        public bool HealReady   => Time.time >= nextHealReady;

        float nextMeleeReady, nextDashReady, nextRangedReady, nextHealReady;

        public void ConsumeMelee()  { if (stats != null) nextMeleeReady  = Time.time + stats.meleeCooldown; }
        public void ConsumeDash()   { if (stats != null) nextDashReady   = Time.time + stats.dashCooldown; }
        public void ConsumeRanged() { if (stats != null) nextRangedReady = Time.time + stats.rangedCooldown; }
        public void ConsumeHeal()   { if (stats != null) nextHealReady   = Time.time + stats.healCooldown; }

        // ── Lifecycle ────────────────────────────────────────────
        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Anim  = GetComponent<Animator>();

            if (stats == null)
            {
                Debug.LogError($"[{name}] EnemyController has no EnemyStatsSO assigned.", this);
                enabled = false;
                return;
            }

            CurrentHealth = stats.maxHealth;
            Agent.speed = stats.moveSpeed;
            Agent.angularSpeed = stats.turnSpeedDeg;

            animConfig.Initialize(Anim);
            audioConfig.Initialize(gameObject);

            // Force the tag so skills' CompareTag("Enemy") always works on new prefabs.
            if (!CompareTag("Enemy")) gameObject.tag = "Enemy";

            StateMachine = new EnemyStateMachine();
        }

        protected virtual void Start()
        {
            CacheReferences();
            vfxConfig.SpawnSpawnFx(transform);
            audioConfig.PlaySpawn();
            BuildStateMachine();
        }

        void CacheReferences()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                PlayerTransform = go.transform;
                PlayerHealth    = go.GetComponent<PlayerHealth>();
            }
        }

        protected virtual void OnEnable()  => EnemyRegistry.Register(this);
        protected virtual void OnDisable() => EnemyRegistry.Unregister(this);

        protected virtual void Update()
        {
            if (PlayerTransform == null) CacheReferences();
            if (!isDead) StateMachine?.Tick(Time.deltaTime);
            HandleKnockback();
        }

        void HandleKnockback()
        {
            if (knockbackTimer <= 0f) return;

            Agent.isStopped = true;
            Agent.velocity  = Vector3.zero;
            transform.position += knockbackVelocity * Time.deltaTime;

            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 8f);
            knockbackTimer   -= Time.deltaTime;

            if (knockbackTimer <= 0f)
            {
                knockbackVelocity = Vector3.zero;
                Agent.isStopped   = false;
            }
        }

        // ── Damage / Heal ────────────────────────────────────────

        /// Public entry point for typing-system skills (SendMessage("TakeDamage", int)).
        public void TakeDamage(int amount) => ApplyDamage(amount);

        public void ApplyDamage(float amount)
        {
            if (isDead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            animConfig.TriggerHit();
            audioConfig.PlayGetHit();
            vfxConfig.SpawnHitImpact(transform);
            OnDamaged?.Invoke(amount);

            if (CurrentHealth <= 0f) Die();
        }

        /// Knockback without Rigidbody — works with any NavMeshAgent enemy.
        public void ApplyKnockback(Vector3 force, float duration = 0.35f)
        {
            if (isDead) return;
            knockbackVelocity = force;
            knockbackTimer    = duration;
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f || stats == null) return;

            float before = CurrentHealth;
            CurrentHealth = Mathf.Min(stats.maxHealth, CurrentHealth + amount);
            float delta = CurrentHealth - before;
            if (delta > 0f) OnHealed?.Invoke(delta);
        }

        void Die()
        {
            if (isDead) return;
            isDead = true;
            if (debugLogStates) Debug.Log($"[{name}] Died.", this);
            OnDeath?.Invoke();
            StateMachine?.ChangeState(new DeadState(this));
        }

        // ── Animation event entry points (called by EnemyAnimationEventRelay) ──
        public void OnAnimAttackHit()   => StateMachine?.RaiseAttackHit();
        public void OnAnimDashImpact()  => StateMachine?.RaiseDashImpact();
        public void OnAnimRangedFire()  => StateMachine?.RaiseRangedFire();
        public void OnAnimHealOrbFire() => StateMachine?.RaiseHealOrbFire();
        public void OnAnimFootstep()    => StateMachine?.RaiseFootstep();

        // ── State machine setup ──────────────────────────────────
        /// Subclasses build their initial states here and call StateMachine.ChangeState(...).
        protected abstract void BuildStateMachine();

        public void DebugState(string msg)
        {
            if (debugLogStates) Debug.Log($"[{name}] {msg}", this);
        }

        // ── Helpers ──────────────────────────────────────────────
        public float DistanceToPlayer()
        {
            if (PlayerTransform == null) return float.PositiveInfinity;
            return Vector3.Distance(transform.position, PlayerTransform.position);
        }

        /// Smoothly rotate to face the player (Y-axis only).
        public void FacePlayer(float dt)
        {
            if (PlayerTransform == null || stats == null) return;
            Vector3 to = PlayerTransform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, stats.turnSpeedDeg * dt);
        }
    }
}
