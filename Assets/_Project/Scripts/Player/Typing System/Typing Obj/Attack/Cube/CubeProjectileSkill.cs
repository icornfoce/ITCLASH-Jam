using UnityEngine;
using ITCLASH.Enemies;

/// <summary>
/// CubeProjectileSkill — Skill สำหรับไอเทม "cube"
///
/// วิธีใช้:
///   1. สร้าง Prefab ของ Cube (GameObject ที่มี Rigidbody + Collider)
///   2. แปะ Script นี้ลงบน Prefab นั้น
///   3. ลาก Prefab ใส่ช่อง itemSkill ใน ItemInfo ของ "cube" ใน ItemData
///
/// การทำงาน:
///   - TypingSystem สร้าง Prefab ลอยอยู่หน้า Player
///   - เมื่อผู้เล่นกด Right-Click → TypingSystem.ReleaseItem() เรียก Activate()
///   - Cube จะถูกยิงออกไปในทิศที่ Player หันหน้า
///   - ถ้าชนศัตรู → ApplyDamage + ApplyKnockback
/// </summary>
public class CubeProjectileSkill : BaseItemSkill
{
    // ============================================================
    // INSPECTOR FIELDS
    // ============================================================

    [Header("─── Projectile Settings ───")]
    [Tooltip("แรงยิงออกไป (m/s)")]
    [SerializeField] private float launchForce = 20f;

    [Tooltip("แรง Upward ตอนยิง (เพิ่มเพื่อให้โค้งขึ้นเล็กน้อย)")]
    [SerializeField] private float upwardForce = 2f;

    [Tooltip("ทำลาย Projectile หลังจากกี่วินาที (0 = ไม่ทำลาย)")]
    [SerializeField] private float lifetime = 5f;

    // ─────────────────────────────────────────────────────────

    [Header("─── Damage Settings ───")]
    [Tooltip("ความเสียหายที่สร้าง")]
    [SerializeField] private float damage = 30f;

    // ─────────────────────────────────────────────────────────

    [Header("─── Knockback Settings ───")]
    [Tooltip("ขนาดแรงกระเด็น")]
    [SerializeField] private float knockbackForce = 15f;

    [Tooltip("นานแค่ไหนที่ศัตรูจะถูกดันออก (วินาที)")]
    [SerializeField] private float knockbackDuration = 0.4f;

    // ─────────────────────────────────────────────────────────

    [Header("─── VFX ───")]
    [Tooltip("VFX ตอนชนศัตรู (ถ้าไม่มีก็ปล่อยว่าง)")]
    [SerializeField] private GameObject hitVFXPrefab;

    // ─────────────────────────────────────────────────────────

    [Header("─── Audio ───")]
    [SerializeField] private AudioClip hitSFX;

    // ============================================================
    // PRIVATE STATE
    // ============================================================

    private Rigidbody rb;
    private bool hasHit = false;

    // ============================================================
    // LIFECYCLE
    // ============================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        // ยังไม่ยิง — รอ Activate()
        rb.isKinematic = true;
    }

    // ============================================================
    // ACTIVATE — เรียกจาก TypingSystem.PerformRelease()
    // ============================================================

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);

        // หลุดออกจาก Parent (TypingSystem สร้างให้เป็น child ของ Player)
        transform.SetParent(null);

        // เปิด Physics
        rb.isKinematic = false;
        rb.useGravity  = true;

        // ค้นหากล้องหลักเพื่อยิงเข้ากลางเป้า Crosshair
        Camera mainCam = Camera.main;
        Vector3 targetPoint;

        if (mainCam != null)
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            targetPoint = ray.GetPoint(100f); // ค่าเริ่มต้นถ้ายิงไม่โดนอะไร ให้พุ่งไป 100 เมตร

            // ยิง Raycast ทะลุทุกอย่าง แล้วหาจุดแรกที่ไม่ใช่ตัว Player และไม่ใช่ตัวไอเทมเอง
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // ข้ามการชนกับตัวผู้เล่น
                if (hit.collider.transform.root == playerTransform.root) continue;
                // ข้ามการชนกับไอเทมชิ้นนี้เอง
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;

                targetPoint = hit.point;
                break;
            }
        }
        else
        {
            // ถ้าไม่มีกล้อง ใช้หน้า Player ตามเดิม
            targetPoint = playerTransform.position + playerTransform.forward * 100f;
        }

        // คำนวณทิศทางจากตำแหน่งไอเทม ไปยังจุดเป้าหมาย
        Vector3 shootDir = (targetPoint - transform.position).normalized;
        
        // Failsafe: ถ้าจุดที่ชนอยู่ใกล้กว่าตัวไอเทม จนทำให้ทิศทางยิงพุ่งถอยหลัง ให้บังคับพุ่งไปข้างหน้าตรงๆ ตามกล้องเลย
        if (mainCam != null && Vector3.Dot(shootDir, mainCam.transform.forward) < 0f)
        {
            shootDir = mainCam.transform.forward;
        }

        // เพิ่มความโค้งขึ้นเล็กน้อย
        shootDir += Vector3.up * (upwardForce / launchForce);
        shootDir.Normalize();

        rb.AddForce(shootDir * launchForce, ForceMode.VelocityChange);

        Debug.Log($"[CubeProjectileSkill] 🚀 ยิง Cube! Force: {launchForce}, Dir: {shootDir}");

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    // ============================================================
    // COLLISION — ชนศัตรูแล้วสร้าง Damage + Knockback
    // ============================================================

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // ค้นหา EnemyController จาก object ที่ชน (รองรับทั้ง root และ child colliders)
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();

        if (enemy != null)
        {
            hasHit = true;

            // ── Damage ──
            enemy.ApplyDamage(damage);

            // ── Knockback ──
            Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
            knockDir.y = 0.3f; // เพิ่มความสูงเล็กน้อย

            IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
                knockable.ApplyKnockback(knockDir * knockbackForce, knockbackDuration);

            Debug.Log($"[CubeProjectileSkill] 💥 ชน '{collision.gameObject.name}' → DMG: {damage}, Knockback: {knockbackForce}");

            SpawnHitVFX(collision.contacts[0].point);
            PlayHitSFX();
            Destroy(gameObject);
        }
        else
        {
            // ชนพื้น/กำแพง
            SpawnHitVFX(collision.contacts[0].point);
            Destroy(gameObject, 0.5f);
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void SpawnHitVFX(Vector3 pos)
    {
        if (hitVFXPrefab != null)
            Destroy(Instantiate(hitVFXPrefab, pos, Quaternion.identity), 2f);
    }

    private void PlayHitSFX()
    {
        if (hitSFX != null)
            AudioSource.PlayClipAtPoint(hitSFX, transform.position);
    }
}
