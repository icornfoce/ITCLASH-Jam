using UnityEngine;
using System.Collections;

public class LuminantSkill : BaseBuffSkill
{
    [Header("─── Luminant Settings ───")]
    [Tooltip("พรีแฟบแอ่งไฟที่จะถูกวางลงบนพื้น (ถ้าไม่ใส่จะสร้าง Plane สีส้มแทน)")]
    public GameObject firePuddlePrefab;

    [Tooltip("ความถี่ในการวางแอ่งไฟแต่ละรอบ (วินาที)")]
    public float dropInterval = 0.35f;

    [Tooltip("ระยะเวลาที่แอ่งไฟอยู่บนพื้น (วินาที)")]
    public float puddleLifetime = 8f;

    [Header("─── Fire Damage ───")]
    [Tooltip("ดาเมจต่อติ๊กที่ศัตรูจะโดนเมื่อยืนบนไฟ")]
    public float fireDamagePerTick = 5f;

    [Tooltip("ความถี่ที่ไฟทำดาเมจ (วินาที)")]
    public float fireDamageInterval = 0.5f;

    [Tooltip("ขนาดของแอ่งไฟ (รัศมี)")]
    public float puddleRadius = 1.5f;

    [Header("─── Movement Check ───")]
    [Tooltip("ระยะเคลื่อนที่ขั้นต่ำถึงจะวางไฟ (ป้องกันวางซ้อนตอนยืนนิ่ง)")]
    public float minMoveDistance = 0.3f;

    private bool isActive = false;
    private Vector3 lastDropPosition;
    private Transform cachedRoot;

    protected override void ApplyBuff(Transform playerTransform)
    {
        // หา root ของผู้เล่น
        cachedRoot = playerTransform.root;

        isActive = true;
        lastDropPosition = playerTransform.position;
        StartCoroutine(DropFireRoutine(playerTransform));
        Debug.Log("[LuminantSkill] เริ่มทิ้งรอยไฟตามทางเดิน!");
    }

    private IEnumerator DropFireRoutine(Transform playerTransform)
    {
        // วางแอ่งแรกทันทีที่เริ่ม
        DropFire(playerTransform);

        while (isActive)
        {
            yield return new WaitForSeconds(dropInterval);

            if (!isActive) break;
            if (playerTransform == null) break;

            float distMoved = Vector3.Distance(playerTransform.position, lastDropPosition);
            if (distMoved >= minMoveDistance)
            {
                DropFire(playerTransform);
                lastDropPosition = playerTransform.position;
            }
        }
    }

    private void DropFire(Transform playerTransform)
    {
        // Raycast จากเหนือผู้เล่นลงไปหาพื้น (เริ่มสูงและระยะไกล เพื่อให้เจอพื้นแน่นอน)
        Vector3 origin = playerTransform.position + Vector3.up * 10f;
        bool foundGround = false;
        Vector3 spawnPos = Vector3.zero;

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 50f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // ข้ามตัวผู้เล่น
            if (cachedRoot != null && hit.collider.transform.root == cachedRoot) continue;
            // ข้ามศัตรู
            if (hit.collider.CompareTag("Enemy")) continue;
            // ข้าม trigger collider
            if (hit.collider.isTrigger) continue;

            spawnPos = hit.point + Vector3.up * 0.02f;
            foundGround = true;
            break;
        }

        // ถ้าไม่เจอพื้นเลย → ไม่วาง
        if (!foundGround)
        {
            Debug.Log("[LuminantSkill] หาพื้นไม่เจอ ข้ามรอบนี้");
            return;
        }

        // สร้าง prefab หรือ fallback plane
        GameObject puddle;
        if (firePuddlePrefab != null)
        {
            puddle = Instantiate(firePuddlePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Fallback — สร้าง Cylinder สีส้มแทน prefab
            puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.transform.position = spawnPos;
            puddle.transform.localScale = new Vector3(puddleRadius * 2f, 0.05f, puddleRadius * 2f);
            Renderer rend = puddle.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Standard"));
                rend.material.color = new Color(1f, 0.4f, 0f, 0.8f); // ส้มไฟ
                rend.material.SetFloat("_Mode", 3f);
                rend.material.EnableKeyword("_ALPHABLEND_ON");
            }
            Debug.Log("[LuminantSkill] ไม่มี firePuddlePrefab — ใช้ Fallback Cylinder แทน");
        }

        // เพิ่ม Collider Trigger ถ้ายังไม่มี
        Collider col = puddle.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = puddle.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = puddleRadius;
        }
        else
        {
            col.isTrigger = true;
        }

        // เพิ่ม FirePuddle script ทำดาเมจ
        FirePuddle fire = puddle.GetComponent<FirePuddle>();
        if (fire == null) fire = puddle.AddComponent<FirePuddle>();
        fire.damagePerTick = fireDamagePerTick;
        fire.damageInterval = fireDamageInterval;

        Destroy(puddle, puddleLifetime);

        Debug.Log($"[LuminantSkill] วางแอ่งไฟที่ {spawnPos}");
    }

    protected override void RemoveBuff(Transform playerTransform)
    {
        isActive = false;
        Debug.Log("[LuminantSkill] หยุดทิ้งรอยไฟ");
    }
}
