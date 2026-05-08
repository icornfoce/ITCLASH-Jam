using UnityEngine;
using System.Collections;

public class ChronostasisSkill : BaseBuffSkill
{
    [Header("Chronostasis Settings")]
    [Tooltip("ความช้าของเวลา (ยิ่งใกล่ 0 ยิ่งช้า)")]
    public float timeScaleMultiplier = 0.1f;

    protected override void ApplyBuff(Transform playerTransform)
    {
        Time.timeScale = timeScaleMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log($"[ChronostasisSkill] หยุดเวลาโลก! TimeScale = {Time.timeScale}");
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("[ChronostasisSkill] เวลาโลกกลับมาเดินปกติ");
    }
}
