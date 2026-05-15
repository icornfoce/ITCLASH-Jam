using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ITCLASH.Enemies
{
    public class MiniBoss : MonoBehaviour
    {
        public event System.Action OnBossDeathEvent;
        
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

    [Header("--- Skill Targeting ---")]
    [Range(0f, 100f)] public float playerTargetChance = 50f;
    [Tooltip("รัศมีขั้นต่ำรอบตัว Player (เพื่อไม่ให้เกิดทับตัวเป๊ะๆ)")]
    public float minPlayerTargetRadius = 3.0f;
    [Tooltip("รัศมีสูงสุดรอบตัว Player")]
    public float playerTargetRadius = 7.0f;

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

    private Bounds mapFloorBounds;
    private bool hasMapBounds = false;

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
        
        CalculateFloorBounds();
    }

    public void StartFight()
    {
        StartCoroutine(SkillCycle());
        StartCoroutine(StartInitialWaveDelayed());
    }

    // Cinematic logic removed (now handled by BossManager)
    

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

    private IEnumerator ExecuteWaveAttack(List<List<Vector3>> waves, float warningDelay, float waveDelay)
    {
        // Iterate through each wave (e.g. each ray or each expansion step)
        for (int i = 0; i < waves.Count; i++)
        {
            List<Vector3> currentWavePoints = waves[i];
            
            // Start the sequence for this entire wave: Warnings -> Delay -> Damage
            StartCoroutine(ExecuteSingleWave(currentWavePoints, warningDelay));
            
            // Small delay before starting the next wave's warning
            yield return new WaitForSeconds(waveDelay);
        }
    }

    private IEnumerator ExecuteSingleWave(List<Vector3> points, float delay)
    {
        List<GameObject> warnings = new List<GameObject>();
        
        // Spawn all warnings for this wave (with a tiny stagger to avoid lag spikes)
        foreach (var p in points)
        {
            if (warningPrefab != null) warnings.Add(SpawnSkillObject(warningPrefab, p, Quaternion.identity));
            if (points.Count > 5) yield return new WaitForSeconds(0.01f); 
        }

        yield return new WaitForSeconds(delay);

        // Clean up warnings
        foreach (var w in warnings) if (w != null) ReturnToPool(warningPrefab, w);
        
        // Trigger damage for this wave
        PlaySound(ultimateDamageSFX);
        if (damagePrefab != null)
        {
            foreach (var p in points)
            {
                SpawnSkillObject(damagePrefab, p, Quaternion.identity, 0.5f);
            }
        }
    }

    private IEnumerator SkyRain(int count)
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);
        
        for (int i = 0; i < count; i++) 
        { 
            Vector3 pos = GetRandomGroundPosition(); 
            if (pos != transform.position)
            {
                // Spawn warning and schedule damage automatically
                StartCoroutine(SpawnPointSkill(pos, 1.5f, 1f));
            }
            yield return new WaitForSeconds(0.15f); // Stagger the individual drops
        }
        
        yield return new WaitForSeconds(2.0f); // Buffertime for last drop to finish
        isCasting = false;
    }

    private IEnumerator SpawnPointSkill(Vector3 pos, float warningDuration, float damageDuration)
    {
        if (warningPrefab == null) yield break;
        
        GameObject warning = SpawnSkillObject(warningPrefab, pos, Quaternion.identity);
        yield return new WaitForSeconds(warningDuration);
        
        ReturnToPool(warningPrefab, warning);
        
        // สำหรับสกิลทั่วไปที่กระจายตัว อาจไม่เล่นเสียงทุกจุดเพื่อไม่ให้หนวกหูเกินไป
        // แต่ถ้าต้องการให้เล่นเสียงทุกจุด สามารถเปิดคอมเมนต์บรรทัดล่างได้ครับ
        // PlaySound(ultimateDamageSFX); 
        
        if (damagePrefab != null) SpawnSkillObject(damagePrefab, pos, Quaternion.identity, damageDuration);
    }

    private IEnumerator VoidZone()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);
        
        for (int i = 0; i < voidZoneCount; i++) 
        { 
            Vector3 pos = GetRandomGroundPosition(); 
            if (pos != transform.position)
            {
                if (voidZonePrefab != null) SpawnSkillObject(voidZonePrefab, pos, Quaternion.identity, voidZoneDuration);
            }
            yield return new WaitForSeconds(0.2f); 
        }
        
        yield return new WaitForSeconds(1f);
        isCasting = false;
    }

    private void CalculateFloorBounds()
    {
        // ค้นหาวัตถุทั้งหมดที่มี Layer ตรงกับ floorLayer เพื่อหาขอบเขตทั้งหมดของแมพ
        Collider[] allColliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        bool first = true;
        foreach (var col in allColliders)
        {
            // ตรวจสอบว่า collider นี้อยู่ใน floorLayer หรือไม่
            if (((1 << col.gameObject.layer) & floorLayer) != 0)
            {
                if (first) { mapFloorBounds = col.bounds; first = false; }
                else mapFloorBounds.Encapsulate(col.bounds);
            }
        }
        hasMapBounds = !first;
        if (hasMapBounds) Debug.Log($"<color=green>[MiniBoss]</color> Map Floor Bounds Calculated: {mapFloorBounds}");
    }

    private Vector3 GetRandomGroundPosition()
    {
        // ตัดสินใจว่าจะสุ่มใกล้ตัวผู้เล่นหรือสุ่มทั่วทั้งแมพตามเปอร์เซ็นต์ที่ตั้งไว้
        bool targetPlayer = (Random.Range(0f, 100f) < playerTargetChance) && playerTransform != null;
        
        if (targetPlayer)
        {
            Vector3 centerPos = playerTransform.position;
            float radius = playerTargetRadius;
            for (int i = 0; i < 30; i++)
            {
                float r = Random.Range(minPlayerTargetRadius, radius);
                float angle = Random.Range(0f, 360f);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * r;
                Vector3 targetPos = centerPos + offset;

                Vector3[] floorHits = GetFloorPositions(new Vector2(targetPos.x, targetPos.z), false);
                if (floorHits != null && floorHits.Length > 0) return floorHits[0];
            }
        }
        else if (hasMapBounds)
        {
            // สุ่มตำแหน่งที่ใดก็ได้ภายในขอบเขตของ Floor ทั้งหมดในแมพ
            for (int i = 0; i < 50; i++)
            {
                float x = Random.Range(mapFloorBounds.min.x, mapFloorBounds.max.x);
                float z = Random.Range(mapFloorBounds.min.z, mapFloorBounds.max.z);
                
                Vector3[] floorHits = GetFloorPositions(new Vector2(x, z), false);
                if (floorHits != null && floorHits.Length > 0) return floorHits[0];
            }
        }

        // Fallback เป็นตำแหน่งเดิม (รอบตัวบอส) ถ้าสุ่มข้างบนไม่เจอ
        for (int i = 0; i < 20; i++)
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

        // ส่งสัญญาณบอก BossManager
        OnBossDeathEvent?.Invoke();
        
        // ลบ Boss ออกจากฉากหลังจากดีเลย์
        Destroy(gameObject, 10f); 
    }

    // Cinematics and UI logic removed (now handled by BossManager)
    private void DisablePlayerControl() { /* Moved to BossManager logic or equivalent */ }
    private void HandleGameOverUI() { /* Moved to BossManager */ }
}
}
