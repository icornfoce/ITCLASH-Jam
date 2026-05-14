using UnityEngine;

public class BossSpawnProjectile : MonoBehaviour
{
    private GameObject minionPrefab;
    private Transform targetTransform;
    private float speed = 20f;
    private GameObject arrivalVFX;
    private bool isInitialized = false;

    // ฟังก์ชันสั่งยิง (รับเป็น Transform แทนเพื่อให้มันพุ่งหาจุดนั้นแม่นๆ)
    public void Launch(GameObject prefab, Transform target, float moveSpeed, GameObject vfx = null)
    {
        minionPrefab = prefab;
        targetTransform = target;
        speed = moveSpeed > 0 ? moveSpeed : 20f;
        arrivalVFX = vfx;
        isInitialized = true;
        
        Debug.Log($"[Projectile] Simple Launch towards: {target.name}");
    }

    private void Update()
    {
        if (!isInitialized || targetTransform == null) return;

        // พุ่งเข้าหาจุดเกิดตรงๆ
        Vector3 targetPos = targetTransform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        
        // หันหัวไปทางเป้าหมาย
        transform.LookAt(targetPos);

        // ถ้าถึงจุดหมาย (ระยะห่างน้อยกว่า 0.2 เมตร)
        if (Vector3.Distance(transform.position, targetPos) < 0.2f)
        {
            SpawnMinion();
        }
    }

    private void SpawnMinion()
    {
        Debug.Log("[Projectile] Reached target, spawning minion.");
        
        if (arrivalVFX != null)
        {
            Instantiate(arrivalVFX, transform.position, Quaternion.identity);
        }

        if (minionPrefab != null)
        {
            Instantiate(minionPrefab, transform.position, Quaternion.identity);
        }

        // ทำลายตัวเองทิ้ง
        Destroy(gameObject);
    }
}
