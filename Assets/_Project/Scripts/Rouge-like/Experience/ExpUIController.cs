using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ใส่ UI Slider สำหรับแสดงหลอด EXP (ถ้ามี)")]
    public Slider expSlider; 
    [Tooltip("ใส่ Image (ที่มี Image Type = Filled) สำหรับหลอด EXP แทน Slider ได้")]
    public Image expFillImage; 
    [Tooltip("ช่องสำหรับตัวหนังสือบอกเลเวลปัจจุบัน")]
    public TextMeshProUGUI levelText;
    [Tooltip("ช่องสำหรับตัวหนังสือบอกจำนวน EXP ตัวเลขเต็มๆ (เช่น 20 / 100)")]
    public TextMeshProUGUI expText;

    private void Start()
    {
        if (PlayerExperience.Instance != null)
        {
            // ทำการลงทะเบียน Event เพื่อรับค่าเมื่อหลอดเปลี่ยน
            PlayerExperience.Instance.OnExpChanged += UpdateExpUI;
            PlayerExperience.Instance.OnLevelUp += UpdateLevelUI;

            // ตั้งค่าค่าเริ่มต้น
            UpdateLevelUI(PlayerExperience.Instance.currentLevel);
            UpdateExpUI(PlayerExperience.Instance.currentExp, PlayerExperience.Instance.maxExpForNextLevel);
        }

        // บังคับให้สเกลของ Slider อยู่ที่ 0 ถึง 1 เท่านั้น
        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
        }
    }

    private void OnDestroy()
    {
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnExpChanged -= UpdateExpUI;
            PlayerExperience.Instance.OnLevelUp -= UpdateLevelUI;
        }
    }

    private void UpdateExpUI(float currentExp, float maxExp)
    {
        // คำนวณเปอร์เซ็นต์แบบเป๊ะๆ
        float fillAmount = 0f;
        if (maxExp > 0f)
        {
            fillAmount = currentExp / maxExp;
        }
        
        // 1. อัปเดตข้อความแสดงจํานวน EXP อย่างเช่น "20 / 100" แบบชัดเจน
        if (expText != null)
        {
            expText.text = $"{Mathf.FloorToInt(currentExp)} / {Mathf.FloorToInt(maxExp)}";
        }

        // 2. อัปเดตค่าหลอดทันทีแบบพุ่งพรวด ไม่มีอนิเมชั่นค่อยๆ วิ่ง (สไตล์เกม Mega Bonk)
        if (expSlider != null)
        {
            expSlider.value = fillAmount;
        }
        
        if (expFillImage != null)
        {
            expFillImage.fillAmount = fillAmount;
        }
    }

    private void UpdateLevelUI(int newLevel)
    {
        // อัปเดตตัวเลขเลเวล
        if (levelText != null)
        {
            levelText.text = $"Lv. {newLevel}";
        }

        // รีเซ็ตหลอดกลับไปเป็น 0 ทันทีเมื่อเลเวลอัพ
        if (expSlider != null) expSlider.value = 0f;
        if (expFillImage != null) expFillImage.fillAmount = 0f;
    }
}
