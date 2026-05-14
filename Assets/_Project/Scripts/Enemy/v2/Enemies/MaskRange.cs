using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// MaskRange: หน้ากากสายยิงไกล
    /// - ยืนอยู่ตำแหน่งกลาง (Mid-line) ระหว่างสายประชิดและสายฮีล
    /// - ยิงกระสุนโจมตีใส่ผู้เล่นจากระยะไกล
    /// - มีระบบถอยหนีเหมือนหน้ากากตัวอื่นๆ
    /// </summary>
    public class MaskRange : EnemyController
    {
        [Header("Attack Settings")]
        public GameObject projectilePrefab; 
        public Transform shootPoint;        
        public Transform targetTransform;   
        public float damage = 10f;
        public float attackCooldown = 3f;
        public float projectileSpeed = 15f;
        public float attackRange = 12f;     

        [Header("Accuracy Settings (%)")]
        [Range(0, 100)] public float missChance = 20f;   
        [Range(0, 100)] public float randomChance = 10f; 

        [Header("Positioning")]
        public float midRangeDistance = 9f; 
        public float retreatDistance = 5f;  

        [Header("Orientation & Float")]
        public Vector3 _rotationOffset;
        public Vector2 _randomHeightRange = new Vector2(2f, 4f);

        [Header("Wall Avoidance")]
        public float wallCheckDistance = 2.5f;     
        public float wallAvoidTurnSpeed = 270f;    
        public LayerMask wallLayers;               
        
        private float nextAttackTime;
        private float timeOffset;

        protected override void BuildStateMachine() { }

        protected override void Awake()
        {
            base.Awake();
            if (visualTransform == null) visualTransform = transform;
            floatHeight = Random.Range(_randomHeightRange.x, _randomHeightRange.y);
            timeOffset = Random.Range(0f, 10f);
        }

        protected override void Update()
        {
            base.Update(); // เรียกตัวแม่ก่อนเสมอเพื่อหา Player และหันหน้า
            if (PlayerTransform == null || !IsAlive || isSpawning) return;


            // --- Wall Avoidance Check (8-Direction Scan) ---
            if (HandleWallAvoidance()) return;

            // การหันหน้าจะใช้ระบบของ base.Update() แทนเพื่อให้เสถียรขึ้น


            float dist = DistanceToPlayer();
            if (dist <= attackRange && Time.time >= nextAttackTime && HasLineOfSight())
            {
                FireProjectile();
            }

            MaintainPosition(dist);
        }

        private bool HandleWallAvoidance()
        {
            if (Agent == null || !Agent.isOnNavMesh) return false;

            Vector3 origin = transform.position;
            Vector3[] directions = {
                transform.forward,
                -transform.forward,
                transform.right,
                -transform.right,
                (transform.forward + transform.right).normalized,
                (transform.forward - transform.right).normalized,
                (-transform.forward + transform.right).normalized,
                (-transform.forward - transform.right).normalized,
            };

            bool frontBlocked = Physics.Raycast(origin, transform.forward, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore);
            if (!frontBlocked) return false;

            Vector3 bestDir = Vector3.zero;
            float maxClearDist = 0f;

            foreach (var dir in directions)
            {
                float clearDist = wallCheckDistance;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallCheckDistance * 3f, wallLayers, QueryTriggerInteraction.Ignore))
                    clearDist = hit.distance;
                else
                    clearDist = wallCheckDistance * 3f;

                if (clearDist > maxClearDist)
                {
                    maxClearDist = clearDist;
                    bestDir = dir;
                }
            }

            if (bestDir == Vector3.zero) return true;

            bestDir.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(bestDir) * Quaternion.Euler(_rotationOffset);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, wallAvoidTurnSpeed * Time.deltaTime);

            Vector3 avoidWorldPos = transform.position + bestDir * 4f;
            if (Physics.Raycast(avoidWorldPos + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 20f, wallLayers, QueryTriggerInteraction.Ignore))
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(groundHit.point, out UnityEngine.AI.NavMeshHit navHit, 4f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    Agent.isStopped = false;
                    Agent.SetDestination(navHit.position);
                }
            }
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * wallCheckDistance);
        }

        private void MaintainPosition(float currentDist)
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            if (currentDist < retreatDistance)
            {
                Vector3 retreatDir = (transform.position - PlayerTransform.position).normalized;
                retreatDir.y = 0;
                Agent.SetDestination(PlayerTransform.position + retreatDir * (retreatDistance + 2f));
                Agent.isStopped = false;
            }
            else if (currentDist > attackRange || currentDist > midRangeDistance + 1f)
            {
                Agent.isStopped = false;
                Agent.SetDestination(PlayerTransform.position);
            }
            else
            {
                Agent.isStopped = true;
                Agent.velocity = Vector3.zero;
            }
        }

        private void FireProjectile()
        {
            if (projectilePrefab == null) return;
            nextAttackTime = Time.time + attackCooldown;
            Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position;
            Vector3 targetPos = targetTransform != null ? targetTransform.position : 
                (Camera.main != null ? Camera.main.transform.position : PlayerTransform.position + Vector3.up * 1.5f);
            Vector3 fireDir = (targetPos - spawnPos).normalized;
            float roll = Random.Range(0f, 100f);
            if (roll < randomChance)
                fireDir = Quaternion.Euler(Random.Range(-30, 30), Random.Range(-45, 45), 0) * fireDir;
            else if (roll < (randomChance + missChance))
            {
                float sideOffset = Random.value > 0.5f ? 2.5f : -2.5f;
                Vector3 sideDir = Vector3.Cross(fireDir, Vector3.up).normalized;
                targetPos += sideDir * sideOffset;
                fireDir = (targetPos - spawnPos).normalized;
            }
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(fireDir));
            var mover = proj.AddComponent<MaskProjectileMover>();
            mover.Setup(damage, projectileSpeed);
        }



        private bool HasLineOfSight()
        {
            if (PlayerTransform == null) return false;
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 target = Camera.main != null ? Camera.main.transform.position : PlayerTransform.position + Vector3.up * 1.5f;
            Vector3 dir = (target - origin).normalized;
            float dist = Vector3.Distance(origin, target);
            int mask = ~LayerMask.GetMask("Enemy", "Projectile", "Ignore Raycast");
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("Player")) return true;
                return false; 
            }
            return true;
        }
    }

    public class MaskProjectileMover : MonoBehaviour
    {
        private float damage;
        private float speed;
        public void Setup(float dmg, float spd) { damage = dmg; speed = spd; Destroy(gameObject, 4f); }
        void Update() { transform.Translate(Vector3.forward * speed * Time.deltaTime); }
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var hp = other.GetComponent<PlayerHealth>();
                if (hp != null) hp.TakeDamage((int)damage);
                Destroy(gameObject);
            }
            else if (other.gameObject.layer == 0 || other.CompareTag("Untagged")) Destroy(gameObject);
        }
    }
}
