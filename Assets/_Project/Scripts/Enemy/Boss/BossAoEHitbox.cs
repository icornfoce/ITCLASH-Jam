using UnityEngine;

public class BossAoEHitbox : MonoBehaviour
{
    public int damage = 30;
    public float lifeTime = 2.0f;
    public ParticleSystem spawnVFX;

    private void OnEnable()
    {
        if (spawnVFX != null) spawnVFX.Play();
        // ใช้ Invoke เพื่อปิดการทำงานตามเวลา lifeTime (คืน Pool)
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void OnDisable()
    {
        // ยกเลิก Invoke เมื่อถูกปิด เพื่อป้องกัน Error
        CancelInvoke();
        if (spawnVFX != null) spawnVFX.Stop();
    }

    private void ReturnToPool()
    {
        // ปิดการทำงานเพื่อให้ระบบ Pool ใน MiniBoss รับรู้และนำกลับมาใช้ใหม่
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // พยายามหา PlayerHealth (ปรับตามชื่อสคริปต์จริงที่มีในโปรเจกต์)
            var pHealth = other.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damage);
            }
        }
    }
}
