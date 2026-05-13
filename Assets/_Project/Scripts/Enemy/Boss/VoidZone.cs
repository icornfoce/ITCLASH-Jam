using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VoidZone : MonoBehaviour
{
    [Header("Settings")]
    public float damagePerTick = 10f;
    public float tickInterval = 1f;
    public float slowAmount = 0.5f; // 0.5 means 50% speed
    public float slowLingerDuration = 2f; // ระยะเวลาที่สโลว์ค้างอยู่หลังเดินออก
    public float lifetime = 5f;

    private List<FirstPersonController> playersInZone = new List<FirstPersonController>();
    private Dictionary<FirstPersonController, Coroutine> damageCoroutines = new Dictionary<FirstPersonController, Coroutine>();

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[VoidZone] Object entered: {other.name}");
        FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
        if (player == null) player = other.GetComponentInChildren<FirstPersonController>();

        if (player != null && !playersInZone.Contains(player))
        {
            Debug.Log($"[VoidZone] Player detected: {player.name}. Applying slow and damage.");
            playersInZone.Add(player);
            player.AddSpeedMultiplier(slowAmount);
            
            // Start DoT
            Coroutine dot = StartCoroutine(DealDamageOverTime(player));
            damageCoroutines.Add(player, dot);
        }
        else if (player == null)
        {
            Debug.Log($"[VoidZone] Object {other.name} is not a Player (FirstPersonController missing).");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
        if (player == null) player = other.GetComponentInChildren<FirstPersonController>();

        if (player != null && playersInZone.Contains(player))
        {
            Debug.Log($"[VoidZone] Player {player.name} left the zone. Linger slow started.");
            RemovePlayer(player, true); // true = apply linger
        }
    }

    private void OnDestroy()
    {
        // Clean up all players speed when zone is destroyed
        foreach (var player in playersInZone)
        {
            if (player != null)
            {
                player.RemoveSpeedMultiplier(slowAmount);
                player.AddTimedSpeedMultiplier(slowAmount, slowLingerDuration);
            }
        }
    }

    private void RemovePlayer(FirstPersonController player, bool applyLinger)
    {
        playersInZone.Remove(player);
        if (player != null)
        {
            player.RemoveSpeedMultiplier(slowAmount);
            if (applyLinger)
            {
                player.AddTimedSpeedMultiplier(slowAmount, slowLingerDuration);
            }
        }

        if (damageCoroutines.ContainsKey(player))
        {
            StopCoroutine(damageCoroutines[player]);
            damageCoroutines.Remove(player);
        }
    }

    private IEnumerator DealDamageOverTime(FirstPersonController player)
    {
        while (true)
        {
            if (player != null)
            {
                player.TakeDamage(damagePerTick);
            }
            yield return new WaitForSeconds(tickInterval);
        }
    }
}
