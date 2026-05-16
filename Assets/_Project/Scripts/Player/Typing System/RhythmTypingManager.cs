using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// RhythmTypingManager — ระบบพิมพ์ตามจังหวะ BGM
///
/// ตัวอักษรจะค่อยๆ ปรากฏทีละตัวตามจังหวะ Beat ของเพลง
/// แต่ละตัวอักษรจะมีวงกลมบีบตัวเข้ามา (Shrinking Ring)
/// - กดตรงจังหวะพอดี → Time.timeScale = 0 (หยุดเวลา)
/// - กดใกล้จังหวะ → Time.timeScale ช้าลง
/// - กดพลาด → Time.timeScale ไหลปกติ (1.0)
///
/// วิธีติดตั้ง:
///   1. สร้าง Panel ใน Canvas สำหรับแสดง Rhythm Letters
///   2. ลาก BGM AudioSource มาใส่
///   3. ตั้งค่า BPM ให้ตรงกับเพลง
///   4. ลาก LetterSlotPrefab (ดู RhythmLetterSlot) มาใส่
/// </summary>
public class RhythmTypingManager : MonoBehaviour
{
    [Header("─── BGM Settings ───")]
    [Tooltip("AudioSource ที่เล่น BGM อยู่")]
    [SerializeField] private AudioSource bgmSource;

    [Tooltip("BPM ของเพลง BGM (Beats Per Minute)")]
    [SerializeField] private float bpm = 120f;

    [Tooltip("Offset เวลาเริ่มต้นของ Beat แรก (วินาที) — ใช้ปรับถ้า BGM ไม่ได้เริ่ม Beat ที่ 0")]
    [SerializeField] private float beatOffset = 0f;

    [Header("─── Timing Windows ───")]
    [Tooltip("ช่วงเวลาที่ถือว่า 'Perfect' (วินาที ± จากจังหวะ Beat)")]
    [SerializeField] private float perfectWindow = 0.05f;

    [Tooltip("ช่วงเวลาที่ถือว่า 'Good' (วินาที ± จากจังหวะ Beat)")]
    [SerializeField] private float goodWindow = 0.15f;

    [Tooltip("ช่วงเวลาที่ถือว่า 'OK' (วินาที ± จากจังหวะ Beat)")]
    [SerializeField] private float okWindow = 0.25f;

    [Header("─── Time Scale Effects ───")]
    [Tooltip("TimeScale เมื่อกดได้ Perfect")]
    [SerializeField] private float perfectTimeScale = 0f;

    [Tooltip("TimeScale เมื่อกดได้ Good")]
    [SerializeField] private float goodTimeScale = 0.1f;

    [Tooltip("TimeScale เมื่อกดได้ OK")]
    [SerializeField] private float okTimeScale = 0.3f;

    [Tooltip("TimeScale เมื่อกดพลาด (ปกติ)")]
    [SerializeField] private float missTimeScale = 1f;

    [Tooltip("ระยะเวลาที่ TimeScale Effect คงอยู่ (วินาที, Unscaled)")]
    [SerializeField] private float timeEffectDuration = 0.5f;

    [Tooltip("ความเร็วในการ Lerp กลับสู่ TimeScale ปกติ")]
    [SerializeField] private float timeScaleRecoverySpeed = 3f;

    [Header("─── UI References ───")]
    [Tooltip("Panel Container ที่จะวาง Letter Slots (ใช้ HorizontalLayoutGroup)")]
    [SerializeField] private Transform letterContainer;

    [Tooltip("Prefab ของ Letter Slot (ดู RhythmLetterSlot)")]
    [SerializeField] private GameObject letterSlotPrefab;

    [Header("─── Osu! Style Settings ───")]
    [Tooltip("เปิดเพื่อสุ่มตำแหน่งจุดเกิดของตัวอักษรบนหน้าจอ (อย่าลืมลบ HorizontalLayoutGroup ออกจาก Panel ด้วย!)")]
    [SerializeField] private bool randomPositions = true;
    
    [Tooltip("ขอบเขต X และ Y ในการสุ่มตำแหน่งจากจุดกึ่งกลาง Panel")]
    [SerializeField] private Vector2 randomPositionRange = new Vector2(300f, 150f);

    [Header("─── Visual Feedback ───")]
    [Tooltip("สี Ring ตอน Perfect")]
    [SerializeField] private Color perfectColor = new Color(0f, 1f, 0.5f, 1f);    // เขียว

    [Tooltip("สี Ring ตอน Good")]
    [SerializeField] private Color goodColor = new Color(0.2f, 0.6f, 1f, 1f);     // ฟ้า

    [Tooltip("สี Ring ตอน OK")]
    [SerializeField] private Color okColor = new Color(1f, 0.8f, 0f, 1f);         // เหลือง

