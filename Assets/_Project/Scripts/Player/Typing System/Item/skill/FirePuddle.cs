using UnityEngine;
using ITCLASH.Enemies;

[RequireComponent(typeof(Collider))]
public class FirePuddle : MonoBehaviour
{
    [Header("Damage")]
    public float tickDamage = 5f;
    public float tickInterval = 0.5f;

    private float nextTick;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < nextTick) return;

        var enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || !enemy.IsAlive) return;

        enemy.ApplyDamage(tickDamage);
        nextTick = Time.time + tickInterval;
    }
}
