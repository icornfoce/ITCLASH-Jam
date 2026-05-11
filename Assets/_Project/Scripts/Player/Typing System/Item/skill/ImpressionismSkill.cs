using UnityEngine;
using ITCLASH.Enemies;

/// <summary>
/// ImpressionismSkill — ยิง Beam หลากสี random ทุก cast
/// แดง=DMG burst, น้ำเงิน=Slow, เขียว=Heal ตัวเอง, เหลือง=Knockback
/// </summary>
public class ImpressionismSkill : BaseItemSkill
{
    [Header("─── Impressionism Settings ───")]
    [Tooltip("Red: ดาเมจระเบิดใส่ศัตรูที่โดน")]
    public float burstDamage = 50f;

    [Space]
    [Tooltip("Blue: เปอร์เซ็นต์ความเร็วที่ลด (0 = หยุดนิ่ง, 1 = ปกติ)")]
    [Range(0f, 1f)]
    public float slowPercent = 0.6f;

    [Tooltip("Blue: ระยะเวลาชะลอความเร็ว (วินาที)")]
    public float slowDuration = 4f;

    [Space]
    [Tooltip("Green: ปริมาณ HP ที่ฟื้นฟูให้กับผู้เล่น")]
    public float healAmount = 30f;

    [Space]
    [Tooltip("Yellow: แรงกระเด็นที่ผลักศัตรูออกห่างจากผู้เล่น")]
    public float yellowKnockbackForce = 25f;

    [Header("─── Beam Settings ───")]
    [Tooltip("จุดที่ลำแสงจะยิงออกไป (ถ้าไม่ใส่ จะยิงจากกล้องหรือตัวผู้เล่น)")]
    public Transform shootPoint;

    [Tooltip("ระยะยิงสูงสุดของ Beam (เมตร)")]
    public float beamRange = 50f;

    [Tooltip("ระยะเวลาที่ Beam จะปรากฏ (วินาที)")]
    public float beamDuration = 0.4f;

    [Tooltip("ความกว้างของ Beam")]
    public float beamWidth = 0.15f;

    [Header("─── VFX & Audio ───")]
    [Tooltip("VFX ตอน Beam ชนเป้าหมาย")]
    public GameObject hitVFXPrefab;

    [Tooltip("เสียงตอน Beam ชน")]
    public AudioClip hitSFX;

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(transform.position);

        // สุ่มสี
        int colorIndex = Random.Range(0, 4);
        Color beamColor = GetBeamColor(colorIndex);
        string colorName = GetColorName(colorIndex);

        Debug.Log($"[Impressionism] สุ่มได้สี: {colorName}");

        Vector3 rayOrigin;
        Vector3 rayDirection;

        // ยิง Raycast จากกล้อง (เพื่อให้เล็งตามเป้า Crosshair เสมอ)
        Camera cam = Camera.main;
        if (cam != null)
        {
            rayOrigin = cam.transform.position;
            rayDirection = cam.transform.forward;
        }
        else
        {
            rayOrigin = playerTransform.position + Vector3.up * 1.5f;
            rayDirection = playerTransform.forward;
        }

        // Raycast หาเป้าหมายที่อยู่ตรงกลางจอ
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, beamRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 endPoint = rayOrigin + rayDirection * beamRange;

        foreach (RaycastHit hit in hits)
        {
            // ข้ามตัวผู้เล่นและตัวสกิลเอง
            if (hit.collider.transform.root == playerTransform.root) continue;
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;

            endPoint = hit.point;

            // หาศัตรู
            EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();

            // ใช้เอฟเฟกต์ตามสี
            ApplyEffect(colorIndex, enemy, playerTransform, hit);

            // VFX & SFX ตอนชน
            if (hitVFXPrefab != null)
                Destroy(Instantiate(hitVFXPrefab, hit.point, Quaternion.identity), 2f);
            if (hitSFX != null)
                AudioSource.PlayClipAtPoint(hitSFX, hit.point);

            break; // ชนแค่อันแรก
        }

        // จุดเริ่มต้นของเส้นแสง Beam (ถ้ามี shootPoint ให้พุ่งจากปลายกระบอก, ถ้าไม่มีให้ออกมาจากกล้อง)
        Vector3 visualStartPoint = (shootPoint != null) ? shootPoint.position : rayOrigin;

        // สร้าง Beam visual
        CreateBeamVisual(visualStartPoint, endPoint, beamColor);

