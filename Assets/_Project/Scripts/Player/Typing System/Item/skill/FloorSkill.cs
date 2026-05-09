using UnityEngine;
using ITCLASH.Enemies;

public class FloorSkill : BaseAoESkill
{
    [Header("Floor Settings")]
    public float damage = 40f;
    public float knockback = 20f;

    public override void Activate(Transform playerTransform)
    {
        Vector3? aimPoint = TargetPosition;

        if (!aimPoint.HasValue)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    aimPoint = hit.point;
                }
            }
        }

        if (aimPoint.HasValue) transform.position = aimPoint.Value + Vector3.up * 0.5f;

        base.Activate(playerTransform);
    }

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);

            IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                knockable.ApplyKnockback(Vector3.up * knockback, 0.4f);
            }

            Debug.Log($"[FloorSkill] พื้นระเบิดอัด {enemyObj.name} กระเด็นลอยขึ้นฟ้า!");
        }
    }
}
