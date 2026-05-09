using UnityEngine;
using System.Collections;

public class ChronostasisSkill : BaseBuffSkill
{
    [Header("Chronostasis Settings")]
    [Tooltip("ความช้าของเวลา (ยิ่งใกล้ 0 ยิ่งช้า)")]
    public float timeScaleMultiplier = 0.1f;
    [Tooltip("ระยะเวลาที่ใช้ในการค่อยๆ กลับมาเป็นปกติ (วินาที)")]
    public float recoveryDuration = 1.5f;

    private FirstPersonController fpc;
    private float originalWalkSpeed;

    protected override void ApplyBuff(Transform playerTransform)
    {
        Time.timeScale = timeScaleMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // ชดเชยความเร็วให้ผู้เล่นเดินได้ปกติในขณะที่โลกช้าลง
        fpc = playerTransform.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            originalWalkSpeed = fpc.walkSpeed;
            // คูณความเร็วเพิ่มขึ้นเท่ากับสัดส่วนที่เวลาช้าลง
            fpc.walkSpeed = originalWalkSpeed / timeScaleMultiplier;
        }

        Debug.Log($"[ChronostasisSkill] หยุดเวลาโลก! TimeScale = {Time.timeScale} (Player เดินปกติ)");
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        // เริ่ม Coroutine เพื่อค่อยๆ คืนค่าเวลา (ใช้บน Player เพราะตัวสกิลกำลังจะถูกทำลาย)
        if (playerTransform != null)
        {
            MonoBehaviour runner = playerTransform.GetComponent<MonoBehaviour>();
            runner.StartCoroutine(RestoreTimeRoutine());
        }
        else
        {
            ResetTime();
        }
    }

    private IEnumerator RestoreTimeRoutine()
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < recoveryDuration)
        {
            elapsed += Time.unscaledDeltaTime; // ต้องใช้ unscaled เพราะเวลากำลังช้าอยู่
            float t = elapsed / recoveryDuration;
            
            float currentScale = Mathf.Lerp(startScale, 1f, t);
            Time.timeScale = currentScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // ค่อยๆ ปรับความเร็วผู้เล่นกลับมาเป็นปกติพร้อมกับเวลา
            if (fpc != null)
            {
                fpc.walkSpeed = originalWalkSpeed / currentScale;
            }

            yield return null;
        }

        ResetTime();
    }

    private void ResetTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (fpc != null)
        {
            fpc.walkSpeed = originalWalkSpeed;
        }
        Debug.Log("[ChronostasisSkill] เวลาโลกกลับมาเดินปกติ");
    }
}
