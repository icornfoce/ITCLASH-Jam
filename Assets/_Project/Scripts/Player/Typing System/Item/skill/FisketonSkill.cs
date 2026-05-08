using UnityEngine;
using System.Collections;
using ITCLASH.Enemies;

public class FisketonSkill : BaseBuffSkill
{
    [Header("Fisketon Settings")]
    public float damage = 20f;
    public float knockback = 15f;
    public float aoeRadius = 5f;
    public float hitInterval = 0.5f; // ตีรัวๆ รอบตัว
    public LayerMask enemyLayer;

    private bool isSpinning = false;

    protected override void ApplyBuff(Transform playerTransform)
    {
        isSpinning = true;
        StartCoroutine(SpinRoutine(playerTransform));
        Debug.Log("[FisketonSkill] ปลาหมุนเริ่มทำงาน! AoE รอบตัว!");
    }

    private IEnumerator SpinRoutine(Transform playerTransform)
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
