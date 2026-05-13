using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.Events;

namespace ITCLASH.Enemies
{
    /// Base for every enemy archetype. Owns HP, nav, anim/audio/VFX, player tracking,
    /// state machine, and cooldowns. Subclasses implement BuildStateMachine().
    [RequireComponent(typeof(NavMeshAgent))]
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
        public Transform visualTransform;
        [SerializeField] Transform muzzlePoint;
        [SerializeField] Transform orbSpawnPoint;
        [SerializeField] Transform dashImpactPoint;

        // ── Events ───────────────────────────────────────────────
        [Header("Floating & Bobbing")]
        public bool alwaysFacePlayer = true;
        public bool useFloating = true;
        public float floatHeight = 2.0f;
        public float bobSpeed = 1.5f;
        public float bobAmount = 0.2f;

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

        [Header("Spawn Settings")]
        [Tooltip("เวลาที่มอนสเตอร์จะ 'เกิด' (อยู่นิ่งๆ) ก่อนจะเริ่มทำงาน")]
        [SerializeField] float spawnDelay = 1.5f;

        protected bool isDead;
        protected bool isSpawning = true;

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

            // เปิดระบบลอยอัตโนมัติ
            useFloating = true; 

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

            if (!CompareTag("Enemy")) gameObject.tag = "Enemy";
            StateMachine = new EnemyStateMachine();
            
            if (Agent != null && alwaysFacePlayer)
            {
                Agent.updateRotation = false;
            }

            // ตั้งค่าความสูง Agent ให้ต่ำเพื่อให้ลอยได้อิสระ
            if (Agent != null && useFloating)
            {
                Agent.height = 0.5f;
                Debug.Log($"<color=cyan>[{name}]</color> Floating System Initialized. Target: {floatHeight}");
            }
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

        protected virtual void OnEnable()
        {
            EnemyRegistry.Register(this);
        }

        protected virtual void OnDisable()
        {
            EnemyRegistry.Unregister(this);
        }

        protected virtual void Update()
        {
            if (PlayerTransform == null) CacheReferences();
            if (!isDead && !isSpawning) StateMachine?.Tick(Time.deltaTime);
            HandleKnockback();

            // --- DEBUG KEY: กด L เพื่อเช็คว่าทำไมไม่ลอย ---
            if (Input.GetKeyDown(KeyCode.L))
            {
                string status = $"<color=yellow>[{name} Debug]</color>\n" +
                               $"- UseFloating: {useFloating}\n" +
                               $"- IsOnNavMesh: {Agent.isOnNavMesh}\n" +
                               $"- Current BaseOffset: {Agent.baseOffset}\n" +
                               $"- Target FloatHeight: {floatHeight}\n" +
                               $"- Agent Height: {Agent.height}";
                Debug.Log(status);
            }

            // ── Face Player Always ──
            if (alwaysFacePlayer && !isDead && PlayerTransform != null)
            {
                FacePlayer(Time.deltaTime);
            }

            // ── Floating & Bobbing ──
            if (useFloating && !isDead)
            {
                HandleFloating();
            }

            // Update walking animation if animator exists
            if (Anim != null)
            {
                bool isWalking = Agent.velocity.magnitude > 0.1f && !Agent.isStopped;
                animConfig.SetWalking(isWalking);
            }
        }

        private float logTimer = 0f;

        private void HandleFloating()
        {
            if (Agent == null) return;

            // คำนวณความสูงเป้าหมายแบบแกว่งขึ้นลง (Dynamic Height)
            // ทำให้ตำแหน่ง Y ของศัตรูเปลี่ยนตลอดเวลาอย่างเป็นธรรมชาติ
            float dynamicHeight = floatHeight + (Mathf.Sin(Time.time * bobSpeed) * bobAmount);

            // 1. ปรับความสูง Agent ให้ลอยตามค่าที่แกว่ง (ทำให้มันขยับตลอดเวลา)
            Agent.baseOffset = Mathf.Lerp(Agent.baseOffset, dynamicHeight, Time.deltaTime * 2f);

            // 2. Periodic Log (ทุกๆ 1 วินาที)
            logTimer += Time.deltaTime;
            if (logTimer >= 1f)
            {
                logTimer = 0f;
                Debug.Log($"<color=white>[{name} Floating]</color> Y-Offset: {Agent.baseOffset:F2} (Bobbing...)");
            }
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
