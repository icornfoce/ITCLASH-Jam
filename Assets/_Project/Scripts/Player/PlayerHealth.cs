using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, ITCLASH.Enemies.IDamageable
{
    // IDamageable Implementation
    public Transform Transform => transform;
    public float HealthPercent => GetHealthNormalized();
    public bool IsAlive => !isDead;
    public void ApplyDamage(float amount) => TakeDamage(amount);
    // Heal(float) is already implemented below

    [Header("Health Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    public bool isGodMode = false; // ติ๊กเพื่อเป็นอมตะ


    [Header("Events")]
    // Event that passes the normalized health (0.0 to 1.0) useful for UI health bars
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    [Header("Blood UI Settings")]
    [Tooltip("ลาก Q_Vignette_Base (ที่ตั้งค่าสีแดงไว้) มาใส่ช่องนี้")]
    public Q_Vignette_Base damageVignette;
    public Color bloodColor = Color.red; // สีของเลือดที่ต้องการ
    [Range(0f, 1f)] public float bloodStartThreshold = 0.5f; // เลือดต่ำกว่ากี่ % ถึงเริ่มโชว์ขอบแดง
    public float bloodStartScale = 0.25f; // ขนาดขอบจอเริ่มต้น
    public float bloodMaxScale = 0.6f;   // ขนาดขอบจอสูงสุด (ตอนเลือด 0)
    public float maxAlpha = 1.0f;        // ความเข้มสูงสุดของเลือด
    
    [Header("Pulse Settings")]
    [Tooltip("ความเร็วในการกระพริบ (ยิ่งมากยิ่งเร็ว)")]
    public float pulseSpeed = 4f;
    [Tooltip("ความแรงของการกระพริบ (0 = ไม่กระพริบ, 1 = หายไปเลยแล้วกลับมาเข้มสุด)")]
    [Range(0f, 1f)] public float pulseAmount = 0.3f;

    [Header("Death & Quit")]
    public CanvasGroup fadeGroup; // UI สีดำที่จะให้เฟดตอนตาย
    public float deathFadeDuration = 2f;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(GetHealthNormalized());

        // ตั้งค่าเริ่มต้นของ UI เลือด
        if (damageVignette != null)
        {
            UpdateBloodUI();
        }
    }

    void Update()
    {
        if (isDead) return;

        if (damageVignette != null)
        {
            UpdateBloodUI();
        }
    }

    private void UpdateBloodUI()
    {
        float hNormalized = GetHealthNormalized();
        
        // คำนวณความเข้ม (Intensity) จาก 0 ถึง 1 ตามเลือดที่เหลือ
        float intensity = Mathf.InverseLerp(bloodStartThreshold, 0f, hNormalized);
        
        // --- เพิ่มระบบกระพริบ (Pulsing) ---
        if (intensity > 0)
        {
            // ใช้ Sine wave ในการสร้างค่ากระพริบ 0 ถึง 1
            // ยิ่งเลือดน้อย (intensity มาก) การกระพริบอาจจะเร็วขึ้นตามความเหมาะสม (optional)
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            
            // ปรับความเข้มตาม pulse
            // จะเป็นการแกว่งระหว่าง intensity เดิม กับ intensity * (1 - pulseAmount)
            intensity = intensity * (1f - (pulse * pulseAmount));
        }
        // --------------------------------

        // 1. จัดการความโปร่งใส (Alpha)
        float alpha = intensity * maxAlpha;
        SetVignetteAlpha(alpha);

        // 2. จัดการขนาด (Scale)
        float currentScale = Mathf.Lerp(bloodStartScale, bloodMaxScale, intensity);
        damageVignette.SetVignetteMainScale(currentScale);
        damageVignette.SetVignetteSkyScale(currentScale);

        // เปิด/ปิด GameObject ตามความเหมาะสม
        if (alpha > 0 && !damageVignette.gameObject.activeSelf)
        {
            damageVignette.gameObject.SetActive(true);
        }
        else if (alpha <= 0 && damageVignette.gameObject.activeSelf)
        {
            damageVignette.gameObject.SetActive(false);
        }
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (damageVignette == null || damageVignette.cornerImages == null) return;
        foreach (var img in damageVignette.cornerImages)
        {
            if (img != null)
            {
                Color c = bloodColor;
                c.a = alpha;
                img.color = c;
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead || isGodMode) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke(GetHealthNormalized());

        Debug.Log($"<color=red>[PLAYER DAMAGE]</color> Took {damageAmount} damage! Current HP: {currentHealth} / {maxHealth} ({GetHealthNormalized() * 100f:F1}%)");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(GetHealthNormalized());
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died.");
        OnDeath?.Invoke();
        
        // Disable movement
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // เริ่มขั้นตอนจบเกม (เฟดดำแล้วปิดเกม)
        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        if (fadeGroup != null)
        {
            fadeGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < deathFadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = Mathf.Clamp01(elapsed / deathFadeDuration);
                yield return null;
            }
        }

        Debug.Log("Death fade complete. Quitting game...");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public float GetHealthNormalized()
    {
        return currentHealth / maxHealth;
    }
}
