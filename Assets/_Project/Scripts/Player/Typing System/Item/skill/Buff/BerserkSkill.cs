using UnityEngine;
using System.Collections;

public class BerserkSkill : BaseBuffSkill
{
    [Header("Berserk Settings")]
    public float maxDamageMultiplier = 3f;

    // ต้องอ้างอิงถึง PlayerHealth เพื่อดูเลือดที่หายไป
    // สมมติว่ามี PlayerHealth
    // private PlayerHealth pHealth;

    protected override void ApplyBuff(Transform playerTransform)
    {
        Debug.Log("[BerserkSkill] โหมดบ้าคลั่ง! ยิ่งเลือดน้อยยิ่งตีแรง!");
        // pHealth = playerTransform.GetComponent<PlayerHealth>();
        StartCoroutine(BerserkRoutine(playerTransform));
    }

    private IEnumerator BerserkRoutine(Transform playerTransform)
    {
        while (true)
        {
            // คำนวณดาเมจแบบ Real-time ตามเลือด
            // float hpPercent = pHealth.currentHP / pHealth.maxHP;
            // float dmgMulti = Mathf.Lerp(maxDamageMultiplier, 1f, hpPercent);
            // PlayerCombat.Instance.SetDamageMultiplier(dmgMulti);

            yield return new WaitForSeconds(0.5f);
        }
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        StopAllCoroutines();
        // PlayerCombat.Instance.SetDamageMultiplier(1f);
        Debug.Log("[BerserkSkill] ออกจากโหมดบ้าคลั่ง");
    }
}
