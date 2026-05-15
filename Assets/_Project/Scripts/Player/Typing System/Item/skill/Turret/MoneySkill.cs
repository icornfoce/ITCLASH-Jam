using UnityEngine;
using System.Collections;
using ITCLASH.Enemies;

public class MoneySkill : BaseTurretSkill
{
    [Header("─── Money Turret Settings ───")]
    [Tooltip("ความเสียหายที่ยิงออกไปแต่ละนัด")]
    public float damagePerShot = 10f;
    [Tooltip("EXP หรือเงินที่ได้จากการยิงโดน")]
    public float expPerHit = 5f;

    [Tooltip("พรีแฟบกระสุนที่จะยิงออกไป (ถ้าไม่ใส่จะยิงเข้าเป้าทันที)")]
    public GameObject bulletPrefab;

    [Tooltip("จุดที่จะให้กระสุนพุ่งออกไป (ปลายกระบอกปืน)")]
    public Transform shootPoint;

    [Tooltip("ความเร็วกระสุน (m/s)")]
    public float bulletSpeed = 30f;

    [Tooltip("ทำลายกระสุนหลังจากกี่วินาที")]
    public float bulletLifetime = 5f;

    [Header("─── Aiming / Pivot ───")]
    [Tooltip("จุดหมุนของป้อมที่จะหมุนเล็งไปหาศัตรู (ถ้าไม่ใส่จะใช้ตัวเองเป็นจุดหมุน)")]
    public Transform aimPivot;

    [Tooltip("ความเร็วในการหมุนเล็ง (องศา/วินาที)")]
    public float aimRotationSpeed = 360f;

    [Header("─── Health ───")]
    [Tooltip("เลือดของป้อม (0 = อมตะ ไม่มีวันพัง)")]
    public float maxHealth = 50f;

    [Header("─── Spawn / Death Animation ───")]
    [Tooltip("ระยะเวลา Animation ตอนเกิด (วินาที)")]
    public float spawnDuration = 0.6f;

    [Tooltip("ระยะเวลา Animation ตอนตาย (วินาที)")]
    public float deathDuration = 0.5f;

    [Tooltip("ความเร็วหมุนรอบตัวเอง ตอนเกิด/ตาย (องศา/วินาที)")]
    public float spinSpeed = 720f;

    private float currentHealth;
    private Transform currentTarget;
    private bool isDying = false;
    private Vector3 originalScale;

    protected override void OnTurretDeployed(Transform playerTransform)
    {
        Debug.Log("[MoneySkill] OnTurretDeployed called!");
        currentHealth = maxHealth;
        originalScale = transform.localScale;

        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        StartCoroutine(SpawnAnimation());
    }

    protected override void Update()
    {
        if (isDying) return;
        FindNearestEnemy();
        AimAtTarget();
        base.Update();
    }

    protected override void OnDurationExpired()
    {
        Die();
    }

