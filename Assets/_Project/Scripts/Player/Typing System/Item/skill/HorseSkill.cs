using UnityEngine;

public class HorseSkill : BaseBuffSkill
{
    [Header("Horse Settings")]
    [Tooltip("ตัวคูณความเร็วเดิน (เช่น 2 = เดินเร็ว 2 เท่า)")]
    public float speedMultiplier = 2f;

    private PlayerController playerController;

    protected override void ApplyBuff(Transform playerTransform)
    {
        playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController == null) return;

        playerController.walkSpeed *= speedMultiplier;
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        if (playerController == null) return;

        playerController.walkSpeed /= speedMultiplier;
        playerController = null;
    }
}
