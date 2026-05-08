using UnityEngine;
using ITCLASH.Enemies;

public class CataclysmSkill : BaseAoESkill
{
    [Header("Cataclysm Settings")]
    public float damage = 80f;
    public float slowDuration = 5f;

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            // ถ้ามีระบบเดินช้า: enemy.ApplySlow(0.2f, slowDuration);
            Debug.Log($"[CataclysmSkill] {enemyObj.name} โดนอุกกาบาตทับ Damage: {damage} แถมเดินช้ามาก!");
        }
    }
}
