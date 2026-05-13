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

    [Header("--- Sky Rain ---")]
    public int rainCountPhase1 = 5;
    public int rainCountPhase2 = 12;

    [Header("--- Summon (Phase 2 - In Front) ---")]
    public GameObject[] summonPrefabs;
    public int summonCount = 3;
    public float summonMinDistance = 4f;
    public float summonMaxDistance = 10f;
    public float summonAngleRange = 60f;
    public GameObject summonWarningPrefab;

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
    private int lastSkillIndex = -1;
    private Coroutine skillLoopCoroutine;
    private List<GameObject> activeSkillObjects = new List<GameObject>();
    private Dictionary<GameObject, Queue<GameObject>> prefabPools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

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

        skillLoopCoroutine = StartCoroutine(SkillCycle());
    }

    private Vector3 GetRandomGroundPosition()
    {
        for (int i = 0; i < 50; i++)
        {
            float r = Random.Range(2f, globalRandomRadius);
            float angle = Random.Range(0f, 360f);
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * r;
            Vector3 spawnPos = transform.position + offset;
            Vector3[] hits = GetFloorPositions(new Vector2(spawnPos.x, spawnPos.z), false);
            if (hits != null && hits.Length > 0) return hits[0];
        }
        return transform.position;
    }

    private GameObject SpawnSkillObject(GameObject prefab, Vector3 pos, Quaternion rot, float autoReturnDelay = -1f)
    {
        if (prefab == null) return null;
        if (!prefabPools.ContainsKey(prefab)) prefabPools[prefab] = new Queue<GameObject>();

        GameObject obj = null;
        if (prefabPools[prefab].Count > 0)
        {
            obj = prefabPools[prefab].Dequeue();
            if (obj != null)
            {
                obj.transform.position = pos;
                obj.transform.rotation = rot;
                obj.SetActive(true);
            }
            else obj = Instantiate(prefab, pos, rot);
        }
        else obj = Instantiate(prefab, pos, rot);

        if (obj != null && !activeSkillObjects.Contains(obj)) activeSkillObjects.Add(obj);
        if (autoReturnDelay > 0 && obj != null) StartCoroutine(ReturnToPoolAfterDelay(prefab, obj, autoReturnDelay));
        return obj;
    }

    private void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (obj == null || prefab == null) return;
        obj.SetActive(false);
        if (prefabPools.ContainsKey(prefab)) prefabPools[prefab].Enqueue(obj);
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject prefab, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(prefab, obj);
    }

    private void Update()
    {
        if (isDead) return;
        HandleVisuals();
    }

    private void HandleVisuals()
    {
        if (visualTransform == null) return;
        float newY = initialVisualLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visualTransform.localPosition = new Vector3(initialVisualLocalPos.x, newY, initialVisualLocalPos.z);
        if (alwaysFacePlayer && playerTransform != null)
        {
            Vector3 direction = playerTransform.position - visualTransform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                visualTransform.rotation = targetRotation * Quaternion.Euler(faceRotationOffset);
            }
        }
    }

    public void ApplyDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        StartCoroutine(HitEffectRoutine());
        if (currentHealth <= maxHealth * 0.5f && !isPhase2) EnterPhase2();
        if (currentHealth <= 0) Die();
    }

    private IEnumerator HitEffectRoutine()
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            foreach (Material m in r.materials) if (m.HasProperty("_Color")) m.color = hitColor;
        }
        yield return new WaitForSeconds(hitFlashDuration);
        foreach (Renderer r in renderers)
        {
            if (r == null || !originalColors.ContainsKey(r)) continue;
            Color[] colors = originalColors[r];
            for (int i = 0; i < r.materials.Length; i++)
                if (i < colors.Length && r.materials[i].HasProperty("_Color")) r.materials[i].color = colors[i];
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
                    if (skill == 0) yield return StartCoroutine(SkyRain(rainCountPhase1));
                    else yield return StartCoroutine(SeparateEightRays());
                }
                else
                {
                    int skill = Random.Range(0, 3);
                    if (skill == 0) yield return StartCoroutine(SkyRain(rainCountPhase2));
                    else if (skill == 1) yield return StartCoroutine(OverdriveBarrage());
                    else yield return StartCoroutine(SummonMobs());
                }
            }
            yield return new WaitForSeconds(skillCooldown);
        }
    }

    private Vector3[] GetFloorPositions(Vector2 xzPos, bool allFloors)
    {
        Vector3 rayStart = new Vector3(xzPos.x, transform.position.y + 50f, xzPos.y);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 100f, floorLayer);
        if (hits.Length == 0) return null;
        List<Vector3> validPositions = new List<Vector3>();
        if (allFloors) foreach (var hit in hits) validPositions.Add(hit.point);
        else
        {
            RaycastHit topHit = hits[0];
            foreach (var h in hits) if (h.point.y > topHit.point.y) topHit = h;
            validPositions.Add(topHit.point);
        }
        return validPositions.ToArray();
    }

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
                Vector3 offset = dir * (currentRadius + Random.Range(-pointRandomness, pointRandomness));
                Vector3 targetPos = transform.position + offset;
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
            if (warningPrefab != null)
                foreach (var p in wave)
                {
                    wList.Add(SpawnSkillObject(warningPrefab, p, Quaternion.identity));
                    yield return null; // กระจายการเสกทีละเฟรมเพื่อลด Lag
                }
            waveWarnings.Add(wList);
        }
        yield return new WaitForSeconds(initialDelay);
        for (int i = 0; i < waves.Count; i++)
        {
            foreach (var w in waveWarnings[i]) ReturnToPool(warningPrefab, w);
            PlaySound(ultimateDamageSFX);
            if (damagePrefab != null)
                foreach (var p in waves[i])
                {
                    SpawnSkillObject(damagePrefab, p, Quaternion.identity, 0.5f);
                    yield return null; // กระจายการเสกดาเมจ
                }
            yield return new WaitForSeconds(waveDelay);
        }
    }

    private IEnumerator SkyRain(int count)
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);
        List<Vector3> spawnPoints = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomGroundPosition();
            if (pos != transform.position) spawnPoints.Add(pos);
        }
        yield return StartCoroutine(SpawnWarningAndDamage(spawnPoints, 1.5f, 1.0f));
        isCasting = false;
    }

    private IEnumerator SpawnWarningAndDamage(List<Vector3> points, float delay, float damageDuration)
    {
        List<GameObject> warnings = new List<GameObject>();
        if (warningPrefab != null)
            foreach (Vector3 p in points)
            {
                warnings.Add(SpawnSkillObject(warningPrefab, p, Quaternion.identity));
                yield return null; // กระจาย spawn
            }
        yield return new WaitForSeconds(delay);
        foreach (GameObject w in warnings) ReturnToPool(warningPrefab, w);
        if (damagePrefab != null)
            foreach (Vector3 p in points)
            {
                SpawnSkillObject(damagePrefab, p, Quaternion.identity, damageDuration);
                yield return null;
            }
    }

    private IEnumerator SummonMobs()
    {
        isCasting = true;
        if (animator != null) animator.SetTrigger(attackAnimTrig);
        yield return new WaitForSeconds(0.5f);
        if (summonPrefabs != null && summonPrefabs.Length > 0)
        {
            List<Vector3> spawnPoints = new List<Vector3>();
            int attempts = 0;
            while (spawnPoints.Count < summonCount && attempts < 50)
            {
                attempts++;
                float randomAngle = Random.Range(-summonAngleRange, summonAngleRange);
                Vector3 dir = Quaternion.Euler(0, randomAngle, 0) * transform.forward;
                Vector3 targetPos = transform.position + (dir * Random.Range(summonMinDistance, summonMaxDistance));
                Vector3[] floorHits = GetFloorPositions(new Vector2(targetPos.x, targetPos.z), false);
                if (floorHits != null && floorHits.Length > 0)
                {
                    Vector3 pos = floorHits[0];
                    bool tooClose = false;
                    foreach (var s in spawnPoints) if (Vector3.Distance(pos, s) < 2f) { tooClose = true; break; }
                    if (!tooClose) spawnPoints.Add(pos);
                }
            }
            List<GameObject> warnings = new List<GameObject>();
            GameObject activeWarningPrefab = summonWarningPrefab != null ? summonWarningPrefab : warningPrefab;
            if (activeWarningPrefab != null)
                foreach (Vector3 p in spawnPoints)
                {
                    warnings.Add(SpawnSkillObject(activeWarningPrefab, p, Quaternion.identity));
                    yield return null;
                }
            yield return new WaitForSeconds(1.5f);
            foreach (GameObject w in warnings) ReturnToPool(activeWarningPrefab, w);
            foreach (Vector3 p in spawnPoints)
            {
                GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];
                if (prefab != null)
                {
                    Instantiate(prefab, p, Quaternion.identity);
                    yield return null; // กระจายการเสกมอนสเตอร์
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
            Vector3 pos = GetRandomGroundPosition();
            if (pos != transform.position) spawnPoints.Add(pos);
        }
        if (voidZonePrefab != null) foreach (Vector3 p in spawnPoints) SpawnSkillObject(voidZonePrefab, p, Quaternion.identity, voidZoneDuration);
        yield return new WaitForSeconds(1f);
        isCasting = false;
    }

    private void PlaySound(AudioClip clip) { if (clip != null && audioSource != null) audioSource.PlayOneShot(clip); }
    public void ForceUseSummon() { if (!isDead && !isCasting) StartCoroutine(SummonMobs()); }
    public void ForceUseVoidZone() { if (!isDead && !isCasting) StartCoroutine(VoidZone()); }
    public void ForceSkillThenDie() { if (!isDead) StartCoroutine(ForceSkillThenDieRoutine()); }
    private IEnumerator ForceSkillThenDieRoutine() { yield return StartCoroutine(VoidZone()); Die(); }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();
        foreach (var obj in activeSkillObjects) if (obj != null) obj.SetActive(false);
        activeSkillObjects.Clear();
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 3f);
    }
}
