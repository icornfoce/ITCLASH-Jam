using UnityEngine;

public class OrbitProjectile : MonoBehaviour
{
    private float radius;
    private float damage;
    private float rotationSpeed;
    private float currentAngle;

    public void Setup(float radius, float damage, float rotationSpeed, float startAngle)
    {
        this.radius = radius;
        this.damage = damage;
        this.rotationSpeed = rotationSpeed;
        this.currentAngle = startAngle;
        
        // เซ็ตตำแหน่งเริ่มต้น
        UpdatePosition();
    }

    void Update()
    {
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;
        
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        float x = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
        
        // ปรับตำแหน่ง relative กับ Player (ตัวแม่)
        transform.localPosition = new Vector3(x, 1f, z); // สูงจากพื้น 1 เมตร
    }

    private void OnTriggerEnter(Collider other)
    {
        // เช็คจาก Component EnemyHealth โดยตรง (ไม่ต้องใช้ Tag เพื่อป้องกัน Error)
        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health == null) health = other.GetComponentInParent<EnemyHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);
            
            // ใส่ VFX หรือ Debug เพื่อดูว่าทำงานหรือไม่
            Debug.Log($"<color=orange>[Orbit]</color> Hit {other.name} | Damage: {damage}");
        }
    }
}
