using UnityEngine;
using ITCLASH.Enemies;

public class BlankSkill : BaseAoESkill
{
    [Header("Blank Settings")]
    public float blindDuration = 4f;

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy == null) return;

        var existing = enemy.GetComponent<BlindEffect>();
        if (existing != null)
        {
            existing.Refresh(blindDuration);
        }
        else
        {
            enemy.gameObject.AddComponent<BlindEffect>().Setup(enemy, blindDuration);
        }

        Debug.Log($"[BlankSkill] {enemyObj.name} โดนลบการมองเห็น! (Stun {blindDuration} วิ)");
    }
}
