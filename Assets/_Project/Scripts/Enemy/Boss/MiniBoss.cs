using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using ITCLASH.Enemies;

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
    public LayerMask floorLayer;
    [Tooltip("รัศมีการสุ่มตำแหน่งสกิลรอบตัวบอส")]
    public float globalRandomRadius = 25f;

    [Header("--- Eight Rays (Phase 1) ---")]
    public int raysCount = 8;
    public float raySpacing = 1.5f;
    public int rayLength = 12;

    [Header("--- Overdrive Scatter (Phase 2) ---")]
    public int overdriveWaves = 6;
    public int pointsPerWave = 8;
    public float waveSpacing = 4f;
    public float pointRandomness = 3f;

    [Header("--- Prefabs ---")]
    public GameObject warningPrefab;
    public GameObject damagePrefab;
    public GameObject voidZonePrefab; 
    public int voidZoneCount = 1;
    public float voidZoneDuration = 5f;

    [Header("--- VFX & SFX ---")]
    public ParticleSystem phaseChangeVFX;
    public ParticleSystem ultimateCastVFX;
    public AudioClip phaseChangeSFX;
    public AudioClip ultimateCastSFX;
    public AudioClip ultimateDamageSFX;
    public AudioClip spawnAlertSFX;
    public ParticleSystem spawnAlertVFX;

    [Header("--- Cinematics & UI ---")]
    public PlayableDirector introTimeline;
    public PlayableDirector deathTimeline;
    [Tooltip("UI ทั้งหมดที่จะให้หายไปตอนจบเกม (เช่น HP Bar, Crosshair)")]
    public GameObject gameUIContainer;
    [Tooltip("UI ตอนชนะ (Victory Screen)")]
    public GameObject victoryUI;

    [Header("--- HP Threshold Waves (Shield Mechanic) ---")]
    public GameObject spawnProjectilePrefab;
    
    [System.Serializable]
    public class BossWaveThreshold
    {
        public string waveName = "Wave X";
        public List<GameObject> enemyPrefabs;
        public int spawnCount = 4;
        public float damageToBoss = 250f;
    }
    
    public List<BossWaveThreshold> hpThresholdWaves;
    private int currentWaveThresholdIndex = 0;
    private bool isInvulnerable = false;
    private ITCLASH.Spawners.WaveManager waveManager;

    [Header("--- Animator Strings ---")]
    public string attackAnimTrig = "attack";
    public string phase2AnimTrig = "state2";

    [Header("--- Visual Behavior ---")]
    public Transform visualTransform;
    public float bobSpeed = 1.5f;
    public float bobHeight = 0.3f;
    public bool alwaysFacePlayer = true;
    public Vector3 faceRotationOffset = Vector3.zero;

    // References
    private Animator animator;
    private AudioSource audioSource;
    private Transform playerTransform;
    private Vector3 initialVisualLocalPos;

    // State
    private bool isPhase2 = false;
    private bool isDead = false;
    public bool IsDead => isDead;
    private bool isCasting = false;
    private List<GameObject> activeSkillObjects = new List<GameObject>();
    private Dictionary<GameObject, Queue<GameObject>> prefabPools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        waveManager = Object.FindFirstObjectByType<ITCLASH.Spawners.WaveManager>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.materials != null)
            {
                Color[] colors = new Color[r.materials.Length];
                for (int i = 0; i < r.materials.Length; i++)
                    if (r.materials[i].HasProperty("_Color")) colors[i] = r.materials[i].color;
                originalColors[r] = colors;
            }
        }
        if (visualTransform != null) initialVisualLocalPos = visualTransform.localPosition;

        if (BossHealthUI.Instance != null) BossHealthUI.Instance.Initialize(this, "The Corrupted Mask");

        // เริ่มระบบ Cinematic หรือเริ่มเกมทันที
        if (introTimeline != null)
        {
            StartCoroutine(IntroCinematicSequence());
        }
        else
        {
            StartCoroutine(SkillCycle());
            StartCoroutine(StartInitialWaveDelayed());
        }
    }

    private IEnumerator IntroCinematicSequence()
    {
        isCasting = true; // ล็อคบอสไว้ไม่ให้ใช้สกิล
        introTimeline.Play();
        
        // รอจนกว่าคัทซีนจะเล่นจบ
        yield return new WaitUntil(() => introTimeline.state != PlayState.Playing);
        
        isCasting = false;
        StartCoroutine(SkillCycle());
        StartCoroutine(StartInitialWaveDelayed());
    }

    private IEnumerator StartInitialWaveDelayed()
    {
        yield return new WaitForSeconds(2f);
        if (hpThresholdWaves.Count > 0) StartCoroutine(SpawnMinionsThresholdSequence(hpThresholdWaves[0]));
    }

    public float GetCurrentHealth() => currentHealth;

    public void ApplyBossDamage(float amount)
    {
        if (isDead || isInvulnerable) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        StartCoroutine(HitEffectRoutine());
        if (currentHealth <= maxHealth * 0.5f && !isPhase2) EnterPhase2();
        if (currentHealth <= 0) Die();
        else
        {
            currentWaveThresholdIndex++;
            if (currentWaveThresholdIndex < hpThresholdWaves.Count)
                StartCoroutine(SpawnMinionsThresholdSequence(hpThresholdWaves[currentWaveThresholdIndex]));
        }
    }

    public void ApplyDamage(float amount) { /* Immune */ }

    [Header("--- Fixed Spawn Settings ---")]
    [Tooltip("ลาก Transform ของจุดเกิดต่างๆ ในแมพมาใส่ที่นี่ มอนสเตอร์จะเกิดตรงนี้เสมอ")]
    public List<Transform> fixedSpawnPoints = new List<Transform>();
    private int nextSpawnPointIndex = 0;

    private IEnumerator SpawnMinionsThresholdSequence(BossWaveThreshold waveData)
    {
        isInvulnerable = true;
        Debug.Log($"<color=cyan>[MiniBoss]</color> Starting Wave: {waveData.waveName} (Spawn Count: {waveData.spawnCount})");
        
        PlaySound(spawnAlertSFX);
        if (spawnAlertVFX != null) Instantiate(spawnAlertVFX, transform.position, Quaternion.identity);
        
        yield return new WaitForSeconds(1.0f);

        if (fixedSpawnPoints != null && fixedSpawnPoints.Count > 0 && waveData.enemyPrefabs != null && waveData.enemyPrefabs.Count > 0)
        {
            for (int i = 0; i < waveData.spawnCount; i++)
            {
                Transform targetPoint = fixedSpawnPoints[nextSpawnPointIndex];
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % fixedSpawnPoints.Count;

                GameObject prefabToSpawn = waveData.enemyPrefabs[Random.Range(0, waveData.enemyPrefabs.Count)];
                
                if (spawnProjectilePrefab != null)
                {
                    // วิธีที่ 1: ยิงกระสุนไปที่จุดเกิด
                    GameObject projObj = Instantiate(spawnProjectilePrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
                    
                    Collider bossCol = GetComponent<Collider>();
                    Collider projCol = projObj.GetComponent<Collider>();
                    if (bossCol != null && projCol != null) Physics.IgnoreCollision(bossCol, projCol);

                    BossSpawnProjectile proj = projObj.GetComponent<BossSpawnProjectile>();
                    if (proj != null)
                    {
                        Debug.Log($"[MiniBoss] Shooting towards Transform: {targetPoint.name}");
                        // ส่ง targetPoint (Transform) ไปตรงๆ เลยครับ
                        proj.Launch(prefabToSpawn, targetPoint, 25f, ultimateCastVFX != null ? ultimateCastVFX.gameObject : null);
                    }
                }
                else
                {
                    // วิธีที่ 2: เสกลงจุดเกิดตรงๆ (Fallback ถ้าไม่มีพรีแฟบกระสุน)
                    Debug.Log($"[MiniBoss] No projectile prefab, spawning {prefabToSpawn.name} directly at {targetPoint.position}");
                    Instantiate(prefabToSpawn, targetPoint.position, Quaternion.identity);
                    if (ultimateCastVFX != null) Instantiate(ultimateCastVFX, targetPoint.position, Quaternion.identity);
                }
                
                yield return new WaitForSeconds(0.3f);
            }
        }
        else
        {
            Debug.LogError($"<color=red>[MiniBoss]</color> CANNOT SPAWN! Please check if 'fixedSpawnPoints' list is empty or 'Enemy Prefabs' in the wave is empty!");
        }
        
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(MonitorMinions(waveData.damageToBoss));
        yield return new WaitForSeconds(0.5f);
        isInvulnerable = false;
    }

    private IEnumerator MonitorMinions(float damageOnClear)
    {
        yield return new WaitForSeconds(0.5f);
        List<EnemyController> waveMinions = new List<EnemyController>(EnemyRegistry.All);
        while (true)
        {
            bool anyAlive = false;
            foreach (var m in waveMinions) if (m != null && m.IsAlive) { anyAlive = true; break; }
            if (!anyAlive) break;
            yield return new WaitForSeconds(0.5f);
        }
        ApplyBossDamage(damageOnClear);
    }

    private IEnumerator HitEffectRoutine()
    {
        foreach (Renderer r in renderers) if (r != null) foreach (Material m in r.materials) if (m.HasProperty("_Color")) m.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        foreach (Renderer r in renderers)
        {
            if (r == null || !originalColors.ContainsKey(r)) continue;
            Color[] colors = originalColors[r];
            for (int i = 0; i < r.materials.Length; i++) if (i < colors.Length && r.materials[i].HasProperty("_Color")) r.materials[i].color = colors[i];
        }
    }

    private void Update() { if (!isDead) HandleVisuals(); }

    private void HandleVisuals()
    {
        if (visualTransform == null) return;
        float newY = initialVisualLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visualTransform.localPosition = new Vector3(initialVisualLocalPos.x, newY, initialVisualLocalPos.z);
        if (alwaysFacePlayer && playerTransform != null)
        {
            Vector3 direction = playerTransform.position - visualTransform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f) visualTransform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(faceRotationOffset);
        }
    }

    private void EnterPhase2()
    {
        isPhase2 = true;
        if (animator != null) animator.SetTrigger(phase2AnimTrig);
        if (phaseChangeVFX != null) Instantiate(phaseChangeVFX, transform.position, Quaternion.identity);
        PlaySound(phaseChangeSFX);
    }

    private IEnumerator SkillCycle()
    {
        yield return new WaitForSeconds(2f);
        while (!isDead)
        {
            if (!isCasting)
            {
                if (!isPhase2)
                {
                    int skill = Random.Range(0, 2);
                    if (skill == 0) yield return StartCoroutine(SkyRain(5));
                    else yield return StartCoroutine(SeparateEightRays());
                }
                else
                {
                    int skill = Random.Range(0, 3);
                    if (skill == 0) yield return StartCoroutine(SkyRain(10));
                    else if (skill == 1) yield return StartCoroutine(OverdriveBarrage());
                    else yield return StartCoroutine(VoidZone());
                }
            }
            yield return new WaitForSeconds(skillCooldown);
        }
    }

    // --- Skills Implementation ---
    private IEnumerator SeparateEightRays()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        PlaySound(ultimateCastSFX);
        yield return new WaitForSeconds(0.5f);
        List<List<Vector3>> waves = new List<List<Vector3>>();
        for (int i = 0; i < raysCount; i++)
        {
            Vector3 centerPos = GetRandomGroundPosition();
            float randomAngle = Random.Range(0f, 360f);
            Vector3 dir = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
            for (int j = 1; j <= rayLength; j++)
            {
                if (waves.Count < j) waves.Add(new List<Vector3>());
                Vector3 targetPos = centerPos + (dir * j * raySpacing);
                Vector3[] floorHits = GetFloorPositions(new Vector2(targetPos.x, targetPos.z), false);
                if (floorHits != null && floorHits.Length > 0) waves[j-1].Add(floorHits[0]);
            }
        }
        yield return StartCoroutine(ExecuteWaveAttack(waves, 1.2f, 0.08f));
        isCasting = false;
    }

    private IEnumerator OverdriveBarrage()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        if (ultimateCastVFX != null) Instantiate(ultimateCastVFX, transform.position, Quaternion.identity);
        PlaySound(ultimateCastSFX);
        yield return new WaitForSeconds(0.5f);
        List<List<Vector3>> waves = new List<List<Vector3>>();
        List<Vector3> usedPositions = new List<Vector3>();
        for (int w = 1; w <= overdriveWaves; w++)
        {
            List<Vector3> wavePoints = new List<Vector3>();
            float currentRadius = w * waveSpacing;
            for (int i = 0; i < pointsPerWave; i++)
            {
                float angle = (i * (360f/pointsPerWave)) + Random.Range(-20f, 20f);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 targetPos = transform.position + (dir * (currentRadius + Random.Range(-pointRandomness, pointRandomness)));
                Vector3[] floorHits = GetFloorPositions(new Vector2(targetPos.x, targetPos.z), false);
                if (floorHits != null && floorHits.Length > 0)
                {
                    Vector3 pos = floorHits[0];
                    bool tooClose = false;
                    foreach (var used in usedPositions) if (Vector3.Distance(pos, used) < 2f) { tooClose = true; break; }
                    if (!tooClose) { wavePoints.Add(pos); usedPositions.Add(pos); }
                }
            }
            if (wavePoints.Count > 0) waves.Add(wavePoints);
        }
        yield return StartCoroutine(ExecuteWaveAttack(waves, 1.5f, 0.15f));
        isCasting = false;
    }

    private IEnumerator ExecuteWaveAttack(List<List<Vector3>> waves, float initialDelay, float waveDelay)
    {
        List<List<GameObject>> waveWarnings = new List<List<GameObject>>();
        foreach (var wave in waves)
        {
            List<GameObject> wList = new List<GameObject>();
            if (warningPrefab != null) foreach (var p in wave) wList.Add(SpawnSkillObject(warningPrefab, p, Quaternion.identity));
            waveWarnings.Add(wList);
        }
        yield return new WaitForSeconds(initialDelay);
        for (int i = 0; i < waves.Count; i++)
        {
            foreach (var w in waveWarnings[i]) ReturnToPool(warningPrefab, w);
            PlaySound(ultimateDamageSFX);
            if (damagePrefab != null) foreach (var p in waves[i]) SpawnSkillObject(damagePrefab, p, Quaternion.identity, 0.5f);
            yield return new WaitForSeconds(waveDelay);
        }
    }

    private IEnumerator SkyRain(int count)
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);
        List<Vector3> spawnPoints = new List<Vector3>();
        for (int i = 0; i < count; i++) { Vector3 pos = GetRandomGroundPosition(); if (pos != transform.position) spawnPoints.Add(pos); }
        List<GameObject> warnings = new List<GameObject>();
        if (warningPrefab != null) foreach (Vector3 p in spawnPoints) warnings.Add(SpawnSkillObject(warningPrefab, p, Quaternion.identity));
        yield return new WaitForSeconds(1.5f);
        foreach (GameObject w in warnings) ReturnToPool(warningPrefab, w);
        if (damagePrefab != null) foreach (Vector3 p in spawnPoints) SpawnSkillObject(damagePrefab, p, Quaternion.identity, 1f);
        isCasting = false;
    }

    private IEnumerator VoidZone()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);
        List<Vector3> spawnPoints = new List<Vector3>();
        for (int i = 0; i < voidZoneCount; i++) { Vector3 pos = GetRandomGroundPosition(); if (pos != transform.position) spawnPoints.Add(pos); }
        if (voidZonePrefab != null) foreach (Vector3 p in spawnPoints) SpawnSkillObject(voidZonePrefab, p, Quaternion.identity, voidZoneDuration);
        yield return new WaitForSeconds(1f);
        isCasting = false;
    }

    private Vector3 GetRandomGroundPosition()
    {
        for (int i = 0; i < 50; i++)
        {
            float r = Random.Range(2f, globalRandomRadius);
            float angle = Random.Range(0f, 360f);
            Vector3 targetPos = transform.position + (new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * r);
            Vector3[] floorHits = GetFloorPositions(new Vector2(targetPos.x, targetPos.z), false);
            if (floorHits != null && floorHits.Length > 0) return floorHits[0];
        }
        return transform.position;
    }

    private Vector3[] GetFloorPositions(Vector2 xzPos, bool allFloors)
    {
        // ยิง Raycast โดยข้าม Triggers ทั้งหมด เพื่อไม่ให้ติด Collider ตัวเอง
        RaycastHit[] hits = Physics.RaycastAll(
            new Vector3(xzPos.x, transform.position.y + 50f, xzPos.y), 
            Vector3.down, 
            100f, 
            floorLayer, 
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0) return null;
        List<Vector3> valid = new List<Vector3>();
        if (allFloors)
        {
            foreach (var h in hits) valid.Add(h.point);
        }
        else 
        { 
            RaycastHit top = hits[0]; 
            foreach (var h in hits) if (h.point.y > top.point.y) top = h; 
            valid.Add(top.point); 
        }
        return valid.ToArray();
    }

    private GameObject SpawnSkillObject(GameObject prefab, Vector3 pos, Quaternion rot, float delay = -1f)
    {
        if (prefab == null) return null;
        if (!prefabPools.ContainsKey(prefab)) prefabPools[prefab] = new Queue<GameObject>();
        GameObject obj = prefabPools[prefab].Count > 0 ? prefabPools[prefab].Dequeue() : Instantiate(prefab, pos, rot);
        obj.transform.position = pos; obj.transform.rotation = rot; obj.SetActive(true);
        if (delay > 0) StartCoroutine(ReturnAfterDelay(prefab, obj, delay));
        return obj;
    }

    private void ReturnToPool(GameObject prefab, GameObject obj) { if (obj != null) { obj.SetActive(false); prefabPools[prefab].Enqueue(obj); } }
    private IEnumerator ReturnAfterDelay(GameObject prefab, GameObject obj, float d) { yield return new WaitForSeconds(d); ReturnToPool(prefab, obj); }

    // --- Dev Panel Commands ---
    public void ForceUseSummon() { if (!isDead && !isCasting) StartCoroutine(SummonMobsInternal()); }
    public void ForceUseVoidZone() { if (!isDead && !isCasting) StartCoroutine(VoidZone()); }
    public void ForceSkillThenDie() { if (!isDead) StartCoroutine(ForceSkillThenDieRoutine()); }
    private IEnumerator ForceSkillThenDieRoutine() { yield return StartCoroutine(VoidZone()); Die(); }
    private IEnumerator SummonMobsInternal() { if (hpThresholdWaves.Count > currentWaveThresholdIndex) yield return StartCoroutine(SpawnMinionsThresholdSequence(hpThresholdWaves[currentWaveThresholdIndex])); }

    private void PlaySound(AudioClip clip) { if (clip != null && audioSource != null) audioSource.PlayOneShot(clip); }
    private void Die() 
    { 
        if (isDead) return;
        isDead = true; 
        StopAllCoroutines(); 
        
        Collider col = GetComponent<Collider>(); 
        if (col != null) col.enabled = false; 

        // เริ่มคัทซีนตอนตาย
        if (deathTimeline != null)
        {
            StartCoroutine(DeathCinematicSequence());
        }
        else
        {
            // ถ้าไม่มีคัทซีน ให้จบเกมแบบปกติ
            HandleGameOverUI();
            Destroy(gameObject, 3f); 
        }
    }

    private IEnumerator DeathCinematicSequence()
    {
        // 1. ล็อคการควบคุมผู้เล่น
        DisablePlayerControl();

        // 2. เล่นคัทซีน
        deathTimeline.Play();

        // 3. ซ่อน UI เกม
        if (gameUIContainer != null) gameUIContainer.SetActive(false);

        // รอคัทซีนจบ
        yield return new WaitUntil(() => deathTimeline.state != PlayState.Playing);

        // 4. โชว์ UI ชนะ
        HandleGameOverUI();
    }

    private void DisablePlayerControl()
    {
        if (playerTransform != null)
        {
            // ปิดสคริปต์ควบคุมตัวละคร
            var controller = playerTransform.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;

            // ปิดสคริปต์ควบคุมกล้อง (หาในลูกๆ หรือจุดที่เก็บกล้องไว้)
            var cam = playerTransform.GetComponentInChildren<FirstPersonCamera>();
            if (cam != null) cam.enabled = false;

            // ปลดล็อคเมาส์มาให้กดปุ่มตอนจบ
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleGameOverUI()
    {
        if (victoryUI != null) victoryUI.SetActive(true);
        if (gameUIContainer != null) gameUIContainer.SetActive(false);
        
        // ถ้าไม่มีอะไรทำต่อ อาจจะทำลาย Boss ทิ้ง
        // Destroy(gameObject, 1f); 
    }
}