    [Tooltip("สี Ring ตอน Miss")]
    [SerializeField] private Color missColor = new Color(1f, 0.2f, 0.2f, 1f);     // แดง

    [Header("─── Audio Feedback ───")]
    [SerializeField] private AudioClip perfectSFX;
    [SerializeField] private AudioClip goodSFX;
    [SerializeField] private AudioClip missSFX;
    [SerializeField] private AudioSource sfxSource;

    // ============================================================
    // RUNTIME STATE
    // ============================================================

    private string currentWord = "";
    private int currentLetterIndex = 0;
    private List<RhythmLetterSlot> activeSlots = new List<RhythmLetterSlot>();
    private bool isActive = false;

    // Beat tracking
    private float beatInterval;       // ระยะเวลาระหว่าง Beat (วินาที)
    private int lastRevealedBeat = -1;
    private float baseTimeScale = 0.2f; // TimeScale พื้นฐานขณะพิมพ์ (จาก TypingSystem)
    private float fallbackBeatTimer = 0f; // Timer สำรองเมื่อไม่มี BGM
    private int fallbackBeatCount = 0;    // นับ Beat จาก Timer สำรอง

    // Time effect
    private float timeEffectTimer = 0f;
    private float targetTimeScale = 0.2f;
    private bool hasTimeEffect = false;

    // ============================================================
    // PROPERTIES
    // ============================================================

    /// <summary>ระบบ Rhythm กำลังทำงานอยู่หรือไม่</summary>
    public bool IsActive => isActive;

    /// <summary>ตัวอักษรถัดไปที่ต้องพิมพ์</summary>
    public char CurrentExpectedChar => (currentLetterIndex < currentWord.Length) 
        ? currentWord[currentLetterIndex] 
        : '\0';

    /// <summary>จำนวนตัวที่พิมพ์ถูกแล้ว</summary>
    public int TypedCount => currentLetterIndex;

    /// <summary>พิมพ์เสร็จทั้งคำแล้วหรือยัง</summary>
    public bool IsWordCompleted => isActive && currentLetterIndex >= currentWord.Length;

    /// <summary>คำที่กำลังพิมพ์อยู่ทั้งคำ</summary>
    public string CurrentWord => currentWord;

    // ============================================================
    // PUBLIC API
    // ============================================================

    /// <summary>
    /// เริ่มระบบ Rhythm Typing สำหรับคำที่กำหนด
    /// </summary>
    /// <param name="word">คำที่ต้องพิมพ์</param>
    /// <param name="typingBaseTimeScale">TimeScale พื้นฐานของ TypingSystem (จะใช้เป็นค่าเริ่มต้น)</param>
    /// <param name="customContainer">Panel ที่จะวาง Letter Slots (ถ้าไม่ใส่จะใช้ letterContainer พื้นฐาน)</param>
    public void StartRhythmTyping(string word, float typingBaseTimeScale, Transform customContainer = null)
    {
        if (string.IsNullOrEmpty(word)) return;

        currentWord = word.ToLower();
        currentLetterIndex = 0;
        baseTimeScale = typingBaseTimeScale;
        targetTimeScale = baseTimeScale;
        hasTimeEffect = false;
        timeEffectTimer = 0f;

        // คำนวณระยะเวลาระหว่าง Beat
        beatInterval = 60f / bpm;

        // ตั้งจุดอ้างอิง Beat ให้ตรงกับ BGM ที่กำลังเล่นอยู่
        lastRevealedBeat = -1;
        fallbackBeatTimer = 0f;
        fallbackBeatCount = 0;

        // ล้าง Slot เก่า
        ClearSlots();

        Transform targetContainer = customContainer != null ? customContainer : letterContainer;

        // สร้าง Slot สำหรับแต่ละตัวอักษร (ซ่อนไว้ก่อน)
        for (int i = 0; i < currentWord.Length; i++)
        {
            GameObject slotObj = Instantiate(letterSlotPrefab, targetContainer);
            RhythmLetterSlot slot = slotObj.GetComponent<RhythmLetterSlot>();

            if (slot != null)
            {
                slot.Initialize(currentWord[i], beatInterval, perfectWindow, goodWindow, okWindow);
                slot.SetColors(perfectColor, goodColor, okColor, missColor);
                slot.Hide(); // ยังไม่แสดง รอจนถึง Beat
                activeSlots.Add(slot);
            }

            // สุ่มตำแหน่งแบบ Osu! (ต้องไม่มี LayoutGroup คุมอยู่)
            if (randomPositions)
            {
                RectTransform rect = slotObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float rx = UnityEngine.Random.Range(-randomPositionRange.x, randomPositionRange.x);
                    float ry = UnityEngine.Random.Range(-randomPositionRange.y, randomPositionRange.y);
                    rect.anchoredPosition = new Vector2(rx, ry);
                }
            }
        }

