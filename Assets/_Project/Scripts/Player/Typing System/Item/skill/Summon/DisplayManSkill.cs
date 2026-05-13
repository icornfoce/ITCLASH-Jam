using UnityEngine;

public class DisplayManSkill : BaseSummonSkill
{
    [Header("Display Man Settings")]
    public float attackPower = 50f;

    protected override void OnSummonCreated(GameObject summonedEntity, Transform playerTransform)
    {
        Debug.Log($"[DisplayManSkill] Summon ตัวช่วยประหลาด {summonedEntity.name} ออกมา! โจมตีแรงและอยู่นาน");
        // ถ้ามีสคริปต์ลูกน้อง ให้เซ็ตค่า AttackPower
        // CompanionController comp = summonedEntity.GetComponent<CompanionController>();
        // if (comp != null) comp.SetDamage(attackPower);
    }
}