        // ทำลายตัวสกิล
        // ซ่อนโมเดลทันที
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Destroy(gameObject, beamDuration + 0.1f);
    }

    private void ApplyEffect(int colorIndex, EnemyController enemy, Transform playerTransform, RaycastHit hit)
    {
        switch (colorIndex)
        {
            case 0: // Red - Burst DMG
                if (enemy != null) enemy.ApplyDamage(burstDamage);
                Debug.Log("[Impressionism] 🔴 สีแดง! Burst DMG!");
                break;

            case 1: // Blue - Slow
                if (enemy != null && enemy.Agent != null)
                {
                    var slow = enemy.GetComponent<SlowEffect>();
                    if (slow != null) slow.RefreshSlow(slowPercent, slowDuration);
                    else enemy.gameObject.AddComponent<SlowEffect>().Setup(enemy.Agent, slowPercent, slowDuration);
                }
                Debug.Log("[Impressionism] 🔵 สีน้ำเงิน! Slow!");
                break;

            case 2: // Green - Heal player
                if (playerTransform != null)
                {
                    var ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null) ph.Heal(healAmount);
                }
                Debug.Log("[Impressionism] 🟢 สีเขียว! Heal ผู้เล่น!");
                break;

            case 3: // Yellow - Knockback
                if (enemy != null && playerTransform != null)
                {
                    IKnockbackable knockable = enemy.GetComponentInParent<IKnockbackable>();
                    if (knockable != null)
                    {
                        Vector3 dir = hit.collider.transform.position - playerTransform.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude > 0.0001f)
                            knockable.ApplyKnockback(dir.normalized * yellowKnockbackForce, 0.4f);
                    }
                }
                Debug.Log("[Impressionism] 🟡 สีเหลือง! Knockback!");
                break;
        }
    }

    [Header("─── Light Settings ───")]
    [Tooltip("ความสว่างของแสงที่จุดเกิด Beam")]
    public float lightIntensity = 8f;

    [Tooltip("รัศมีแสงที่จุดเกิด Beam")]
    public float lightRange = 8f;

    [Tooltip("ความสว่างของแสงที่จุดชน")]
    public float hitLightIntensity = 5f;

    [Tooltip("รัศมีแสงที่จุดชน")]
    public float hitLightRange = 5f;

    private void CreateBeamVisual(Vector3 start, Vector3 end, Color color)
    {
        Color glowColor = color * 3f;

        // ─── LineRenderer (ลำแสง) ───
        GameObject beamObj = new GameObject("ImpressionismBeam");
        LineRenderer lr = beamObj.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = beamWidth;
        lr.endWidth = beamWidth * 0.5f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = glowColor;
        lr.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.3f);
        Destroy(beamObj, beamDuration);

        // ─── แสงที่จุดเกิด (ต้นทาง Beam) ───
        GameObject originLightObj = new GameObject("BeamOriginLight");
        originLightObj.transform.position = start;
        Light originLight = originLightObj.AddComponent<Light>();
        originLight.type = LightType.Point;
        originLight.color = color;
        originLight.intensity = lightIntensity;
        originLight.range = lightRange;
        originLight.shadows = LightShadows.None;
        StartCoroutine(FadeLight(originLight, beamDuration));
        Destroy(originLightObj, beamDuration + 0.1f);

        // ─── แสงที่จุดชน (ปลายทาง Beam) ───
        if (Vector3.Distance(start, end) > 0.5f)
        {
            GameObject hitLightObj = new GameObject("BeamHitLight");
            hitLightObj.transform.position = end;
            Light hitLight = hitLightObj.AddComponent<Light>();
            hitLight.type = LightType.Point;
            hitLight.color = color;
            hitLight.intensity = hitLightIntensity;
            hitLight.range = hitLightRange;
            hitLight.shadows = LightShadows.None;
            StartCoroutine(FadeLight(hitLight, beamDuration));
            Destroy(hitLightObj, beamDuration + 0.1f);
        }
    }

    private System.Collections.IEnumerator FadeLight(Light light, float duration)
    {
        float startIntensity = light.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (light == null) yield break;
            light.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / duration);
            yield return null;
        }
    }

    private Color GetBeamColor(int index)
    {
        switch (index)
        {
            case 0: return new Color(1f, 0.2f, 0.2f);   // แดง
            case 1: return new Color(0.2f, 0.4f, 1f);    // น้ำเงิน
            case 2: return new Color(0.2f, 1f, 0.3f);    // เขียว
            case 3: return new Color(1f, 0.9f, 0.2f);    // เหลือง
            default: return Color.white;
        }
    }

    private string GetColorName(int index)
    {
        switch (index)
        {
            case 0: return "แดง (Burst DMG)";
            case 1: return "น้ำเงิน (Slow)";
            case 2: return "เขียว (Heal)";
            case 3: return "เหลือง (Knockback)";
            default: return "???";
        }
    }
}
