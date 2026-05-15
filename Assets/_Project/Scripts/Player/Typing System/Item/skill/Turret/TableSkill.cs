using UnityEngine;
using ITCLASH.Enemies; // เพิ่ม namespace สำหรับ IDamageable

public class TableSkill : BaseTurretSkill, IDamageable
{
    [Header("─── Table Settings ───")]
    [Tooltip("เลือดของโต๊ะ")]
    public float maxHealth = 100f;
    [Tooltip("หมุนโต๊ะให้ขวางผู้เล่น (90 องศา) หรือไม่")]
    public bool placePerpendicular = true;

    private float currentHealth;

    // --- IDamageable Implementation ---
    public Transform Transform => transform;
    public float HealthPercent => currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0;

    public void ApplyDamage(float amount)
    {
        if (!IsAlive) return;

        currentHealth -= amount;
        Debug.Log($"[TableSkill] โต๊ะโดนโจมตี! เลือดเหลือ {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Die()
    {
        Debug.Log("[TableSkill] โต๊ะพัง!");
        // ยกเลิก timer ของ BaseTurretSkill และทำลายทิ้ง
        CancelDurationTimer(); 
        // ถ้ามี Effect ตอนพัง สามารถใส่ตรงนี้ได้
        Destroy(gameObject);
    }

    protected override void OnTurretDeployed(Transform playerTransform)
    {
        Debug.Log("[TableSkill] วางโต๊ะกีดขวางศัตรูเรียบร้อย!");
        currentHealth = maxHealth;

        // 1. ปรับมุมของโต๊ะตามผู้เล่น
        float yRotation = playerTransform.eulerAngles.y;
        if (placePerpendicular)
        {
            yRotation += 90f; // หมุน 90 องศาให้ขวางทาง
        }
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 2. ให้ Player เดินผ่านโต๊ะได้ (Ignore Collision)
        Collider[] playerCols = playerTransform.GetComponentsInChildren<Collider>();
        Collider[] tableCols = GetComponentsInChildren<Collider>();
        foreach (var pCol in playerCols)
        {
            foreach (var tCol in tableCols)
            {
                if (pCol != null && tCol != null)
                {
                    Physics.IgnoreCollision(pCol, tCol, true);
                }
            }
        }

        // 3. เพิ่ม NavMeshObstacle เพื่อให้ศัตรูเดินหลบโต๊ะ
        var obstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.carving = true;
        // ขนาดและตำแหน่งชดเชยการหมุนของโมเดลลูก (Y = -90 องศา)
        // สลับแกน X กับ Z ของ Size และแปลงพิกัด Center
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        obstacle.size = new Vector3(1.2f, 1.2f, 2.5f);
        obstacle.center = new Vector3(-0.3f, 0.6f, 0f);

        // เปลี่ยนเลเยอร์ให้เป็น Default หรือ Obstacle (ถ้ามี)
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    protected override void PerformTurretAction()
    {
        // โต๊ะไม่ต้องทำอะไรนอกจากขวางทาง เลยปล่อยว่างไว้
    }
}
