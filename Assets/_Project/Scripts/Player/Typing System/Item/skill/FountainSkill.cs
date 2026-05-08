using UnityEngine;
using ITCLASH.Enemies;

public class FountainSkill : BaseAoESkill
{
    [Header("Fountain Settings")]
    public float damage = 30f;
    public float slowDuration = 4f;

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            // ถ้ามีระบบสโลว์ ให้เรียก: enemy.ApplySlow(0.4f, slowDuration);
            Debug.Log($"[FountainSkill] ศัตรู {enemyObj.name} โดนน้ำพุ Damage {damage} และ Slow");
        }
    }
}
