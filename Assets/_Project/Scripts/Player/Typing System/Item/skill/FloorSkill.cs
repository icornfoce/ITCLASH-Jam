using UnityEngine;
using ITCLASH.Enemies;

public class FloorSkill : BaseAoESkill
{
    [Header("Floor Settings")]
    public float damage = 40f;
    public float knockback = 20f;

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            
            IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                // ดันศัตรูให้ลอยขึ้น
                knockable.ApplyKnockback(Vector3.up * knockback, 0.4f);
            }

            Debug.Log($"[FloorSkill] พื้นระเบิดอัด {enemyObj.name} กระเด็นลอยขึ้นฟ้า!");
        }
    }
}
