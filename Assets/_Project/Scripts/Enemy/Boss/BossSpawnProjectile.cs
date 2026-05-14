using UnityEngine;
using System.Collections;

public class BossSpawnProjectile : MonoBehaviour
{
    private GameObject minionPrefab;
    private Vector3 targetPosition;
    private float travelDuration;
    private GameObject arrivalVFX;

    public void Launch(GameObject prefab, Vector3 target, float duration, GameObject vfx = null)
    {
        minionPrefab = prefab;
        targetPosition = target;
        travelDuration = duration;
        arrivalVFX = vfx;
        StartCoroutine(TravelRoutine());
    }

    private IEnumerator TravelRoutine()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelDuration;
            
            // Parabola arch effect
            float height = Mathf.Sin(t * Mathf.PI) * 5f; 
            transform.position = Vector3.Lerp(startPos, targetPosition, t) + Vector3.up * height;
            
            if (targetPosition - transform.position != Vector3.zero)
                transform.rotation = Quaternion.LookRotation((targetPosition + Vector3.up * height) - transform.position);

            yield return null;
        }

        transform.position = targetPosition;

        // On Arrival
        if (arrivalVFX != null)
        {
            Instantiate(arrivalVFX, targetPosition, Quaternion.identity);
        }

        if (minionPrefab != null)
        {
            Instantiate(minionPrefab, targetPosition, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
