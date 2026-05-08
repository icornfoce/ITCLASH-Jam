using UnityEngine;

public class TableSkill : BaseTurretSkill
{
    protected override void OnTurretDeployed(Transform playerTransform)
    {
        Debug.Log("[TableSkill] วางโต๊ะกีดขวางศัตรูเรียบร้อย!");
        // เปลี่ยนเลเยอร์หรือตั้งค่าให้เป็นสิ่งกีดขวาง (Obstacle)
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    protected override void PerformTurretAction()
    {
        // โต๊ะไม่ต้องทำอะไรนอกจากขวางทาง เลยปล่อยว่างไว้
    }
}
