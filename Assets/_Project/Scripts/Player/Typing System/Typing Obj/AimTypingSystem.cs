using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

/// <summary>
/// AimTypingSystem.cs — ระบบเล็งและพิมพ์คำ
///
/// วิธีติดตั้ง:
///   1. ติด Script นี้บน GameObject เดียวกับ Player หรือ CameraHolder
///   2. ลาก Component ต่างๆ มาใส่ใน Inspector ตามหัวข้อด้านล่าง
///   3. สร้าง UI Panel สำหรับ Aim Typing (ดูคู่มือ)
///
/// วิธีเล่น:
///   - เล็งไปที่ Object ที่มี Tag "Scannable" → Crosshair เปลี่ยน
///   - คลิกซ้าย → ล็อกเป้า + กล้องซูม + UI ปรากฏ
///   - พิมพ์คำที่แนะนำ → กด Enter
///   - คำถูกเพิ่มเข้า ItemData ให้ TypingSystem (กด E) ใช้ได้ทันที
/// </summary>
public class AimTypingSystem : MonoBehaviour
{
    // ============================================================
    // INSPECTOR FIELDS
    // ============================================================

    [Header("─── Scan Settings ───")]
    [Tooltip("ระยะ Raycast สูงสุด (เมตร)")]
    [SerializeField] private float scanRange = 15f;

    [Tooltip("Layer ที่ Raycast จะกระทบ (ปกติใช้ Everything = -1)")]
    [SerializeField] private LayerMask scanLayerMask = ~0;

    [Tooltip("ยิง Raycast ทุกกี่วินาที (0.1 = ประหยัด CPU, 0.05 = ตอบสนองเร็ว)")]
    [SerializeField] private float scanInterval = 0.1f;

    // ───────────────────────────────────────────────────────────

    [Header("─── Zoom Settings ───")]
    [Tooltip("FOV ปกติของกล้อง")]
    [SerializeField] private float normalFOV = 60f;

    [Tooltip("FOV ตอนล็อกเป้า (ค่าน้อย = ซูมมากขึ้น)")]
    [SerializeField] private float zoomFOV = 40f;

    [Tooltip("ความเร็วในการซูม")]
    [SerializeField] private float zoomSpeed = 8f;

    // ───────────────────────────────────────────────────────────

    [Header("─── UI References ───")]
    [Tooltip("Panel หลักของ Aim Typing (ซ่อนไว้ก่อน)")]
    [SerializeField] private GameObject aimTypingUI;

    [Tooltip("TMP_InputField ช่องพิมพ์")]
    [SerializeField] private TMP_InputField inputField;

    [Tooltip("TMP_Text แสดงคำที่ต้องพิมพ์ (แสดงบน Crosshair หรือด้านบนจอ)")]
    [SerializeField] private TMP_Text targetWordLabel;

    [Tooltip("Image Crosshair ปกติ")]
    [SerializeField] private GameObject crosshairNormal;

    [Tooltip("Image Crosshair ตอนเล็งวัตถุที่สแกนได้ (เปลี่ยนสี/รูปร่าง)")]
    [SerializeField] private GameObject crosshairHighlight;

    // ───────────────────────────────────────────────────────────

    [Header("─── Word Learned Popup ───")]
    [Tooltip("Panel Popup 'New Word Learned!' (ซ่อนไว้ก่อน, ควรมี CanvasGroup)")]
    [SerializeField] private GameObject wordLearnedPopup;

    [Tooltip("TMP_Text ในPopup บอกชื่อคำที่เรียนได้")]
    [SerializeField] private TMP_Text wordLearnedText;

    [Tooltip("ระยะเวลา Popup แสดง (วินาที)")]
    [SerializeField] private float popupDuration = 2.5f;

    // ───────────────────────────────────────────────────────────

    [Header("─── Vignette Effects ───")]
    [Tooltip("ลาก Q_Vignette_Base (สีเล็ง) มาใส่")]
    [SerializeField] private Q_Vignette_Base aimVignette;

    [Tooltip("ความเร็ว Fade ของ Vignette")]
    [SerializeField] private float vignetteFadeSpeed = 10f;

