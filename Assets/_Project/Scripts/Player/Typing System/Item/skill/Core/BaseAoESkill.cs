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
    [Tooltip("ระยะห่างจากตัวผู้เล่นตอนเริ่มเกิด (หน่วยเป็นเมตร)")]
    public float spawnForwardOffset = 2.0f;

    private Vector3 impactPoint = Vector3.zero; // จุดที่ปะทะพื้นจริง

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);
        transform.SetParent(null);
        
        // --- ถ้าไม่ได้เล็งเป้าเจาะจง ให้ขยับตำแหน่งไปข้างหน้า Player ก่อนพุ่ง ---
        if (!TargetPosition.HasValue)
        {
            // ขยับไปข้างหน้า + ขึ้นข้างบนนิดหน่อยเพื่อให้พ้นตัวผู้เล่น
            transform.position = playerTransform.position + (playerTransform.forward * spawnForwardOffset) + (Vector3.up * 1.2f);
        }

        // --- ระบบ Ground Snap สำหรับกรณีที่มีเป้าหมาย (จากการเล็ง) ---
        if (TargetPosition.HasValue)
        {
            // ลองยิง Ray ลงพื้นเพื่อ Snap ทันที
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 10f))
            {
                transform.position = hit.point;
                impactPoint = hit.point;
                
                // หยุดฟิสิกส์เพราะเราวางบนพื้นแล้ว
                Rigidbody r = GetComponent<Rigidbody>();
                if (r != null) { r.isKinematic = true; r.useGravity = false; }
                
                TriggerAoE();
                return;
            }
        }

        // --- ระบบฟิสิกส์ปกติ (กรณีที่ไม่มีเป้าหมาย หรือ Snap ไม่เจอ) ---
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        { 
            rb.isKinematic = false; 
            rb.useGravity = true; 
            
            // เพิ่มแรงพุ่งไปข้างหน้า + แรงกดลงพื้นเล็กน้อย
            Vector3 force = (playerTransform.forward * throwForce) + (Vector3.down * 2f);
            rb.AddForce(force, ForceMode.Impulse);
        }

        // กรณีฉุกเฉิน: ถ้าไม่โดนพื้นภายใน 5 วินาที ให้ระเบิดเอง
        Invoke(nameof(TriggerAoE), 5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. เก็บจุดที่ปะทะพื้นจริงๆ
        impactPoint = collision.contacts[0].point;
        
        // 2. ย้ายตำแหน่งวัตถุให้ติดพื้นตรงจุดที่ชน
        transform.position = impactPoint;

        // 3. หยุดระบบฟิสิกส์เพื่อให้วัตถุค้างอยู่ที่พื้น
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // เมื่อแตะพื้นหรือวัตถุ ให้ระเบิดทันทีและยกเลิก Invoke เดิม
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