        isActive = true;
        Debug.Log($"[RhythmTyping] Started for word: '{currentWord}' | BPM: {bpm} | Beat Interval: {beatInterval:F3}s");
    }

    /// <summary>
    /// หยุดระบบ Rhythm Typing
    /// </summary>
    public void StopRhythmTyping()
    {
        isActive = false;
        hasTimeEffect = false;
        ClearSlots();
        currentWord = "";
        currentLetterIndex = 0;
        lastRevealedBeat = -1;
        fallbackBeatTimer = 0f;
        fallbackBeatCount = 0;
        Debug.Log("[RhythmTyping] Stopped.");
    }

    /// <summary>
    /// เรียกตอนผู้เล่นกดปุ่ม — ตรวจสอบจังหวะและให้คะแนน
    /// </summary>
    /// <param name="typedChar">ตัวอักษรที่กด</param>
    /// <returns>ระดับความแม่นยำ: Perfect / Good / OK / Miss</returns>
    public RhythmRating ProcessKeyPress(char typedChar)
    {
        if (!isActive || currentLetterIndex >= currentWord.Length)
            return RhythmRating.Miss;

        char expected = currentWord[currentLetterIndex];

        // ตรวจสอบว่าพิมพ์ตัวถูกหรือไม่
        if (char.ToLower(typedChar) != char.ToLower(expected))
        {
            // พิมพ์ผิดตัว → Miss
            ApplyTimeEffect(RhythmRating.Miss);
            if (currentLetterIndex < activeSlots.Count)
                activeSlots[currentLetterIndex].ShowMiss();
            
            PlayFeedbackSFX(RhythmRating.Miss);

            // ข้ามไปตัวถัดไปเลย
            currentLetterIndex++;
            if (currentLetterIndex < activeSlots.Count)
            {
                activeSlots[currentLetterIndex].ActivateRing();
            }

            // ตรวจสอบว่าพิมพ์ครบทั้งคำแล้วหรือยัง (แม้จะพิมพ์ผิดตัวสุดท้าย)
            if (currentLetterIndex >= currentWord.Length)
            {
                Debug.Log("[RhythmTyping] Word completed with a miss at the end!");
            }

            return RhythmRating.Miss;
        }

        // พิมพ์ถูกตัว → ตรวจสอบจังหวะ
        RhythmRating rating = EvaluateTiming();
        ApplyTimeEffect(rating);

        // อัปเดต UI ของ Slot ปัจจุบัน
        if (currentLetterIndex < activeSlots.Count)
        {
            activeSlots[currentLetterIndex].ShowResult(rating);
        }

        PlayFeedbackSFX(rating);
        currentLetterIndex++;

        // เปิด Ring สำหรับตัวถัดไป (ถ้ายังเหลือ)
        if (currentLetterIndex < activeSlots.Count)
        {
            activeSlots[currentLetterIndex].ActivateRing();
        }

        // ตรวจสอบว่าพิมพ์ครบทั้งคำแล้วหรือยัง
        if (currentLetterIndex >= currentWord.Length)
        {
            Debug.Log("[RhythmTyping] Word completed!");
        }

        return rating;
    }

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Update()
    {
        if (!isActive) return;

        // ─── Beat-based Letter Reveal ───
        RevealLettersOnBeat();

        // ─── Time Effect Recovery ───
        HandleTimeEffectRecovery();
    }

    // ============================================================
    // BEAT TRACKING
    // ============================================================

    /// <summary>
    /// เปิดเผยตัวอักษรทีละตัวตามจังหวะ Beat ของ BGM
    /// ถ้าไม่มี BGM จะใช้ Timer สำรองนับจังหวะเอง
    /// </summary>
    private void RevealLettersOnBeat()
    {
        int currentBeat;

        if (bgmSource != null && bgmSource.isPlaying)
        {
            // ── โหมด BGM: นับ Beat จากเวลาของเพลง ──
            float currentBGMTime = bgmSource.time - beatOffset;
            currentBeat = Mathf.FloorToInt(currentBGMTime / beatInterval);
        }
        else
        {
            // ── โหมด Fallback: ไม่มี BGM ให้ใช้ Timer นับเอง ──
            fallbackBeatTimer += Time.unscaledDeltaTime;
            currentBeat = Mathf.FloorToInt(fallbackBeatTimer / beatInterval);
        }

        // ถ้า Beat ใหม่ ให้เปิดตัวอักษรถัดไป
        if (currentBeat > lastRevealedBeat)
        {
            int lettersToReveal = currentBeat - lastRevealedBeat;
            lastRevealedBeat = currentBeat;

            for (int i = 0; i < lettersToReveal; i++)
            {
                int revealIndex = GetNextHiddenSlotIndex();
                if (revealIndex >= 0 && revealIndex < activeSlots.Count)
                {
                    activeSlots[revealIndex].Reveal();

                    // ถ้าเป็นตัวที่ต้องพิมพ์ตอนนี้ ให้เปิด Ring ด้วย
                    if (revealIndex == currentLetterIndex)
                    {
                        activeSlots[revealIndex].ActivateRing();
                    }
                }
            }
        }
    }

    private int GetNextHiddenSlotIndex()
    {
        for (int i = 0; i < activeSlots.Count; i++)
        {
            if (!activeSlots[i].IsRevealed)
                return i;
        }
        return -1;
    }

    // ============================================================
    // TIMING EVALUATION
    // ============================================================

    /// <summary>
    /// ประเมินว่าตอนที่กดอยู่ ตรงจังหวะแค่ไหน
    /// </summary>
    private RhythmRating EvaluateTiming()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
            return RhythmRating.OK; // ถ้าไม่มี BGM ให้ถือว่า OK เสมอ

        float currentBGMTime = bgmSource.time - beatOffset;

        // หาว่าจังหวะ Beat ที่ใกล้ที่สุดอยู่ที่เวลาเท่าไหร่
        float nearestBeat = Mathf.Round(currentBGMTime / beatInterval) * beatInterval;
        float timeDiff = Mathf.Abs(currentBGMTime - nearestBeat);

        if (timeDiff <= perfectWindow)
            return RhythmRating.Perfect;
        else if (timeDiff <= goodWindow)
            return RhythmRating.Good;
        else if (timeDiff <= okWindow)
            return RhythmRating.OK;
        else
            return RhythmRating.Miss;
    }

    // ============================================================
    // TIME SCALE EFFECTS
    // ============================================================

    /// <summary>
    /// ใส่ Effect เวลาตามระดับความแม่นยำ
    /// </summary>
    private void ApplyTimeEffect(RhythmRating rating)
    {
        float newTimeScale;

        switch (rating)
        {
            case RhythmRating.Perfect:
                newTimeScale = perfectTimeScale;
                break;
            case RhythmRating.Good:
                newTimeScale = goodTimeScale;
                break;
            case RhythmRating.OK:
                newTimeScale = okTimeScale;
                break;
            default:
                newTimeScale = missTimeScale;
                break;
        }

        targetTimeScale = newTimeScale;
        timeEffectTimer = timeEffectDuration;
        hasTimeEffect = true;

        // ใส่ Effect ทันที
        Time.timeScale = targetTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Debug.Log($"[RhythmTyping] Rating: {rating} | TimeScale → {targetTimeScale:F2}");
    }

    /// <summary>
    /// ค่อยๆ คืน TimeScale กลับสู่ค่าพื้นฐาน
    /// </summary>
    private void HandleTimeEffectRecovery()
    {
        if (!hasTimeEffect) return;

        timeEffectTimer -= Time.unscaledDeltaTime;

        if (timeEffectTimer <= 0f)
        {
            // หมดเวลา Effect → ค่อยๆ Lerp กลับ
            float current = Time.timeScale;
            float recovered = Mathf.MoveTowards(current, baseTimeScale, timeScaleRecoverySpeed * Time.unscaledDeltaTime);

            Time.timeScale = recovered;
            Time.fixedDeltaTime = 0.02f * recovered;

            if (Mathf.Approximately(recovered, baseTimeScale))
            {
                hasTimeEffect = false;
            }
        }
    }

    // ============================================================
    // AUDIO FEEDBACK
    // ============================================================

    private void PlayFeedbackSFX(RhythmRating rating)
    {
        AudioSource source = sfxSource != null ? sfxSource : bgmSource;
        if (source == null) return;

        AudioClip clip = null;

        switch (rating)
        {
            case RhythmRating.Perfect:
                clip = perfectSFX;
                break;
            case RhythmRating.Good:
                clip = goodSFX ?? perfectSFX;
                break;
            case RhythmRating.Miss:
                clip = missSFX;
                break;
        }

        if (clip != null)
        {
            source.PlayOneShot(clip);
        }
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void ClearSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null && slot.gameObject != null)
                Destroy(slot.gameObject);
        }
        activeSlots.Clear();
    }
}

/// <summary>
/// ระดับความแม่นยำในการกดจังหวะ
/// </summary>
public enum RhythmRating
{
    Perfect,    // กดตรงจังหวะพอดี → หยุดเวลา
    Good,       // กดใกล้จังหวะ → เวลาช้าลง
    OK,         // กดพอได้ → เวลาช้าลงนิดหน่อย
    Miss        // กดพลาด → เวลาไหลปกติ
}
