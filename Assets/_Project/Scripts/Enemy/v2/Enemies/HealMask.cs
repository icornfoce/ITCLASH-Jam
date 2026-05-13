using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// HealMask: หน้ากากสาย Support
    /// - ยืนคุมเชิงอยู่แนวหลังสุดเสมอ
    /// - หันหน้ามองเพื่อนที่เลือดน้อยที่สุด
    /// - ยิงกระสุนฮีลให้เพื่อน 3 ตัวที่เลือดน้อยที่สุดพร้อมกัน
    /// </summary>
    public class HealMask : EnemyController
    {
        [Header("Heal Settings")]
        public float healAmount = 20f;
        public float healCooldown = 4f;
        public GameObject healProjectilePrefab; 
        public Transform shootPoint;           
        public int maxTargets = 3;
        public float healRange = 15f;          

        [Header("Support Positioning")]
        public float safeDistance = 12f;       
        public float spacingFromAllies = 3f;   

        [Header("Orientation & Float")]
        public Vector3 _rotationOffset;
        public Vector2 _randomHeightRange = new Vector2(2.5f, 4.5f); 

        [Header("Wall Avoidance")]
        public float wallCheckDistance = 2.5f;     
        public float wallAvoidTurnSpeed = 270f;    
        public LayerMask wallLayers;               
        
        private float nextHealTime;
        private float timeOffset;
        private EnemyController currentLookTarget;

        protected override void BuildStateMachine() { }

        protected override void Awake()
        {
            base.Awake();
            if (visualTransform == null) visualTransform = transform;
            floatHeight = Random.Range(_randomHeightRange.x, _randomHeightRange.y);
            timeOffset = Random.Range(0f, 10f);

            // เซ็ตค่า baseOffset ทันทีก่อน Agent จะดูดลงพื้น
            if (Agent != null) Agent.baseOffset = floatHeight;
        }

        protected override void Update()
        {
            if (PlayerTransform == null || !IsAlive || isSpawning) return;

            if (useFloating && Agent != null)
            {
                float bobY = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmount;
                Agent.baseOffset = floatHeight + bobY;
            }

            // --- Wall Avoidance Check (8-Direction Scan) ---
            if (HandleWallAvoidance()) return;

            List<EnemyController> alliesToHeal = FindLowestHPAllies(maxTargets);
            if (alliesToHeal.Count > 0)
            {
                currentLookTarget = alliesToHeal[0];
                FaceTarget(currentLookTarget.transform.position + Vector3.up);
                float distToAlly = Vector3.Distance(transform.position, currentLookTarget.transform.position);
                if (distToAlly <= healRange)
                {
                    if (Time.time >= nextHealTime) StartCoroutine(HealBatchRoutine(alliesToHeal));
                    MaintainBacklinePosition();
                }
                else MoveTowardsAlly(currentLookTarget.transform.position);
            }
            else
            {
                FaceTarget(PlayerTransform.position + Vector3.up * 1.5f);
                MaintainBacklinePosition();
            }
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

            bool frontBlocked = Physics.Raycast(origin, transform.forward, wallCheckDistance, wallLayers);
            if (!frontBlocked) return false;

            Vector3 bestDir = Vector3.zero;
            float maxClearDist = 0f;

            foreach (var dir in directions)
            {
                float clearDist = wallCheckDistance;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallCheckDistance * 3f, wallLayers))
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
            if (Physics.Raycast(avoidWorldPos + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 20f))
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

        private void MoveTowardsAlly(Vector3 allyPos)
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            Agent.isStopped = false;
            Agent.SetDestination(allyPos);
        }

        private void MaintainBacklinePosition()
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            Vector3 retreatDir = (transform.position - PlayerTransform.position).normalized;
            retreatDir.y = 0;
            Vector3 backlinePos = PlayerTransform.position + retreatDir * safeDistance;
            Agent.SetDestination(backlinePos);
            Agent.stoppingDistance = 2f;
            Agent.isStopped = false;
        }

        private List<EnemyController> FindLowestHPAllies(int count)
        {
            return EnemyRegistry.All
                .Where(e => e != this && e.IsAlive && e.HealthPercent < 1.0f)
                .OrderBy(e => e.HealthPercent)
                .Take(count)
                .ToList();
        }

        private IEnumerator HealBatchRoutine(List<EnemyController> targets)
        {
            nextHealTime = Time.time + healCooldown;
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;
                FireProjectile(target);
                yield return new WaitForSeconds(0.25f);
            }
        }

        private void FireProjectile(EnemyController target)
        {
            if (healProjectilePrefab == null) return;
            Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position;
            GameObject proj = Instantiate(healProjectilePrefab, spawnPos, Quaternion.identity);
            var mover = proj.AddComponent<HealProjectileMover>();
            mover.Setup(target, healAmount);
        }

        private void FaceTarget(Vector3 pos)
        {
            Vector3 to = pos - transform.position;
            if (to.sqrMagnitude < 0.01f) return;
            Quaternion targetRot = Quaternion.LookRotation(to.normalized);
            targetRot *= Quaternion.Euler(_rotationOffset);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, stats.turnSpeedDeg * Time.deltaTime);
        }
    }

    public class HealProjectileMover : MonoBehaviour
    {
        private EnemyController target;
        private float healAmount;
        private float speed = 12f;
        public void Setup(EnemyController t, float amount) 
        { 
            target = t; healAmount = amount; Destroy(gameObject, 5f);
        }
        void Update()
        {
            if (target == null || !target.IsAlive) { Destroy(gameObject); return; }
            Vector3 targetPos = target.transform.position + Vector3.up;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            transform.LookAt(targetPos);
            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                target.Heal(healAmount);
                Destroy(gameObject);
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 0 || other.CompareTag("Untagged")) Destroy(gameObject);
        }
    }
}
