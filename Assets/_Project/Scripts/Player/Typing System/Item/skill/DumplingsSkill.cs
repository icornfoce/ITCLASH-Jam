using UnityEngine;

public class DumplingsSkill : BaseTurretSkill
{
    [Header("─── Dumplings Turret Settings ───")]
    [Tooltip("ความเสียหายที่ยิงออกไปแต่ละนัด")]
    public float damagePerShot = 10f;
    
    [Tooltip("พรีแฟบกระสุนที่จะยิงออกไป (ถ้าไม่ใส่จะยิงเข้าเป้าทันที)")]
    public GameObject bulletPrefab;
    
    [Tooltip("จุดที่จะให้กระสุนพุ่งออกไป (ปลายกระบอกปืน)")]
    public Transform shootPoint;

    protected override void PerformTurretAction()
    {
        // 1. หาเป้าหมายที่ใกล้ที่สุดในรัศมี
        Collider[] hits = Physics.OverlapSphere(transform.position, targetingRange);
        Transform target = null;
        float minDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    target = hit.transform;
                }
            }
        }

        // 2. ถ้ายิงโดนใครสักคน
        if (target != null)
        {
            Debug.Log($"[DumplingsSkill] ยิงใส่ {target.name} ทำดาเมจ {damagePerShot}");
            
            // ถ้ายิงเป็นกระสุน
            if (bulletPrefab != null && shootPoint != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
                bullet.transform.LookAt(target.position + Vector3.up);
                bullet.GetComponent<Rigidbody>()?.AddForce(bullet.transform.forward * 30f, ForceMode.VelocityChange);
            }
            else
            {
                // ถ้ายิงตรงๆ หักเลือดเลย (Hitscan)
                ITCLASH.Enemies.EnemyController enemy = target.GetComponentInParent<ITCLASH.Enemies.EnemyController>();
                if (enemy != null) enemy.ApplyDamage(damagePerShot);
            }
        }
    }
}
