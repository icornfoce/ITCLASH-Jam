using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ใส่ RawImage หลอดเลือด (ระบบจะยึดติดฝั่งซ้ายให้อัตโนมัติ)")]
    public RawImage healthRawImage;
    
    [Tooltip("ใส่ Image หลอดเลือดสีฟ้า")]
    public Image healthFillImage; 
    public Slider healthSlider; 
    public TextMeshProUGUI healthText; 

    [Header("Animation Settings")]
    [Tooltip("ความเร็วในการเลื่อนหลอดเลือด (ยิ่งเยอะยิ่งเร็ว)")]
    public float animationSpeed = 5f;

    [Header("Pulse Settings (Low Health)")]
    [Tooltip("เลือดต่ำกว่ากี่ % ถึงจะเริ่มกระพริบ")]
    public float pulseThreshold = 0.3f;
    [Tooltip("ความเร็วในการกระพริบ")]
    public float pulseSpeed = 8f;
    [Tooltip("ความแรงของการกระพริบ (ปรับขนาด/สี)")]
    public float pulseAmount = 0.15f;
    public Color lowHealthColor = Color.red;

    private float maxRawWidth;
    private Rect originalUV;
    private Vector2 originalAnchoredPosition;
    private Color originalFillColor;
    private Vector3 originalScale;

    private float targetHealth = 1f;
    private float currentVisualHealth = 1f;

    void Awake()
    {
        if (healthRawImage != null)
        {
            maxRawWidth = healthRawImage.rectTransform.sizeDelta.x;
            originalUV = healthRawImage.uvRect;
            originalAnchoredPosition = healthRawImage.rectTransform.anchoredPosition;
        }

        if (healthFillImage != null)
        {
            originalFillColor = healthFillImage.color;
            originalScale = healthFillImage.rectTransform.localScale;
        }
    }

    public void UpdateHealthBar(float normalizedHealth)
    {
        // ตั้งเป้าหมายให้หลอดเลือดรู้ว่าต้องลดไปถึงจุดไหน
        targetHealth = normalizedHealth;
    }

    void Update()
    {
        // ทำแอนิเมชันให้หลอดเลือดค่อยๆ ลดลงอย่างนุ่มนวล (Lerp)
        if (Mathf.Abs(currentVisualHealth - targetHealth) > 0.001f)
        {
            currentVisualHealth = Mathf.Lerp(currentVisualHealth, targetHealth, Time.deltaTime * animationSpeed);
            ApplyVisualUpdate(currentVisualHealth);
        }
        else if (currentVisualHealth != targetHealth)
        {
            // ปัดเศษให้เท่ากันพอดีเมื่อใกล้เคียงมากๆ
            currentVisualHealth = targetHealth;
            ApplyVisualUpdate(currentVisualHealth);
        }

        // เพิ่มระบบกระพริบเมื่อเลือดต่ำ
        HandleLowHealthPulse();
    }

    private void HandleLowHealthPulse()
    {
        if (currentVisualHealth <= pulseThreshold && currentVisualHealth > 0)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; // 0 to 1
            
            // 1. ปรับสีและ Scale ของหลอดเลือด
            if (healthFillImage != null)
            {
                healthFillImage.color = Color.Lerp(originalFillColor, lowHealthColor, pulse);
                healthFillImage.rectTransform.localScale = originalScale * (1f + pulse * pulseAmount);
            }
            
            // 2. ปรับ Scale ของ Text ให้กระพริบตามไปด้วย
            if (healthText != null)
            {
                healthText.transform.localScale = Vector3.one * (1f + pulse * pulseAmount);
            }
        }
        else
        {
            // คืนค่าปกติถ้าเลือดสูงกว่าเกณฑ์
            if (healthFillImage != null)
            {
                healthFillImage.color = originalFillColor;
                healthFillImage.rectTransform.localScale = originalScale;
            }
            if (healthText != null)
            {
                healthText.transform.localScale = Vector3.one;
            }
        }
    }

    private void ApplyVisualUpdate(float healthValue)
    {
        // 1. อัปเดต RawImage
        if (healthRawImage != null)
        {
            // ปรับความกว้างตามเลือด (ทำให้หลอดสั้นลง)
            float newWidth = maxRawWidth * healthValue;
            healthRawImage.rectTransform.sizeDelta = new Vector2(newWidth, healthRawImage.rectTransform.sizeDelta.y);
            
            // ปรับ uvRect ตามสัดส่วนเลือด (เพื่อให้ภาพไม่โดนบีบเบี้ยว)
            // ผลลัพธ์คือภาพจะดูเหมือนโดน "ตัด" จากฝั่งขวาออกไปเรื่อยๆ
            healthRawImage.uvRect = new Rect(0, 0, healthValue, 1);
            
            // รักษาตำแหน่งเดิมไว้ (อิงตาม Pivot ที่ควรจะเป็น 0 เพื่อให้ลดจากขวาไปซ้าย)
            healthRawImage.rectTransform.anchoredPosition = originalAnchoredPosition;
        }

        // 2. อัปเดต Image Fill
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = healthValue;
        }

        // 3. อัปเดต Slider
        if (healthSlider != null)
        {
            healthSlider.value = healthValue;
        }

        // 4. อัปเดต Text
        if (healthText != null)
        {
            int hpNumber = Mathf.RoundToInt(healthValue * 100f);
            healthText.text = hpNumber.ToString();
        }
    }
}
