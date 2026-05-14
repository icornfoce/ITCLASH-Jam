using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.Events;

namespace ITCLASH.Enemies
{
    public abstract class EnemyController : MonoBehaviour, IDamageable, IKnockbackable
    {
        // ── Data ──
        [Header("Data")]
        [SerializeField] protected EnemyStatsSO stats;
        public EnemyStatsSO Stats => stats;

        // ── Presentation ──
        [Header("Presentation")]
        [SerializeField] protected EnemyAnimationConfig animConfig  = new EnemyAnimationConfig();
        [SerializeField] protected EnemyAudioConfig     audioConfig = new EnemyAudioConfig();
        [SerializeField] protected EnemyVFXConfig       vfxConfig   = new EnemyVFXConfig();

        public EnemyAnimationConfig Animation => animConfig;
        public EnemyAudioConfig Audio => audioConfig;
        public EnemyVFXConfig VFX => vfxConfig;

        [Header("Anchors (optional)")]
        public Transform visualTransform;
        [SerializeField] Transform muzzlePoint;
        [SerializeField] Transform orbSpawnPoint;
        [SerializeField] Transform dashImpactPoint;

        public Transform MuzzlePoint => muzzlePoint != null ? muzzlePoint : transform;
        public Transform OrbSpawnPoint => orbSpawnPoint != null ? orbSpawnPoint : transform;
        public Transform DashImpactPoint => dashImpactPoint != null ? dashImpactPoint : transform;

        [Header("Floating & Bobbing")]
        public bool alwaysFacePlayer = true;
        public bool useFloating = true;
        public float floatHeight = 2.5f;
        public float bobSpeed = 1.5f;
        public float bobAmount = 0.2f;
        
        [Header("Events")]
        public UnityEvent<float> OnDamaged = new UnityEvent<float>();
        public UnityEvent<float> OnHealed  = new UnityEvent<float>();
        public UnityEvent OnDeath          = new UnityEvent();

        [Header("Debug")]
        [SerializeField] bool debugLogStates;

        public NavMeshAgent Agent { get; private set; }
        public Animator Anim { get; private set; }
        public EnemyStateMachine StateMachine { get; private set; }
        public Transform PlayerTransform { get; private set; }
        public PlayerHealth PlayerHealth { get; private set; }

        public float CurrentHealth { get; private set; }
        public float HealthPercent => stats != null && stats.maxHealth > 0f
            ? Mathf.Clamp01(CurrentHealth / stats.maxHealth) : 0f;
        public bool IsAlive => !isDead;
        public Transform Transform => transform;

        [SerializeField] float spawnDelay = 1.5f;
        protected bool isDead;
        protected bool isSpawning = true;

        Vector3 knockbackVelocity = Vector3.zero;
        float knockbackTimer = 0f;

        // ── Cooldowns ──
        public bool MeleeReady  => Time.time >= nextMeleeReady;
        public bool DashReady   => Time.time >= nextDashReady;
        public bool RangedReady => Time.time >= nextRangedReady;
        public bool HealReady   => Time.time >= nextHealReady;

        float nextMeleeReady, nextDashReady, nextRangedReady, nextHealReady;

        public void ConsumeMelee()  { if (stats != null) nextMeleeReady  = Time.time + stats.meleeCooldown; }
        public void ConsumeDash()   { if (stats != null) nextDashReady   = Time.time + stats.dashCooldown; }
        public void ConsumeRanged() { if (stats != null) nextRangedReady = Time.time + stats.rangedCooldown; }
        public void ConsumeHeal()   { if (stats != null) nextHealReady   = Time.time + stats.healCooldown; }

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Anim  = GetComponentInChildren<Animator>();
            useFloating = true; 

            if (stats != null)
            {
                CurrentHealth = stats.maxHealth;
                Agent.speed = stats.moveSpeed;
                Agent.angularSpeed = stats.turnSpeedDeg;
            }

            animConfig.Initialize(Anim);
            audioConfig.Initialize(gameObject);
            StateMachine = new EnemyStateMachine();
            
            if (Agent != null && alwaysFacePlayer) Agent.updateRotation = false;
        }

        protected virtual void Start()
        {
            CacheReferences();
            vfxConfig.SpawnSpawnFx(transform);
            audioConfig.PlaySpawn();
            BuildStateMachine();
            StartCoroutine(SpawnDelayRoutine());
        }

        private IEnumerator SpawnDelayRoutine()
        {
            isSpawning = true;
            yield return new WaitForSeconds(spawnDelay);
            isSpawning = false;
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

        protected virtual void OnEnable() => EnemyRegistry.Register(this);
        protected virtual void OnDisable() => EnemyRegistry.Unregister(this);

        protected virtual void Update()
        {
            if (PlayerTransform == null) CacheReferences();
            if (!isDead && !isSpawning) StateMachine?.Tick(Time.deltaTime);
            HandleKnockback();

            if (alwaysFacePlayer && !isDead && PlayerTransform != null) FacePlayer(Time.deltaTime);
            if (useFloating && !isDead) HandleFloating();

            if (Anim != null)
            {
                bool isWalking = Agent.velocity.magnitude > 0.1f && !Agent.isStopped;
                animConfig.SetWalking(isWalking);
            }
        }

        private void HandleFloating()
        {
            if (Agent == null) return;

            // ระบบลอยแบบดั้งเดิม: ใช้แค่ baseOffset และการแกว่ง (Bobbing)
            float targetHeight = floatHeight + (Mathf.Sin(Time.time * bobSpeed) * bobAmount);
            
            // ค่อยๆ Lerp ค่า baseOffset
            Agent.baseOffset = Mathf.Lerp(Agent.baseOffset, targetHeight, Time.deltaTime * 2f);
        }

        void HandleKnockback()
        {
            if (knockbackTimer <= 0f) return;
            Agent.isStopped = true;
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 8f);
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f) Agent.isStopped = false;
        }

        public void TakeDamage(int amount) => ApplyDamage(amount);
        public void ApplyDamage(float amount)
        {
            if (isDead || isSpawning || amount <= 0f) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            animConfig.TriggerHit();
            audioConfig.PlayGetHit();
            vfxConfig.SpawnHitImpact(transform);
            OnDamaged?.Invoke(amount);
            if (CurrentHealth <= 0f) Die();
        }

        public void ApplyKnockback(Vector3 force, float duration = 0.35f)
        {
            if (isDead) return;
            knockbackVelocity = force;
            knockbackTimer = duration;
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
            OnDeath?.Invoke();
            StateMachine?.ChangeState(new DeadState(this));
        }

        public void OnAnimAttackHit()   => StateMachine?.RaiseAttackHit();
        public void OnAnimDashImpact()  => StateMachine?.RaiseDashImpact();
        public void OnAnimRangedFire()  => StateMachine?.RaiseRangedFire();
        public void OnAnimHealOrbFire() => StateMachine?.RaiseHealOrbFire();
        public void OnAnimFootstep()    => StateMachine?.RaiseFootstep();

        protected abstract void BuildStateMachine();

        public void DebugState(string msg) { if (debugLogStates) Debug.Log($"[{name}] {msg}", this); }

        public float DistanceToPlayer()
        {
            if (PlayerTransform == null) return float.PositiveInfinity;
            return Vector3.Distance(transform.position, PlayerTransform.position);
        }

        public void FacePlayer(float dt)
        {
            if (PlayerTransform == null || stats == null) return;
            Vector3 to = PlayerTransform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, stats.turnSpeedDeg * dt);
        }
    }
}
