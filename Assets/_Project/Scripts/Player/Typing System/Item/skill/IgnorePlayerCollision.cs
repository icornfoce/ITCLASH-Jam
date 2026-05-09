using UnityEngine;

public class IgnorePlayerCollision : MonoBehaviour
{
    private void Start()
    {
        // 1. ค้นหาตัวผู้เล่นในฉาก (อ้างอิงจาก Tag "Player")
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 2. ดึง Collider ของผู้เล่น (CharacterController ถือว่าเป็น Collider ชนิดนึง)
            Collider playerCollider = player.GetComponent<Collider>();
            
            // 3. ดึง Collider ของวัตถุนี้ (ที่จำเป็นต้องมีไว้ให้กระสุนหรือศัตรูชน)
            Collider myCollider = GetComponent<Collider>();

            if (playerCollider != null && myCollider != null)
            {
                // 4. สั่งเอนจิ้น Unity ว่า "ให้เพิกเฉยการชนกันระหว่าง 2 สิ่งนี้"
                Physics.IgnoreCollision(playerCollider, myCollider, true);
            }
        }
    }
}
