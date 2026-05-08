using UnityEngine;

public abstract class BaseSummonSkill : BaseItemSkill
{
    [Header("─── Summon Settings ───")]
    [Tooltip("Prefab ของมอนสเตอร์/ตัวช่วย ที่จะเรียกออกมา")]
    public GameObject summonPrefab;
    [Tooltip("ให้ตัวช่วยอยู่ได้นานแค่ไหน (0 = อยู่จนตาย)")]
    public float summonDuration = 0f;
    [Tooltip("เสกให้ลอยกลางอากาศหรือร่วงลงพื้น")]
    public bool dropToGround = true;

    [Header("─── VFX & Audio ───")]
    public GameObject spawnVFX;
    public AudioClip spawnSFX;

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);
        
        // 1. เล่นเอฟเฟกต์เสก
        if (spawnVFX != null) Instantiate(spawnVFX, transform.position, Quaternion.identity);
        if (spawnSFX != null) AudioSource.PlayClipAtPoint(spawnSFX, transform.position);

        // 2. ถ้ามี Prefab ให้เสกออกมา
        if (summonPrefab != null)
        {
            // เสกมาตรงตำแหน่งของไอเทม
            GameObject minion = Instantiate(summonPrefab, transform.position, Quaternion.identity);
            
            // ตั้งค่าเพิ่มเติมตามคลาสลูก
            OnSummonCreated(minion, playerTransform);

            // ถ้ามีระยะเวลาจำกัด ให้ทำลายทิ้งเมื่อหมดเวลา
            if (summonDuration > 0f)
            {
                Destroy(minion, summonDuration);
            }
        }

        // 3. ทำลายไอเทมตัวสัญลักษณ์ทิ้งทันที
        Destroy(gameObject);
    }

    /// <summary>
    /// ถูกเรียกเมื่อตัวช่วยเกิดมาแล้ว ให้คลาสลูกนำไปสั่งงานต่อ (เช่น สั่งให้โจมตีศัตรูตัวไหน)
    /// </summary>
    protected abstract void OnSummonCreated(GameObject summonedEntity, Transform playerTransform);
}
