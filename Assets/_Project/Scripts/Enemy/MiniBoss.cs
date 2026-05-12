using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MiniBoss : MonoBehaviour
{
    [Header("--- Boss Stats ---")]
    public float maxHealth = 1000f;
    private float currentHealth;
    
    [Tooltip("เวลารอระหว่างการใช้แต่ละสกิล")]
    public float skillCooldown = 3f;

    [Header("--- Material Hit Effect ---")]
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.1f;
    private Renderer[] renderers;
    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();

    [Header("--- Environment Setup ---")]
    [Tooltip("เลเยอร์ที่ถือว่าเป็น 'พื้น' (เอาไว้เช็คขอบเหวและชั้น 2)")]
    public LayerMask floorLayer;

    [Header("--- Ultimate (8 Rays) ---")]
    public int raysCount = 8;
    public float distanceBetweenObjects = 1.5f;
    [Tooltip("ระยะยืดสูงสุดเผื่อกันกระตุก (ช่อง)")]
    public int maxRayLength = 30; 

    [Header("--- Overdrive (Circles) ---")]
    public int overdriveRings = 4;
    public float ringSpacing = 3f;

    [Header("--- Sky Rain ---")]
    public int rainCountPhase1 = 5;
    public int rainCountPhase2 = 12;

    [Header("--- Summon ---")]
    public GameObject[] summonPrefabs;
    public int summonCount = 3;
    public float summonRadius = 5f;
    [Tooltip("Prefab สำหรับเตือนก่อนมอนจะเกิด (ถ้าไม่ใส่จะไปใช้ warningPrefab แทน)")]
    public GameObject summonWarningPrefab;

    [Header("--- Prefabs ---")]
    public GameObject warningPrefab;
    public GameObject damagePrefab;
    public GameObject voidZonePrefab; 
    [Tooltip("จำนวน Void Zone ที่จะเสกต่อการใช้สกิล 1 ครั้ง")]
    public int voidZoneCount = 1;
    [Header("--- VFX & SFX ---")]
    public ParticleSystem phaseChangeVFX;
    public ParticleSystem ultimateCastVFX;
    public AudioClip phaseChangeSFX;
    public AudioClip ultimateCastSFX;
    public AudioClip ultimateDamageSFX;

    [Header("--- Animator Strings ---")]
    public string attackAnimTrig = "attack";
    public string phase2AnimTrig = "state2";
    
    [Header("--- Visual Behavior (Floating & LookAt) ---")]
    [Tooltip("ลากโมเดลหน้ากากมาใส่ตรงนี้ เพื่อให้มันลอยและหมุนหาผู้เล่นโดยไม่กระทบตำแหน่งหลัก")]
    public Transform visualTransform;
    public float bobSpeed = 1.5f;
    public float bobHeight = 0.3f;
    public bool alwaysFacePlayer = true;
    [Tooltip("หากหน้ากากหันผิดทิศ ให้ปรับ Offset ตรงนี้ (เช่น Y = 90)")]
    public Vector3 faceRotationOffset = Vector3.zero;

    // References
    private Animator animator;
    private AudioSource audioSource;
    private Transform playerTransform;
    private Vector3 initialVisualLocalPos;

    // State
    private bool isPhase2 = false;
    private bool isDead = false;
    private bool isCasting = false;
    private int lastSkillIndex = -1;
    private Coroutine skillLoopCoroutine;
    private Bounds groundBounds;
    private bool hasGroundBounds = false;

    private void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // เก็บสีดั้งเดิมของโมเดลไว้ทำ Hit Flash
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Color[] colors = new Color[r.materials.Length];
            for (int i = 0; i < r.materials.Length; i++)
            {
                if (r.materials[i].HasProperty("_Color"))
                {
                    colors[i] = r.materials[i].color;
                }
            }
            originalColors.Add(r, colors);
        }

        if (visualTransform != null)
        {
            initialVisualLocalPos = visualTransform.localPosition;
        }

        CalculateGroundBounds();
        skillLoopCoroutine = StartCoroutine(SkillLoop());
    }

    /// <summary>ตรวจจับ Bounds ของพื้นที่อยู่ใน floorLayer อัตโนมัติ</summary>
    private void CalculateGroundBounds()
    {
        // ค้นหา Collider รอบตัวบอส (แทนที่จะเป็น 0,0,0) เพื่อความแม่นยำ
        Collider[] floorColliders = Physics.OverlapBox(
            transform.position,
            new Vector3(500f, 500f, 500f),
            Quaternion.identity,
            floorLayer
        );

        if (floorColliders.Length == 0)
        {
            Debug.LogWarning("<color=red>[BOSS]</color> No floor colliders found on floorLayer! Skills may not work correctly.");
            hasGroundBounds = false;
            return;
        }

        // รวม Bounds ของทุก collider เข้าด้วยกัน
        groundBounds = floorColliders[0].bounds;
        foreach (var col in floorColliders)
        {
            groundBounds.Encapsulate(col.bounds);
        }
        hasGroundBounds = true;
        Debug.Log($"<color=green>[BOSS]</color> Ground detected: {floorColliders.Length} colliders, XZ area = ({groundBounds.min.x:F1},{groundBounds.min.z:F1}) to ({groundBounds.max.x:F1},{groundBounds.max.z:F1})");
    }

    /// <summary>สุ่มตำแหน่งบนพื้นภายใน Bounds ที่ตรวจจับไว้</summary>
    private Vector2 GetRandomGroundXZ()
    {
        if (hasGroundBounds)
        {
            float x = Random.Range(groundBounds.min.x, groundBounds.max.x);
            float z = Random.Range(groundBounds.min.z, groundBounds.max.z);
            return new Vector2(x, z);
        }
        // Fallback: ถ้าไม่เจอพื้น ใช้รัศมีคงที่รอบผู้เล่น
        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;
        Vector2 circle = Random.insideUnitCircle * 20f;
        return new Vector2(center.x + circle.x, center.z + circle.y);
    }

    private void Update()
    {
        if (isDead) return;

        HandleVisuals();
    }

    private void HandleVisuals()
    {
        if (visualTransform == null) return;

        // 1. Floating Effect (Bobbing)
        float newY = initialVisualLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visualTransform.localPosition = new Vector3(initialVisualLocalPos.x, newY, initialVisualLocalPos.z);

        // 2. LookAt Player
        if (alwaysFacePlayer && playerTransform != null)
        {
            Vector3 direction = playerTransform.position - visualTransform.position;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                visualTransform.rotation = targetRotation * Quaternion.Euler(faceRotationOffset);
            }
        }
    }

    private IEnumerator SkillLoop()
    {
        yield return new WaitForSeconds(2f);

        while (!isDead)
        {
            if (!isCasting)
            {
                int randomSkill;
                if (!isPhase2)
                {
                    // Phase 1 (สุ่ม 0-1 และห้ามซ้ำท่าเดิม)
                    do
                    {
                        randomSkill = Random.Range(0, 2);
                    } while (randomSkill == lastSkillIndex);
                    
                    lastSkillIndex = randomSkill;
                    Debug.Log($"<color=cyan>[BOSS]</color> Phase 1 Skill Selected: {randomSkill}");

                    if (randomSkill == 0) yield return StartCoroutine(UltimateBarrage());
                    else yield return StartCoroutine(SkyRain(rainCountPhase1));
                }
                else
                {
                    // Phase 2 (สุ่ม 0-3 และห้ามซ้ำท่าเดิม)
                    do
                    {
                        randomSkill = Random.Range(0, 4);
                    } while (randomSkill == lastSkillIndex);
                    
                    lastSkillIndex = randomSkill;
                    Debug.Log($"<color=magenta>[BOSS]</color> Phase 2 Skill Selected: {randomSkill}");

                    if (randomSkill == 0) yield return StartCoroutine(OverdriveBarrage());
                    else if (randomSkill == 1) yield return StartCoroutine(SummonMobs());
                    else if (randomSkill == 2) yield return StartCoroutine(VoidZone());
                    else yield return StartCoroutine(SkyRain(rainCountPhase2)); 
                }
            }
            yield return new WaitForSeconds(skillCooldown);
        }
    }

    // -------------------------------------------------------------------------------------------------
    // FLOOR DETECTION
    // -------------------------------------------------------------------------------------------------

    // คืนค่าจุดบนพื้นจากการยิง Raycast ลงมา
    private Vector3[] GetFloorPositions(Vector2 xzPos, bool allFloors)
    {
        Vector3 rayStart = new Vector3(xzPos.x, transform.position.y + 50f, xzPos.y);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 100f, floorLayer);
        
        if (hits.Length == 0) return null; // ไม่เจอพื้น (ตกขอบ)

        List<Vector3> validPositions = new List<Vector3>();
        
        if (allFloors)
        {
            // เอาทุกชั้นที่เจอ
            foreach (var hit in hits)
            {
                validPositions.Add(hit.point);
            }
        }
        else
        {
            // หาจุดที่สูงสุด (กันทะลุลงไปชั้นล่าง)
            RaycastHit topHit = hits[0];
            foreach (var h in hits) 
            {
                if (h.point.y > topHit.point.y) topHit = h;
            }
            
            validPositions.Add(topHit.point);
        }

        return validPositions.ToArray();
    }

    // -------------------------------------------------------------------------------------------------
    // SKILLS
    // -------------------------------------------------------------------------------------------------

    private IEnumerator UltimateBarrage()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        if (ultimateCastVFX != null) ultimateCastVFX.Play();
        PlaySound(ultimateCastSFX);

        yield return new WaitForSeconds(0.5f);

        float angleStep = 360f / raysCount;
        bool[] activeRays = new bool[raysCount];
        for (int i = 0; i < raysCount; i++) activeRays[i] = true;

        List<List<Vector3>> waves = new List<List<Vector3>>();

        for (int j = 1; j <= maxRayLength; j++)
        {
            List<Vector3> ringPoints = new List<Vector3>();
            int activeCount = 0;

            for (int i = 0; i < raysCount; i++)
            {
                if (!activeRays[i]) continue;

                float angle = i * angleStep;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 targetPos = transform.position + (dir * j * distanceBetweenObjects);
                Vector2 xzPos = new Vector2(targetPos.x, targetPos.z);
                
                Vector3[] floorHits = GetFloorPositions(xzPos, true);
                
                if (floorHits == null || floorHits.Length == 0)
                {
                    activeRays[i] = false; 
                    continue; 
                }
                
                ringPoints.AddRange(floorHits);
                activeCount++;
            }

            if (activeCount == 0) break; 
            waves.Add(ringPoints);
        }

        yield return StartCoroutine(ExecuteWaveAttack(waves, 1.5f, 0.15f));
        isCasting = false;
    }

    private IEnumerator OverdriveBarrage()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        if (ultimateCastVFX != null) ultimateCastVFX.Play();
        PlaySound(ultimateCastSFX);

        yield return new WaitForSeconds(0.5f);

        int manyRaysCount = 16; // เพิ่มแฉกให้เยอะขึ้นตามคำขอ
        float angleStep = 360f / manyRaysCount;
        bool[] activeRays = new bool[manyRaysCount];
        for (int i = 0; i < manyRaysCount; i++) activeRays[i] = true;

        List<List<Vector3>> waves = new List<List<Vector3>>();

        for (int j = 1; j <= 10; j++) // ระยะทาง 10 ระดับ
        {
            List<Vector3> ringPoints = new List<Vector3>();
            int activeCount = 0;

            for (int i = 0; i < manyRaysCount; i++)
            {
                if (!activeRays[i]) continue;

                float angle = i * angleStep;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 targetPos = transform.position + (dir * j * ringSpacing);
                Vector2 xzPos = new Vector2(targetPos.x, targetPos.z);
                
                Vector3[] floorHits = GetFloorPositions(xzPos, false);
                
                if (floorHits == null || floorHits.Length == 0)
                {
                    activeRays[i] = false; 
                    continue; 
                }
                
                ringPoints.Add(floorHits[0]);
                activeCount++;
            }

            if (activeCount == 0) break; 
            waves.Add(ringPoints);
        }

        Debug.Log($"<color=yellow>[BOSS] OverdriveBarrage (16 rays)</color> found {waves.Count} waves.");
        yield return StartCoroutine(ExecuteWaveAttack(waves, 1.5f, 0.15f));
        isCasting = false;
    }

    private IEnumerator ExecuteWaveAttack(List<List<Vector3>> waves, float initialDelay, float waveDelay)
    {
        // 1. สร้างจุดเตือน (Warning) ทั้งหมดพร้อมกันตั้งแต่ต้น
        List<List<GameObject>> waveWarnings = new List<List<GameObject>>();
        foreach (var wave in waves)
        {
            List<GameObject> wList = new List<GameObject>();
            if (warningPrefab != null)
            {
                foreach (var p in wave) wList.Add(Instantiate(warningPrefab, p, Quaternion.identity));
            }
            waveWarnings.Add(wList);
        }

        // 2. หน่วงเวลาให้ผู้เล่นเห็น
        yield return new WaitForSeconds(initialDelay);

        // 3. ปล่อยดาเมจทีละคลื่น
        for (int i = 0; i < waves.Count; i++)
        {
            // ลบจุดเตือนของคลื่นนี้ (ถ้ายังไม่หายไปเอง)
            foreach (var w in waveWarnings[i])
            {
                if (w != null) Destroy(w);
            }

            // เสกดาเมจตามตำแหน่งเดิมที่บันทึกไว้
            PlaySound(ultimateDamageSFX);
            List<GameObject> damages = new List<GameObject>();

            if (damagePrefab != null)
            {
                foreach (var p in waves[i])
                {
                    damages.Add(Instantiate(damagePrefab, p, Quaternion.identity));
                }
            }

            // ตั้งเวลาลบดาเมจ
            StartCoroutine(DestroyDamages(damages, 1.5f));

            // หน่วงเวลาก่อนปล่อยคลื่นถัดไป
            yield return new WaitForSeconds(waveDelay);
        }
    }

    private IEnumerator DestroyDamages(List<GameObject> damages, float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var d in damages) if (d != null) Destroy(d);
    }

    private IEnumerator SkyRain(int count)
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);

        List<Vector3> spawnPoints = new List<Vector3>();

        int attempts = 0;
        int maxAttempts = 200; // \u0e40\u0e1e\u0e34\u0e48\u0e21\u0e08\u0e33\u0e19\u0e27\u0e19 attempts \u0e40\u0e1e\u0e37\u0e48\u0e2d\u0e43\u0e2b\u0e43\u0e2b\u0e49\u0e2a\u0e38\u0e48\u0e21\u0e08\u0e19\u0e40\u0e08\u0e2d\u0e1e\u0e37\u0e49\u0e19\u0e08\u0e23\u0e34\u0e07\u0e46 \u0e08\u0e19\u0e04\u0e23\u0e1a\u0e08\u0e33\u0e19\u0e27\u0e19
        while (spawnPoints.Count < count && attempts < maxAttempts)
        {
            attempts++;
            Vector2 xzPos = GetRandomGroundXZ();
            
            Vector3[] floorHits = GetFloorPositions(xzPos, false);
            if (floorHits != null && floorHits.Length > 0)
            {
                // \u0e40\u0e1e\u0e34\u0e48\u0e21\u0e40\u0e09\u0e1e\u0e32\u0e30\u0e08\u0e38\u0e14\u0e17\u0e35\u0e48\u0e42\u0e14\u0e19\u0e1e\u0e37\u0e49\u0e19\u0e08\u0e23\u0e34\u0e07\u0e40\u0e17\u0e48\u0e32\u0e19\u0e31\u0e49\u0e19
                spawnPoints.Add(floorHits[0]);
            }
        }

        Debug.Log($"<color=yellow>[BOSS] SkyRain (Phase {(isPhase2 ? "2" : "1")})</color> Found {spawnPoints.Count}/{count} valid floor points (attempts: {attempts}).");
        yield return StartCoroutine(SpawnWarningAndDamage(spawnPoints, 1.5f));
        isCasting = false;
    }

    private IEnumerator SummonMobs()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);

        if (summonPrefabs != null && summonPrefabs.Length > 0)
        {
            List<Vector3> spawnPoints = new List<Vector3>();
            float angleStep = 360f / summonCount;
            
            for (int i = 0; i < summonCount; i++)
            {
                float angle = i * angleStep;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 targetPos = transform.position + (dir * summonRadius);
                Vector2 xzPos = new Vector2(targetPos.x, targetPos.z);
                
                Vector3[] floorHits = GetFloorPositions(xzPos, false);
                if (floorHits != null && floorHits.Length > 0)
                {
                    spawnPoints.Add(floorHits[0]);
                }
            }

            List<GameObject> warnings = new List<GameObject>();
            GameObject activeWarningPrefab = summonWarningPrefab != null ? summonWarningPrefab : warningPrefab;
            if (activeWarningPrefab != null)
            {
                foreach (Vector3 p in spawnPoints)
                {
                    warnings.Add(Instantiate(activeWarningPrefab, p, Quaternion.identity));
                }
            }

            yield return new WaitForSeconds(1.5f);

            foreach (GameObject w in warnings)
            {
                if (w != null) Destroy(w);
            }

            foreach (Vector3 p in spawnPoints)
            {
                if (summonPrefabs == null || summonPrefabs.Length == 0)
                {
                    Debug.LogError("<color=red>[BOSS]</color> SummonMobs failed: summonPrefabs is not assigned!");
                    break;
                }

                GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];
                if (prefab != null)
                {
                    Instantiate(prefab, p, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("<color=yellow>[BOSS]</color> SummonMobs: One of the prefabs in the list is null!");
                }
            }
        }

        yield return new WaitForSeconds(1f);
        isCasting = false;
    }

    private IEnumerator VoidZone()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);

        List<Vector3> spawnPoints = new List<Vector3>();

        for (int i = 0; i < voidZoneCount; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            bool foundPos = false;
            int attempts = 0;

            // 1. Find a valid ground position
            while (!foundPos && attempts < 30)
            {
                attempts++;
                Vector2 xzPos = GetRandomGroundXZ();
                Vector3[] floorHits = GetFloorPositions(xzPos, false);
                if (floorHits != null && floorHits.Length > 0)
                {
                    spawnPos = floorHits[0];
                    // ตรวจสอบไม่ให้ทับซ้อนกับจุดเดิมที่สุ่มได้ในรอบนี้ (ถ้าเป็นไปได้)
                    bool tooClose = false;
                    foreach (Vector3 p in spawnPoints)
                    {
                        if (Vector3.Distance(spawnPos, p) < 3f) { tooClose = true; break; }
                    }
                    if (!tooClose) foundPos = true;
                }
            }

            // Fallback to player position if no random ground found for the first one
            if (!foundPos && i == 0 && playerTransform != null)
            {
                Vector3[] playerFloorHits = GetFloorPositions(new Vector2(playerTransform.position.x, playerTransform.position.z), false);
                if (playerFloorHits != null && playerFloorHits.Length > 0)
                {
                    spawnPos = playerFloorHits[0];
                    foundPos = true;
                }
            }

            if (foundPos)
            {
                spawnPoints.Add(spawnPos);
            }
        }

        if (spawnPoints.Count > 0)
        {
            // 2. Spawn Warnings
            List<GameObject> warnings = new List<GameObject>();
            if (warningPrefab != null)
            {
                foreach (Vector3 p in spawnPoints)
                {
                    warnings.Add(Instantiate(warningPrefab, p, Quaternion.identity));
                }
            }
            else
            {
                Debug.LogWarning("<color=red>[BOSS]</color> VoidZone Warning failed: warningPrefab is null!");
            }

            yield return new WaitForSeconds(1.5f); // Warning duration

            // 3. Spawn actual Void Zones
            foreach (GameObject w in warnings) if (w != null) Destroy(w);

            if (voidZonePrefab != null)
            {
                foreach (Vector3 p in spawnPoints)
                {
                    GameObject vz = Instantiate(voidZonePrefab, p, Quaternion.identity);
                    Debug.Log($"<color=green>[BOSS]</color> VoidZone spawned at {p}.");
                }
            }
            else
            {
                Debug.LogError("<color=red>[BOSS]</color> VoidZone failed: voidZonePrefab is null!");
            }
        }

        yield return new WaitForSeconds(1f);
        isCasting = false;
    }

    private IEnumerator SpawnWarningAndDamage(List<Vector3> points, float delay)
    {
        List<GameObject> warnings = new List<GameObject>();
        if (warningPrefab != null)
        {
            foreach (Vector3 p in points) warnings.Add(Instantiate(warningPrefab, p, Quaternion.identity));
        }

        yield return new WaitForSeconds(delay);

        // ลบ Warning ที่ยังเหลืออยู่
        foreach (GameObject w in warnings)
        {
            if (w != null) Destroy(w);
        }

        PlaySound(ultimateDamageSFX);
        List<GameObject> damages = new List<GameObject>();
        
        if (damagePrefab != null)
        {
            foreach (Vector3 p in points)
            {
                damages.Add(Instantiate(damagePrefab, p, Quaternion.identity));
            }
        }

        yield return new WaitForSeconds(1.5f);
        foreach (GameObject d in damages) if (d != null) Destroy(d);
    }

    // -------------------------------------------------------------------------------------------------
    // DAMAGE & PHASES
    // -------------------------------------------------------------------------------------------------

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        StartCoroutine(FlashHit());

        if (!isPhase2 && currentHealth <= (maxHealth / 2f))
        {
            StartCoroutine(EnterPhase2());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyDamage(float damage) { TakeDamage(Mathf.RoundToInt(damage)); }

    /// <summary>บังคับให้ Boss ใช้สกิล Summon ทันที (ใช้จาก Dev Panel)</summary>
    public void ForceUseSummon()
    {
        if (isDead) return;
        StopAllCoroutines(); // หยุดการทำงานทั้งหมดเพื่อเคลียร์สถานะ
        isCasting = false;
        StartCoroutine(SummonMobs());
        // เริ่ม SkillLoop ใหม่หลังจาก Summon เสร็จ
        StartCoroutine(ResumeSkillLoopAfterSummon());
        Debug.Log("<color=orange>[DEV]</color> Boss force-used SummonMobs!");
    }

    /// <summary>บังคับให้ Boss ใช้สกิล VoidZone ทันที (ใช้จาก Dev Panel)</summary>
    public void ForceUseVoidZone()
    {
        if (isDead) return;
        StopAllCoroutines(); // หยุดการทำงานทั้งหมดเพื่อเคลียร์สถานะ
        isCasting = false;
        StartCoroutine(VoidZone());
        // เริ่ม SkillLoop ใหม่หลังจาก VoidZone เสร็จ
        StartCoroutine(ResumeSkillLoopAfterVoidZone());
        Debug.Log("<color=orange>[DEV]</color> Boss force-used VoidZone!");
    }

    /// <summary>บังคับให้ Boss เล่นสกิล (random) แล้วตายทันทีหลังจากนั้น</summary>
    public void ForceSkillThenDie()
    {
        if (isDead) return;
        StopAllCoroutines();
        StartCoroutine(SkillThenDieRoutine());
        Debug.Log("<color=orange>[DEV]</color> Boss will play a skill then die!");
    }

    private IEnumerator SkillThenDieRoutine()
    {
        // สุ่มสกิลที่จะเล่น
        int skill = Random.Range(0, isPhase2 ? 4 : 2);
        if (skill == 0)      yield return StartCoroutine(UltimateBarrage());
        else if (skill == 1) yield return StartCoroutine(SkyRain(rainCountPhase1));
        else if (skill == 2) yield return StartCoroutine(OverdriveBarrage());
        else if (skill == 3) yield return StartCoroutine(SummonMobs());

        // ตายทันทีหลังสกิลเสร็จ
        TakeDamage(Mathf.CeilToInt(currentHealth) + 1);
    }


    private IEnumerator ResumeSkillLoopAfterSummon()
    {
        yield return new WaitUntil(() => !isCasting);
        yield return new WaitForSeconds(skillCooldown);
        skillLoopCoroutine = StartCoroutine(SkillLoop());
    }

    private IEnumerator ResumeSkillLoopAfterVoidZone()
    {
        yield return new WaitUntil(() => !isCasting);
        yield return new WaitForSeconds(skillCooldown);
        skillLoopCoroutine = StartCoroutine(SkillLoop());
    }

    private IEnumerator FlashHit()
    {
        foreach (Renderer r in renderers)
        {
            for (int i = 0; i < r.materials.Length; i++)
            {
                if (r.materials[i].HasProperty("_Color")) r.materials[i].color = hitColor;
            }
        }

        yield return new WaitForSeconds(hitFlashDuration);

        foreach (Renderer r in renderers)
        {
            if (originalColors.ContainsKey(r))
            {
                Color[] orig = originalColors[r];
                for (int i = 0; i < r.materials.Length; i++)
                {
                    if (r.materials[i].HasProperty("_Color") && i < orig.Length)
                        r.materials[i].color = orig[i];
                }
            }
        }
    }

    private IEnumerator EnterPhase2()
    {
        isPhase2 = true;
        isCasting = true;

        // หยุด SkillLoop เดิมโดยใช้ Reference ที่เก็บไว้
        if (skillLoopCoroutine != null)
        {
            StopCoroutine(skillLoopCoroutine);
            skillLoopCoroutine = null;
        }

        // เล่น Animation / VFX / SFX เปลี่ยนเฟส
        if (animator != null) animator.SetTrigger(phase2AnimTrig);
        if (phaseChangeVFX != null) phaseChangeVFX.Play();
        PlaySound(phaseChangeSFX);

        yield return new WaitForSeconds(2.0f);

        // *** ท่า Entrance ของ Phase 2: ปล่อย SummonMobs ทันที ***
        Debug.Log("<color=magenta>[BOSS]</color> Phase 2 Entry Skill: SummonMobs!");
        yield return StartCoroutine(SummonMobs());

        // หลัง Summon เสร็จ: รีเซ็ต lastSkillIndex ให้เป็น 1 (SummonMobs)
        // เพื่อไม่ให้สุ่มได้ Summon ซ้ำทันที
        lastSkillIndex = 1;
        isCasting = false;

        // เริ่ม SkillLoop ปกติในโหมด Phase 2
        skillLoopCoroutine = StartCoroutine(SkillLoop());
        Debug.Log("<color=magenta>[BOSS]</color> Phase 2 SkillLoop started!");
    }

    private void Die()
    {
        isDead = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 3f);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }
}
