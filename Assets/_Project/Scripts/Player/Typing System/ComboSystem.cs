using UnityEngine;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float comboMaxTime = 10f;
    [SerializeField] private float fadeSpeed = 5f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI ratingText; // เพิ่ม Text โชว์ Perfect/Good/Miss
    [SerializeField] private UnityEngine.UI.Slider comboTimerSlider;
    [SerializeField] private UnityEngine.UI.Image sliderFillImage; // สีของหลอด
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Dynamic Effects")]
    [SerializeField] private Color[] comboColors; // สีที่จะเปลี่ยนตามระดับ Combo
    [SerializeField] private float shakeAmount = 10f;
    [SerializeField] private Vector3 popScale = new Vector3(1.3f, 1.3f, 1.3f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip perfectSFX;
    [SerializeField] private AudioClip goodSFX;
    [SerializeField] private AudioClip missSFX;

    public int CurrentCombo { get; private set; }
    private float comboTimer;
    private Vector3 originalScale;
    private Coroutine popCoroutine;

    private void Awake()
    {
        if (comboText != null)
            originalScale = comboText.transform.localScale;
            
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
            
        ResetCombo();
    }

    private void Update()
    {
        if (CurrentCombo > 0)
        {
            comboTimer -= Time.deltaTime;
            
            // Optional: Update color or shake as timer runs out
            if (comboTimer <= 0)
            {
                ResetCombo();
            }
            
            // Smooth fade in
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
                
            if (comboTimerSlider != null)
            {
                comboTimerSlider.maxValue = comboMaxTime;
                comboTimerSlider.value = comboTimer;
                
                // เปลี่ยนสีหลอดตามเวลาที่เหลือ
                if (sliderFillImage != null)
                {
                    float timeRatio = comboTimer / comboMaxTime;
                    sliderFillImage.color = Color.Lerp(Color.red, Color.green, timeRatio);
                }
            }
        }
        else
        {
            // Smooth fade out
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    public void AddCombo(RhythmRating rating)
    {
        if (rating == RhythmRating.Miss)
        {
            ResetCombo();
            ShowRatingUI(rating);
            PlayRatingSFX(rating);
            return;
        }

        CurrentCombo++;
        comboTimer = comboMaxTime;
        if (comboTimerSlider != null) comboTimerSlider.value = comboMaxTime;
        
        UpdateUI();
        UpdateColor();
        TriggerPopEffect();
        ShowRatingUI(rating);
        PlayRatingSFX(rating);
    }

    private void UpdateColor()
    {
        if (comboColors == null || comboColors.Length == 0 || comboText == null) return;
        
        // เปลี่ยนสีตามจำนวน Combo (เช่น ทุก 10 combo เปลี่ยนสี)
        int colorIndex = Mathf.Clamp(CurrentCombo / 10, 0, comboColors.Length - 1);
        comboText.color = comboColors[colorIndex];
    }

    private void ShowRatingUI(RhythmRating rating)
    {
        if (ratingText == null) return;
        ratingText.text = rating.ToString().ToUpper();
        
        // ตั้งสีตาม Rating
        switch(rating)
        {
            case RhythmRating.Perfect: ratingText.color = Color.cyan; break;
            case RhythmRating.Good: ratingText.color = Color.green; break;
            case RhythmRating.OK: ratingText.color = Color.yellow; break;
            case RhythmRating.Miss: ratingText.color = Color.red; break;
        }

        // รีเซ็ต Animation ของ ratingText (ถ้ามี) หรือแค่ทำ Pop
        ratingText.transform.localScale = Vector3.one * 1.5f;
        StartCoroutine(FadeRatingText());
    }

    private IEnumerator FadeRatingText()
    {
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (ratingText != null)
                ratingText.transform.localScale = Vector3.Lerp(ratingText.transform.localScale, Vector3.one, Time.deltaTime * 10f);
            yield return null;
        }
    }

    private void PlayRatingSFX(RhythmRating rating)
    {
        if (audioSource == null) return;

        AudioClip clip = null;
        switch(rating)
        {
            case RhythmRating.Perfect: clip = perfectSFX; break;
            case RhythmRating.Good: clip = goodSFX; break;
            case RhythmRating.Miss: clip = missSFX; break;
        }

        if (clip != null) audioSource.PlayOneShot(clip);
    }

    // สำหรับใช้ตอนจบคำ (ถ้าอยากให้ Combo เพิ่มตอนเสกของสำเร็จ)
    public void AddWordCombo()
    {
        CurrentCombo += 5; // โบนัสจบคำ
        comboTimer = comboMaxTime;
        UpdateUI();
        TriggerPopEffect();
    }

    public void ResetCombo()
    {
        if (CurrentCombo > 0) 
        {
            Debug.Log("<color=red>[ComboSystem] Combo Reset!</color>");
            TriggerBreakEffect(); // แสดงเอฟเฟกต์ตอนแตก
        }
        CurrentCombo = 0;
        comboTimer = 0;
        if (comboTimerSlider != null) comboTimerSlider.value = 0;
        UpdateUI();
    }

    private void TriggerBreakEffect()
    {
        if (comboText == null) return;
        
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(BreakSequence());
    }

    private IEnumerator BreakSequence()
    {
        // สั่นแรงๆ แล้วค่อยๆ ร่วงหรือหดหาย
        float t = 0f;
        Vector3 originalPos = comboText.transform.localPosition;
        comboText.color = Color.red; // เปลี่ยนเป็นสีแดงตอนแตก

        while (t < 0.5f)
        {
            t += Time.deltaTime;
            comboText.transform.localPosition = originalPos + new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0);
            comboText.transform.localScale = Vector3.Lerp(originalScale * 1.5f, Vector3.zero, t * 2f);
            yield return null;
        }

        comboText.transform.localPosition = originalPos;
        comboText.transform.localScale = originalScale;
        comboText.text = ""; // ซ่อนข้อความ
        popCoroutine = null;
    }

    private void UpdateUI()
    {
        if (comboText != null)
        {
            comboText.text = CurrentCombo > 0 ? $"COMBO x{CurrentCombo}" : "";
        }
    }

    private void TriggerPopEffect()
    {
        if (comboText == null) return;
        
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopSequence());
    }

    private IEnumerator PopSequence()
    {
        float t = 0f;
        Vector3 shakeOffset = Vector3.zero;

        // สุ่มสั่น (Shake) เล็กน้อยตอน Pop
        shakeOffset = new Vector3(Random.Range(-shakeAmount, shakeAmount), Random.Range(-shakeAmount, shakeAmount), 0);
        comboText.transform.localPosition += shakeOffset;

        comboText.transform.localScale = popScale;
        
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            comboText.transform.localScale = Vector3.Lerp(popScale, originalScale, t);
            yield return null;
        }
        
        comboText.transform.localScale = originalScale;
        comboText.transform.localPosition -= shakeOffset;
        popCoroutine = null;
    }
}
