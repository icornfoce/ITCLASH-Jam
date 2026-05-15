using UnityEngine;
using ITCLASH.Enemies;

public class SocratesSkill : BaseTurretSkill, IDamageable
{
    // IDamageable implementation
    public Transform Transform => transform;
    public float HealthPercent => 1.0f; // อมตะจนกว่าจะหมดเวลา
    public bool IsAlive => isActive;
    public void ApplyDamage(float amount) { /* รูปปั้นโสเครตีสเป็นอมตะ */ }
    public void Heal(float amount) { }

    [Header("Socrates Settings")]
    public float aggroRadius = 15f;

    [Header("VFX / SFX")]
    [Tooltip("VFX ที่แสดงอยู่ (จะรอให้หายไปก่อนค่อยทำลาย)")]
    public ParticleSystem activeVFX;
    [Tooltip("SFX ที่กำลังเล่นอยู่ (จะรอให้จบก่อนค่อยทำลาย)")]
    public AudioSource activeSFX;
    [Tooltip("โมเดลของรูปปั้น (จะหายไปทันทีเมื่อหมดเวลา)")]
    public GameObject visualModel;

    protected override void OnTurretDeployed(Transform playerTransform)
    {
        Debug.Log("[SocratesSkill] รูปปั้นโสเครตีสปรากฏตัว! เตรียมดึง Aggro");
        EnemyController.RegisterSummon(transform, true); // ลงทะเบียนเป็น Priority Target
    }

    private void OnDisable()
    {
        EnemyController.UnregisterSummon(transform);
    }

    private void OnDestroy()
    {
        EnemyController.UnregisterSummon(transform);
    }

    protected override void OnDurationExpired()
    {
        // แทนที่จะทำลายทันที ให้เริ่มขั้นตอนการจางหาย
        StartCoroutine(DespawnRoutine());
    }

    private System.Collections.IEnumerator DespawnRoutine()
    {
        isActive = false; // หยุดการดึง Aggro และ Action อื่นๆ
        EnemyController.UnregisterSummon(transform); // ถอดการดึงความสนใจทันทีที่เริ่มหายไป
        
        // ซ่อนโมเดลทันทีเพื่อให้ดูเหมือนรูปปั้นหายไปแล้ว
        if (visualModel != null) visualModel.SetActive(false);
        else 
        {
            // Fallback: ถ้าไม่ได้ตั้งค่า visualModel ให้หา MeshRenderer ในลูกๆ
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        Debug.Log("[SocratesSkill] กำลังรอให้ SFX/VFX จบก่อนทำลาย Object...");

        // รอจนกว่า Particle และ Sound จะเล่นจบ
        bool isPlaying = true;
        float timeout = 5f; // กันค้าง (เผื่อ Particle เป็น Loop)
        float elapsed = 0f;

        while (isPlaying && elapsed < timeout)
        {
            isPlaying = false;
            if (activeVFX != null && (activeVFX.isPlaying || activeVFX.particleCount > 0)) isPlaying = true;
            if (activeSFX != null && activeSFX.isPlaying) isPlaying = true;
            
            if (isPlaying)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
        }

        Debug.Log("[SocratesSkill] SFX/VFX จบแล้ว ทำลาย Object จริง");
        Destroy(gameObject);
    }

    protected override void PerformTurretAction()
    {
        if (!isActive) return;

        // ระบบ Taunt: บังคับให้ศัตรูในระยะหันมาหาทันที
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                var enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    // ถ้าศัตรูกำลังไล่ตามอย่างอื่นอยู่ ให้สั่งให้มันอัปเดตเป้าหมายทันที
                    // ในระบบ StateMachine ปัจจุบัน ChaseState จะดึง GetCombatTarget ทุก Tick อยู่แล้ว
                    // การเรียก Debug หรือคำสั่ง Force อื่นๆ (ถ้ามี) สามารถใส่ตรงนี้ได้
                    Debug.Log($"[SocratesSkill] Taunted {hit.name}");
                }
            }
        }
    }
}
