using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// SummonMaskAttack:
    /// - พฤติกรรม: ลอยนิ่งๆ -> ถอยหลัง -> พุ่งใส่ศัตรูที่ใกล้ที่สุด -> ถอยกลับ
    /// - เป็นตัวช่วยของผู้เล่น (Summon)
    /// </summary>
    public class SummonMaskAttack : EnemyController
    {
        [Header("Summon Settings")]
        public bool isSummon = true;
        public float detectionRange = 25f;
        
        [Header("Attack Settings")]
        public float attackRange = 6f;
        public float windUpDist = 1.5f;     
        public float dashSpeed = 15f;       
        public float returnSpeed = 5f;      
        public float attackCooldown = 1.5f; // เร็วกว่าศัตรูเล็กน้อย
        public float knockbackForce = 15f;  
        public float retreatDistance = 4f;  
        public float damage = 10f;
        
        [Header("Group Settings")]
        public static int ActiveSummonAttackers = 0;
        public int maxSimultaneousAttackers = 3; // เสกมาช่วยรุมได้เยอะกว่า
        public float spacingRadius = 2f;         

        [Header("Targeting")]
        public Transform currentTarget;        

        [Header("Random Height")]
        public Vector2 _randomHeightRange = new Vector2(1.5f, 3.5f); 

        [Header("Wall Avoidance")]
        public float wallCheckDistance = 2.0f;     
        public float wallAvoidTurnSpeed = 360f;    
        public LayerMask wallLayers;               

        public enum MaskState { Idle, WindUp, Dashing, Returning, Cooldown }
        [Header("Internal State (Debug)")]
        public MaskState currentState = MaskState.Idle;
        
        private Vector3 startAttackPos;
        private Vector3 dashTargetPos;      
        private float nextAttackTime;
        private float timeOffset;
        private float dashStartTime;        
        private float maxDashDuration = 1.5f; 

        protected override void BuildStateMachine() { } 

        protected override void Awake()
        {
            base.Awake();
            if (visualTransform == null) visualTransform = transform;
            floatHeight = Random.Range(_randomHeightRange.x, _randomHeightRange.y);
            timeOffset = Random.Range(0f, 10f);
            
            // ปิดการหันหน้าเข้าหาผู้เล่นแบบอัตโนมัติ เพื่อให้หันไปหาศัตรูแทน
            alwaysFacePlayer = false;
        }

        protected override void Update()
        {
            // base.Update จัดการเรื่อง Floating และ Knockback พื้นฐาน
            base.Update();
            
            if (!IsAlive || isSpawning) return;

            // ระบบหาเป้าหมายศัตรู
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || (currentTarget.GetComponent<EnemyController>() != null && !currentTarget.GetComponent<EnemyController>().IsAlive))
            {
                FindTargetEnemy();
            }

            // หันหน้าหาเป้าหมาย (ศัตรู หรือ ผู้เล่นถ้าว่างงาน)
            Transform faceTarget = currentTarget != null ? currentTarget : PlayerTransform;
            if (faceTarget != null)
            {
                FaceTarget(faceTarget.position, Time.deltaTime);
            }

            float distToTarget = GetDistanceToTarget();

            switch (currentState)
            {
                case MaskState.Idle:
                    HandleIdle(distToTarget);
                    break;
                case MaskState.WindUp:
                    HandleWindUp();
                    break;
                case MaskState.Dashing:
                    HandleDashing();
                    break;
                case MaskState.Returning:
                    HandleReturning();
                    break;
                case MaskState.Cooldown:
                    HandleCooldown(distToTarget);
                    break;
            }
        }

        private void FaceTarget(Vector3 targetPos, float dt)
        {
            if (visualTransform == null) return;
            Vector3 direction = targetPos - visualTransform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction) * Quaternion.Euler(visualRotationOffset);
                float step = (stats != null ? stats.turnSpeedDeg : 360f) * dt;
                visualTransform.rotation = Quaternion.RotateTowards(visualTransform.rotation, targetRot, step);
            }
        }

        private float GetDistanceToTarget()
        {
            Transform target = currentTarget != null ? currentTarget : PlayerTransform;
            if (target == null) return float.MaxValue;
            return Vector3.Distance(transform.position, target.position);
        }

        private void FindTargetEnemy()
        {
            float closestDist = detectionRange;
            Transform closest = null;
            
            // ค้นหาจาก EnemyRegistry เพื่อหาศัตรูที่ยังไม่ตาย
            foreach (var enemy in EnemyRegistry.All)
            {
                if (enemy == null || enemy == this || !enemy.IsAlive) continue;
                
                // ไม่โจมตีพวกเดียวกันที่ถูกเสกออกมาเหมือนกัน
                if (enemy is SummonMaskAttack) continue;
                
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy.transform;
                }
            }
            currentTarget = closest;
        }

        private bool HasLineOfSight()
        {
            Transform target = currentTarget != null ? currentTarget : PlayerTransform;
            if (target == null) return false;
            
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 targetPos = target.position + Vector3.up * 1.5f;
            Vector3 dir = (targetPos - origin).normalized;
            float dist = Vector3.Distance(origin, targetPos);
            
            // Mask สำหรับ Raycast (เช็คว่าติดกำแพงไหม)
            int mask = ~LayerMask.GetMask("Projectile", "Ignore Raycast"); 
            
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            {
                // ถ้าชนเป้าหมาย หรือ ชนตัวลูกของเป้าหมาย ถือว่าเห็น
                if (hit.transform.root == target.root) return true;
                
                // ถ้าเป็น Summon แล้วชนผู้เล่น ให้ถือว่ามองทะลุไปหาศัตรูได้ (จะได้ไม่ติดตัวผู้เล่น)
                if (hit.collider.CompareTag("Player") && currentTarget != null) return true;
                
                return false; 
            }
            return true;
        }

        private void HandleIdle(float dist)
        {
            if (HandleWallAvoidance()) return; 
            ApplyGroupSpacing();

            if (currentTarget != null)
            {
                if (dist <= attackRange && Time.time >= nextAttackTime && HasLineOfSight())
                {
                    if (ActiveSummonAttackers < maxSimultaneousAttackers) StartWindUp();
                    else MoveTowardsTarget(attackRange);
                }
                else MoveTowardsTarget(attackRange - 1f);
            }
            else
            {
                // ถ้าไม่มีศัตรู ให้บินไปหาผู้เล่น (Stop ระยะ 3 เมตร)
                MoveTowardsTarget(3f);
            }
        }

        private void StartWindUp()
        {
            currentState = MaskState.WindUp;
            ActiveSummonAttackers++;
            startAttackPos = transform.position;
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
                Agent.velocity = Vector3.zero;
            }
            
            Transform target = currentTarget != null ? currentTarget : PlayerTransform;
            Vector3 backDir = (transform.position - target.position).normalized;
            Vector3 targetBack = transform.position + backDir * windUpDist;
            StartCoroutine(WindUpRoutine(targetBack));
        }

        private IEnumerator WindUpRoutine(Vector3 target)
        {
            float elapsed = 0;
            float retreatDuration = 0.6f; 
            Vector3 start = transform.position;
            while (elapsed < retreatDuration)
            {
                Vector3 nextPos = Vector3.Lerp(start, target, elapsed / retreatDuration);
                WarpTo(nextPos);
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.4f);
            
            // คำนวณจุดพุ่ง (เป้าหมายศัตรู)
            if (currentTarget != null) dashTargetPos = currentTarget.position + Vector3.up * 1.0f;
            else dashTargetPos = PlayerTransform.position + Vector3.up * 1.5f;
            
            currentState = MaskState.Dashing;
            dashStartTime = Time.time;
        }

        private void HandleWindUp() { }

        private void HandleDashing()
        {
            Vector3 nextPos = Vector3.MoveTowards(transform.position, dashTargetPos, dashSpeed * Time.deltaTime);
            WarpTo(nextPos);
            
            Transform target = currentTarget != null ? currentTarget : PlayerTransform;
            float distToTarget = Vector3.Distance(transform.position, target.position);
            
            // ถ้าชนโดนเป้าหมาย
            if (distToTarget < 1.5f) TriggerHitTarget();
            else if (Vector3.Distance(transform.position, dashTargetPos) < 0.5f || (Time.time - dashStartTime) > maxDashDuration)
                currentState = MaskState.Returning;
        }

        private void TriggerHitTarget()
        {
            currentState = MaskState.Returning;
            
            Transform target = currentTarget != null ? currentTarget : PlayerTransform;
            if (target == null) return;

            // สร้างความเสียหาย
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(damage);
                Debug.Log($"[SummonMaskAttack] โจมตี {target.name} สร้างดาเมจ {damage}!");
            }

            // แรงผลัก
            IKnockbackable knockable = target.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                knockable.ApplyKnockback((target.position - transform.position).normalized * knockbackForce);
            }

            Vector3 escapeDir = (transform.position - target.position).normalized;
            if (escapeDir == Vector3.zero) escapeDir = -transform.forward;
            WarpTo(target.position + escapeDir * 2.0f);
        }

        private void HandleReturning()
        {
            Vector3 nextPos = Vector3.MoveTowards(transform.position, startAttackPos, returnSpeed * 2f * Time.deltaTime);
            WarpTo(nextPos);
            if (Vector3.Distance(transform.position, startAttackPos) < 0.3f || (Time.time - dashStartTime) > (maxDashDuration * 2.5f)) EndAttack();
        }

        private void EndAttack()
        {
            ActiveSummonAttackers = Mathf.Max(0, ActiveSummonAttackers - 1);
            currentState = MaskState.Cooldown;
            nextAttackTime = Time.time + attackCooldown;
            if (Agent != null && Agent.isOnNavMesh) Agent.isStopped = false;
        }

        private void HandleCooldown(float dist)
        {
            if (HandleWallAvoidance()) return;
            ApplyGroupSpacing();
            MoveTowardsTarget(attackRange);
            if (Time.time >= nextAttackTime) currentState = MaskState.Idle;
        }

        private void MoveTowardsTarget(float stopDist)
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            
            Transform target = currentTarget != null ? currentTarget : PlayerTransform;
            if (target == null) return;

            float dist = Vector3.Distance(transform.position, target.position);
            
            // ถ้าใกล้เป้าหมายเกินไปและเป็นศัตรู ให้รักษาระยะ (ถ้าเป็นผู้เล่น ไม่ต้องหนี)
            if (dist < retreatDistance && currentTarget != null)
            {
                Vector3 retreatDir = (transform.position - target.position).normalized;
                retreatDir.y = 0;
                Agent.isStopped = false;
                Agent.SetDestination(target.position + retreatDir * (retreatDistance + 2f));
            }
            else if (dist > stopDist)
            {
                Agent.isStopped = false;
                Agent.SetDestination(target.position);
            }
            else
            {
                Agent.isStopped = true;
                Agent.velocity = Vector3.zero;
            }
        }

        private void WarpTo(Vector3 pos)
        {
            if (Agent != null && Agent.isActiveAndEnabled && Agent.isOnNavMesh) Agent.Warp(pos);
            else transform.position = pos;
        }

        private void ApplyGroupSpacing()
        {
            if (currentState == MaskState.Dashing) return;
            foreach (var enemy in EnemyRegistry.All)
            {
                if (enemy == this || enemy == null) continue;
                if (Vector3.Distance(transform.position, enemy.transform.position) < spacingRadius)
                {
                    Vector3 pushDir = (transform.position - enemy.transform.position).normalized;
                    pushDir.y = 0; 
                    WarpTo(transform.position + pushDir * Time.deltaTime * 2f);
                }
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
            Quaternion targetRot = Quaternion.LookRotation(bestDir);
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
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (currentState == MaskState.WindUp || currentState == MaskState.Dashing || currentState == MaskState.Returning)
                ActiveSummonAttackers = Mathf.Max(0, ActiveSummonAttackers - 1);
        }
    }
}
