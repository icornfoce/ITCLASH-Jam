using UnityEngine;
using System.Collections;

public class LuminantSkill : BaseBuffSkill
{
    [Header("Luminant Settings")]
    public GameObject firePuddlePrefab;
    public float dropInterval = 0.5f;

    private bool isWalking = false;

    protected override void ApplyBuff(Transform playerTransform)
    {
        isWalking = true;
        StartCoroutine(DropFireRoutine(playerTransform));
        Debug.Log("[LuminantSkill] เริ่มทิ้งรอยไฟตามทางเดิน!");
    }

    private IEnumerator DropFireRoutine(Transform playerTransform)
    {
        while (isWalking)
        {
            if (firePuddlePrefab != null)
            {
                // เสกแอ่งไฟลงพื้น
                GameObject puddle = Instantiate(firePuddlePrefab, playerTransform.position, Quaternion.identity);
                // ตั้งเวลาลบแอ่งไฟ
                Destroy(puddle, 5f);
            }
            yield return new WaitForSeconds(dropInterval);
        }
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        isWalking = false;
        Debug.Log("[LuminantSkill] หยุดทิ้งรอยไฟ");
    }
}
