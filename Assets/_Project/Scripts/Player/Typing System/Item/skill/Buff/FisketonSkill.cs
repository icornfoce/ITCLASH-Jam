using UnityEngine;
using System.Collections;
using ITCLASH.Enemies;

public class FisketonSkill : BaseAoESkill
{
    [Header("─── Fisketon Damage Settings ───")]
    public float damage = 30f;
    public float knockbackForce = 10f;
    
    [Header("─── Spin Settings ───")]
    [Tooltip("ความเร็วในการหมุนตอนลอยอยู่ในอากาศ")]
    public Vector3 spinSpeed = new Vector3(0, 720f, 0);
    
    [Header("─── Visual Offset ───")]
    [Tooltip("ลากส่วนที่เป็น Model มาใส่ที่นี่ เพื่อให้มันหมุนโดยไม่กระทบกับตัวหลัก")]
    public Transform visualModel;
    [Tooltip("ปรับแก้ Rotation เริ่มต้นของปลา (ถ้าปลาหันผิดทิศ)")]
    public Vector3 rotationOffset = Vector3.zero;

    private void Start()
    {
        // ถ้าไม่ได้ลาก visualModel มา ให้ลองหาลูกตัวแรก
        if (visualModel == null && transform.childCount > 0)
        {
            visualModel = transform.GetChild(0);
        }

        // เซ็ต Rotation เริ่มต้นให้โมเดลตาม Offset
        if (visualModel != null)
        {
            visualModel.localRotation = Quaternion.Euler(rotationOffset);
        }
    }

    private void Update()
    {
        if (visualModel != null)
        {
            // หมุนโมเดลลูกไปเรื่อยๆ (จะหยุดหมุนเองเมื่อ Object ถูกทำลาย/ระเบิด)
            visualModel.Rotate(spinSpeed * Time.deltaTime, Space.Self);
        }
    }

    protected override void ApplyAoEEffect(GameObject enemyObj)
    {
        EnemyController enemy = enemyObj.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            
            // ใส่แรงผลัก (Knockback)
            IKnockbackable knockable = enemyObj.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                Vector3 dir = (enemyObj.transform.position - transform.position).normalized;
                dir.y = 0.2f; // ยกขึ้นนิดหน่อย
                knockable.ApplyKnockback(dir * knockbackForce, 0.2f);
            }
        }
    }
}
