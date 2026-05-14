using UnityEngine;
using System.Collections;

public class DumplingsSkill : BaseTurretSkill
{
    [Header("─── Dumplings Turret Settings ───")]
    [Tooltip("ความเสียหายที่ยิงออกไปแต่ละนัด")]
    public float damagePerShot = 10f;

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
        Debug.Log("[DumplingsSkill] OnTurretDeployed called!");
        currentHealth = maxHealth;
        originalScale = transform.localScale;

        // ตั้งป้อมให้ตรง — เอาเฉพาะแกน Y (หันซ้ายขวา) ลบ X, Z (เอียง) ออก
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        // ปิด Rigidbody ระหว่าง spawn animation
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        // isActive ยังคงเป็น true (จาก base class) → ป้อมยิงได้ทันที
        // animation เป็นแค่ visual effect ไม่บล็อกการทำงาน
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
            // ลองหาจาก Tag ก่อน
            bool isEnemy = c.CompareTag("Enemy");

            // ถ้า Tag ไม่ตรง ลองหาจาก EnemyController component
            if (!isEnemy)
            {
                isEnemy = c.GetComponentInParent<ITCLASH.Enemies.EnemyController>() != null;
            }

            if (isEnemy)
            {
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < bestDist) { bestDist = d; best = c.transform; }
            }
        }

        currentTarget = best;
    }

    private void AimAtTarget()
    {
        if (currentTarget == null) return;
        Transform pivot = aimPivot != null ? aimPivot : transform;
        
        // เล็งไปที่กึ่งกลางตัวศัตรู (บวก Vector3.up)
        Vector3 targetPos = currentTarget.position + Vector3.up;
        Vector3 dir = targetPos - pivot.position;
        
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        pivot.rotation = Quaternion.RotateTowards(pivot.rotation, targetRot, aimRotationSpeed * Time.deltaTime);
    }

    protected override void PerformTurretAction()
    {
        if (currentTarget == null)
        {
            Debug.Log("[DumplingsSkill] ไม่มีเป้าหมาย ข้ามรอบนี้");
            return;
        }

        Debug.Log($"[DumplingsSkill] ยิงใส่ {currentTarget.name} ทำดาเมจ {damagePerShot}");

        if (bulletPrefab != null && shootPoint != null)
        {
            // คำนวณทิศทางจาก shootPoint ไปหาศัตรู
            Vector3 direction = (currentTarget.position + Vector3.up - shootPoint.position).normalized;

            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.LookRotation(direction));

            // ถ้ากระสุนไม่มี Rigidbody → เพิ่มให้อัตโนมัติ
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb == null)
            {
                bulletRb = bullet.AddComponent<Rigidbody>();
                Debug.Log("[DumplingsSkill] กระสุนไม่มี Rigidbody → เพิ่มให้อัตโนมัติ");
            }

            bulletRb.useGravity = false;
            bulletRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bulletRb.linearVelocity = direction * bulletSpeed;

            // ทำลายกระสุนหลังหมดเวลา
            Destroy(bullet, bulletLifetime);

            Debug.Log($"[DumplingsSkill] กระสุนเกิดที่ {shootPoint.position}, ทิศทาง {direction}, ความเร็ว {bulletSpeed}");
        }
        else
        {
            // Hitscan — ไม่มีกระสุน ยิงตรงเลย
            Debug.Log($"[DumplingsSkill] Hitscan mode (bulletPrefab={bulletPrefab}, shootPoint={shootPoint})");
            ITCLASH.Enemies.EnemyController enemy = currentTarget.GetComponentInParent<ITCLASH.Enemies.EnemyController>();
            if (enemy != null) enemy.ApplyDamage(damagePerShot);
        }
    }

    // ─── Health ───

    public void TakeDamage(float damage)
    {
        if (isDying || maxHealth <= 0f) return;
        currentHealth -= damage;
        Debug.Log($"[DumplingsSkill] โดนตี! เหลือเลือด {currentHealth}/{maxHealth}");
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

    // ─── Animations ───

    private IEnumerator SpawnAnimation()
    {
        Debug.Log("[DumplingsSkill] SpawnAnimation started!");
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        // จำ rotation เดิมไว้ → คืนค่าหลัง animation จบ
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

        // คืนค่า scale และ rotation กลับเป็นเดิม
        transform.localScale = originalScale;
        transform.rotation = originalRotation;

        // เปิด Rigidbody หลัง animation จบ
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }

        Debug.Log("[DumplingsSkill] SpawnAnimation finished! ป้อมพร้อมใช้งาน!");
    }

    private IEnumerator DeathAnimation()
    {
        Debug.Log("[DumplingsSkill] DeathAnimation started!");
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

        Debug.Log("[DumplingsSkill] ป้อมถูกทำลาย!");
        Destroy(gameObject);
    }
}
