using UnityEngine;

/// <summary>
/// แอ่งไฟบนพื้น — สร้างดาเมจใส่ศัตรูที่เดินผ่านทุกๆ damageInterval วินาที
/// </summary>
public class FirePuddle : MonoBehaviour
{
    [HideInInspector] public float damagePerTick = 5f;
    [HideInInspector] public float damageInterval = 0.5f;

    [HideInInspector] public float lifetime = 8f;
    public float spawnDuration = 0.3f;
    public float deathDuration = 0.5f;

    private System.Collections.Generic.List<ITCLASH.Enemies.EnemyController> enemiesInside = new System.Collections.Generic.List<ITCLASH.Enemies.EnemyController>();
    private float nextDamageTime = 0f;
    private Vector3 targetScale;

    private void Start()
    {
        // ต้องมี Rigidbody เพื่อให้ Trigger ทำงานได้ชัวร์ๆ
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // ไม่ให้ตกตามแรงโน้มถ่วง
            rb.useGravity = false;
        }

        targetScale = transform.localScale;
        StartCoroutine(LifeCycleRoutine());
    }

    private void Update()
    {
        if (Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + damageInterval;
            
            // ลบตัวที่ตายแล้วหรือถูกลบออกไปจาก List
            enemiesInside.RemoveAll(e => e == null || e.gameObject == null);

            foreach (var enemy in enemiesInside)
            {
                enemy.ApplyDamage(damagePerTick);
                Debug.Log($"🔥 [FirePuddle] เผาศัตรู {enemy.gameObject.name} ทำดาเมจ {damagePerTick} (เหลือเลือด {enemy.CurrentHealth})");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ITCLASH.Enemies.EnemyController enemy = other.GetComponentInParent<ITCLASH.Enemies.EnemyController>();
        if (enemy != null && !enemiesInside.Contains(enemy))
        {
            enemiesInside.Add(enemy);
            Debug.Log($"[FirePuddle] ศัตรู {enemy.gameObject.name} เดินเข้ากองไฟ!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ITCLASH.Enemies.EnemyController enemy = other.GetComponentInParent<ITCLASH.Enemies.EnemyController>();
        if (enemy != null && enemiesInside.Contains(enemy))
        {
            enemiesInside.Remove(enemy);
            Debug.Log($"[FirePuddle] ศัตรู {enemy.gameObject.name} เดินออกจากกองไฟ");
        }
    }

    private System.Collections.IEnumerator LifeCycleRoutine()
    {
        // 1. เกิดมาค่อยๆ ขยายใหญ่ (Scale Up)
        float elapsed = 0f;
        transform.localScale = Vector3.zero;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spawnDuration);
            // Easing out
            float eased = 1f - (1f - t) * (1f - t);
            transform.localScale = targetScale * eased;
            yield return null;
        }
        transform.localScale = targetScale;

        // 2. รอก่อนจะหมดเวลา (หักลบเวลาเกิดกับเวลาดับ)
        float waitTime = lifetime - spawnDuration - deathDuration;
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        // 3. ใกล้ตายค่อยๆ หดเล็กลง (Scale Down)
        elapsed = 0f;
        Vector3 currentScale = transform.localScale;
        while (elapsed < deathDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathDuration);
            // Easing in
            float eased = t * t;
            transform.localScale = currentScale * (1f - eased);
            yield return null;
        }

        Destroy(gameObject);
    }
}
