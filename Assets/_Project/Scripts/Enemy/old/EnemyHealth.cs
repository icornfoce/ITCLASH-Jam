using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float currentHealth;

    [Header("Visuals")]
    [SerializeField] private GameObject deathVFX;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        // สั่งทำลายตัวเอง (Gem จะถูก Spawn อัตโนมัติจาก OnDestroy ใน Enemy.cs หรือ Range enemy.cs)
        Destroy(gameObject);
    }
}