    [Tooltip("ความเข้มสูงสุดของ Vignette (0-1)")]
    [SerializeField] private float maxVignetteAlpha = 0.7f;

    // ───────────────────────────────────────────────────────────

    [Header("─── Audio ───")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip lockOnSFX;       // เสียงตอนล็อกเป้า
    [SerializeField] private AudioClip successSFX;      // เสียงตอนพิมพ์ถูก
    [SerializeField] private AudioClip failSFX;         // เสียงตอนพิมพ์ผิด
    [SerializeField] private AudioClip[] typingSFXPool; // เสียงกดคีย์บอร์ด

    // ───────────────────────────────────────────────────────────

    [Header("─── VFX ───")]
    [Tooltip("VFX Prefab ตอน Scan สำเร็จ")]
    [SerializeField] private GameObject successVFXPrefab;

    // ───────────────────────────────────────────────────────────

    [Header("─── Combat ───")]
    [Tooltip("DMG คูณกี่เท่าเมื่อใช้ระบบนี้สำเร็จ (เช่น 2.0 = x2)")]
    [SerializeField] private float aimDamageMultiplier = 2f;

    // ───────────────────────────────────────────────────────────

    [Header("─── Data & Systems ───")]
    [Tooltip("ItemData เดียวกับที่ TypingSystem ใช้")]
    [SerializeField] private ItemData itemData;
    
    [Tooltip("ลาก TypingSystem มาใส่ เพื่อให้เมื่อพิมพ์ Aim สำเร็จ จะเสกไอเทมและใช้งานเลย")]
    [SerializeField] private TypingSystem typingSystem;

    // ───────────────────────────────────────────────────────────

    [Header("─── Slow Motion ───")]
    [Range(0.05f, 1f)]
    [Tooltip("ค่า TimeScale ตอนล็อกเป้า (0.2 = ช้า 5 เท่า)")]
    [SerializeField] private float slowTimeScale = 0.2f;

    // ============================================================
    // PRIVATE STATE
    // ============================================================

    private Camera playerCamera;

    // Raycast
    private float scanTimer;
    private GameObject currentHoveredObject;

    // States
    private bool isZooming;        // กำลัง Scope/Zoom อยู่ (กดคลิกขวา)
    private bool isAimTyping;      // Text Input เปิดอยู่ (เจอ Bullet แล้ว)
    private GameObject lockedTarget;
    private string currentTargetWord;

    // Internal
    private bool isSlowed;
    private bool needsFocus;
    private bool isInternalUpdate;
    private string lastInput;

    // Zoom
    private float targetFOV;

    // Vignette
    private float currentVignetteAlpha;

    // O(1) lookup สำหรับตรวจคำซ้ำใน Dict
    private readonly HashSet<string> learnedWordsCache =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    // ============================================================
    // LIFECYCLE
    // ============================================================

    private void Awake()
    {
        // ค้นหา Camera
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = GetComponentInParent<Camera>();

        if (playerCamera == null)
            Debug.LogError($"[AimTypingSystem] ❌ ไม่พบ Camera! กรุณาตั้ง Tag กล้องเป็น 'MainCamera' หรือลาก Script นี้ไปไว้ที่เดียวกับกล้อง", this);

        targetFOV = normalFOV;

        // สร้าง Cache จาก ItemData ที่มีอยู่แล้ว
        RebuildWordCache();

        // ซ่อน UI ทั้งหมดตอนเริ่ม
        if (aimTypingUI != null)        aimTypingUI.SetActive(false);
        if (wordLearnedPopup != null)   wordLearnedPopup.SetActive(false);
        if (crosshairHighlight != null) crosshairHighlight.SetActive(false);
        if (crosshairNormal != null)    crosshairNormal.SetActive(true);   // แสดง Normal Crosshair ตอนเริ่ม
        if (targetWordLabel != null)    targetWordLabel.gameObject.SetActive(false);

        if (aimVignette != null)
        {
            SetVignetteAlpha(0f);
            aimVignette.gameObject.SetActive(false);
        }

        if (inputField != null)
            inputField.onValueChanged.AddListener(OnInputValueChanged);
    }

    private void Update()
    {
        HandleRaycast();
        HandleInput();
        HandleZoom();
        HandleVignette();
        HandleFocus();
    }

    // ============================================================
    // RAYCAST — ยิงเฉพาะตอน Zoom อยู่และยังไม่ได้พิมพ์
    // ============================================================

    private void HandleRaycast()
    {
        // ยิงเฉพาะตอน Zoom + ยังไม่ได้เปิด Input
        if (!isZooming || isAimTyping) return;

        scanTimer += Time.unscaledDeltaTime;
        if (scanTimer < scanInterval) return;
        scanTimer = 0f;

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, scanRange, scanLayerMask))
        {
            if (hit.collider.CompareTag("Scannable"))
            {
                // เจอ Bullet → ล็อกเป้าอัตโนมัติ + เปิด Input
                if (hit.collider.gameObject != lockedTarget)
                    LockTarget(hit.collider.gameObject);
                return;
            }
        }

