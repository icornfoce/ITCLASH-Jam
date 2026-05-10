using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TypingSystem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private GameObject typingUI; 
    [SerializeField] private TMP_InputField inputField;

    [Header("─── Aim Typing Guard ───")]
    [Tooltip("ลาก AimTypingSystem มาใส่ — ป้องกัน TypingUI โผล่ตอนกำลัง Aim อยู่")]
    [SerializeField] private AimTypingSystem aimTypingSystem;

    [Header("Vignette Effects")]
    [Tooltip("ลาก Q_Vignette_Base (ตั้งเป็นสีโทนมืด) มาใส่ช่องนี้")]
    [SerializeField] private Q_Vignette_Base typingVignette;
    [Tooltip("ความเร็วในการเข้มขึ้น/จางลงของ Overlay")]
    [SerializeField] private float overlayFadeSpeed = 10f;
    [Tooltip("ความเข้มสูงสุดตอนพิมพ์ (0-1)")]
    [SerializeField] private float maxOverlayAlpha = 0.8f;

    private float currentOverlayAlpha = 0f;

    [Header("Matched Items Slots")]
    [SerializeField] private ItemInfo firstItem;
    [SerializeField] private ItemInfo secondItem;

    [Header("Settings")]
    [Range(0.1f, 1f)] [SerializeField] private float slowTimeScale = 0.2f;
    [SerializeField] private Transform shakeTransform; 
    
    [Header("Shake Settings (Local Fallback)")]
    [SerializeField] private int shakeWordLengthThreshold = 10;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    [Header("Shake Settings (Scriptable Object)")]
    [SerializeField] private TypingShakeSettings shakeSettings; 
    
    private bool isSlowed = false;
    private bool needsFocus = false;

    [Header("Audio Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSFX;
    [SerializeField] private AudioClip matchSFX;
    [SerializeField] private AudioClip combineSFX;
    [SerializeField] private AudioClip releaseSFX;
    [SerializeField] private AudioClip errorSFX;

    [Header("Visual Effects (VFX)")]
    [SerializeField] private GameObject matchVFXPrefab;
    [SerializeField] private GameObject combineVFXPrefab;
    [SerializeField] private GameObject releaseVFXPrefab;
    [SerializeField] private GameObject correctTypingVFXPrefab;
    [SerializeField] private GameObject errorTypingVFXPrefab;
    [SerializeField] private GameObject typingSuccessVFXPrefab;
    [SerializeField] private GameObject typingFailureVFXPrefab;

    [Header("Real-time SFX")]
    [SerializeField] private AudioClip[] typingSFXPool;
    [SerializeField] private AudioClip typingSuccessSFX;
    [SerializeField] private AudioClip typingFailureSFX;

    [Header("Flow State Settings")]
    [SerializeField] private int wordsToTriggerFlow = 5;
    // [SerializeField] private float flowTriggerWindow = 30f;
    [SerializeField] private float flowDuration = 10f;
    // [SerializeField] private int lettersToAutoComplete = 3;
    [SerializeField] private AudioClip flowStateStartSFX;
    [SerializeField] private ComboSystem comboSystem;

    private List<float> typedWordTimestamps = new List<float>();
    private float flowStateEndTime = -1f;
    private bool isFlowStateActive => Time.time < flowStateEndTime;
    private bool isInternalUpdate = false;
    private string lastInput = "";

    // เก็บตำแหน่งเป้าหมายล่าสุดสำหรับแต่ละช่อง
    private Vector3? firstItemTargetPos;
    private Vector3? secondItemTargetPos;

    [Header("Item Spawning Settings")]
    [SerializeField] private Transform playerTransform; // ลาก Player มาใส่ตรงนี้ (ถ้าไม่ใส่จะหา Tag "Player" อัตโนมัติ)
    [SerializeField] private Vector3 firstSlotOffset = new Vector3(-0.6f, 1.8f, -1.0f);
    [SerializeField] private Vector3 secondSlotOffset = new Vector3(0.6f, 1.8f, -1.0f);
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private float followSpeed = 5f;
    [Range(0f, 1f)] [SerializeField] private float itemAlpha = 0.5f; // ความโปร่งใสของไอเทมตอนที่ยังไม่ปล่อย
    
    private GameObject spawnedFirst;
    private GameObject spawnedSecond;
    private Vector3 currentShakeOffset;

    // Properties to access the current main item
    public string ItemName => firstItem?.itemName ?? string.Empty;
    public GameObject ItemPrefab => firstItem?.itemPrefab;
    public Vector3 ItemSize => firstItem?.itemSize ?? Vector3.zero;

    private void Awake()
    {
        // รีเซ็ตสถานะการปลดล็อคทั้งหมดเมื่อเริ่มเกมใหม่ (ล็อคทุกอย่างไว้ก่อน)
        // ผู้เล่นจะต้องไป Scan (AimTypingSystem) หรือเดินชนไอเทม (Item.cs) เพื่อปลดล็อค
        if (itemData != null)
        {
            foreach (var item in itemData.items)
            {
                item.isUnlocked = false;
            }
        }

        // ถ้าไม่ได้ลาก Player มาใน Inspector ให้หาจาก Tag "Player"
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else playerTransform = transform; // ถ้าหาไม่เจอจริงๆ ให้ใช้ตัวเองไปก่อน
        }

        // กำหนดโปร่งใสเริ่มต้น
        if (typingVignette != null)
        {
            currentOverlayAlpha = 0f;
            SetTypingVignetteAlpha(0f);
            typingVignette.gameObject.SetActive(false);
        }

        // Subscribe to input changes
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputValueChanged);
        }
    }



    void Update()
    {
        // ยิงด้วยคลิกซ้าย และห้ามยิงตอนที่กำลังเล็ง (Zooming) อยู่
        bool isAiming = aimTypingSystem != null && aimTypingSystem.IsZooming;
        if (Input.GetMouseButtonDown(0) && !isAiming)
        {
            ReleaseItem();
            
            // ให้คลิกซ้ายปิดหน้าต่างพิมพ์และค่อยๆ จาง Vignette เหมือนการกด ESC
            if (isSlowed)
            {
                SetSlowMotion(false);
            }
        }

        // ป้องกัน TypingSystem เปิดตอน AimTypingSystem กำลังทำงานอยู่
        bool aimActive = aimTypingSystem != null && aimTypingSystem.IsAimTyping;
        if (Input.GetKeyDown(KeyCode.E) && !isSlowed && !aimActive)
        {
            OpenTyping();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isSlowed)
        {
            SetSlowMotion(false);
        }

        // ตรวจสอบการกด Enter เพื่อส่งคำศัพท์ (ส่งเฉพาะตอนที่เปิดหน้าต่างพิมพ์อยู่)
        if (isSlowed && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            if (inputField != null)
            {
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    string textToSubmit = inputField.text;

                    // If not in Flow State, only count what was NOT selected (actually typed)
                    if (!isFlowStateActive)
                    {
                        // The typed length is where the selection starts
                        int typedLength = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                        textToSubmit = inputField.text.Substring(0, typedLength);
                    }

                    Debug.Log($"[TypingSystem] Submitting: {textToSubmit}");
                    TryMatchItem(textToSubmit.Trim(), null);
                }
                else
                {
                    SetSlowMotion(false);
                }
                
                inputField.text = "";
            }
        }

        // อัปเดตความเข้มของ Q Vignette ตอนพิมพ์
        if (typingVignette != null)
        {
            float targetAlpha = isSlowed ? maxOverlayAlpha : 0f;
            currentOverlayAlpha = Mathf.MoveTowards(currentOverlayAlpha, targetAlpha, overlayFadeSpeed * Time.unscaledDeltaTime);
            SetTypingVignetteAlpha(currentOverlayAlpha);

            if (currentOverlayAlpha > 0 && !typingVignette.gameObject.activeSelf)
            {
                typingVignette.gameObject.SetActive(true);
            }
            else if (currentOverlayAlpha <= 0 && typingVignette.gameObject.activeSelf)
            {
                typingVignette.gameObject.SetActive(false);
            }
        }

        // บังคับโฟกัสช่องพิมพ์จนกว่าจะสำเร็จ
        if (needsFocus && inputField != null)
        {
            // บังคับให้สถานะพร้อมใช้งาน
            inputField.interactable = true;
            inputField.enabled = true;

            if (!inputField.isFocused)
            {
                inputField.Select();
                inputField.ActivateInputField();
                
                if (EventSystem.current != null)
                {
                    // บังคับเลือกวัตถุ
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                    
                    // จำลองการคลิก
                    PointerEventData eventData = new PointerEventData(EventSystem.current);
                    inputField.OnPointerClick(eventData);
                }
            }
            else
            {
                needsFocus = false;
                // เลื่อนไปท้ายสุดเพื่อความพร้อม
                inputField.MoveTextEnd(false);
            }
        }

        // ทำให้ไอเทมที่ลอยอยู่มีการขยับขึ้นลง (Bobbing Effect)
        ApplyFloatingEffect();
    }

    private Vector3? GetCameraLookPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        // ยิง Ray จากกึ่งกลางหน้าจอ (เป้าเล็ง)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // ระยะสูงสุด 50 เมตร
        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            return hit.point;
        }
        
        return null;
    }

    void LateUpdate()
    {
        if (currentShakeOffset != Vector3.zero)
        {
            Transform target = shakeTransform != null ? shakeTransform : (Camera.main != null ? Camera.main.transform : null);
            if (target != null)
            {
                target.localPosition += currentShakeOffset;
            }
        }
    }

    private void SetTypingVignetteAlpha(float alpha)
    {
        if (typingVignette.cornerImages == null) return;
        for (int i = 0; i < typingVignette.cornerImages.Length; i++)
        {
            if (typingVignette.cornerImages[i] != null)
            {
                Color c = typingVignette.cornerImages[i].color;
                c.a = alpha;
                typingVignette.cornerImages[i].color = c;
            }
        }
    }

    private void ApplyFloatingEffect()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        
        if (spawnedFirst != null)
        {
            Vector3 targetLocalPos = new Vector3(
                firstSlotOffset.x, 
                firstSlotOffset.y + bob, 
                firstSlotOffset.z - (firstItem.itemSize.z * 0.5f)
            );
            // ใช้ Lerp เพื่อให้การตาม Player ดูนุ่มนวลขึ้น
            spawnedFirst.transform.localPosition = Vector3.Lerp(
                spawnedFirst.transform.localPosition, 
                targetLocalPos, 
                Time.deltaTime * followSpeed
            );
            // ให้หันหน้าไปทางเดียวกับ Player
            spawnedFirst.transform.localRotation = Quaternion.Lerp(
                spawnedFirst.transform.localRotation, 
                Quaternion.identity, 
                Time.deltaTime * followSpeed
            );
        }
        
        if (spawnedSecond != null)
        {
            Vector3 targetLocalPos = new Vector3(
                secondSlotOffset.x, 
                secondSlotOffset.y + bob, 
                secondSlotOffset.z - (secondItem.itemSize.z * 0.5f)
            );
            spawnedSecond.transform.localPosition = Vector3.Lerp(
                spawnedSecond.transform.localPosition, 
                targetLocalPos, 
                Time.deltaTime * followSpeed
            );
            spawnedSecond.transform.localRotation = Quaternion.Lerp(
                spawnedSecond.transform.localRotation, 
                Quaternion.identity, 
                Time.deltaTime * followSpeed
            );
        }
    }

    public bool TryMatchItem(string input, Vector3? targetPos = null)
    {
        Debug.Log($"<color=white>[TypingSystem] Attempting to match: '{input}'</color>");
        
        if (itemData == null) 
        {
            Debug.LogError("[TypingSystem] ItemData is NOT assigned in the Inspector!");
            return false;
        }

        if (itemData.items == null || itemData.items.Count == 0)
        {
            Debug.LogWarning("[TypingSystem] ItemData list is empty!");
        }

        // Check for special commands
        if (string.Equals(input, "logout", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[TypingSystem] Logout command triggered.");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
            SetSlowMotion(false);
            return true;
        }

        foreach (var item in itemData.items)
        {
            bool isMatch = string.Equals(item.itemName, input, System.StringComparison.OrdinalIgnoreCase);
            
            if (isMatch)
            {
                Debug.Log($"[TypingSystem] MATCH FOUND: '{item.itemName}' (Unlocked: {item.isUnlocked})");
                
                if (item.isUnlocked) // เช็คว่าปลดล็อค (แตะในด่าน) แล้วหรือยัง
                {
                    // Determine settings (Use SO if assigned, otherwise use local)
                    int threshold = shakeSettings != null ? shakeSettings.wordLengthThreshold : shakeWordLengthThreshold;
                    float duration = shakeSettings != null ? shakeSettings.duration : shakeDuration;
                    float magnitude = shakeSettings != null ? shakeSettings.magnitude : shakeMagnitude;

                    Debug.Log($"[TypingSystem] Triggering Effects for '{input}'. Length: {input.Length}, Threshold: {threshold}");
                    
                    // Shake screen if word is long enough
                    if (input.Length >= threshold)
                    {
                        StartCoroutine(ShakeScreen(duration, magnitude));
                    }
                    else
                    {
                        Debug.Log($"[TypingSystem] Word too short for shake ({input.Length} < {threshold})");
                    }

                    SpawnVFX(correctTypingVFXPrefab, playerTransform.position);
                    
                    // Track for Combo and Flow State (Success)
                    if (comboSystem != null) 
                    {
                        comboSystem.AddCombo();
                    }
                    else
                    {
                        Debug.LogError("[TypingSystem] CRITICAL: ComboSystem is NOT assigned in the Inspector!");
                    }
                    
                    RecordTypedWord();

                    ProcessItemMatch(item, targetPos);
                    SetSlowMotion(false);
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[TypingSystem] Item '{item.itemName}' is locked!");
                    break; 
                }
            }
        }

        Debug.LogWarning($"[TypingSystem] No match found for '{input}' in {itemData.items.Count} items.");
        
        // ถ้าพิมพ์ผิด หรือหาไม่เจอ ให้ปิดหน้าต่างพิมพ์เหมือนกัน
        SpawnVFX(errorTypingVFXPrefab, playerTransform.position);
        SetSlowMotion(false);
        PlaySFX(errorSFX);
        return false;
    }

    private void ProcessItemMatch(ItemInfo matchedItem, Vector3? targetPos)
    {
        // ถ้าช่องแรกว่าง ให้ใส่ช่องแรก
        if (firstItem == null || string.IsNullOrEmpty(firstItem.itemName))
        {
            firstItem = matchedItem;
            firstItemTargetPos = targetPos; // จำตำแหน่งเป้าหมาย
            spawnedFirst = SpawnItemAtOffset(firstItem, firstSlotOffset);
            
            PlaySFX(matchSFX);
            Vector3 vfxPos = spawnedFirst != null ? spawnedFirst.transform.position : playerTransform.position;
            SpawnVFX(matchVFXPrefab, vfxPos);
            Debug.Log($"<color=cyan>First Item: {firstItem.itemName}</color>");
        }
        // ถ้าช่องสองว่าง ให้ใส่ช่องสอง
        else if (secondItem == null || string.IsNullOrEmpty(secondItem.itemName))
        {
            secondItem = matchedItem;
            secondItemTargetPos = targetPos; // จำตำแหน่งเป้าหมาย
            spawnedSecond = SpawnItemAtOffset(secondItem, secondSlotOffset);
            
            PlaySFX(matchSFX);
            Vector3 vfxPos = spawnedSecond != null ? spawnedSecond.transform.position : playerTransform.position;
            SpawnVFX(matchVFXPrefab, vfxPos);
            Debug.Log($"<color=cyan>Second Item: {secondItem.itemName}</color>");
            CheckCombination();
        }
        // ถ้าเต็มทั้งสองช่อง ให้เปลี่ยนอันที่สองเป็นอันใหม่
        else
        {
            if (spawnedSecond != null) Destroy(spawnedSecond);
            secondItem = matchedItem;
            secondItemTargetPos = targetPos; // จำตำแหน่งเป้าหมาย
            spawnedSecond = SpawnItemAtOffset(secondItem, secondSlotOffset);
            
            PlaySFX(matchSFX);
            Vector3 vfxPos = spawnedSecond != null ? spawnedSecond.transform.position : playerTransform.position;
            SpawnVFX(matchVFXPrefab, vfxPos);
            CheckCombination();
        }
    }

    private GameObject SpawnItemAtOffset(ItemInfo item, Vector3 relativeOffset)
    {
        if (item.itemPrefab == null) return null;

        // สร้างไอเทมโดยให้มันเป็นลูกของ playerTransform เพื่อให้มันเคลื่อนที่ตาม Player
        GameObject obj = Instantiate(item.itemPrefab, playerTransform);
        obj.transform.localScale = item.itemSize;
        
        // กำหนดตำแหน่ง local ทันที (Update จะคอยคุมตำแหน่ง floating ต่อ)
        obj.transform.localPosition = new Vector3(
            relativeOffset.x, 
            relativeOffset.y, 
            relativeOffset.z - (item.itemSize.z * 0.5f)
        );
        
        obj.transform.localRotation = Quaternion.identity;

        // ทำให้ไอเทมดูใสๆ (Transparency)
        ApplyTransparency(obj, itemAlpha);

        return obj;
    }

    private void ApplyTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // ใช้ .materials เพื่อสร้าง instance ของ material มาแก้ (ไม่กระทบไฟล์ต้นฉบับ)
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;

                    // --- สำหรับ Standard Shader ---
                    if (alpha < 1.0f)
                    {
                        mat.SetFloat("_Mode", 3); // 3 คือ Transparent mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                    }
                    else
                    {
                        mat.SetFloat("_Mode", 0); // 0 คือ Opaque mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.SetInt("_ZWrite", 1);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.DisableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = -1;
                    }

                    // --- สำหรับ URP (Universal Render Pipeline) ---
                    if (mat.HasProperty("_Surface"))
                    {
                        mat.SetFloat("_Surface", alpha < 1.0f ? 1 : 0); // 1 = Transparent, 0 = Opaque
                        mat.SetFloat("_Blend", 0); // 0 = Alpha blend
                    }
                }
            }
        }
    }

    private void CheckCombination()
    {
        foreach (var combo in itemData.combinations)
        {
            bool match1 = (combo.itemAName == firstItem.itemName && combo.itemBName == secondItem.itemName);
            bool match2 = (combo.itemBName == firstItem.itemName && combo.itemAName == secondItem.itemName);

            if (match1 || match2)
            {
                Debug.Log($"<color=yellow>Combination Success! Result: {combo.resultItem.itemName}</color>");
                
                // ทำลายของเก่าทั้งคู่
                if (spawnedFirst != null) Destroy(spawnedFirst);
                if (spawnedSecond != null) Destroy(spawnedSecond);

                // ตั้งค่าไอเทมใหม่
                firstItem = combo.resultItem;
                secondItem = null;
                spawnedSecond = null;
                
                // สร้างไอเทมผลลัพธ์
                spawnedFirst = SpawnItemAtOffset(firstItem, firstSlotOffset);
                
                PlaySFX(combineSFX);
                SpawnVFX(combineVFXPrefab, spawnedFirst.transform.position);
                return;
            }
        }
        
        Debug.Log("No combination found. Keeping both items in their slots.");
        // ไม่ต้องทำอะไร ปล่อยให้มันลอยอยู่คนละฝั่งตามปกติ
    }

    public void SetSpawnedItemsVisibility(bool isVisible)
    {
        if (spawnedFirst != null) spawnedFirst.SetActive(isVisible);
        if (spawnedSecond != null) spawnedSecond.SetActive(isVisible);
    }

    public void ReleaseItem()
    {
        // ปล่อยชิ้นที่สองก่อน (ถ้ามี)
        if (secondItem != null)
        {
            PerformRelease(ref secondItem, ref spawnedSecond, secondItemTargetPos);
            secondItemTargetPos = null; // ล้างตำแหน่ง
        }
        // ถ้าไม่มีชิ้นที่สอง ให้ปล่อยชิ้นแรก
        else if (firstItem != null)
        {
            PerformRelease(ref firstItem, ref spawnedFirst, firstItemTargetPos);
            firstItemTargetPos = null; // ล้างตำแหน่ง
        }
    }

    private void PerformRelease(ref ItemInfo item, ref GameObject obj, Vector3? targetPos = null)
    {
        if (item == null) return;

        // ── เรียกใช้ Skill ของไอเทมนี้ (ถ้ามี) ──
        if (item.itemSkill != null)
        {
            Vector3 spawnPos;
            bool isProjectile = item.itemSkill.GetComponent<BaseProjectileSkill>() != null;

            // ถ้ามีตำแหน่งเป้าหมาย (จากการ Aim) และไม่ใช่ Projectile ให้ใช้ตำแหน่งนั้นเลย!
            if (targetPos.HasValue && !isProjectile)
            {
                spawnPos = targetPos.Value;
            }
            else if (isProjectile && Camera.main != null)
            {
                // ถ้าเป็น Projectile ให้เกิดจากกล้องเพื่อให้ออกมาจากมุมมองผู้เล่นตรงๆ
                Transform camTransform = Camera.main.transform;
                spawnPos = camTransform.position
                    + camTransform.right * item.skillSpawnOffset.x
                    // ข้ามการบวกแกน Y เพราะกล้องอยู่ที่ระดับสายตาแล้ว ถ้าบวกอีกเดี๋ยวจะทะลุหัว
                    + camTransform.forward * (item.skillSpawnOffset.z > 0 ? item.skillSpawnOffset.z : 1f);
            }
            else
            {
                // ถ้าไม่มีเป้าหมาย หรือไม่ใช่ Projectile ให้เกิดหน้า Player ตามปกติ
                spawnPos = playerTransform.position
                    + playerTransform.right * item.skillSpawnOffset.x
                    + playerTransform.up * item.skillSpawnOffset.y
                    + playerTransform.forward * item.skillSpawnOffset.z;
            }

            // คำนวณองศาการเกิด โดยอ้างอิงจากมุมที่ Player หันหน้าอยู่ และบวกด้วยองศาที่ตั้งไว้ใน ItemData
            Quaternion spawnRot = playerTransform.rotation * Quaternion.Euler(item.skillSpawnEulerAngles);

            GameObject skillObj = Instantiate(item.itemSkill, spawnPos, spawnRot);
            
            BaseItemSkill skill = skillObj.GetComponent<BaseItemSkill>();
            if (skill != null)
            {
                skill.TargetPosition = targetPos; // ส่งตำแหน่งเป้าหมายไปให้ Skill
                skill.Activate(playerTransform);
            }
        }
        
        PlaySFX(releaseSFX);
        SpawnVFX(releaseVFXPrefab, playerTransform.position);

        Debug.Log($"Released: {item.itemName}");

        // ทำลายไอเทมที่ลอยอยู่ (ถ้ามี)
        if (obj != null) Destroy(obj);
        
        // ล้างสถานะ
        item = null;
        obj = null;
    }

    public void OpenTyping()
    {
        Debug.Log("[TypingSystem] Opening Typing UI (Slow Motion Active)");
        SetSlowMotion(true);
        PlaySFX(openSFX);
    }

    private void SetSlowMotion(bool state)
    {
        isSlowed = state;
        Time.timeScale = isSlowed ? slowTimeScale : 1f;
        Time.fixedDeltaTime = 0.02f * (isSlowed ? slowTimeScale : 1f);

        if (typingUI != null)
        {
            typingUI.SetActive(isSlowed);
        }

        if (isSlowed && inputField != null)
        {
            inputField.text = "";
            lastInput = "";
            needsFocus = true; // เริ่มกระบวนการบังคับโฟกัสใน Update
            
            // ถ้ามี CanvasGroup ให้เปิดให้หมด
            CanvasGroup cg = typingUI.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            // ไม่ต้องปลดล็อกเมาส์ (ซ่อนไว้ตามเดิมตามที่ต้องการ)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            needsFocus = false;
            // ล็อกเมาส์ไว้ปกติ
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            // PlayOneShot is great because it can overlap and isn't affected by AudioSource.Stop()
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnInputValueChanged(string newValue)
    {
        // Don't trigger feedback if we are in the middle of an internal update
        if (isInternalUpdate || !isSlowed) return;

        // Play random typing sound if pool is available
        if (typingSFXPool != null && typingSFXPool.Length > 0 && !string.IsNullOrEmpty(newValue))
        {
            if (audioSource != null) audioSource.PlayOneShot(typingSFXPool[Random.Range(0, typingSFXPool.Length)]);
        }

        if (string.IsNullOrEmpty(newValue)) return;

        // Unified Prediction Logic
        bool isDeleting = !string.IsNullOrEmpty(lastInput) && newValue.Length < lastInput.Length;
        lastInput = newValue;

        if (!isDeleting && itemData != null)
        {
            foreach (var item in itemData.items)
            {
                // Only consider unlocked items that start with the input
                if (item.isUnlocked && item.itemName.Length > newValue.Length &&
                    item.itemName.StartsWith(newValue, System.StringComparison.OrdinalIgnoreCase))
                {
                    isInternalUpdate = true;
                    int typedLength = newValue.Length;
                    inputField.text = item.itemName;
                    inputField.selectionAnchorPosition = typedLength;
                    inputField.selectionFocusPosition = item.itemName.Length;
                    isInternalUpdate = false;
                    break; // Found first match
                }
            }
        }

        // Keep the VFX feedback (using same text matching logic)
        bool hasPossibleMatch = false;
        if (itemData != null)
        {
            foreach (var item in itemData.items)
            {
                if (item.isUnlocked && item.itemName.StartsWith(newValue, System.StringComparison.OrdinalIgnoreCase))
                {
                    hasPossibleMatch = true;
                    break;
                }
            }
        }

        if (hasPossibleMatch)
        {
            SpawnVFX(typingSuccessVFXPrefab, playerTransform.position);
        }
        else
        {
            SpawnVFX(typingFailureVFXPrefab, playerTransform.position);
        }
    }

    private void RecordTypedWord()
    {
        float currentTime = Time.time;
        
        // Use combo count to trigger Flow State
        int currentCount = comboSystem != null ? comboSystem.CurrentCombo : 0;

        if (currentCount >= wordsToTriggerFlow && !isFlowStateActive)
        {
            flowStateEndTime = currentTime + flowDuration;
            Debug.Log($"<color=magenta>[TypingSystem] FLOW STATE ACTIVATED! Combo: {currentCount}</color>");
            PlaySFX(flowStateStartSFX);
        }
    }

    // Right-click the TypingSystem component and select "Test Shake" to trigger this manually
    [ContextMenu("Test Shake")]
    public void TestShake()
    {
        if (Application.isPlaying) StartCoroutine(ShakeScreen(0.5f, 0.5f));
        else Debug.LogWarning("You can only test the shake while in Play Mode.");
    }

    private IEnumerator ShakeScreen(float duration, float magnitude)
    {
        Debug.Log($"<color=yellow>[TypingSystem] SHAKE START! Duration: {duration}, Magnitude: {magnitude}</color>");
        
        Camera cam = Camera.main;
        Rect originalRect = cam != null ? cam.rect : new Rect(0, 0, 1, 1);
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float xOffset = Random.Range(-1f, 1f) * magnitude;
            float yOffset = Random.Range(-1f, 1f) * magnitude;

            // Method 1: Transform Offset (Applied in LateUpdate)
            currentShakeOffset = new Vector3(xOffset, yOffset, 0);

            // Method 2: Viewport Rect (Shifts the actual rendered image - Impossible to override)
            if (cam != null)
            {
                float rectX = originalRect.x + (xOffset * 0.05f);
                float rectY = originalRect.y + (yOffset * 0.05f);
                cam.rect = new Rect(rectX, rectY, originalRect.width, originalRect.height);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        currentShakeOffset = Vector3.zero;
        if (cam != null) cam.rect = originalRect;

        Debug.Log("<color=yellow>[TypingSystem] SHAKE FINISHED.</color>");
    }

    private void SpawnVFX(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
            // Optional: Auto-destroy VFX after 2 seconds to keep the scene clean
            Destroy(vfx, 2f);
        }
    }



    public void ClearMatch()
    {
        if (spawnedFirst != null) Destroy(spawnedFirst);
        if (spawnedSecond != null) Destroy(spawnedSecond);
        
        firstItem = null;
        secondItem = null;
        spawnedFirst = null;
        spawnedSecond = null;
    }
}
