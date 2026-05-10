using UnityEngine;

public abstract class BaseProjectileSkill : BaseItemSkill
{
    [Header("─── Projectile Settings ───")]
    [Tooltip("แรงยิงออกไป (m/s)")]
    public float launchForce = 20f;
    [Tooltip("แรง Upward ตอนยิง (เพิ่มเพื่อให้โค้งขึ้นเล็กน้อย)")]
    public float upwardForce = 2f;
    [Tooltip("ทำลาย Projectile หลังจากกี่วินาที (0 = ไม่ทำลาย)")]
    public float lifetime = 5f;

    [Header("─── VFX & Audio ───")]
    public GameObject hitVFXPrefab;
    public AudioClip hitSFX;

    protected Rigidbody rb;
    protected bool hasHit = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true; // รอ Activate ก่อนค่อยเปิด
    }

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);
        transform.SetParent(null);

        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 shootDir = CalculateAimDirection(playerTransform);
        rb.AddForce(shootDir * launchForce, ForceMode.VelocityChange);

        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// ระบบคำนวณเป้าหมาย (เหมือน Cube) ให้พุ่งเข้า Crosshair ตรงๆ
    /// </summary>
    protected virtual Vector3 CalculateAimDirection(Transform playerTransform)
    {
        Vector3 targetPoint = playerTransform.position + playerTransform.forward * 100f;

        if (TargetPosition.HasValue)
        {
            targetPoint = TargetPosition.Value;
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                targetPoint = ray.GetPoint(100f);

                RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.collider.transform.root == playerTransform.root) continue;
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                    
                    targetPoint = hit.point;
                    break;
                }
            }
        }

        Vector3 shootDir = (targetPoint - transform.position).normalized;
        Camera cam = Camera.main;
        if (!TargetPosition.HasValue && cam != null && Vector3.Dot(shootDir, cam.transform.forward) < 0f)
        {
            shootDir = cam.transform.forward;
        }

        shootDir += Vector3.up * (upwardForce / launchForce);
        return shootDir.normalized;
    }

    /// <summary>
    /// คลาสลูกจะต้อง Override ฟังก์ชันนี้ไปจัดการพฤติกรรมตอนชนเป้าหมาย
    /// </summary>
    protected abstract void OnHit(Collision collision);

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;
        
        OnHit(collision);
    }

    protected void SpawnHitVFX(Vector3 pos)
    {
        if (hitVFXPrefab != null) Destroy(Instantiate(hitVFXPrefab, pos, Quaternion.identity), 2f);
    }

    protected void PlayHitSFX(Vector3 pos)
    {
        if (hitSFX != null) AudioSource.PlayClipAtPoint(hitSFX, pos);
    }
}
