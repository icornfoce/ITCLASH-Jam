using UnityEngine;
using System.Collections;

public class KnightSkill : BaseBuffSkill
{
    [Header("Knight Settings")]
    public float speedMultiplier = 1.5f;
    
    // อ้างอิงถึง FirstPersonController หรือสคริปต์เดินของผู้เล่น
    private FirstPersonController fpc;
    private float originalSpeed;

    protected override void ApplyBuff(Transform playerTransform)
    {
        fpc = playerTransform.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            originalSpeed = fpc.walkSpeed;
            fpc.walkSpeed *= speedMultiplier;
            Debug.Log($"[KnightSkill] วิ่งเร็วขึ้น! ความเร็วปัจจุบัน: {fpc.walkSpeed}");
        }
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        if (fpc != null)
        {
            fpc.walkSpeed = originalSpeed;
            Debug.Log("[KnightSkill] ความเร็วกลับเป็นปกติ");
        }
    }
}