    private void FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, targetingRange);
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (Collider c in hits)
        {
            bool isEnemy = c.CompareTag("Enemy");

            if (!isEnemy)
            {
                isEnemy = c.GetComponentInParent<EnemyController>() != null;
            }

            if (isEnemy)
            {
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < bestDist) { bestDist = d; best = c.transform; }
            }
        }

        currentTarget = best;
    }

    private Vector3 GetTargetCenter(Transform target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center;
        return target.position + Vector3.up;
    }

    private void AimAtTarget()
    {
        if (currentTarget == null) return;
        Transform pivot = aimPivot != null ? aimPivot : transform;
        
        // เล็งไปที่จุดศูนย์กลางของศัตรู
        Vector3 targetPos = GetTargetCenter(currentTarget);
        Vector3 dir = targetPos - pivot.position;
        
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        pivot.rotation = Quaternion.RotateTowards(pivot.rotation, targetRot, aimRotationSpeed * Time.deltaTime);
    }

    protected override void PerformTurretAction()
    {
        if (currentTarget == null)
        {
            Debug.Log("[MoneySkill] ไม่มีเป้าหมาย ข้ามรอบนี้");
            return;
        }

        Debug.Log($"[MoneySkill] ยิงใส่ {currentTarget.name} ทำดาเมจ {damagePerShot}");

        if (bulletPrefab != null && shootPoint != null)
        {
            Vector3 targetPos = GetTargetCenter(currentTarget);
            Vector3 direction = (targetPos - shootPoint.position).normalized;

            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.LookRotation(direction));

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb == null)
            {
                bulletRb = bullet.AddComponent<Rigidbody>();
            }

            bulletRb.useGravity = false;
            bulletRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bulletRb.linearVelocity = direction * bulletSpeed;

            // ตรวจสอบและติด Script ทำดาเมจให้กระสุน
            MoneyBullet dmgScript = bullet.GetComponent<MoneyBullet>();
            if (dmgScript == null) dmgScript = bullet.AddComponent<MoneyBullet>();
            dmgScript.damage = damagePerShot;
            dmgScript.exp = expPerHit; // ส่งค่า EXP ไปให้กระสุนด้วย

            // ตรวจสอบว่ามี Collider ไหม ถ้าไม่มีเพิ่มให้
            if (bullet.GetComponent<Collider>() == null)
            {
                SphereCollider sc = bullet.AddComponent<SphereCollider>();
                sc.radius = 0.3f;
                sc.isTrigger = true;
            }

            Destroy(bullet, bulletLifetime);

            Debug.Log($"[MoneySkill] ยิงกระสุนออกไป!");
        }
        else
        {
            Debug.Log($"[MoneySkill] Hitscan mode");
            EnemyController enemy = currentTarget.GetComponentInParent<EnemyController>();
            if (enemy != null) 
            {
                enemy.ApplyDamage(damagePerShot);
                Debug.Log($"[MoneySkill] ยิงศัตรู {currentTarget.name} ได้รับ EXP {expPerHit}!");
                // PlayerLevel.Instance.AddExp(expPerHit);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDying || maxHealth <= 0f) return;
        currentHealth -= damage;
        Debug.Log($"[MoneySkill] โดนตี! เหลือเลือด {currentHealth}/{maxHealth}");
        if (currentHealth <= 0f) Die();
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0f) return 1f;
        return Mathf.Clamp01(currentHealth / maxHealth);
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;
        isActive = false;
        CancelDurationTimer();
        StartCoroutine(DeathAnimation());
    }

    private IEnumerator SpawnAnimation()
    {
        Debug.Log("[MoneySkill] SpawnAnimation started!");
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        Quaternion originalRotation = transform.rotation;

        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spawnDuration);
            float eased = 1f - (1f - t) * (1f - t);
            transform.localScale = originalScale * eased;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        transform.localScale = originalScale;
        transform.rotation = originalRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }

        Debug.Log("[MoneySkill] SpawnAnimation finished! ป้อมพร้อมใช้งาน!");
    }

    private IEnumerator DeathAnimation()
    {
        Debug.Log("[MoneySkill] DeathAnimation started!");
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < deathDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathDuration);
            float eased = t * t;
            transform.localScale = startScale * (1f - eased);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        Debug.Log("[MoneySkill] ป้อมถูกทำลาย!");
        Destroy(gameObject);
    }
}

// ─────────────────────────────────────────────────────────────
// คลาสสำหรับติดไว้ที่ตัวกระสุนเพื่อให้ทำดาเมจและให้ EXP เมื่อชน
// ─────────────────────────────────────────────────────────────
public class MoneyBullet : MonoBehaviour
{
    public float damage;
    public float exp;
    private bool hasHit = false;

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject hitObj)
    {
        if (hasHit) return;

        // ข้ามการชนกับป้อมตัวเอง หรือกระสุนด้วยกัน หรือผู้เล่น
        if (hitObj.CompareTag("Player") || hitObj.CompareTag("Bullet") || hitObj.name.Contains("Money")) return;

        var enemy = hitObj.GetComponentInParent<ITCLASH.Enemies.EnemyController>();
        if (enemy != null)
        {
            hasHit = true;
            enemy.ApplyDamage(damage);
            
            Debug.Log($"[MoneyBullet] ยิงโดนศัตรู! ทำดาเมจ {damage} และได้รับ EXP {exp}!");
            // PlayerLevel.Instance.AddExp(exp); // เปิดใช้งานได้ถ้ามีระบบ PlayerLevel
            
            Destroy(gameObject);
        }
        else 
        {
            // ชนกำแพงหรือฉาก
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
