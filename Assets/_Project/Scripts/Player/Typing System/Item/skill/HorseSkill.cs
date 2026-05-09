using UnityEngine;
using System.Collections;
using ITCLASH.Enemies;

public class HorseSkill : BaseBuffSkill
{
    [Header("Horse Settings")]
    public float dashSpeed = 30f;
    public float dashDuration = 0.5f;
    public float damage = 40f;
    public float knockback = 20f;
    public LayerMask enemyLayer;

    protected override void ApplyBuff(Transform playerTransform)
    {
        Debug.Log("[HorseSkill] พุ่งชนศัตรูเหมือนม้า!");
        StartCoroutine(DashRoutine(playerTransform));
    }

    private IEnumerator DashRoutine(Transform playerTransform)
    {
        float timer = 0f;
        CharacterController cc = playerTransform.GetComponent<CharacterController>();

        while (timer < dashDuration)
        {
            if (cc != null)
            {
                cc.Move(playerTransform.forward * dashSpeed * Time.deltaTime);
            }
            else
            {
                playerTransform.position += playerTransform.forward * dashSpeed * Time.deltaTime;
            }

            // ชนศัตรูระหว่างทาง
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, 2f, enemyLayer);
            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    enemy.ApplyDamage(damage);
                    IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
                    if (knockable != null)
                    {
                        knockable.ApplyKnockback(playerTransform.forward * knockback, 0.3f);
                    }
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        Debug.Log("[HorseSkill] หยุดพุ่ง");
    }
}
