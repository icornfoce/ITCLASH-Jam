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
        public float windUpDist = 1.5f;     // ระยะที่ถอยหลังก่อนพุ่ง
        public float dashSpeed = 15f;       // ความเร็วตอนพุ่ง
        public float returnSpeed = 5f;      // ความเร็วตอนถอยกลับ
        public float attackCooldown = 2f;
        public float knockbackForce = 15f;  // แรงกระเด็นที่ใส่ให้ผู้เล่น
        public float retreatDistance = 4f;  // ถอยหนีถ้าผู้เล่นเข้าใกล้เกินระยะนี้
        
        [Header("Group Settings")]
        public static int ActiveAttackers = 0;
        public int maxSimultaneousAttackers = 2; // ห้ามโจมตีพร้อมกันเกินกี่ตัว
        public float spacingRadius = 2f;         // ระยะห่างระหว่างเพื่อนร่วมกลุ่ม

        [Header("Targeting (Optional)")]
        public Transform targetTransform;        // ลาก "กล้อง" หรือ "หัวผู้เล่น" มาใส่ตรงนี้ได้เลย (ถ้าว่างไว้มันจะหาเอง)

        [Header("Orientation Settings")]
        public Vector3 _rotationOffset = new Vector3(0, 0, 0); // เผื่อโมเดลหันหน้าผิดทาง

        [Header("Random Height")]
        public Vector2 _randomHeightRange = new Vector2(1.5f, 3.5f); // ช่วงความสูงที่สุ่มได้

        public enum MaskState { Idle, WindUp, Dashing, Returning, Cooldown }
        [Header("Internal State (Debug)")]
        public MaskState currentState = MaskState.Idle;
        
        public Vector3 startAttackPos;
        public Vector3 dashTargetPos;      // ตำแหน่งที่ล็อคไว้พุ่งใส่
        public float nextAttackTime;
        public float timeOffset;
        public float dashStartTime;        // เวลาที่เริ่มพุ่ง (Failsafe)
        public float maxDashDuration = 1.5f; // เวลาพุ่งสูงสุด (ถ้าเกินนี้ให้ถอยกลับเลย)

        protected override void BuildStateMachine() { } // ไม่ใช้ StateMachine มาตรฐาน

        protected override void Awake()
        {
            base.Awake();
            if (visualTransform == null) visualTransform = transform;

            // สุ่มความสูงเฉพาะตัวของหน้ากากตัวนี้
            floatHeight = Random.Range(_randomHeightRange.x, _randomHeightRange.y);
            timeOffset = Random.Range(0f, 10f);
        }

        protected override void Update()
        {
            if (PlayerTransform == null || !IsAlive || isSpawning) return;

            // --- Bobbing Effect (จัดการผ่าน baseOffset เพื่อความสมูท) ---
            if (useFloating && Agent != null)
            {
                float bobY = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmount;
                Agent.baseOffset = floatHeight + bobY;
            }

            // --- AI Logic ---
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

            // หันหน้าหาเป้าหมาย (รวมก้มเงย)
            CustomFacePlayer(Time.deltaTime);
        }

        private void CustomFacePlayer(float dt)
        {
            if (PlayerTransform == null || stats == null) return;

            // เป้าหมายการมอง:
            // - ถ้ากำลังพุ่ง (Dashing): มองที่จุดที่ล็อคไว้ (พุ่งไปไหนมองไปนั่น)
            // - ถ้ากำลังเตรียม (WindUp) หรือปกติ: มองจิกไปที่ตัวผู้เล่น (เตรียมเล็ง)
            Vector3 lookTarget = (currentState == MaskState.Dashing) 
                ? dashTargetPos 
                : PlayerTransform.position + Vector3.up * 1.5f;

            Vector3 to = lookTarget - transform.position;
            if (to.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(to.normalized);
            targetRot *= Quaternion.Euler(_rotationOffset);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, stats.turnSpeedDeg * dt);
        }

        private bool HasLineOfSight()
        {
            if (PlayerTransform == null) return false;
            
            // ยกจุดยิงให้สูงขึ้นเล็กน้อย (ประมาณระดับตา)
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 target = PlayerTransform.position + Vector3.up * 1.5f;
            Vector3 dir = (target - origin).normalized;
            float dist = Vector3.Distance(origin, target);

            // สร้าง LayerMask เพื่อ "ข้าม" พวกเดียวกันเอง (Enemy) และพวกกระสุน (Projectile)
            // เราสนใจแค่ว่ามันชน "กำแพง" (Default) หรือ "ผู้เล่น" (Player) หรือไม่
            int mask = ~LayerMask.GetMask("Enemy", "Projectile", "Ignore Raycast");

            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, mask))
            {
                if (hit.collider.CompareTag("Player")) return true;
                return false; // ติดกำแพงหรือสิ่งกีดขวางอื่นๆ
            }
            return true;
        }

        private void HandleIdle(float dist)
        {
            ApplyGroupSpacing();

            // ต้องอยู่ในระยะ และ มี Line of Sight (ไม่มีกำแพงกั้น) ถึงจะเริ่มโจมตี
            if (dist <= attackRange && Time.time >= nextAttackTime && HasLineOfSight())
            {
                if (ActiveAttackers < maxSimultaneousAttackers)
                {
                    StartWindUp();
                }
                else
                {
                    MoveTowardsPlayer(attackRange);
                }
            }
            else
            {
                // ถ้าติดกำแพง หรือระยะไม่ถึง ให้เดินอ้อม/เดินเข้าไปหา
                MoveTowardsPlayer(attackRange - 1f);
            }
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
            float retreatDuration = 0.6f; // ระยะเวลาค่อยๆ ถอยหลัง
            Vector3 start = transform.position;

            // 1. ค่อยๆ ถอยหลังเตรียมตัว (Wind Up)
            while (elapsed < retreatDuration)
            {
                Vector3 nextPos = Vector3.Lerp(start, target, elapsed / retreatDuration);
                WarpTo(nextPos);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 2. หยุดนิ่งเพื่อเล็ง (Aiming Phase)
            // ในช่วงนี้ CustomFacePlayer ใน Update() จะค่อยๆ หันหน้าไปหาผู้เล่นตาม turnSpeed
            float aimDuration = 0.4f; // ปรับเวลาเล็งตรงนี้ได้
            yield return new WaitForSeconds(aimDuration);

            // 3. ล็อคเป้าหมาย (Lock On) ณ วินาทีสุดท้ายก่อนพุ่ง
            if (targetTransform != null)
            {
                dashTargetPos = targetTransform.position;
            }
            else
            {
                // ถ้าไม่มีการลากเป้าใส่ไว้ ให้หาจากกล้องผู้เล่นอัตโนมัติ
                dashTargetPos = PlayerTransform.position + Vector3.up * 1.5f;
                var playerCtrl = PlayerTransform.GetComponent<PlayerController>();
                if (playerCtrl != null && playerCtrl.mainCamera != null)
                {
                    dashTargetPos = playerCtrl.mainCamera.position;
                }
            }

            // 4. เริ่มพุ่ง!
            currentState = MaskState.Dashing;
            dashStartTime = Time.time;
        }

        private void HandleWindUp() { }

        private void HandleDashing()
        {
            Vector3 nextPos = Vector3.MoveTowards(transform.position, dashTargetPos, dashSpeed * Time.deltaTime);
            WarpTo(nextPos);

            float distToTarget = Vector3.Distance(transform.position, dashTargetPos);
            float distToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            
            // 1. ชนผู้เล่น
            if (distToPlayer < 1.5f)
            {
                TriggerHitPlayer();
            }
            // 2. ถึงจุดหมาย หรือ พุ่งนานเกินไป (Failsafe)
            else if (distToTarget < 0.5f || (Time.time - dashStartTime) > maxDashDuration)
            {
                currentState = MaskState.Returning;
            }
        }

        private void TriggerHitPlayer()
        {
            currentState = MaskState.Returning;

            if (PlayerHealth != null) PlayerHealth.TakeDamage(10);

            PlayerController playerCtrl = PlayerTransform.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                Vector3 toPlayer = (PlayerTransform.position - transform.position).normalized;
                playerCtrl.ApplyKnockback(toPlayer * knockbackForce);
            }

            // ดีดตัวออกมาเล็กน้อยเพื่อให้เห็นว่าชนแล้ว
            Vector3 escapeDir = (transform.position - PlayerTransform.position).normalized;
            if (escapeDir == Vector3.zero) escapeDir = -transform.forward;
            
            Vector3 escapePos = PlayerTransform.position + escapeDir * 2.0f; 
            WarpTo(escapePos);
        }

        private void HandleReturning()
        {
            float actualReturnSpeed = returnSpeed * 2f;
            Vector3 nextPos = Vector3.MoveTowards(transform.position, startAttackPos, actualReturnSpeed * Time.deltaTime);
            WarpTo(nextPos);

            // ถึงจุดเริ่มต้น หรือ นานเกินไป (Failsafe)
            if (Vector3.Distance(transform.position, startAttackPos) < 0.3f || (Time.time - dashStartTime) > (maxDashDuration * 2.5f))
            {
                EndAttack();
            }
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
            ApplyGroupSpacing();
            MoveTowardsPlayer(attackRange);
            if (Time.time >= nextAttackTime) currentState = MaskState.Idle;
        }

        private void MoveTowardsPlayer(float stopDist)
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            
            float dist = DistanceToPlayer();

            // --- ถอยหนีถ้าใกล้เกินไป ---
            if (dist < retreatDistance)
            {
                Vector3 retreatDir = (transform.position - PlayerTransform.position).normalized;
                retreatDir.y = 0;
                Vector3 retreatPos = PlayerTransform.position + retreatDir * (retreatDistance + 2f);
                
                Agent.isStopped = false;
                Agent.SetDestination(retreatPos);
            }
            // --- เดินเข้าหาถ้าไกลเกินไป ---
            else if (dist > stopDist)
            {
                Agent.isStopped = false;
                Agent.SetDestination(PlayerTransform.position);
            }
            // --- อยู่นิ่งๆ ในระยะพอดี ---
            else
            {
                Agent.isStopped = true;
                Agent.velocity = Vector3.zero;
            }
        }

        private void WarpTo(Vector3 pos)
        {
            if (Agent != null && Agent.isActiveAndEnabled && Agent.isOnNavMesh)
            {
                Agent.Warp(pos);
            }
            else
            {
                transform.position = pos;
            }
        }

        private void ApplyGroupSpacing()
        {
            if (currentState == MaskState.Dashing) return;

            foreach (var enemy in EnemyRegistry.All)
            {
                if (enemy == this || enemy == null) continue;
                float d = Vector3.Distance(transform.position, enemy.transform.position);
                if (d < spacingRadius)
                {
                    Vector3 pushDir = (transform.position - enemy.transform.position).normalized;
                    pushDir.y = 0; // ผลักกันเฉพาะแนวราบ ไม่ให้กดกันจมดิน
                    Vector3 nextPos = transform.position + pushDir * Time.deltaTime * 2f;
                    WarpTo(nextPos);
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (currentState == MaskState.WindUp || currentState == MaskState.Dashing || currentState == MaskState.Returning)
            {
                ActiveAttackers = Mathf.Max(0, ActiveAttackers - 1);
            }
        }
    }
}
