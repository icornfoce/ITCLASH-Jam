using UnityEngine;

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

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);
        transform.SetParent(null);
        
        // ทำให้พุ่งตกลงพื้นแบบอิสระ
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        { 
            rb.isKinematic = false; 
            rb.useGravity = true; 
        }

        // ตั้งเวลาทำงาน
        Invoke(nameof(TriggerAoE), delayBeforeExplode);
    }

    private void TriggerAoE()
    {
        // 1. เล่นเอฟเฟกต์
        if (explosionVFX != null) Instantiate(explosionVFX, transform.position, Quaternion.identity);
        if (explosionSFX != null) AudioSource.PlayClipAtPoint(explosionSFX, transform.position);

        // 2. ค้นหาศัตรูในระยะ
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        foreach (Collider hit in hitEnemies)
        {
            ApplyAoEEffect(hit.gameObject);
        }
        
        // 3. ทำลายตัวเอง (หน่วงเวลาไว้นิดหน่อยเพื่อให้เอฟเฟกต์ทำงานจบ)
        // เราจะไม่ปิด Renderer ทันที เพื่อให้โมเดลสกิลค้างอยู่ตามเวลาที่กำหนด
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
