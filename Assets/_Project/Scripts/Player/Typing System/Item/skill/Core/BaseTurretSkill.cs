using UnityEngine;

public abstract class BaseTurretSkill : BaseItemSkill
{
    [Header("─── Turret Settings ───")]
    [Tooltip("ระยะเวลาที่ป้อมจะอยู่บนสนาม (วินาที)")]
    public float duration = 10f;
    [Tooltip("ความถี่ในการทำงาน (เช่น ยิงทุกๆ 1 วินาที)")]
    public float actionInterval = 1f;
    [Tooltip("ระยะตรวจจับเป้าหมาย")]
    public float targetingRange = 10f;
    
    protected bool isActive = false;
    private float timer = 0f;
    private float destroyAt = -1f; // ใช้ timer แทน Destroy เพื่อให้ยกเลิกได้

    public override void Activate(Transform playerTransform)
    {
        // ถ้าไม่ได้เล็งเป้าไว้ (กดใช้ตรงๆ) ให้เกิดด้านหน้าผู้เล่น
        if (!TargetPosition.HasValue)
        {
            // หาจุดเกิดห่างไปด้านหน้า 2 เมตร (บวกความสูงเผื่อไว้กันชนพื้นตอน Raycast)
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * 2f + Vector3.up * 2f;
            
            // ยิง Raycast ลงไปหาพื้น
            if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 10f))
            {
                transform.position = hit.point;
            }
            else
            {
                transform.position = playerTransform.position + playerTransform.forward * 2f;
            }

            transform.rotation = playerTransform.rotation;
        }

        PlayVoice(transform.position);
        transform.SetParent(null);
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        { 
            rb.isKinematic = false; 
            rb.useGravity = true; 
        }

        isActive = true;
        if (duration > 0f) destroyAt = Time.time + duration;
        
        OnTurretDeployed(playerTransform);
    }

    protected virtual void Update()
    {
        // เช็คว่าถึงเวลาหมดอายุหรือยัง
        if (destroyAt > 0f && Time.time >= destroyAt)
        {
            destroyAt = -1f; // ป้องกันเรียกซ้ำ
            OnDurationExpired();
            return;
        }

        if (!isActive) return;
        
        timer += Time.deltaTime;
        if (timer >= actionInterval)
        {
            timer = 0f;
            PerformTurretAction();
        }
    }

    /// <summary>
    /// ยกเลิก timer หมดอายุ (เช่น ตอนป้อมตายก่อนเวลา)
    /// </summary>
    protected void CancelDurationTimer()
    {
        destroyAt = -1f;
    }

    /// <summary>
    /// เรียกเมื่อ duration หมดเวลา — override ได้เพื่อเล่น death animation ก่อน Destroy
    /// </summary>
    protected virtual void OnDurationExpired()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// เรียกครั้งแรกตอนป้อมลงพื้น
    /// </summary>
    protected virtual void OnTurretDeployed(Transform playerTransform) { }

    /// <summary>
    /// เรียกทุกๆ actionInterval (เช่น ยิงปืน, ดึง Aggro)
    /// </summary>
    protected abstract void PerformTurretAction();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetingRange);
    }
}
