using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public abstract class BaseAoESkill : BaseItemSkill
{
    [Header("─── AoE Settings ───")]
    [Tooltip("รัศมีวงกว้างของสกิล")]
    public float radius = 5f;
    [Tooltip("ดีเลย์ก่อนที่จะระเบิด / ทำงาน (วินาที)")]
    public float delayBeforeExplode = 0.5f;
    [Tooltip("Layer ของศัตรูที่ต้องการให้โดนผลกระทบ")]
    public LayerMask enemyLayer;

    [Header("─── VFX & Audio ───")]
    public GameObject explosionVFX;
    public AudioClip explosionSFX;
    [Tooltip("ระยะเวลาที่ตัวสกิลจะค้างอยู่หลังจากระเบิดแล้ว (วินาที)")]
    public float lifeSpanAfterExplode = 1.5f;

    [Header("─── Throw & Scale ───")]
    [Tooltip("แรงพุ่งไปข้างหน้าเมื่อเริ่มใช้สกิล (ยิ่งเยอะยิ่งไกล)")]
    public float throwForce = 1f;
    [Tooltip("ให้ VFX ขยายตามขนาด Radius หรือไม่")]
    public bool autoScaleVFX = true;

    [Header("─── Spawn Settings ───")]
    [Tooltip("ระยะห่างไปข้างหน้า Player ตอน Spawn (ห่างพอไม่โดน Player)")]
    public float spawnForwardOffset = 2.5f;

    [Header("─── Player Ignore ───")]
    [Tooltip("Layer ของ Player — สกิลจะไม่ชนและไม่โดนผลกระทบจาก Player")]
    public LayerMask playerLayer;

    private Vector3 impactPoint = Vector3.zero; // จุดที่ปะทะพื้นจริง

    // เก็บ Collider ของ Player ไว้ Ignore
    private Collider _playerCollider;

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);
        transform.SetParent(null);

        // ── Ignore collision กับ Player ทุก Collider ──
        _playerCollider = playerTransform.GetComponentInChildren<Collider>();
        if (_playerCollider != null)
        {
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            foreach (var col in myColliders)
                Physics.IgnoreCollision(col, _playerCollider, true);
        }

        // ── Spawn ข้างหน้า Player พร้อม Raycast ลงพื้น ──
        Vector3 spawnOrigin;
        if (TargetPosition.HasValue)
        {
            spawnOrigin = TargetPosition.Value + Vector3.up * 3f;
        }
        else
        {
            // ข้างหน้า Player ในระดับเอว + สูงขึ้น 3m เพื่อ arc ลงพื้น
            spawnOrigin = playerTransform.position
                          + playerTransform.forward * spawnForwardOffset
                          + Vector3.up * 3f;
        }
        transform.position = spawnOrigin;

        // Raycast หาพื้นจาก spawnOrigin ลงมา
        if (Physics.Raycast(spawnOrigin, Vector3.down, out RaycastHit groundHit, 20f))
        {
            transform.position = groundHit.point;
            impactPoint = groundHit.point;

            Rigidbody r = GetComponent<Rigidbody>();
            if (r != null) { r.isKinematic = true; r.useGravity = false; }

            TriggerAoE();
            return;
        }

        // Fallback: ถ้า Raycast ไม่เจอพื้น ใช้ Physics arc ปกติ
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            Vector3 force = (playerTransform.forward * throwForce) + (Vector3.down * 2f);
            rb.AddForce(force, ForceMode.Impulse);
        }

        Invoke(nameof(TriggerAoE), 5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // ถ้าชน Player ให้ข้ามไปเลย (ป้องกัน Physics.IgnoreCollision ทำงานไม่ทัน)
        if (_playerCollider != null && collision.collider == _playerCollider) return;

        // ชน Layer ของ Player โดยตรง
        if ((playerLayer.value & (1 << collision.gameObject.layer)) != 0) return;

        impactPoint = collision.contacts[0].point;
        transform.position = impactPoint;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        CancelInvoke(nameof(TriggerAoE));
        TriggerAoE();
    }

    private void TriggerAoE()
    {
        // ใช้จุดที่ปะทะพื้น ถ้าไม่มีให้ใช้ตำแหน่งปัจจุบัน (กรณีระเบิดกลางอากาศ)
        Vector3 finalPos = (impactPoint != Vector3.zero) ? impactPoint : transform.position;

        // 1. เล่นเอฟเฟกต์ที่จุดปะทะ
        if (explosionVFX != null) 
        {
            GameObject vfx = Instantiate(explosionVFX, finalPos, Quaternion.identity);
            if (autoScaleVFX)
            {
                // ปรับขนาด VFX ให้สัมพันธ์กับรัศมี (สมมติว่ารัศมี 1 หน่วยใน VFX เท่ากับ 1 หน่วยในรัศมีดาเมจ)
                vfx.transform.localScale = Vector3.one * (radius * 2f);
            }
            
            // ทำลาย VFX พร้อมกับตัวอ็อบเจกต์ของสกิล
            Destroy(vfx, lifeSpanAfterExplode);
        }
        if (explosionSFX != null) AudioSource.PlayClipAtPoint(explosionSFX, finalPos);

        // 2. ค้นหาศัตรูในระยะจากจุดปะทะ
        Collider[] hitEnemies = Physics.OverlapSphere(finalPos, radius, enemyLayer);
        foreach (Collider hit in hitEnemies)
        {
            ApplyAoEEffect(hit.gameObject);
        }
        
        // 3. ทำลายตัวเอง (หน่วงเวลาไว้นิดหน่อยเพื่อให้เอฟเฟกต์ทำงานจบ)
        Destroy(gameObject, lifeSpanAfterExplode);
    }

    /// <summary>
    /// คลาสลูกจะต้องจัดการว่าจะทำอะไรกับศัตรู (เช่น ลดเลือด, สตั้น, ทำให้เดินช้า)
    /// ถูกเรียกตามจำนวนศัตรูที่โดนรัศมี
    /// </summary>
    protected abstract void ApplyAoEEffect(GameObject enemy);
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
