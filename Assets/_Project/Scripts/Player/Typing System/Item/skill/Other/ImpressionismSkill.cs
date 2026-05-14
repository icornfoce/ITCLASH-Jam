using UnityEngine;

/// <summary>
/// ImpressionismSkill — สุ่มยิงลูกบอล 1 ใน 4 สีต่อ 1 cast
///   แดง   = Burst DMG
///   น้ำเงิน = Slow
///   เขียว  = Heal ผู้เล่น
///   เหลือง = Knockback
///
/// ตัวสกิลนี้เป็นแค่ "dispatcher" — ลูกบอลแต่ละสีเป็น Prefab แยกของตัวเอง
/// (ดู ImpressionismBall.cs)
/// </summary>
public class ImpressionismSkill : BaseItemSkill
{
    [Header("─── Ball Prefabs (ใส่ตามลำดับ Red, Blue, Green, Yellow) ───")]
    [Tooltip("Prefab ของลูกบอลทั้ง 4 สี — สุ่ม 1 ลูกทุกครั้งที่ใช้สกิล")]
    public GameObject[] ballPrefabs = new GameObject[4];

    [Header("─── Launch Settings ───")]
    [Tooltip("จุดที่จะ spawn ลูกบอล (ถ้าเว้นว่างจะใช้ตำแหน่งของสกิลเอง)")]
    public Transform shootPoint;

    [Tooltip("ความเร็วลูกบอล (m/s)")]
    public float ballSpeed = 25f;

    [Tooltip("ระยะเล็งสูงสุด (เมตร)")]
    public float maxRange = 50f;

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);

        if (ballPrefabs == null || ballPrefabs.Length == 0)
        {
            Debug.LogWarning("[ImpressionismSkill] ไม่มี ball prefab ให้ใช้!");
            CleanupSelf();
            return;
        }

        int idx = Random.Range(0, ballPrefabs.Length);
        GameObject prefab = ballPrefabs[idx];
        if (prefab == null)
        {
            Debug.LogWarning($"[ImpressionismSkill] ball prefab index {idx} เป็น null");
            CleanupSelf();
            return;
        }

        Vector3 spawnPos = transform.position;

        // ค้นหา ShootPoint ในตัวผู้เล่นอัตโนมัติ (เพราะ Prefab ลากใส่ไม่ได้)
        if (shootPoint != null) 
        {
            spawnPos = shootPoint.position;
        }
        else if (playerTransform != null)
        {
            // หา object ลูกที่ชื่อ "ShootPoint"
            Transform[] children = playerTransform.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name == "ShootPoint")
                {
                    spawnPos = child.position;
                    break;
                }
            }
        }

        Vector3 dir = CalculateAimDirection(playerTransform, spawnPos);

        GameObject ballObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(dir));
        ImpressionismBall ball = ballObj.GetComponent<ImpressionismBall>();
        if (ball != null)
        {
            ball.Launch(playerTransform, dir, ballSpeed);
        }
        else
        {
            Rigidbody rb = ballObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = dir * ballSpeed;
            }
        }

        Debug.Log($"[ImpressionismSkill] สุ่มได้ลูก: {prefab.name}");

        CleanupSelf();
    }

    private void CleanupSelf()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
        Destroy(gameObject, 0.1f);
    }

    private Vector3 CalculateAimDirection(Transform playerTransform, Vector3 spawnPos)
    {
        Vector3 targetPoint;

        if (TargetPosition.HasValue)
        {
            targetPoint = TargetPosition.Value;
        }
        else
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                targetPoint = ray.GetPoint(maxRange);

                RaycastHit[] hits = Physics.RaycastAll(ray, maxRange);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider.transform.root == playerTransform.root) continue;
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                    targetPoint = hit.point;
                    break;
                }
            }
            else
            {
                targetPoint = playerTransform.position + playerTransform.forward * maxRange;
            }
        }

        Vector3 dir = (targetPoint - spawnPos).normalized;

        Camera mainCam = Camera.main;
        if (!TargetPosition.HasValue && mainCam != null && Vector3.Dot(dir, mainCam.transform.forward) < 0f)
        {
            dir = mainCam.transform.forward;
        }

        return dir.normalized;
    }
}
