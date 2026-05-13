using UnityEngine;
using System.Collections;
using ITCLASH.Enemies;

public class FloorSkill : BaseAoESkill
{
    [Header("─── Floor Burst Settings ───")]
    [Tooltip("ความเสียหายที่ทำกับศัตรู")]
    public float damage = 40f;

    [Tooltip("แรงกระเด็นขึ้นฟ้า")]
    public float knockback = 20f;

    [Header("─── Burst Animation ───")]
    [Tooltip("ระยะที่พื้นจมอยู่ใต้ดินก่อนพุ่งขึ้น")]
    public float burstDepth = 3f;

    [Tooltip("ความสูงที่พื้นพุ่งเกินจุดเป้าหมาย (overshoot)")]
    public float burstOvershoot = 0.8f;

    [Tooltip("ความเร็วในการพุ่งขึ้น (วินาที)")]
    public float burstUpDuration = 0.25f;

    [Tooltip("ความเร็วในการตกลงมาจาก overshoot (วินาที)")]
    public float settleDownDuration = 0.15f;

    [Tooltip("เวลาที่พื้นค้างอยู่ก่อนจมหาย (วินาที)")]
    public float lingerDuration = 1.5f;

    [Tooltip("ความเร็วในการจมลงหายไป (วินาที)")]
    public float sinkDuration = 0.6f;

    [Header("─── Screen Shake ───")]
    [Tooltip("ความแรงสั่นกล้อง (0 = ปิด)")]
    public float shakeIntensity = 0.15f;

    [Tooltip("ระยะเวลาสั่นกล้อง (วินาที)")]
    public float shakeDuration = 0.2f;

    private Vector3 targetGroundPos;
    private bool hasTriggeredDamage = false;

    public override void Activate(Transform playerTransform)
    {
        // ─── หาตำแหน่งที่คลิก ───
        Vector3? aimPoint = TargetPosition;

        if (!aimPoint.HasValue)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    aimPoint = hit.point;
                }
            }
        }

        // ตั้งตำแหน่งเป้าหมาย
        targetGroundPos = aimPoint.HasValue ? aimPoint.Value : transform.position;

        // ─── เริ่มต้นจากใต้ดิน ───
        transform.position = targetGroundPos + Vector3.down * burstDepth;

        // เรียก PlayVoice + SetParent(null) จาก base
        PlayVoice(targetGroundPos);
        transform.SetParent(null);

        // ปิด Rigidbody ระหว่าง animation (เราจะขยับเอง)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // เริ่มอนิเมชันพุ่งขึ้น
        StartCoroutine(BurstSequence());
    }

    private IEnumerator BurstSequence()
    {
        Vector3 startPos = transform.position; // ใต้ดิน
        Vector3 peakPos = targetGroundPos + Vector3.up * burstOvershoot; // จุดสูงสุด
        Vector3 finalPos = targetGroundPos; // จุดพักบนพื้น

        // ─── Phase 1: พุ่งขึ้นจากใต้ดิน → จุดสูงสุด ───
        float elapsed = 0f;
        while (elapsed < burstUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / burstUpDuration);
            // Ease-out (เริ่มเร็ว ค่อยๆ ช้าลง)
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(startPos, peakPos, eased);
            yield return null;
        }
        transform.position = peakPos;

        // ─── ทำดาเมจ + Knockback ตอนพุ่งถึงจุดสูงสุด ───
        TriggerDamageAndKnockback();

        // สั่นกล้อง
        if (shakeIntensity > 0f) StartCoroutine(CameraShake());

        // เล่น VFX + SFX จาก BaseAoESkill
        if (explosionVFX != null) Instantiate(explosionVFX, targetGroundPos, Quaternion.identity);
        if (explosionSFX != null) AudioSource.PlayClipAtPoint(explosionSFX, targetGroundPos);

        // ─── Phase 2: ตกลงมาจาก overshoot → ตำแหน่งพื้น ───
        elapsed = 0f;
        while (elapsed < settleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDownDuration);
            // Ease-in bounce
            float eased = t * t;
            transform.position = Vector3.Lerp(peakPos, finalPos, eased);
            yield return null;
        }
        transform.position = finalPos;

        // ─── Phase 3: ค้างอยู่บนพื้น ───
        yield return new WaitForSeconds(lingerDuration);

        // ─── Phase 4: จมลงใต้ดินแล้วทำลายตัวเอง ───
        Vector3 sinkTarget = finalPos + Vector3.down * burstDepth;
        elapsed = 0f;
        Vector3 sinkStart = transform.position;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sinkDuration);
            // Ease-in (ค่อยๆ เร็วขึ้น)
            float eased = t * t;
            transform.position = Vector3.Lerp(sinkStart, sinkTarget, eased);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void TriggerDamageAndKnockback()
    {
        if (hasTriggeredDamage) return;
        hasTriggeredDamage = true;

        Collider[] hitEnemies = Physics.OverlapSphere(targetGroundPos, radius, enemyLayer);
        foreach (Collider hit in hitEnemies)
        {
            ApplyAoEEffect(hit.gameObject);
        }

        Debug.Log($"[FloorSkill] พื้นพุ่งขึ้น! โดนศัตรู {hitEnemies.Length} ตัว!");
    }

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            // ─── ทำดาเมจ ───
            enemy.ApplyDamage(damage);

            // ─── Knockback ขึ้นฟ้า + ผลักออกจากจุดศูนย์กลาง ───
            IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                // คำนวณทิศทางผลักออกจากจุดกลาง + ดันขึ้นฟ้า
                Vector3 enemyPos = enemy.transform.position;
                Vector3 pushDir = (enemyPos - targetGroundPos).normalized;
                pushDir.y = 0f;

                // ถ้าศัตรูอยู่ตรงจุดศูนย์กลางพอดี → ผลักขึ้นตรงๆ
                if (pushDir.sqrMagnitude < 0.01f)
                    pushDir = Vector3.forward;

                Vector3 knockbackForce = (pushDir * knockback * 0.5f) + (Vector3.up * knockback);
                knockable.ApplyKnockback(knockbackForce, 0.5f);
            }

            Debug.Log($"[FloorSkill] พื้นระเบิดอัด {enemyObj.name} กระเด็นลอยขึ้นฟ้า!");
        }
    }

    private IEnumerator CameraShake()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = shakeIntensity * (1f - elapsed / shakeDuration);
            cam.transform.localPosition = originalPos + Random.insideUnitSphere * strength;
            yield return null;
        }

        cam.transform.localPosition = originalPos;
    }
}
