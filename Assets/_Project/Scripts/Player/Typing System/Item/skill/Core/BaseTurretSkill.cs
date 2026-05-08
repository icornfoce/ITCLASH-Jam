using UnityEngine;

public abstract class BaseTurretSkill : BaseItemSkill
{
    [Header("─── Turret Settings ───")]
    [Tooltip("ระยะเวลาที่ป้อมจะอยู่บนสนาม")]
    public float duration = 10f;
    [Tooltip("ความถี่ในการทำงาน (เช่น ยิงทุกๆ 1 วินาที)")]
    public float actionInterval = 1f;
    [Tooltip("ระยะตรวจจับเป้าหมาย")]
    public float targetingRange = 10f;
    
    protected bool isActive = false;
    private float timer = 0f;

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);
        transform.SetParent(null);
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        { 
            rb.isKinematic = false; 
            rb.useGravity = true; 
        }

        isActive = true;
        if (duration > 0f) Destroy(gameObject, duration);
        
        OnTurretDeployed(playerTransform);
    }

    protected virtual void Update()
    {
        if (!isActive) return;
        
        timer += Time.deltaTime;
        if (timer >= actionInterval)
        {
            timer = 0f;
            PerformTurretAction();
        }
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
