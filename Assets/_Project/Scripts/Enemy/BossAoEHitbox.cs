using UnityEngine;

public class BossAoEHitbox : MonoBehaviour
{
    [Tooltip("ดาเมจที่ทำต่อผู้เล่น")]
    public int damage = 30;

    [Tooltip("เวลาที่วัตถุนี้จะอยู่ในฉากก่อนจะโดนทำลายไปเอง")]
    public float lifeTime = 2.0f;

    [Tooltip("ใส่ Particle หรือ VFX เวลาโผล่ขึ้นมา (ถ้ามี)")]
    public ParticleSystem spawnVFX;

    private void Start()
    {
        if (spawnVFX != null)
        {
            spawnVFX.Play();
        }

        // ทำลายตัวเองเมื่อหมดเวลา
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damage);
            }
        }
    }
}
