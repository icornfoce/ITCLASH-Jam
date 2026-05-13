using UnityEngine;
using System.Collections;
using ITCLASH.Enemies;

public class FisketonSkill : BaseBuffSkill
{
    [Header("Fisketon Settings")]
    public float damage = 20f;
    public float knockback = 15f;
    public float aoeRadius = 5f;
    public float hitInterval = 0.5f; 
    public float rotationSpeed = 360f; // ความเร็วในการหมุนรอบตัว
    public float orbitRadius = 2f;     // ระยะห่างจากตัวผู้เล่น
    public LayerMask enemyLayer;

    private bool isSpinning = false;
    private float currentAngle = 0f;

    protected override void ApplyBuff(Transform playerTransform)
    {
        // เปิดการแสดงผล Mesh (เพราะคลาสเบสสั่งปิดไว้)
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
        }

        isSpinning = true;
        StartCoroutine(SpinRoutine(playerTransform));
        StartCoroutine(DamageRoutine(playerTransform));
        Debug.Log("[FisketonSkill] ปลาหมุนเริ่มทำงาน! หมุนรอบตัวจริง!");
    }

    private IEnumerator SpinRoutine(Transform playerTransform)
    {
        while (isSpinning)
        {
            // คำนวณมุมหมุนตามเวลา
            currentAngle += rotationSpeed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;
            
            // คำนวณตำแหน่ง Orbit รอบผู้เล่น
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitRadius;
            transform.position = playerTransform.position + offset + Vector3.up * 1f;
            
            // หมุนให้หน้าปลาหันไปตามทิศทางการเคลื่อนที่
            transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(rad), 0, Mathf.Cos(rad)));

            yield return null;
        }
    }

    private IEnumerator DamageRoutine(Transform playerTransform)
    {
        while (isSpinning)
        {
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, aoeRadius, enemyLayer);
            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    enemy.ApplyDamage(damage);
                    IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
                    if (knockable != null)
                    {
                        Vector3 dir = (enemy.transform.position - playerTransform.position).normalized;
                        knockable.ApplyKnockback(dir * knockback, 0.2f);
                    }
                }
            }
            yield return new WaitForSeconds(hitInterval);
        }
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        isSpinning = false;
        Debug.Log("[FisketonSkill] ปลาหมุนหยุดทำงาน");
    }
}
