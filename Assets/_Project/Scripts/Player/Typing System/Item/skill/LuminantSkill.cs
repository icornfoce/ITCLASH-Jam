using UnityEngine;
using System.Collections;

public class LuminantSkill : BaseBuffSkill
{
    [Header("─── Luminant Settings ───")]
    [Tooltip("พรีแฟบแอ่งไฟที่จะถูกวางลงบนพื้น")]
    public GameObject firePuddlePrefab;
    
    [Tooltip("ความถี่ในการวางแอ่งไฟแต่ละรอบ (วินาที)")]
    public float dropInterval = 0.35f;
    
    [Tooltip("ระยะเวลาที่แอ่งไฟอยู่บนพื้น (วินาที)")]
    public float puddleLifetime = 8f;

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
                GameObject puddle = Instantiate(firePuddlePrefab, playerTransform.position, Quaternion.identity);
                Destroy(puddle, puddleLifetime);
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
