using UnityEngine;
using System.Collections;

public class HorseSkill : BaseBuffSkill
{
    [Header("─── Horse Settings ───")]
    [Tooltip("ตัวคูณความเร็วเดิน (เช่น 2 = เดินเร็ว 2 เท่า)")]
    public float speedMultiplier = 2f;

    [Tooltip("VFX ฝุ่นที่จะติดที่เท้าผู้เล่นระหว่างบัฟ (optional)")]
    public GameObject dustVFXPrefab;

    [Tooltip("เสียง Neigh เมื่อบัฟเริ่ม (optional)")]
    public AudioClip neighSFX;

    private PlayerController playerController;
    private float originalSpeed;
    private GameObject spawnedDust;

    protected override void ApplyBuff(Transform playerTransform)
    {
        // หา PlayerController จาก root ด้วย ในกรณีที่ playerTransform เป็น child
        playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController == null)
            playerController = playerTransform.GetComponentInParent<PlayerController>();
        if (playerController == null)
            playerController = playerTransform.GetComponentInChildren<PlayerController>();

        if (playerController == null)
        {
            Debug.LogWarning("[HorseSkill] ไม่พบ PlayerController! บัฟไม่ทำงาน");
            return;
        }

        // จำค่าเดิมไว้ก่อน แล้วคูณความเร็ว
        originalSpeed = playerController.walkSpeed;
        playerController.walkSpeed = originalSpeed * speedMultiplier;

        // เสียง Neigh
        if (neighSFX != null)
            AudioSource.PlayClipAtPoint(neighSFX, playerTransform.position);

        // VFX ฝุ่น ติดที่เท้าผู้เล่น
        if (dustVFXPrefab != null)
        {
            spawnedDust = Instantiate(dustVFXPrefab, playerTransform);
            spawnedDust.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        }

        Debug.Log($"[HorseSkill] บัฟความเร็วเริ่มทำงาน! {originalSpeed} → {playerController.walkSpeed} (x{speedMultiplier})");
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        if (playerController != null)
        {
            playerController.walkSpeed = originalSpeed;
            Debug.Log($"[HorseSkill] บัฟหมดแล้ว ความเร็วกลับเป็น {originalSpeed}");
        }

        // ลบ VFX ฝุ่น
        if (spawnedDust != null)
            Destroy(spawnedDust);

        playerController = null;
    }
}
