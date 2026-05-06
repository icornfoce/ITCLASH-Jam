using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Green homing orb spawned by HealCastState. At launch, locks onto the
    /// lowest-HP ally (provided by the caster). Per frame, smoothly turns toward
    /// the target and moves at <c>homingSpeed</c>. On arrival within
    /// <see cref="arrivalDistance"/>, calls <see cref="EnemyController.Heal"/> on the
    /// target and self-destructs.
    ///
    /// If <c>retargetEachFrame</c> is true, re-picks the lowest-HP ally each frame
    /// from <see cref="EnemyRegistry"/> within the cast range. Useful if you want
    /// orbs to "chase" whoever is currently most wounded mid-flight.
    /// </summary>
    public sealed class HealingOrb : MonoBehaviour
    {
        [Header("Behaviour")]
        [Tooltip("How close the orb must get to the target to apply healing.")]
        [Range(0f, 5f)] [SerializeField] float arrivalDistance = 0.5f;

        [Tooltip("How fast the orb's facing turns toward the target (deg/s).")]
        [Range(0f, 1080f)] [SerializeField] float steerDegPerSec = 540f;

        [Tooltip("Gentle initial drift before homing kicks in (sec).")]
        [Range(0f, 1f)] [SerializeField] float launchDriftSeconds = 0.1f;

        [Header("Presentation")]
        [Tooltip("VFX spawned at the target on heal landing.")]
        [SerializeField] GameObject healVFX;

        [Tooltip("SFX played when the orb lands on its target.")]
        [SerializeField] AudioCue healSFX = new AudioCue();

        [Tooltip("AudioSource used for the impact SFX. Auto-added if null.")]
        [SerializeField] AudioSource audioSource;

        // ── Runtime, set by Initialize ───────────────────────────
        EnemyController target;
        EnemyController caster;
        Vector3 sourceCenter;
        float searchRange;
        float homingSpeed;
        float healAmount;
        float lifetimeRemaining;
        bool retargetEachFrame;
        bool initialized;
        float age;

        public void Initialize(
            EnemyController target,
            Vector3 sourceCenter,
            float searchRange,
            float homingSpeed,
            float healAmount,
            float maxLifetime,
            bool retargetEachFrame,
            EnemyController caster)
        {
            this.target = target;
            this.caster = caster;
            this.sourceCenter = sourceCenter;
            this.searchRange = searchRange;
            this.homingSpeed = homingSpeed;
            this.healAmount = healAmount;
            this.lifetimeRemaining = maxLifetime;
            this.retargetEachFrame = retargetEachFrame;
            this.initialized = true;
        }

        void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f;
                }
            }
        }

        void Update()
        {
            if (!initialized) return;

            age += Time.deltaTime;
            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f) { Destroy(gameObject); return; }

            // Re-target if requested or if the original target is gone.
            if (retargetEachFrame || target == null || !target.IsAlive)
            {
                var candidate = EnemyRegistry.FindLowestHpInRange(sourceCenter, searchRange, exclude: caster);
                if (candidate != null) target = candidate;
            }

            if (target == null || !target.IsAlive)
            {
                // Drift forward briefly before fizzling.
                transform.position += transform.forward * homingSpeed * 0.5f * Time.deltaTime;
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float dist = toTarget.magnitude;

            if (dist <= arrivalDistance)
            {
                ApplyHeal();
                return;
            }

            // Smooth steering: rotate forward toward target, then advance.
            if (age >= launchDriftSeconds && toTarget.sqrMagnitude > 0.0001f)
            {
                Quaternion want = Quaternion.LookRotation(toTarget.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, want, steerDegPerSec * Time.deltaTime);
            }

            transform.position += transform.forward * homingSpeed * Time.deltaTime;
        }

        void ApplyHeal()
        {
            if (target != null && target.IsAlive) target.Heal(healAmount);
            if (healVFX != null && target != null) Instantiate(healVFX, target.transform.position, Quaternion.identity);
            healSFX.Play(audioSource);
            Destroy(gameObject);
        }
    }
}
