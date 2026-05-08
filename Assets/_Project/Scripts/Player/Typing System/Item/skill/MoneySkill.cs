using UnityEngine;
using ITCLASH.Enemies;

public class MoneySkill : BaseTurretSkill
{
    [Header("Money Turret Settings")]
    public float expPerHit = 5f;

    protected override void PerformTurretAction()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, targetingRange);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // ยิงใส่ศัตรูและได้ EXP
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    enemy.ApplyDamage(10f);
                    // PlayerLevel.Instance.AddExp(expPerHit);
                    Debug.Log($"[MoneySkill] ยิงศัตรู {hit.name} ได้รับ EXP {expPerHit}!");
                    break; // ยิงทีละตัว
                }
            }
        }
    }
}
