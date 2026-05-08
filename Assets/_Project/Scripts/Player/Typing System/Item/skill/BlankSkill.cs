using UnityEngine;
using ITCLASH.Enemies;

public class BlankSkill : BaseAoESkill
{
    [Header("Blank Settings")]
    public float blindDuration = 4f;

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        // ศัตรูในระยะจะโดนทำให้มองไม่เห็น (Stun / Blind)
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            // ถ้ามีระบบสถานะตาบอด/สตั้น
            // enemy.ApplyStun(blindDuration);
            Debug.Log($"[BlankSkill] {enemyObj.name} โดนลบการมองเห็น! (Stun {blindDuration} วิ)");
        }
    }
}
