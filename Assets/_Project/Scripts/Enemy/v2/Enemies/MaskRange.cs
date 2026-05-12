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
        public Transform targetTransform;   // ลากเป้าหมาย (เช่น หัวผู้เล่น) มาใส่ตรงนี้เพื่อให้กระสุนพุ่งไปหาจุดนี้เสมอ
        public float damage = 10f;
        public float attackCooldown = 3f;
        public float projectileSpeed = 15f;
        public float attackRange = 12f;     // ระยะยิงสูงสุด (ถ้าไกลกว่านี้จะลอยเข้าไปหา)

        [Header("Accuracy Settings (%)")]
        [Range(0, 100)] public float missChance = 20f;   // โอกาสยิงเฉียดๆ (ข้างตัว)
        [Range(0, 100)] public float randomChance = 10f; // โอกาสยิงมั่วไปทางอื่นเลย

        [Header("Positioning")]
        public float midRangeDistance = 9f; // ระยะที่พยายามรักษากับผู้เล่น (อยู่กลางระหว่าง 6 และ 12)
        public float retreatDistance = 5f;  // ถอยหนีถ้าผู้เล่นใกล้เกินไป

        [Header("Orientation & Float")]
        public Vector3 _rotationOffset;
        public Vector2 _randomHeightRange = new Vector2(2f, 4f);
        
        private float nextAttackTime;
        private float timeOffset;

        protected override void BuildStateMachine() { }

        protected override void Awake()
        {
            base.Awake();
            if (visualTransform == null) visualTransform = transform;
            
            // สุ่มความสูงเฉพาะตัว
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

            // --- หันหน้าหาผู้เล่น ---
            FaceTarget(PlayerTransform.position + Vector3.up * 1.5f);

            // --- ระบบโจมตี ---
            float dist = DistanceToPlayer();
            if (dist <= attackRange && Time.time >= nextAttackTime && HasLineOfSight())
            {
                FireProjectile();
            }

            // --- ระบบรักษาตำแหน่ง (Mid-line) ---
            MaintainPosition(dist);
        }

        private void MaintainPosition(float currentDist)
        {
            if (Agent == null || !Agent.isOnNavMesh) return;

            // --- 1. ถอยหนีถ้าใกล้เกินไป ---
            if (currentDist < retreatDistance)
            {
                Vector3 retreatDir = (transform.position - PlayerTransform.position).normalized;
                retreatDir.y = 0;
                Vector3 retreatPos = PlayerTransform.position + retreatDir * (retreatDistance + 2f);
                
                Agent.isStopped = false;
                Agent.SetDestination(retreatPos);
            }
            // --- 2. ลอยเข้าไปหาถ้าไกลเกินระยะโจมตี หรือยังไม่ถึงจุดคุมเชิง ---
            else if (currentDist > attackRange || currentDist > midRangeDistance + 1f)
            {
                Agent.isStopped = false;
                Agent.SetDestination(PlayerTransform.position);
            }
            // --- 3. หยุดเมื่ออยู่ในระยะที่เหมาะสม ---
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
            
            // หาจุดหมายดั้งเดิม (ถ้าไม่ได้ลาก targetTransform ไว้ จะเล็งไปที่หน้าอกผู้เล่น)
            Vector3 targetPos;
            if (targetTransform != null)
            {
                targetPos = targetTransform.position;
            }
            else
            {
                targetPos = PlayerTransform.position + Vector3.up * 1.2f;
            }

            Vector3 fireDir = (targetPos - spawnPos).normalized;

            // --- ระบบคำนวณความแม่นยำ ---
            float roll = Random.Range(0f, 100f);

            if (roll < randomChance)
            {
                // 1. ยิงมั่ว (สุ่มทิศทางกว้างๆ)
                fireDir = Quaternion.Euler(Random.Range(-30, 30), Random.Range(-45, 45), 0) * fireDir;
            }
            else if (roll < (randomChance + missChance))
            {
                // 2. ยิงพลาด (เฉียดข้างๆ ตัว)
                float sideOffset = Random.value > 0.5f ? 2.5f : -2.5f; // พลาดซ้ายหรือขวา
                Vector3 sideDir = Vector3.Cross(fireDir, Vector3.up).normalized;
                targetPos += sideDir * sideOffset;
                fireDir = (targetPos - spawnPos).normalized;
            }
            // 3. ยิงแม่น (ถ้าไม่เข้าเงื่อนไขบน จะใช้ fireDir เดิมที่เล็งผู้เล่นไว้)

            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(fireDir));
            
            // ใส่สคริปต์ควบคุมกระสุน
            var mover = proj.AddComponent<MaskProjectileMover>();
            mover.Setup(damage, projectileSpeed);
        }

        private void FaceTarget(Vector3 pos)
        {
            Vector3 to = pos - transform.position;
            if (to.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(to.normalized);
            targetRot *= Quaternion.Euler(_rotationOffset);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, stats.turnSpeedDeg * Time.deltaTime);
        }

        private bool HasLineOfSight()
        {
            if (PlayerTransform == null) return false;
            
            // ยกจุดยิงให้สูงขึ้นเล็กน้อย
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 target = PlayerTransform.position + Vector3.up * 1.5f;
            Vector3 dir = (target - origin).normalized;
            float dist = Vector3.Distance(origin, target);

            // ข้าม Layer พวกเดียวกันเอง
            int mask = ~LayerMask.GetMask("Enemy", "Projectile", "Ignore Raycast");

            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, mask))
            {
                if (hit.collider.CompareTag("Player")) return true;
                return false; 
            }
            return true;
        }
    }

    // Helper Class สำหรับกระสุนโจมตี
    public class MaskProjectileMover : MonoBehaviour
    {
        private float damage;
        private float speed;

        public void Setup(float dmg, float spd) 
        { 
            damage = dmg;
            speed = spd;
            Destroy(gameObject, 4f); // ทำลายทิ้งหลัง 4 วินาทีถ้าไม่ชนอะไร
        }

        void Update()
        {
            // พุ่งไปข้างหน้าตรงๆ ตามทิศทางที่ถูกยิงออกมา
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // ชนผู้เล่น
            if (other.CompareTag("Player"))
            {
                var hp = other.GetComponent<PlayerHealth>();
                if (hp != null) hp.TakeDamage((int)damage);
                
                Destroy(gameObject);
            }
            // ชนกำแพงหรือสิ่งกีดขวาง
            else if (other.gameObject.layer == 0 || other.CompareTag("Untagged")) 
            {
                Destroy(gameObject);
            }
        }
    }
}
