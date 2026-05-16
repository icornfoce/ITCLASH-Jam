using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// RhythmLetterSlot — UI สำหรับแต่ละตัวอักษรในระบบ Rhythm Typing
///
/// แสดงตัวอักษรพร้อมวงกลมที่ค่อยๆ บีบตัวเข้ามา (Shrinking Ring)
/// เมื่อถึงจังหวะ Beat พอดี วงจะเล็กที่สุด → ผู้เล่นต้องกดตอนนั้น
///
/// Prefab Structure:
///   └─ LetterSlot (RectTransform)
///       ├─ RingOuter (RawImage, Circle) — วงนอกที่หดเข้ามา
///       ├─ RingInner (RawImage, Circle) — วงในแสดงจุดเป้าหมาย (คงที่)
///       └─ LetterText (TMP_Text) — ตัวอักษร
/// </summary>
public class RhythmLetterSlot : MonoBehaviour
{
    [Header("─── UI References ───")]
    [Tooltip("วงนอกที่จะหดเข้ามา (Shrinking Ring)")]
    [SerializeField] private RawImage ringOuter;

    [Tooltip("วงในที่คงที่ แสดงจุดเป้าหมาย")]
    [SerializeField] private RawImage ringInner;

    [Tooltip("ตัวอักษร")]
    [SerializeField] private TMP_Text letterText;

    [Header("─── Animation Settings ───")]
    [Tooltip("ขนาดเริ่มต้นของ Outer Ring (เท่าตัว)")]
    [SerializeField] private float ringStartScale = 3f;

    [Tooltip("ขนาดสุดท้ายของ Outer Ring (1.0 = ตรงกับ Inner Ring)")]
    [SerializeField] private float ringEndScale = 1f;

    [Tooltip("ความเร็ว Pulse กระพริบหลังจากแสดงผลลัพธ์")]
    [SerializeField] private float resultPulseSpeed = 8f;

    [Header("─── Reveal Animation ───")]
    [Tooltip("ระยะเวลา Reveal Animation (วินาที)")]
    [SerializeField] private float revealDuration = 0.3f;

    // ============================================================
    // RUNTIME STATE
    // ============================================================

    private char letter;
    private float beatInterval;
    private float perfectWindow, goodWindow, okWindow;
    private bool isRevealed = false;
    private bool ringActive = false;
    private bool isCompleted = false;
    private float ringTimer = 0f;

    // สีต่างๆ
    private Color perfectColor, goodColor, okColor, missColor;
    private Color defaultRingColor = new Color(1f, 1f, 1f, 0.6f);

    // ============================================================
    // PROPERTIES
    // ============================================================

    /// <summary>ตัวอักษรนี้ถูกเปิดเผยแล้วหรือยัง</summary>
    public bool IsRevealed => isRevealed;

    /// <summary>ตัวอักษรนี้ถูกพิมพ์แล้วหรือยัง</summary>
    public bool IsCompleted => isCompleted;

    // ============================================================
    // PUBLIC API
    // ============================================================