        // เล็งไม่โดนหรือโดนแต่ไม่ใช่ Scannable → ซ่อน Input
        if (lockedTarget != null)
            CancelTyping();

        SetHoveredObject(null);
    }

    private void SetHoveredObject(GameObject obj)
    {
        currentHoveredObject = obj;
        bool hasTarget = obj != null;

        if (targetWordLabel != null)
        {
            targetWordLabel.gameObject.SetActive(hasTarget);
            targetWordLabel.text = hasTarget ? GetWordFromObject(obj) : "";
        }
    }

    // ============================================================
    // INPUT HANDLING
    // ============================================================

    private void HandleInput()
    {
        // ── ESC → ยกเลิกทุกอย่าง ──
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAll();
            return;
        }

        // ── คลิกขวา ──
        if (Input.GetMouseButtonDown(1))
        {
            // ถ้ากำลัง Zoom อยู่ → ออกจาก Zoom
            if (isZooming)
            {
                ExitZoom();
                return;
            }

            // ไม่ได้ Zoom → เข้า Zoom!
            EnterZoom();
            return;
        }

        // ── Enter → ส่งคำ (เฉพาะตอนพิมพ์อยู่) ──
        if (isAimTyping && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SubmitWord();
        }
    }

    private void EnterZoom()
    {
        isZooming   = true;
        targetFOV   = zoomFOV;
        SetSlowMotion(true);

        if (crosshairHighlight != null) crosshairHighlight.SetActive(true);
        if (crosshairNormal != null)    crosshairNormal.SetActive(false);

        // ซ่อนของที่ถืออยู่ชั่วคราวเพื่อให้มองเห็นชัด และป้องกันการ Scan โดนของตัวเอง
        if (typingSystem != null) typingSystem.SetSpawnedItemsVisibility(false);
    }

    private void ExitZoom()
    {
        isZooming   = false;
        targetFOV   = normalFOV;
        SetSlowMotion(false);

        if (crosshairHighlight != null) crosshairHighlight.SetActive(false);
        if (crosshairNormal != null)    crosshairNormal.SetActive(true);

        // แสดงของที่ถืออยู่กลับคืนมา
        if (typingSystem != null) typingSystem.SetSpawnedItemsVisibility(true);

        CancelTyping();
    }

    // ============================================================
    // LOCK TARGET & START TYPING
    // ============================================================

    private void LockTarget(GameObject target)
    {
        lockedTarget        = target;
        currentTargetWord   = GetWordFromObject(target);
        isAimTyping         = true;

        PlaySFX(lockOnSFX);
        SetSlowMotion(true);

        // เปิด UI
        if (aimTypingUI != null) aimTypingUI.SetActive(true);

        // รีเซ็ต InputField
        if (inputField != null)
        {
            inputField.text = "";
            lastInput       = "";
            needsFocus      = true;
        }

        // เริ่มซูม
        targetFOV = zoomFOV;

        // แสดง Autocomplete ทันที (แสดงคำเต็มเป็น selected text)
        ShowAutocomplete(currentTargetWord, 0);
    }

    // ============================================================
    // SUBMIT
    // ============================================================

    private void SubmitWord()
    {
        if (inputField == null) return;

        // ดึงเฉพาะส่วนที่ผู้เล่นพิมพ์จริงๆ (ไม่รวม selected/autocomplete portion)
        int anchorPos   = inputField.selectionAnchorPosition;
        int focusPos    = inputField.selectionFocusPosition;
        int typedLength = Mathf.Min(anchorPos, focusPos);

        // ถ้าไม่มี selection → ถือว่าพิมพ์ทั้งหมด
        if (typedLength == 0 && anchorPos == focusPos)
            typedLength = inputField.text.Length;

        string typed = inputField.text.Substring(0, typedLength).Trim();

        if (string.Equals(typed, currentTargetWord, System.StringComparison.OrdinalIgnoreCase))
            OnTypingSuccess();
        else
            OnTypingFail(typed);

        if (inputField != null) inputField.text = "";
        CancelTyping();  // ปิดแค่ Input — ยังอยู่ในโหมด Zoom เพื่อเล็งต่อได้
    }

    private void OnTypingSuccess()
    {
        PlaySFX(successSFX);
        SpawnVFX(successVFXPrefab, transform.position);

        (bool isNewWord, ItemInfo wordInfo) = RegisterWord(currentTargetWord, lockedTarget);

        if (isNewWord)
        {
            ShowWordLearnedPopup(currentTargetWord);
            
            // สั่งให้ Dictionary UI อัปเดตทันทีที่มีคำใหม่!
            if (DictionaryManager.Instance != null)
            {
                DictionaryManager.Instance.RefreshDictionary();
            }
        }

        Debug.Log($"[AimTyping] ✅ พิมพ์สำเร็จ: '{currentTargetWord}' | คำใหม่: {isNewWord}");

        // ส่งคำไปให้ TypingSystem ใช้งาน (พร้อมระบุตำแหน่งเป้าหมาย)
        if (typingSystem != null)
        {
            typingSystem.TryMatchItem(currentTargetWord, lockedTarget != null ? lockedTarget.transform.position : (Vector3?)null);
        }

        // ออกจาก Aim mode ทันทีที่พิมพ์ถูก
        ExitZoom();
    }


    private void OnTypingFail(string typed)
    {
        PlaySFX(failSFX);
        Debug.Log($"[AimTyping] ❌ พิมพ์ '{typed}' แต่ต้องพิมพ์ '{currentTargetWord}'");
    }

    private void CancelTyping()
    {
        // ปิดแค่ Input — ยังอยู่ในโหมด Zoom
        if (!isAimTyping) return;

        isAimTyping       = false;
        needsFocus        = false;
        lockedTarget      = null;
        currentTargetWord = "";

        SetHoveredObject(null);
        if (aimTypingUI != null) aimTypingUI.SetActive(false);
        if (inputField != null)  inputField.text = "";
    }

    private void CancelAll()
    {
        // ยกเลิกทุกอย่าง (ESC)
        CancelTyping();
        if (isZooming) ExitZoom();
    }

    // ============================================================
    // AUTOCOMPLETE — แสดงคำของ Target เสมอ (ไม่ได้ search Dict)
    // ============================================================

    private void OnInputValueChanged(string newValue)
    {
        if (isInternalUpdate || !isAimTyping) return;

        // เล่นเสียงพิมพ์
        if (typingSFXPool != null && typingSFXPool.Length > 0 && !string.IsNullOrEmpty(newValue))
            if (audioSource != null)
                audioSource.PlayOneShot(typingSFXPool[Random.Range(0, typingSFXPool.Length)]);

        if (string.IsNullOrEmpty(newValue)) return;

        bool isDeleting = !string.IsNullOrEmpty(lastInput) && newValue.Length < lastInput.Length;
        lastInput = newValue;

        // แสดง Autocomplete เฉพาะตอนพิมพ์ถูกทิศ (ไม่ใช่ตอนลบ)
        if (!isDeleting && currentTargetWord.StartsWith(newValue, System.StringComparison.OrdinalIgnoreCase))
            ShowAutocomplete(currentTargetWord, newValue.Length);
    }

    private void ShowAutocomplete(string fullWord, int typedLength)
    {
        if (inputField == null) return;

        isInternalUpdate = true;
        inputField.text = fullWord;
        inputField.selectionAnchorPosition = typedLength;
        inputField.selectionFocusPosition  = fullWord.Length;
        isInternalUpdate = false;
    }

    // ============================================================
    // WORD REGISTRATION — เพิ่มคำเข้า ItemData
    // ============================================================

    /// <summary>
    /// เพิ่มหรือ Unlock คำใน ItemData
    /// คืนค่า true = คำใหม่ที่ยังไม่เคยเรียน
    /// <summary>
    /// เพิ่มหรือ Unlock คำใน ItemData
    /// คืนค่า (คำใหม่ไหม, ข้อมูลคำนั้น)
    /// </summary>
    private (bool, ItemInfo) RegisterWord(string word, GameObject sourceObject)
    {
        if (itemData == null)
        {
            Debug.LogError("[AimTyping] ยังไม่ได้ลาก ItemData มาใส่ใน Inspector!");
            return (false, null);
        }

        // ตรวจด้วย HashSet — O(1) ไม่สนใจว่า Dict มีกี่คำ
        if (learnedWordsCache.Contains(word))
        {
            // คำมีอยู่แล้วใน Dictionary
            ItemInfo foundItem = null;
            bool wasLocked = false;
            
            foreach (var item in itemData.items)
            {
                if (string.Equals(item.itemName, word, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!item.isUnlocked)
                    {
                        item.isUnlocked = true;
                        wasLocked = true; // เพิ่งปลดล็อคสดๆ ร้อนๆ ให้ถือว่าเป็นคำใหม่!
                    }
                    foundItem = item;
                    break;
                }
            }

            return (wasLocked, foundItem);
        }

        // คำใหม่ → สร้าง ItemInfo แล้วใส่เข้า List
        ItemInfo newItem = new ItemInfo
        {
            itemName   = word,
            isUnlocked = true
        };

        itemData.items.Add(newItem);
        learnedWordsCache.Add(word);

        return (true, newItem);
    }

    /// <summary>
    /// สร้าง HashSet Cache ใหม่จาก ItemData ปัจจุบัน
    /// เรียกใช้ใน Awake() หรือตอน Reload Scene
    /// </summary>
    private void RebuildWordCache()
    {
        learnedWordsCache.Clear();
        if (itemData == null) return;
        foreach (var item in itemData.items)
            learnedWordsCache.Add(item.itemName);
    }

    // ============================================================
    // STRING SANITIZATION
    // แปลง gameObject.name เป็นคำที่พิมพ์ได้
    // ============================================================

    /// <summary>
    /// ตัวอย่างการแปลง:
    ///   "Wood_Table (1)"      →  "wood table"
    ///   "EnemySpawner (23)"   →  "enemyspawner"
    ///   "Stone_Floor_Tile"    →  "stone floor tile"
    ///   "Cube"                →  "cube"
    /// </summary>
    private string SanitizeName(string raw)
    {
        // 1. ตัด "(1)", "(23)" ที่ Unity เติมต่อท้ายอัตโนมัติ
        string result = Regex.Replace(raw, @"\s*\(\d+\)\s*$", "");

        // 2. แปลง Underscore → Space
        result = result.Replace("_", " ");

        // 3. Lowercase และ Trim
        result = result.Trim().ToLower();

        // 4. ลบ Space ซ้ำ
        result = Regex.Replace(result, @"\s+", " ");

        return result;
    }

    /// <summary>
    /// อ่านคำจาก Object:
    ///   - ถ้ามี ScanLabel และมีค่า word → ใช้ค่านั้น
    ///   - ถ้าไม่มี → Sanitize gameObject.name อัตโนมัติ
    /// </summary>
    private string GetWordFromObject(GameObject obj)
    {
        ScanLabel label = obj.GetComponent<ScanLabel>();
        if (label != null && !string.IsNullOrEmpty(label.word))
            return label.word.Trim().ToLower();

        return SanitizeName(obj.name);
    }

    // ============================================================
    // POPUP — NEW WORD LEARNED!
    // ============================================================

    private void ShowWordLearnedPopup(string word)
    {
        if (wordLearnedPopup == null) return;
        StopCoroutine(nameof(PopupRoutine));
        StartCoroutine(PopupRoutine(word));
    }

    private IEnumerator PopupRoutine(string word)
    {
        if (wordLearnedText != null)
            wordLearnedText.text = $"New Word Learned!\n\"{word}\"";

        wordLearnedPopup.SetActive(true);

        // Fade In (ถ้ามี CanvasGroup)
        CanvasGroup cg = wordLearnedPopup.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / 0.25f);
                yield return null;
            }
            cg.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(popupDuration);

        // Fade Out
        if (cg != null)
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / 0.35f);
                yield return null;
            }
        }

        wordLearnedPopup.SetActive(false);
    }

    // ============================================================
    // ZOOM
    // ============================================================

    private void HandleZoom()
    {
        if (playerCamera == null) return;
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            Time.unscaledDeltaTime * zoomSpeed
        );
    }

    // ============================================================
    // VIGNETTE
    // ============================================================

    private void HandleVignette()
    {
        if (aimVignette == null) return;

        float target = isAimTyping ? maxVignetteAlpha : 0f;
        currentVignetteAlpha = Mathf.MoveTowards(
            currentVignetteAlpha, target,
            vignetteFadeSpeed * Time.unscaledDeltaTime
        );

        SetVignetteAlpha(currentVignetteAlpha);

        if (currentVignetteAlpha > 0f && !aimVignette.gameObject.activeSelf)
            aimVignette.gameObject.SetActive(true);
        else if (currentVignetteAlpha <= 0f && aimVignette.gameObject.activeSelf)
            aimVignette.gameObject.SetActive(false);
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (aimVignette.cornerImages == null) return;
        foreach (var img in aimVignette.cornerImages)
        {
            if (img == null) continue;
            Color c = img.color;
            c.a     = alpha;
            img.color = c;
        }
    }

    // ============================================================
    // SLOW MOTION
    // ============================================================

    private void SetSlowMotion(bool state)
    {
        isSlowed             = state;
        Time.timeScale       = isSlowed ? slowTimeScale : 1f;
        Time.fixedDeltaTime  = 0.02f * Time.timeScale;

        // ล็อก Cursor เหมือนระบบเดิม
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ============================================================
    // FOCUS — บังคับให้ InputField โฟกัสได้เลย
    // ============================================================

    private void HandleFocus()
    {
        if (!needsFocus || inputField == null) return;

        inputField.interactable = true;
        inputField.enabled      = true;

        if (!inputField.isFocused)
        {
            inputField.Select();
            inputField.ActivateInputField();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
        else
        {
            needsFocus = false;
            // แสดง Autocomplete เต็มรูปแบบทันทีที่ Focus สำเร็จ
            ShowAutocomplete(currentTargetWord, 0);
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void SpawnVFX(GameObject prefab, Vector3 pos)
    {
        if (prefab != null)
            Destroy(Instantiate(prefab, pos, Quaternion.identity), 2f);
    }

    // ─── Public API ─────────────────────────────────────────────

    /// <summary>ดึงค่า Damage Multiplier ของระบบนี้ไปคำนวณ Combat</summary>
    public float GetDamageMultiplier() => aimDamageMultiplier;

    /// <summary>ตรวจว่ากำลัง Aim Typing อยู่ไหม (ใช้จาก Script อื่น)</summary>
    public bool IsAimTyping => isAimTyping;

    /// <summary>ตรวจว่ากำลัง Zoom เล็งอยู่ไหม</summary>
    public bool IsZooming => isZooming;

    /// <summary>Rebuild Cache ถ้าเพิ่มคำใน ItemData จากภายนอก</summary>
    public void RefreshCache() => RebuildWordCache();
}
