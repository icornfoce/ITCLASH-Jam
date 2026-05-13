using UnityEngine;

/// <summary>
/// แอ่งไฟบนพื้น — สร้างดาเมจใส่ศัตรูที่เดินผ่านทุกๆ damageInterval วินาที
/// </summary>
public class FirePuddle : MonoBehaviour
{
    [HideInInspector] public float damagePerTick = 5f;
    [HideInInspector] public float damageInterval = 0.5f;

    private System.Collections.Generic.List<ITCLASH.Enemies.EnemyController> enemiesInside = new System.Collections.Generic.List<ITCLASH.Enemies.EnemyController>();
    private float nextDamageTime = 0f;

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
}
