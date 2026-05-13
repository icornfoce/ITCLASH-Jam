using UnityEngine;
using ITCLASH.Enemies;

public class SocratesSkill : BaseTurretSkill
{
    [Header("Socrates Settings")]
    public float aggroRadius = 15f;

    protected override void OnTurretDeployed(Transform playerTransform)
    {
        Debug.Log("[SocratesSkill] รูปปั้นโสเครตีสปรากฏตัว! เตรียมดึง Aggro");
    }

    protected override void PerformTurretAction()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // ถ้าศัตรูมีระบบ AI เล็งเป้าหมาย ให้สั่งเปลี่ยนเป้ามาที่รูปปั้นนี้
                // EnemyAI ai = hit.GetComponent<EnemyAI>();
                // if (ai != null) ai.SetTarget(transform);
                Debug.Log($"[SocratesSkill] ดึงความสนใจของ {hit.name} มาที่รูปปั้น");
            }
        }
    }
}
