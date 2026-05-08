using UnityEngine;

/// <summary>
/// FirstPersonController — จัดการการเคลื่อนที่ของ Player
///
/// ระบบ Health ถูกย้ายออกไปอยู่ใน PlayerHealth.cs ทั้งหมด
/// Script นี้แค่ Subscribe OnDeath Event เพื่อหยุดการเคลื่อนที่เมื่อตาย
///
/// วิธีใช้:
///   - ติด Script นี้บน Player
///   - ลาก PlayerHealth มาใส่ช่อง playerHealth (หรือปล่อยว่าง → หาอัตโนมัติ)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("Health Reference")]
    [Tooltip("ลาก PlayerHealth มาใส่ (ถ้าว่าง = หาอัตโนมัติจาก GameObject เดียวกัน)")]
    [SerializeField] private PlayerHealth playerHealth;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isDead = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // หา PlayerHealth อัตโนมัติถ้าไม่ได้ลากมาใส่
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        // Subscribe OnDeath เพื่อหยุดเคลื่อนที่เมื่อตาย
        if (playerHealth != null)
            playerHealth.OnDeath.AddListener(OnPlayerDeath);
        else
            Debug.LogWarning("[FirstPersonController] ไม่พบ PlayerHealth! ระบบ Health จะไม่ทำงาน");
    }

    void OnDestroy()
    {
        // ถอด Listener เมื่อถูกทำลาย (ป้องกัน Memory Leak)
        if (playerHealth != null)
            playerHealth.OnDeath.RemoveListener(OnPlayerDeath);
    }

    void Update()
    {
        if (isDead) return; // หยุดทุกอย่างเมื่อตาย
        HandleMovement();
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * walkSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// เรียกใช้ผ่าน PlayerHealth.OnDeath Event
    /// หยุดการเคลื่อนที่ทั้งหมด
    /// </summary>
    private void OnPlayerDeath()
    {
        isDead = true;
        Debug.Log("[FirstPersonController] Player ตายแล้ว — หยุดการเคลื่อนที่");
    }

    // ─── Public API ──────────────────────────────────────────────

    /// <summary>สั่งให้ Player รับ Damage (ส่งต่อไปยัง PlayerHealth)</summary>
    public void TakeDamage(float damage)
    {
        if (playerHealth != null)
            playerHealth.TakeDamage(damage);
    }

    /// <summary>สั่งให้ Player ฮีล (ส่งต่อไปยัง PlayerHealth)</summary>
    public void Heal(float amount)
    {
        if (playerHealth != null)
            playerHealth.Heal(amount);
    }

    /// <summary>ดึงค่า Health ปัจจุบัน (Normalized 0-1) จาก PlayerHealth</summary>
    public float GetHealthNormalized()
    {
        return playerHealth != null ? playerHealth.GetHealthNormalized() : 0f;
    }
}

