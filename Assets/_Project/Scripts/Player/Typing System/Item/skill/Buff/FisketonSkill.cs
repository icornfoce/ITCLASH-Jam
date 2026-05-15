using UnityEngine;
using System.Collections;
using ITCLASH.Enemies;

// สคริปต์ Fisketon แบบโยนออกไปและหมุนที่ตัวเอง (Root)
public class FisketonSkill : BaseAoESkill
{
    [Header("─── Fisketon Settings ───")]
    public float damage = 20f;
    public float knockbackForce = 15f;
    
    [Tooltip("ความเร็วในการหมุน (ปรับแกน X, Y, Z ได้ใน Inspector)")]
    public Vector3 spinSpeed = new Vector3(0, 720f, 0);

    private void Update()
    {
        // หมุนที่ตัวมันเอง (Root Transform) โดยตรง
        transform.Rotate(spinSpeed * Time.deltaTime, Space.Self);
    }

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            
            IKnockbackable knockable = enemyObj.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                // ผลักศัตรูออกจากจุดที่ปลาตก
                Vector3 dir = (enemyObj.transform.position - transform.position).normalized;
                dir.y = 0.2f;
                knockable.ApplyKnockback(dir * knockbackForce, 0.2f);
            }
        }
    }
}
