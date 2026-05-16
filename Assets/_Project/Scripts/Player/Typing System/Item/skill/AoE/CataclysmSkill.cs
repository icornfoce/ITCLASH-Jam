using UnityEngine;
using ITCLASH.Enemies;

public class CataclysmSkill : BaseAoESkill
{
    [Header("Cataclysm Settings")]
    public float damage = 80f;
    public float slowDuration = 8f; // เพิ่มจาก 5 เป็น 8

    private void Awake()
    {
        // ตั้งค่าให้ตัวอุกกาบาตค้างอยู่บนพื้นนานขึ้น (เช่น 4 วินาที)
        lifeSpanAfterExplode = 4f;
    }

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            float finalDamage = damage * PowerMultiplier;
            enemy.ApplyDamage(finalDamage);
            // ถ้ามีระบบเดินช้า: enemy.ApplySlow(0.2f, slowDuration);
            Debug.Log($"[CataclysmSkill] {enemyObj.name} โดนอุกกาบาตทับ Damage: {finalDamage} (Power: {PowerMultiplier:F2}x) แถมเดินช้ามาก!");
        }
    }
}
