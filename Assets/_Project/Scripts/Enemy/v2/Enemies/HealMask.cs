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
        public GameObject healProjectilePrefab; // ลากก้อนพลังงานหรือ Particle ฮีลมาใส่
        public Transform shootPoint;           // จุดปล่อยกระสุน
        public int maxTargets = 3;
        public float healRange = 15f;          // ระยะฮีลสูงสุด (ถ้าเพื่อนไกลเกินจะลอยไปหา)

        [Header("Support Positioning")]
        public float safeDistance = 12f;       // พยายามอยู่ห่างผู้เล่นระดับนี้
        public float spacingFromAllies = 3f;   // ไม่ลอยทับเพื่อน

        [Header("Orientation & Float")]
        public Vector3 _rotationOffset;
        public Vector2 _randomHeightRange = new Vector2(2.5f, 4.5f); // มักจะลอยสูงกว่าสาย Dash
        
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
        }

        protected override void Update()
        {
            if (PlayerTransform == null || !IsAlive || isSpawning) return;

            // --- Bobbing Effect ---
            if (useFloating && Agent != null)
            {
                float bobY = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmount;
                Agent.baseOffset = floatHeight + bobY;
            }

            // --- หาเพื่อนที่เลือดน้อยที่สุด ---
            List<EnemyController> alliesToHeal = FindLowestHPAllies(maxTargets);
            
            if (alliesToHeal.Count > 0)
            {
                currentLookTarget = alliesToHeal[0]; // จ้องตัวที่วิกฤตสุด
                FaceTarget(currentLookTarget.transform.position + Vector3.up);

                float distToAlly = Vector3.Distance(transform.position, currentLookTarget.transform.position);

                if (distToAlly <= healRange)
                {
                    // อยู่ในระยะฮีล -> ทำการฮีล
                    if (Time.time >= nextHealTime)
                    {
                        StartCoroutine(HealBatchRoutine(alliesToHeal));
                    }
                    // และรักษาระยะห่างจากผู้เล่นไปด้วย
                    MaintainBacklinePosition();
                }
                else
                {
                    // ไกลเกินไป -> ลอยไปหาเพื่อน
                    MoveTowardsAlly(currentLookTarget.transform.position);
                }
            }
            else
            {
                // ถ้าไม่มีใครเจ็บ ให้มองผู้เล่นระวังตัว และรักษาระยะห่าง
                FaceTarget(PlayerTransform.position + Vector3.up * 1.5f);
                MaintainBacklinePosition();
            }
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

            // ทิศทางถอยหนีจากผู้เล่น
            Vector3 retreatDir = (transform.position - PlayerTransform.position).normalized;
            retreatDir.y = 0;

            // หาจุดที่อยู่ "หลัง" เพื่อนๆ 
            // วิธีง่ายๆ: เอาตำแหน่งผู้เล่น บวกทิศทางถอยหนีไปไกลๆ
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
                yield return new WaitForSeconds(0.25f); // ยิงรัวๆ ทีละนัด
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

    // Helper Class สำหรับการวิ่งของกระสุนฮีล
    public class HealProjectileMover : MonoBehaviour
    {
        private EnemyController target;
        private float healAmount;
        private float speed = 12f;

        public void Setup(EnemyController t, float amount) 
        { 
            target = t; 
            healAmount = amount;
            Destroy(gameObject, 5f); // กันเหนียวถ้าหาเป้าไม่เจอ
        }

        void Update()
        {
            if (target == null || !target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            // วิ่งเข้าหาตัวเพื่อน (เล็งตรงกลางตัว)
            Vector3 targetPos = target.transform.position + Vector3.up;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            transform.LookAt(targetPos);

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                float oldHP = target.CurrentHealth;
                target.Heal(healAmount);
                float newHP = target.CurrentHealth;

                Debug.Log($"<color=green>[Heal]</color> {gameObject.name} healed {target.gameObject.name}: {oldHP:F0} -> {newHP:F0} HP");
                
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // ชนกำแพงหรือสิ่งกีดขวาง (Layer 0 หรือ Tag Untagged)
            if (other.gameObject.layer == 0 || other.CompareTag("Untagged"))
            {
                Destroy(gameObject);
            }
        }
    }
}