    /// <summary>
    /// ตั้งค่าเริ่มต้นสำหรับ Slot นี้
    /// </summary>
    public void Initialize(char c, float beatIntervalSec, float perfect, float good, float ok)
    {
        letter = c;
        beatInterval = beatIntervalSec;
        perfectWindow = perfect;
        goodWindow = good;
        okWindow = ok;

        if (letterText != null)
        {
            letterText.text = c.ToString().ToUpper();
            letterText.alpha = 0f; // ซ่อนไว้ก่อน
        }

        // ตั้งค่าเริ่มต้นของ Ring
        if (ringOuter != null)
        {
            ringOuter.transform.localScale = Vector3.one * ringStartScale;
            ringOuter.color = defaultRingColor;
            ringOuter.gameObject.SetActive(false);
        }

        if (ringInner != null)
        {
            ringInner.color = new Color(1f, 1f, 1f, 0.3f);
            ringInner.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ตั้งค่าสีสำหรับแต่ละระดับ
    /// </summary>
    public void SetColors(Color perfect, Color good, Color ok, Color miss)
    {
        perfectColor = perfect;
        goodColor = good;
        okColor = ok;
        missColor = miss;
    }

    /// <summary>
    /// ซ่อน Slot ทั้งหมด
    /// </summary>
    public void Hide()
    {
        if (letterText != null) letterText.alpha = 0f;
        if (ringOuter != null) ringOuter.gameObject.SetActive(false);
        if (ringInner != null) ringInner.gameObject.SetActive(false);
        isRevealed = false;
    }

    /// <summary>
    /// เปิดเผยตัวอักษร (ตาม Beat)
    /// </summary>
    public void Reveal()
    {
        isRevealed = true;
        StartCoroutine(RevealAnimation());
    }

    /// <summary>
    /// เปิด Ring Animation (เริ่มบีบวงเข้ามา) — เรียกตอนถึงตัวอักษรที่ต้องพิมพ์
    /// </summary>
    public void ActivateRing()
    {
        if (isCompleted) return;

        ringActive = true;
        ringTimer = 0f;

        if (ringOuter != null)
        {
            ringOuter.gameObject.SetActive(true);
            ringOuter.transform.localScale = Vector3.one * ringStartScale;
            ringOuter.color = defaultRingColor;
        }

        if (ringInner != null)
        {
            ringInner.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// แสดงผลลัพธ์หลังจากพิมพ์ (Perfect, Good, OK)
    /// </summary>
    public void ShowResult(RhythmRating rating)
    {
        isCompleted = true;
        ringActive = false;

        Color resultColor;
        switch (rating)
        {
            case RhythmRating.Perfect: resultColor = perfectColor; break;
            case RhythmRating.Good: resultColor = goodColor; break;
            case RhythmRating.OK: resultColor = okColor; break;
            default: resultColor = missColor; break;
        }

        // เปลี่ยนสีตัวอักษร
        if (letterText != null)
        {
            letterText.color = resultColor;
        }

        // แสดง Ring ผลลัพธ์
        StartCoroutine(ResultAnimation(resultColor, rating));
    }

    /// <summary>
    /// แสดงผล Miss (พิมพ์ผิดตัว)
    /// </summary>
    public void ShowMiss()
    {
        isCompleted = true;
        ringActive = false;

        if (letterText != null)
        {
            letterText.color = missColor;
        }

        StartCoroutine(MissShakeAnimation());
    }

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Update()
    {
        if (!ringActive || isCompleted) return;

        // Ring บีบเข้ามาตาม Beat
        ringTimer += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(ringTimer / beatInterval);

        if (ringOuter != null)
        {
            // Lerp จากขนาดใหญ่ไปขนาดเล็ก
            float currentScale = Mathf.Lerp(ringStartScale, ringEndScale, progress);
            ringOuter.transform.localScale = Vector3.one * currentScale;

            // ค่อยๆ เปลี่ยนสีเป็นสี Perfect เมื่อใกล้จังหวะ
            Color ringColor = Color.Lerp(defaultRingColor, perfectColor, progress * progress);
            ringColor.a = Mathf.Lerp(0.3f, 0.9f, progress);
            ringOuter.color = ringColor;
        }

        // ถ้าหมดเวลา Beat แล้วยังไม่กด → เริ่ม Beat ใหม่
        if (progress >= 1f)
        {
            ringTimer = 0f; // วนรอบ Ring ใหม่
        }
    }

    // ============================================================
    // ANIMATIONS
    // ============================================================

    private IEnumerator RevealAnimation()
    {
        float elapsed = 0f;

        // Pop-in effect
        Vector3 startScale = Vector3.one * 0.3f;
        Vector3 overshoot = Vector3.one * 1.2f;
        Vector3 finalScale = Vector3.one;

        transform.localScale = startScale;

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / revealDuration;

            // Ease-out-back curve
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            // Overshoot
            if (t < 0.7f)
            {
                transform.localScale = Vector3.Lerp(startScale, overshoot, easedT / 0.7f);
            }
            else
            {
                float settleT = (t - 0.7f) / 0.3f;
                transform.localScale = Vector3.Lerp(overshoot, finalScale, settleT);
            }

            if (letterText != null)
            {
                letterText.alpha = Mathf.Clamp01(t * 2f); // Fade-in เร็ว
            }

            yield return null;
        }

        transform.localScale = finalScale;
        if (letterText != null) letterText.alpha = 1f;
    }

    private IEnumerator ResultAnimation(Color color, RhythmRating rating)
    {
        // Flash effect
        float flashDuration = 0.15f;
        float elapsed = 0f;

        // ขยาย Ring ออกแล้วหายไป
        if (ringOuter != null)
        {
            ringOuter.color = color;
            Vector3 expandScale = Vector3.one * (rating == RhythmRating.Perfect ? 2f : 1.5f);

            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / flashDuration;

                ringOuter.transform.localScale = Vector3.Lerp(Vector3.one, expandScale, t);
                Color c = ringOuter.color;
                c.a = 1f - t;
                ringOuter.color = c;

                yield return null;
            }

            ringOuter.gameObject.SetActive(false);
        }

        if (ringInner != null)
        {
            ringInner.gameObject.SetActive(false);
        }

        // Pop effect บนตัวอักษร
        elapsed = 0f;
        float popDuration = 0.2f;
        Vector3 popScale = Vector3.one * (rating == RhythmRating.Perfect ? 1.4f : 1.2f);

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / popDuration;

            // Bounce: ขยาย → หด
            float bounce = Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.Lerp(Vector3.one, popScale, bounce);

            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    private IEnumerator MissShakeAnimation()
    {
        // ซ่อน Ring
        if (ringOuter != null) ringOuter.gameObject.SetActive(false);
        if (ringInner != null) ringInner.gameObject.SetActive(false);

        // Shake ตัวอักษรไปมา
        float duration = 0.3f;
        float elapsed = 0f;
        float shakeMag = 5f;
        Vector3 originalPos = transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float decay = 1f - t;

            float offsetX = Random.Range(-1f, 1f) * shakeMag * decay;
            transform.localPosition = originalPos + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
