using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// Straight-line magic orb fired by RangedCastState.
    /// Uses a SphereCast over each frame's movement to avoid tunnelling. On hitting
    /// the player it deals <c>damage</c> via <see cref="PlayerHealth.TakeDamage(float)"/>;
    /// any solid hit spawns the impact VFX/SFX and destroys the orb.
    /// </summary>
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [Header("Collision")]
        [Tooltip("Layers the projectile collides with — should include Default + Player layers.")]
        [SerializeField] LayerMask hitMask = ~0;

        [Tooltip("Sphere-cast radius for hit detection.")]
        [Range(0f, 2f)] [SerializeField] float castRadius = 0.25f;

        [Header("Presentation")]
        [Tooltip("VFX prefab spawned at impact (leave empty for none).")]
        [SerializeField] GameObject hitVFX;

        [Tooltip("SFX played at impact.")]
        [SerializeField] AudioCue impactSFX = new AudioCue();

        [Tooltip("AudioSource used for impact SFX. Auto-added if null.")]
        [SerializeField] AudioSource audioSource;

        [Header("Trail / Body (optional)")]
        [Tooltip("Optional trail GO disabled at impact for a clean fade.")]
        [SerializeField] GameObject trail;

        // ── Runtime values set by Launch() ────────────────────────
        float speed;
        float damage;
        float lifetimeRemaining;
        Vector3 direction;
        bool launched;

        public void Launch(Vector3 direction, float speed, float damage, float lifetime, Collider ownerCollider = null)
        {
            this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            this.speed = speed;
            this.damage = damage;
            this.lifetimeRemaining = lifetime;
            transform.rotation = Quaternion.LookRotation(this.direction);
            
            if (ownerCollider != null)
            {
                // ป้องกันกระสุนชนตัวคนยิงเอง
                Collider myCollider = GetComponent<Collider>();
                if (myCollider != null) Physics.IgnoreCollision(myCollider, ownerCollider);
            }

            launched = true;
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
            if (!launched) return;

            float step = speed * Time.deltaTime;

            if (Physics.SphereCast(transform.position, castRadius, direction, out var hit, step + 0.05f, hitMask, QueryTriggerInteraction.Ignore))
            {
                ResolveHit(hit);
                return;
            }

            transform.position += direction * step;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f) Despawn(transform.position);
        }

        void ResolveHit(RaycastHit hit)
        {
            Debug.Log($"[Projectile] Hit: {hit.collider.name} (Tag: {hit.collider.tag})");
            
            // หา IDamageable บนเป้าหมาย
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // ตรวจสอบว่าเป้าหมายไม่ใช่ศัตรูเหมือนกัน (เพื่อไม่ให้ยิงโดนกันเอง)
                // เราเช็คจาก Layer หรือ Tag ก็ได้ แต่ในที่นี้เราจะเช็คว่ามันไม่มี EnemyController
                if (hit.collider.GetComponentInParent<EnemyController>() == null)
                {
                    damageable.ApplyDamage(damage);
                    Debug.Log($"[Projectile] Damage {damage} applied to {hit.collider.name}.");
                }
            }
            
            Despawn(hit.point);
        }

        void Despawn(Vector3 atPosition)
        {
            if (hitVFX != null) Instantiate(hitVFX, atPosition, Quaternion.LookRotation(-direction));
            impactSFX.Play(audioSource);

            // Detach and let the trail finish on its own.
            if (trail != null)
            {
                trail.transform.SetParent(null, true);
                Destroy(trail, 2f);
            }
            Destroy(gameObject);
        }
    }
}
