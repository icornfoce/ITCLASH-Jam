using UnityEngine;

public class WindowSkill : BaseBuffSkill
{
    [Header("Window Warp Settings")]
    public float warpDistance = 15f;
    public GameObject warpVFX;

    protected override void ApplyBuff(Transform playerTransform)
    {
        Debug.Log($"[WindowSkill] วาร์ปหนีไปข้างหน้า {warpDistance} เมตร!");

        if (warpVFX != null) Instantiate(warpVFX, playerTransform.position, Quaternion.identity);

        // เช็คว่าชนกำแพงไหม
        Vector3 targetPos = playerTransform.position + playerTransform.forward * warpDistance;
        if (Physics.Raycast(playerTransform.position, playerTransform.forward, out RaycastHit hit, warpDistance))
        {
            targetPos = hit.point - playerTransform.forward * 1f; // วาร์ปไปแคะกำแพง ไม่ให้ทะลุ
        }

        // วาร์ปตัวละคร
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        playerTransform.position = targetPos;
        
        if (cc != null) cc.enabled = true;

        if (warpVFX != null) Instantiate(warpVFX, playerTransform.position, Quaternion.identity);
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        // สกิลนี้ทำงานครั้งเดียวจบ
    }
}
