using UnityEngine;

public class InstantiateSkill : BaseSummonSkill
{
    [Header("Instantiate Settings")]
    [Tooltip("ความเก่งของมอนสเตอร์ (อาจปรับเพิ่มตามจำนวนอักขระในอนาคต)")]
    public float powerMultiplier = 1f;

    protected override void OnSummonCreated(GameObject summonedEntity, Transform playerTransform)
    {
        // สั่งให้ AI ที่เสกมา ตามติดผู้เล่น หรือเริ่มโจมตี
        Debug.Log($"[InstantiateSkill] เสกตัวช่วย {summonedEntity.name} ออกมาช่วยสู้!");
        
        // ถ้าตัวช่วยมีสคริปต์ CompanionController ก็ส่งค่า playerTransform ไปให้ตาม
        // CompanionController comp = summonedEntity.GetComponent<CompanionController>();
        // if (comp != null) comp.Follow(playerTransform);
    }
}
