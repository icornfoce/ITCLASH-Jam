using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// MaskAttack:
    /// - พฤติกรรม: ลอยนิ่งๆ -> ถอยหลัง -> พุ่งใส่ตำแหน่งกล้องผู้เล่นแบบล็อคเป้า -> ถอยกลับ
    /// - ระบบกลุ่ม: จำกัดจำนวนตัวที่พุ่งพร้อมกัน และกระจายตำแหน่งไม่ให้ทับกัน
    /// </summary>
    public class MaskAttack : EnemyController
    {
        [Header("Attack Settings")]
        public float attackRange = 6f;
        public float windUpDist = 1.5f;     
        public float dashSpeed = 15f;       
        public float returnSpeed = 5f;      
        public float attackCooldown = 2f;
        public float knockbackForce = 15f;  
        public float retreatDistance = 4f;  
        
        [Header("Group Settings")]
        public static int ActiveAttackers = 0;
        public int maxSimultaneousAttackers = 2; 
        public float spacingRadius = 2f;         

        [Header("Targeting (Optional)")]
        public Transform targetTransform;        


        [Header("Random Height")]
        public Vector2 _randomHeightRange = new Vector2(1.5f, 3.5f); 

        [Header("Wall Avoidance")]
        public float wallCheckDistance = 2.0f;     // ระยะ Raycast ตรวจจับกำแพง
        public float wallAvoidTurnSpeed = 360f;    // ความเร็วหันหนีกำแพง
        public LayerMask wallLayers;               // ใส่ Layer ที่เป็นกำแพงใน Inspector

        public enum MaskState { Idle, WindUp, Dashing, Returning, Cooldown }
        [Header("Internal State (Debug)")]
        public MaskState currentState = MaskState.Idle;
        
        public Vector3 startAttackPos;
        public Vector3 dashTargetPos;      
        public float nextAttackTime;
        public float timeOffset;
        public float dashStartTime;        
        public float maxDashDuration = 1.5f; 

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
            base.Update();
            if (PlayerTransform == null || !IsAlive || isSpawning) return;


            float dist = DistanceToPlayer();

            switch (currentState)
            {
                case MaskState.Idle:
                    HandleIdle(dist);
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
                    HandleCooldown(dist);
                    break;
            }
        }



        private bool HasLineOfSight()
        {
            if (PlayerTransform == null) return false;
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 target = PlayerTransform.position + Vector3.up * 1.5f;
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

        private void HandleIdle(float dist)
        {
            if (HandleWallAvoidance()) return; 
            ApplyGroupSpacing();

            if (dist <= attackRange && Time.time >= nextAttackTime && HasLineOfSight())
            {
                if (ActiveAttackers < maxSimultaneousAttackers) StartWindUp();
                else MoveTowardsPlayer(attackRange);
            }
            else MoveTowardsPlayer(attackRange - 1f);
        }

        private void StartWindUp()
        {
            currentState = MaskState.WindUp;
            ActiveAttackers++;
            startAttackPos = transform.position;
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
                Agent.velocity = Vector3.zero;
            }
            Vector3 backDir = (transform.position - PlayerTransform.position).normalized;
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
            if (targetTransform != null) dashTargetPos = targetTransform.position;
            else
            {
                dashTargetPos = PlayerTransform.position + Vector3.up * 1.5f;
                var playerCtrl = PlayerTransform.GetComponent<PlayerController>();
                if (playerCtrl != null && playerCtrl.mainCamera != null) dashTargetPos = playerCtrl.mainCamera.position;
            }
            currentState = MaskState.Dashing;
            dashStartTime = Time.time;
        }

        private void HandleWindUp() { }

        private void HandleDashing()
        {
            Vector3 nextPos = Vector3.MoveTowards(transform.position, dashTargetPos, dashSpeed * Time.deltaTime);
            WarpTo(nextPos);
            float distToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distToPlayer < 1.5f) TriggerHitPlayer();
            else if (Vector3.Distance(transform.position, dashTargetPos) < 0.5f || (Time.time - dashStartTime) > maxDashDuration)
                currentState = MaskState.Returning;
        }

        private void TriggerHitPlayer()
        {
            currentState = MaskState.Returning;
            if (PlayerHealth != null) PlayerHealth.TakeDamage(10);
            PlayerController playerCtrl = PlayerTransform.GetComponent<PlayerController>();
            if (playerCtrl != null) playerCtrl.ApplyKnockback((PlayerTransform.position - transform.position).normalized * knockbackForce);
            Vector3 escapeDir = (transform.position - PlayerTransform.position).normalized;
            if (escapeDir == Vector3.zero) escapeDir = -transform.forward;
            WarpTo(PlayerTransform.position + escapeDir * 2.0f);
        }

        private void HandleReturning()
        {
            Vector3 nextPos = Vector3.MoveTowards(transform.position, startAttackPos, returnSpeed * 2f * Time.deltaTime);
            WarpTo(nextPos);
            if (Vector3.Distance(transform.position, startAttackPos) < 0.3f || (Time.time - dashStartTime) > (maxDashDuration * 2.5f)) EndAttack();
        }

        private void EndAttack()
        {
            ActiveAttackers = Mathf.Max(0, ActiveAttackers - 1);
            currentState = MaskState.Cooldown;
            nextAttackTime = Time.time + attackCooldown;
            if (Agent != null && Agent.isOnNavMesh) Agent.isStopped = false;
        }

        private void HandleCooldown(float dist)
        {
            if (HandleWallAvoidance()) return;
            ApplyGroupSpacing();
            MoveTowardsPlayer(attackRange);
            if (Time.time >= nextAttackTime) currentState = MaskState.Idle;
        }

        private void MoveTowardsPlayer(float stopDist)
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            float dist = DistanceToPlayer();
            if (dist < retreatDistance)
            {
                Vector3 retreatDir = (transform.position - PlayerTransform.position).normalized;
                retreatDir.y = 0;
                Agent.isStopped = false;
                Agent.SetDestination(PlayerTransform.position + retreatDir * (retreatDistance + 2f));
            }
            else if (dist > stopDist)
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

            // ตรวจสอบที่ระดับความสูงของตัวศัตรู
            Vector3 origin = transform.position;
            
            // ตรวจสอบ 8 ทิศ
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

            // เช็คว่าข้างหน้าติดกำแพงไหม
            bool frontBlocked = Physics.Raycast(origin, transform.forward, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore);
            if (!frontBlocked) return false;

            // หาทิศที่โล่งที่สุด
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

            // หันไปทิศที่โล่ง
            bestDir.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(bestDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, wallAvoidTurnSpeed * Time.deltaTime);

            // หาจุด NavMesh ที่ตรงกับทิศที่โล่ง (ยิงเลเซอร์ลงพื้น)
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

        protected override void OnDisable()
        {
            base.OnDisable();
            if (currentState == MaskState.WindUp || currentState == MaskState.Dashing || currentState == MaskState.Returning)
                ActiveAttackers = Mathf.Max(0, ActiveAttackers - 1);
        }
    }
}
