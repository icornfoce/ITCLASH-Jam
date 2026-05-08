using UnityEngine;
using ITCLASH.Enemies;

public class ImpressionismSkill : BaseProjectileSkill
{
    [Header("Impressionism Settings")]
    public float burstDamage = 50f;
    public float healAmount = 30f;

    protected override void OnHit(Collision collision)
    {
        int randomEffect = Random.Range(0, 3); // 0, 1, 2

        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            if (randomEffect == 0)
            {
                // 0 = สีแดง (DMG Burst)
                enemy.ApplyDamage(burstDamage);
                Debug.Log("[Impressionism] สีแดง! DMG Burst!");
            }
            else if (randomEffect == 1)
            {
                // 1 = สีน้ำเงิน (Slow)
                // enemy.ApplySlow(0.2f, 5f);
                Debug.Log("[Impressionism] สีน้ำเงิน! Slow!");
            }
        }

        if (randomEffect == 2)
        {
            // 2 = สีเขียว (Heal ผู้เล่น)
            // PlayerHealth.Instance.Heal(healAmount);
            Debug.Log("[Impressionism] สีเขียว! Heal ผู้เล่น!");
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
