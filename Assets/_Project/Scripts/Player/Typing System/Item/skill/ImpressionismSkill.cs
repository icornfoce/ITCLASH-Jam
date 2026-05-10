using UnityEngine;
using ITCLASH.Enemies;

public class ImpressionismSkill : BaseProjectileSkill
{
    [Header("─── Impressionism Settings ───")]
    [Tooltip("Red: ดาเมจระเบิดใส่ศัตรูที่โดน")]
    public float burstDamage = 50f;

    [Space]
    [Tooltip("Blue: เปอร์เซ็นต์ความเร็วที่ลด (0 = หยุดนิ่ง, 1 = ปกติ)")]
    [Range(0f, 1f)]
    public float slowPercent = 0.6f;

    [Tooltip("Blue: ระยะเวลาชะลอความเร็ว (วินาที)")]
    public float slowDuration = 4f;

    [Space]
    [Tooltip("Green: ปริมาณ HP ที่ฟื้นฟูให้กับผู้เล่น")]
    public float healAmount = 30f;

    [Space]
    [Tooltip("Yellow: แรงกระเด็นที่ผลักศัตรูออกห่างจากผู้เล่น")]
    public float yellowKnockbackForce = 25f;

    protected Transform cachedPlayerTransform;

    public override void Activate(Transform playerTransform)
    {
        cachedPlayerTransform = playerTransform;
        base.Activate(playerTransform);
    }

    protected override void OnHit(Collision collision)
    {
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        int randomEffect = Random.Range(0, 4);

        switch (randomEffect)
        {
            case 0: // Red - Burst DMG
                if (enemy != null) enemy.ApplyDamage(burstDamage);
                Debug.Log("[Impressionism] สีแดง! Burst DMG!");
                break;

            case 1: // Blue - Slow
                if (enemy != null && enemy.Agent != null)
                {
                    var slow = enemy.GetComponent<SlowEffect>();
                    if (slow != null) slow.RefreshSlow(slowPercent, slowDuration);
                    else enemy.gameObject.AddComponent<SlowEffect>().Setup(enemy.Agent, slowPercent, slowDuration);
                }
                Debug.Log("[Impressionism] สีน้ำเงิน! Slow!");
                break;

            case 2: // Green - Heal player
                if (cachedPlayerTransform != null)
                {
                    var ph = cachedPlayerTransform.GetComponent<PlayerHealth>();
                    if (ph != null) ph.Heal(healAmount);
                }
                Debug.Log("[Impressionism] สีเขียว! Heal ผู้เล่น!");
                break;

            case 3: // Yellow - Forward knockback (push away from player)
                if (enemy != null && cachedPlayerTransform != null)
                {
                    IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
                    if (knockable != null)
                    {
                        Vector3 dir = collision.transform.position - cachedPlayerTransform.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            knockable.ApplyKnockback(dir.normalized * yellowKnockbackForce, 0.4f);
                        }
                    }
                }
                Debug.Log("[Impressionism] สีเหลือง! Knockback!");
                break;
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
