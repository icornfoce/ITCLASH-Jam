using UnityEngine;
using ITCLASH.Enemies;

/// <summary>
/// ลูกบอลของ Impressionism — แต่ละลูกแทน 1 สี (Red / Blue / Green / Yellow)
/// แดง   = Burst DMG
/// น้ำเงิน = Slow ศัตรู
/// เขียว  = Heal ผู้เล่น
/// เหลือง = Knockback ศัตรู
/// </summary>
public class ImpressionismBall : MonoBehaviour
{
    public enum BallType { Red, Blue, Green, Yellow }

    [Header("─── Ball Identity ───")]
    [Tooltip("ชนิดของลูกบอล (กำหนดเอฟเฟกต์ตอนชน)")]
    public BallType ballType;

    [Header("─── Effect Settings ───")]
    [Tooltip("Red: ความเสียหายระเบิด")]
    public float burstDamage = 50f;

    [Tooltip("Blue: ลดความเร็วกี่ % (0=หยุดนิ่ง, 1=ปกติ)")]
    [Range(0f, 1f)] public float slowPercent = 0.6f;

    [Tooltip("Blue: ระยะเวลาชะลอ (วินาที)")]
    public float slowDuration = 4f;

    [Tooltip("Green: HP ที่ฟื้นฟูให้ผู้เล่น")]
    public float healAmount = 30f;

    [Tooltip("Yellow: แรงผลักศัตรู")]
    public float knockbackForce = 25f;

    [Tooltip("Yellow: ระยะเวลาที่ถูกผลัก")]
    public float knockbackDuration = 0.4f;

    [Header("─── Lifetime / Visual ───")]
    [Tooltip("ทำลายลูกบอลหลังกี่วินาที (ถ้ายังไม่ชนอะไร)")]
    public float lifetime = 5f;

    [Tooltip("สีหลักของลูกบอล (ใช้กับ MaterialPropertyBlock + Light)")]
    public Color tintColor = Color.white;

    [Tooltip("ตัวคูณความเข้มของ emission")]
    public float emissionMultiplier = 3f;

    [Tooltip("ความสว่างของแสงบนลูกบอล (0 = ไม่ใส่ Light)")]
    public float lightIntensity = 4f;

    [Tooltip("รัศมีแสงของลูกบอล")]
    public float lightRange = 6f;

    [Header("─── VFX & Audio ───")]
    public GameObject hitVFXPrefab;
    public AudioClip hitSFX;

    private Rigidbody rb;
    private Transform shooterRoot;
    private Transform cachedPlayer;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        ApplyVisual();
    }

    /// <summary>
    /// เรียกจาก ImpressionismSkill หลังสร้างลูกบอลเสร็จ
    /// </summary>
    public void Launch(Transform playerTransform, Vector3 direction, float speed)
    {
        cachedPlayer = playerTransform;
        shooterRoot = (playerTransform != null) ? playerTransform.root : null;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearVelocity = direction.normalized * speed;

        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void ApplyVisual()
    {
        Color glow = tintColor * emissionMultiplier;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", tintColor);
            mpb.SetColor("_Color", tintColor);
            mpb.SetColor("_EmissionColor", glow);
            r.SetPropertyBlock(mpb);
        }

        if (lightIntensity > 0f && GetComponent<Light>() == null)
        {
            Light l = gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = tintColor;
            l.intensity = lightIntensity;
            l.range = lightRange;
            l.shadows = LightShadows.None;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (shooterRoot != null && collision.transform.root == shooterRoot) return;

        hasHit = true;

        Vector3 hitPoint = (collision.contactCount > 0)
            ? collision.GetContact(0).point
            : transform.position;

        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        ApplyEffect(enemy, hitPoint);

        if (hitVFXPrefab != null)
            Destroy(Instantiate(hitVFXPrefab, hitPoint, Quaternion.identity), 2f);
        if (hitSFX != null)
            AudioSource.PlayClipAtPoint(hitSFX, hitPoint);

        Destroy(gameObject);
    }

    private void ApplyEffect(EnemyController enemy, Vector3 hitPoint)
    {
        switch (ballType)
        {
            case BallType.Red:
                if (enemy != null) enemy.ApplyDamage(burstDamage);
                Debug.Log($"[ImpressionismBall] RED → DMG {burstDamage}");
                break;

            case BallType.Blue:
                if (enemy != null && enemy.Agent != null)
                {
                    var slow = enemy.GetComponent<SlowEffect>();
                    if (slow != null) slow.RefreshSlow(slowPercent, slowDuration);
                    else enemy.gameObject.AddComponent<SlowEffect>().Setup(enemy.Agent, slowPercent, slowDuration);
                }
                Debug.Log($"[ImpressionismBall] BLUE → Slow {slowPercent * 100f:F0}% / {slowDuration}s");
                break;

            case BallType.Green:
                if (cachedPlayer != null)
                {
                    PlayerHealth ph = cachedPlayer.GetComponent<PlayerHealth>();
                    if (ph == null) ph = cachedPlayer.GetComponentInParent<PlayerHealth>();
                    if (ph != null) ph.Heal(healAmount);
                }
                Debug.Log($"[ImpressionismBall] GREEN → Heal {healAmount}");
                break;

            case BallType.Yellow:
                if (enemy != null && cachedPlayer != null)
                {
                    IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
                    if (knockable != null)
                    {
                        Vector3 dir = enemy.transform.position - cachedPlayer.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude > 0.0001f)
                            knockable.ApplyKnockback(dir.normalized * knockbackForce, knockbackDuration);
                    }
                }
                Debug.Log($"[ImpressionismBall] YELLOW → Knockback {knockbackForce}");
                break;
        }
    }
}
