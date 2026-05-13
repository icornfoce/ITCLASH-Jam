using UnityEngine;

[CreateAssetMenu(fileName = "NewHealEffect", menuName = "Rouge-like/Code for Buff/Heal")]
public class HealEffect : UpgradeEffect
{
    public int healAmount = 25;

    public override void Apply(GameObject player)
    {
        // พยายามหา PlayerController เพื่อเพิ่มเลือด
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            // ถ้าคุณมีฟังก์ชัน Heal ใน PlayerController ให้เอา Comment ออก
            // pc.currentHealth = Mathf.Min(pc.maxHealth, pc.currentHealth + healAmount);
            Debug.Log($"<color=green>[Effect]</color> Healed {player.name} for {healAmount} HP!");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[Effect]</color> PlayerController not found on {player.name}, cannot heal.");
        }
    }
}
