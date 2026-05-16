using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using Unity.Cinemachine; 

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

    [Header("─── Highlight Settings ───")]
    [Tooltip("สีที่จะใช้ไฮไลท์ตอนเล็ง")]
    [SerializeField] private Color highlightColor = Color.cyan;
    [Tooltip("ความเข้มของการเรืองแสง (Emission)")]
    [SerializeField] private float emissionIntensity = 2f;
    [Tooltip("ความเร็วในการกระพริบ")]
    [SerializeField] private float flashSpeed = 5f;

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

    [Tooltip("Panel ที่จะวาง Letter Slots ของ Rhythm (ใช้ HorizontalLayoutGroup)")]
    [SerializeField] private Transform rhythmLetterContainer;

    [Tooltip("TMP_InputField ช่องพิมพ์")]
    [SerializeField] private TMP_InputField inputField;

    [Tooltip("สีพื้นหลังไฮไลท์ของตัวอักษรช่วยพิมพ์ (ลดค่า Alpha ให้จางลง)")]
    [SerializeField] private Color autocompleteHighlightColor = new Color(0.1f, 0.4f, 0.8f, 0.15f); // ฟ้าจางมากๆ

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

    [Tooltip("ลาก RhythmTypingManager มาใส่ — ระบบพิมพ์ตามจังหวะ BGM (ถ้าไม่ใส่จะพิมพ์แบบเดิม)")]
    [SerializeField] private RhythmTypingManager rhythmTypingManager;

    // ───────────────────────────────────────────────────────────


    [Header("─── Slow Motion ───")]
    [Range(0.05f, 1f)]
    [Tooltip("ค่า TimeScale ตอนล็อกเป้า (0.2 = ช้า 5 เท่า)")]
    [SerializeField] private float slowTimeScale = 0.2f;

    // ============================================================
    // PRIVATE STATE
    // ============================================================

    private Camera playerCamera;
    private CinemachineCamera vcam;

    // Raycast & Highlighting
    private float scanTimer;
    private GameObject currentHoveredObject;
    
    // โครงสร้างข้อมูลสำหรับเก็บ Renderer และค่าสีดั้งเดิม
    private struct RendererData
    {
        public Renderer renderer;
        public Color[] originalColors;
        public Color[] originalEmissions;
    }
    private List<RendererData> targetRenderers = new List<RendererData>();
    private MaterialPropertyBlock propBlock;

    // States
    private bool isZooming;        // กำลัง Scope/Zoom อยู่ (กดคลิกขวา 1)
    private bool isAimTyping;      // Text Input เปิดอยู่ (กดคลิกขวา 2)
    private GameObject lockedTarget;
    private Vector3 lockedHitPoint; 
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
        propBlock = new MaterialPropertyBlock();

        // ค้นหา Camera
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = GetComponentInParent<Camera>();

        if (playerCamera == null)
            Debug.LogError($"[AimTypingSystem] ❌ ไม่พบ Camera! กรุณาตั้ง Tag กล้องเป็น 'MainCamera' หรือลาก Script นี้ไปไว้ที่เดียวกับกล้อง", this);

        vcam = GetComponent<CinemachineCamera>();
        if (vcam == null) vcam = GetComponentInParent<CinemachineCamera>();

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
        {
            inputField.onValueChanged.AddListener(OnInputValueChanged);
            inputField.selectionColor = autocompleteHighlightColor; // ปรับสีไฮไลท์
        }

        // Fallback for AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnInputValueChanged);
        }
    }

    private void OnDisable()
    {
        ResetHighlight();
    }

    private void Update()
    {
        HandleRaycast();
        HandleInput();
        HandleZoom();
        HandleVignette();
        HandleFocus();
        HandleHighlightEffect();

        // --- ระบบหมุนลูกกลิ้งเมาส์เพื่อเลือกคำศัพท์ขณะเล็ง ---
        if (isAimTyping && itemData != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                HandleScrollSelection(scroll);
            }
        }
    }

    private int scrollIndex = -1;
    private List<ItemInfo> unlockedItemsCache = new List<ItemInfo>();

    private void HandleScrollSelection(float scrollDelta)
    {
        // สร้าง Cache ของไอเทมที่ปลดล็อคแล้ว (ถ้ายังไม่มี)
        if (unlockedItemsCache.Count == 0)
        {
            foreach (var item in itemData.items)
            {
                if (item.isUnlocked) unlockedItemsCache.Add(item);
            }
        }

        if (unlockedItemsCache.Count == 0 || inputField == null) return;

        // เลื่อน Index
        if (scrollDelta > 0) scrollIndex--;
        else scrollIndex++;

        if (scrollIndex < 0) scrollIndex = unlockedItemsCache.Count - 1;
        if (scrollIndex >= unlockedItemsCache.Count) scrollIndex = 0;

        // ใส่คำศัพท์
        isInternalUpdate = true;
        inputField.text = unlockedItemsCache[scrollIndex].itemName;
        inputField.selectionAnchorPosition = 0;
        inputField.selectionFocusPosition = inputField.text.Length;
        isInternalUpdate = false;
        
        Debug.Log($"[AimTyping] Scrolled to: {inputField.text}");
    }

    // ============================================================
    // RAYCAST — ยิงเฉพาะตอน Zoom อยู่และยังไม่ได้พิมพ์
    // ============================================================

    private void HandleRaycast()
    {
        // ยิง Raycast ตลอดเวลาที่ Zoom แต่ยังไม่ได้ Confirm สแกน
        if (!isZooming || isAimTyping) 
        {
            if (!isAimTyping) SetHoveredObject(null);
            return;
        }

        scanTimer += Time.unscaledDeltaTime;
        if (scanTimer < scanInterval) return;
        scanTimer = 0f;

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, scanRange, scanLayerMask))
        {
            if (hit.collider.CompareTag("Scannable"))
            {
                SetHoveredObject(hit.collider.gameObject);
                lockedHitPoint = hit.point; // อัปเดตจุดที่เล็งไว้ตลอด
                return;
            }
        }

        SetHoveredObject(null);
    }

    private void SetHoveredObject(GameObject obj)
    {
        if (currentHoveredObject == obj) return;

        // คืนสีให้วัตถุเก่า
        ResetHighlight();

        currentHoveredObject = obj;
        if (obj == null) return;

        // กวาดเอา Renderer ทั้งหมด
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r == null || r.sharedMaterials == null) continue;

            Material[] origMats = r.sharedMaterials; // ใช้ sharedMaterials เพื่อหลีกเลี่ยงการสร้าง Instance เด็ดขาด
            if (origMats == null || origMats.Length == 0) continue;

            Color[] baseColors = new Color[origMats.Length];
            Color[] emissionColors = new Color[origMats.Length];

            for (int i = 0; i < origMats.Length; i++)
            {
                Material m = origMats[i];
                if (m == null) continue;

                // ดึงค่าสีหลัก (พยายามดึง _BaseColor ของ URP ก่อน)
                if (m.HasProperty("_BaseColor")) baseColors[i] = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) baseColors[i] = m.GetColor("_Color");
                else baseColors[i] = m.color;

                // ดึงค่าสี Emission
                if (m.HasProperty("_EmissionColor")) emissionColors[i] = m.GetColor("_EmissionColor");
                else emissionColors[i] = Color.black;
            }

            targetRenderers.Add(new RendererData 
            { 
                renderer = r,
                originalColors = baseColors,
                originalEmissions = emissionColors
            });
        }

        if (targetWordLabel != null)
        {
            targetWordLabel.gameObject.SetActive(true);
            targetWordLabel.text = GetWordFromObject(obj);
        }
    }

    private void HandleHighlightEffect()
    {
        if (currentHoveredObject == null || targetRenderers.Count == 0 || isAimTyping) return;

        // คำนวณจังหวะการกระพริบ (0.0 ถึง 1.0)
        float pulse = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;

        foreach (var data in targetRenderers)
        {
            if (data.renderer == null) continue;

            for (int i = 0; i < data.originalColors.Length; i++)
            {
                // ดึงบล็อคของคุณสมบัติ
                data.renderer.GetPropertyBlock(propBlock, i);

                // 1. ปรับสีหลัก + Alpha
                Color lerpedColor = Color.Lerp(data.originalColors[i], highlightColor, pulse);
                lerpedColor.a = Mathf.Lerp(data.originalColors[i].a, highlightColor.a * pulse, pulse);
                
                // เซ็ตทั้งสองชื่อเลย เพื่อให้ครอบคลุมทั้ง URP และ Built-in
                propBlock.SetColor("_Color", lerpedColor);
                propBlock.SetColor("_BaseColor", lerpedColor);

                // 2. ปรับ Emission (เรืองแสง)
                Color targetEmission = highlightColor * (emissionIntensity * pulse);
                propBlock.SetColor("_EmissionColor", Color.Lerp(data.originalEmissions[i], targetEmission, pulse));

                // นำไปใส่คืน
                data.renderer.SetPropertyBlock(propBlock, i);
            }
        }
    }

    private void ResetHighlight()
    {
        foreach (var data in targetRenderers)
        {
            if (data.renderer == null) continue;

            for (int i = 0; i < data.originalColors.Length; i++)
            {
                // ถอด PropertyBlock ออก เพื่อให้คืนค่า Material แบบดั้งเดิม
                data.renderer.SetPropertyBlock(null, i);
            }
        }
        targetRenderers.Clear();
    }

    // ============================================================
    // INPUT HANDLING
    // ============================================================

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAll();
            return;
        }

        // ── คลิกซ้าย ──
        if (Input.GetMouseButtonDown(0))
        {
            // 1. ถ้ากำลังพิมพ์อยู่ -> ยกเลิกการแสกน
            if (isAimTyping)
            {
                CancelAll();
                return;
            }

            // 2. ถ้าแค่กำลังซูม (Aiming) อยู่เฉยๆ
            if (isZooming)
            {
                // ตรวจสอบว่าใน TypingSystem มีไอเทมให้ปล่อยไหม
                bool hasItem = typingSystem != null && !string.IsNullOrEmpty(typingSystem.ItemName);
                
                if (!hasItem)
                {
                    // ถ้าไม่มีไอเทมให้ยิง ให้การคลิกซ้ายเป็นการยกเลิกการซูม (แบบเดิม)
                    CancelAll();
                    return;
                }
                else
                {
                    // ถ้ามีไอเทม ให้ปล่อยให้ TypingSystem.Update ทำงาน (ยิงไอเทม)
                    // และเราไม่ต้องเรียก CancelAll() ตรงนี้ เพื่อให้ผู้เล่นยิงขณะซูมได้
                }
            }
        }

        // ── คลิกขวา ──
        if (Input.GetMouseButtonDown(1))
        {
            // 1. ถ้าพิมพ์อยู่ -> ยกเลิกการพิมพ์ (แต่ยัง Zoom อยู่)
            if (isAimTyping)
            {
                CancelTyping();
                return;
            }

            // 2. ถ้ากำลัง Zoom และเล็งวัตถุอยู่ -> กดยืนยันการสแกน (ครั้งที่ 2)
            if (isZooming && currentHoveredObject != null)
            {
                LockTarget(currentHoveredObject, lockedHitPoint);
                return;
            }

            // 3. ถ้ากำลัง Zoom แต่ไม่ได้เล็งอะไร -> ออกจากโหมดเล็ง
            if (isZooming)
            {
                ExitZoom();
                return;
            }

            // 4. ถ้าปกติ -> ลอง Raycast ก่อน ถ้าเล็งวัตถุ Scannable อยู่ ให้เข้าโหมดพิมพ์ทันที (1 คลิก)
            {
                Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                Ray ray = playerCamera.ScreenPointToRay(screenCenter);

                if (Physics.Raycast(ray, out RaycastHit hit, scanRange, scanLayerMask)
                    && hit.collider.CompareTag("Scannable"))
                {
                    // เข้า Zoom + Lock Target ในคลิกเดียว
                    EnterZoom();
                    SetHoveredObject(hit.collider.gameObject);
                    LockTarget(hit.collider.gameObject, hit.point);
                    return;
                }
            }

            // 5. ถ้าไม่ได้เล็งอะไร -> เข้าโหมดเล็งก่อน (Zoom only)
            EnterZoom();
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

        // เราเลือกที่จะไม่ซ่อนของที่ถืออยู่ เพื่อให้ผู้เล่นรู้ว่ากำลังถืออะไรอยู่ (เช่น DisplayManSkill)
        // if (typingSystem != null) typingSystem.SetSpawnedItemsVisibility(false);
    }

    private void ExitZoom()
    {
        isZooming   = false;
        targetFOV   = normalFOV;
        SetSlowMotion(false);

        if (crosshairHighlight != null) crosshairHighlight.SetActive(false);
        if (crosshairNormal != null)    crosshairNormal.SetActive(true);

        // ไม่ต้องสั่งให้แสดงผลใหม่เพราะเราไม่ได้ซ่อนไว้แล้ว
        // if (typingSystem != null) typingSystem.SetSpawnedItemsVisibility(true);

        CancelTyping();
    }

    // ============================================================
    // LOCK TARGET & START TYPING
    // ============================================================

    private void LockTarget(GameObject target, Vector3 hitPoint)
    {
        lockedTarget        = target;
        lockedHitPoint      = hitPoint;
        currentTargetWord   = GetWordFromObject(target);
        isAimTyping         = true;

        PlaySFX(lockOnSFX);
        SetSlowMotion(true);

        // รีเซ็ตสถานะการเลือกคำศัพท์ด้วย Scroll
        unlockedItemsCache.Clear();
        scrollIndex = -1;

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

        // ── Rhythm Typing: เริ่มแสดงตัวอักษรตามจังหวะ Beat ทันทีที่ล็อกเป้า ──
        if (rhythmTypingManager != null)
        {
            rhythmTypingManager.StartRhythmTyping(currentTargetWord, slowTimeScale, rhythmLetterContainer);
        }
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

        // ตรวจสอบว่าสิ่งที่แสกนเป็นศัตรูหรือไม่ (มี NavMeshAgent, IDamageable หรือ Tag Enemy)
        bool isEnemy = false;
        if (lockedTarget != null)
        {
            if (lockedTarget.CompareTag("Enemy") || 
                lockedTarget.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null || 
                lockedTarget.GetComponentInParent<ITCLASH.Enemies.IDamageable>() != null)
            {
                isEnemy = true;
            }
        }

        // ส่งคำไปให้ TypingSystem ใช้งาน
        // ถ้ายิงใส่ศัตรู ให้ส่งตำแหน่งเป้าหมายไป (เพื่อให้ยิงติดตามเป้า)
        // แต่ถ้าสแกนสิ่งของรอบตัว (เช่น ตะเกียงบนโต๊ะ) ให้ส่ง null เพื่อให้ยิงตามเป้าเล็ง (Crosshair) ตามปกติ
        if (typingSystem != null)
        {
            typingSystem.TryMatchItem(currentTargetWord, isEnemy ? lockedHitPoint : (Vector3?)null);
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

        // ── หยุดระบบ Rhythm Typing เมื่อยกเลิกการพิมพ์ ──
        if (rhythmTypingManager != null && rhythmTypingManager.IsActive)
        {
            rhythmTypingManager.StopRhythmTyping();
        }

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
        if (this == null) return;
        if (isInternalUpdate || !isAimTyping) return;

        // เล่นเสียงพิมพ์ — ใช้ Pool ของตัวเองก่อน ถ้าว่างให้ Fallback ไปใช้ของ TypingSystem
        if (!string.IsNullOrEmpty(newValue))
        {
            PlayTypingSFX();
        }

        if (string.IsNullOrEmpty(newValue)) return;

        bool isDeleting = !string.IsNullOrEmpty(lastInput) && newValue.Length < lastInput.Length;

        // ── Rhythm Typing: บล็อคไม่ให้ลบตัวอักษร (Backspace) ──
        if (isDeleting && rhythmTypingManager != null && rhythmTypingManager.IsActive)
        {
            isInternalUpdate = true;
            inputField.text = lastInput;
            inputField.caretPosition = inputField.text.Length;
            isInternalUpdate = false;
            return;
        }

        lastInput = inputField.text;

        // ── Rhythm Typing: ส่งตัวอักษรที่เพิ่งพิมพ์ไปประเมินจังหวะ ──
        if (!isDeleting && rhythmTypingManager != null && rhythmTypingManager.IsActive)
        {
            char lastTypedChar = newValue[newValue.Length - 1];
            RhythmRating rating = rhythmTypingManager.ProcessKeyPress(lastTypedChar);
            Debug.Log($"[AimTyping] Rhythm Rating: {rating} for '{lastTypedChar}'");

            if (rating == RhythmRating.Miss)
            {
                // ปล่อยตัวที่พิมพ์ผิดไว้ในช่อง (เพื่อให้ตรงกับที่กด) และให้ RhythmTypingManager ข้ามไปเลย
            }

            if (rhythmTypingManager.IsWordCompleted)
            {
                SubmitWord();
                return;
            }
        }

        // แสดง Autocomplete เฉพาะตอนพิมพ์ถูกทิศ (ไม่ใช่ตอนลบ)
        if (!isDeleting && currentTargetWord.StartsWith(newValue, System.StringComparison.OrdinalIgnoreCase))
            ShowAutocomplete(currentTargetWord, newValue.Length);
    }

    private void PlayTypingSFX()
    {
        // ลองใช้ Pool ของ AimTypingSystem ก่อน
        AudioClip[] pool = typingSFXPool;
        AudioSource source = audioSource;

        // Fallback: ถ้า Pool ว่างให้ขอยืมจาก TypingSystem
        if ((pool == null || pool.Length == 0) && typingSystem != null)
        {
            pool   = typingSystem.TypingSFXPool;
            source = typingSystem.TypingAudioSource;
        }

        if (pool != null && pool.Length > 0 && source != null)
        {
            source.pitch = Random.Range(0.9f, 1.1f);
            source.PlayOneShot(pool[Random.Range(0, pool.Length)]);
        }
        else
        {
            if (pool == null || pool.Length == 0)
                Debug.LogWarning("[AimTypingSystem] Typing SFX Pool is empty in both AimTypingSystem and TypingSystem!");
            else if (source == null)
                Debug.LogWarning("[AimTypingSystem] AudioSource is null — add one to the Player.");
        }
    }

    private void ShowAutocomplete(string fullWord, int typedLength)
    {
        if (inputField == null) return;

        // ── ถ้ากำลังเล่น Rhythm อยู่ ไม่ต้องใส่คำใบ้ล่วงหน้าในช่องพิมพ์ ──
        if (rhythmTypingManager != null && rhythmTypingManager.IsActive)
            return;

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
        // 1. ตัดตัวเลขทั้งหมดออกจากชื่อ (เช่น "Enemy123" -> "Enemy", "Table (1)" -> "Table")
        string result = Regex.Replace(raw, @"\d+", "");

        // 2. แปลง Underscore → Space
        result = result.Replace("_", " ");

        // 3. Lowercase และ Trim
        result = result.Trim().ToLower();

        // 4. ลบวงเล็บเปล่าๆ ที่อาจเหลือจากการตัดตัวเลข (เช่น "Table ()")
        result = result.Replace("()", "").Replace("[]", "");

        // 5. ลบ Space ซ้ำ
        result = Regex.Replace(result, @"\s+", " ");

        return result.Trim();
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
        if (vcam != null)
        {
            // Cinemachine 3 — ปรับที่ Lens.FieldOfView ของ Virtual Camera
            var lens = vcam.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFOV, Time.unscaledDeltaTime * zoomSpeed);
            vcam.Lens = lens;
        }
        else if (playerCamera != null)
        {
            // Fallback: ปรับที่ Camera ตรงๆ (ถ้าไม่ใช้ Cinemachine)
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                Time.unscaledDeltaTime * zoomSpeed
            );
        }
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
        CursorUnlocker.ApplyLock();
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
