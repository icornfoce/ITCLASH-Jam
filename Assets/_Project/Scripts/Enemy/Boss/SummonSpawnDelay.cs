using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ใส่ Component นี้บน Enemy Prefab ที่บอสจะ Summon
/// มอนจะค่อยๆ ลอยขึ้นมาจากพื้น และถูกล็อคไม่ให้เดิน
/// รองรับทั้ง non-humanoid และ Humanoid (Root Motion)
/// </summary>
public class SummonSpawnDelay : MonoBehaviour
{
    [Header("Rise From Ground")]
    [Tooltip("ความลึกใต้ดินที่เริ่ม (ค่าบวก = ต่ำกว่าจุด Spawn)")]
    public float startDepth = 2f;
    [Tooltip("เวลา (วิ) ที่ใช้ลอยขึ้นมา")]
    public float riseDuration = 1.2f;
    [Tooltip("Curve ควบคุมความเร็วการลอย")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Idle After Rise")]
    [Tooltip("เวลา (วิ) ที่มอนยืนนิ่งหลังลอยขึ้นมาแล้วก่อนจะเริ่มไล่")]
    public float idleAfterRise = 0.5f;

    [Header("Ground VFX (Optional)")]
    [Tooltip("Particle / Prefab ที่เสกตรงจุด Spawn (เช่น ฝุ่น, ควัน)")]
    public GameObject groundVFXPrefab;
    public float groundVFXDestroyDelay = 2f;

    // ─── Private ─────────────────────────────────────────────────
    private NavMeshAgent agent;
    private Animator     anim;
    private bool         originalRootMotion;
    private bool         isRising = true;

    // ─── Awake ───────────────────────────────────────────────────
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();

        // บันทึกค่า Root Motion เดิมแล้วปิดก่อน
        if (anim != null)
        {
            originalRootMotion      = anim.applyRootMotion;
            anim.applyRootMotion    = false;
        }
    }

    // ─── Update ──────────────────────────────────────────────────
    /// บังคับล็อค NavMeshAgent ทุก frame ตลอดช่วงที่ยังลอย
    private void Update()
    {
        if (!isRising || agent == null) return;
        agent.isStopped = true;
        agent.velocity  = Vector3.zero;
    }

    // ─── Start ───────────────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(RiseRoutine());
    }

    // ─── Coroutine ───────────────────────────────────────────────
    private IEnumerator RiseRoutine()
    {
        Vector3 surfacePos = transform.position;

        // เสก Ground VFX ที่พื้นผิว
        GameObject vfx = null;
        if (groundVFXPrefab != null)
            vfx = Instantiate(groundVFXPrefab, surfacePos, Quaternion.identity);

        // เริ่มจากใต้ดิน
        transform.position = surfacePos + Vector3.down * startDepth;

        // ─ ลอยขึ้น (ขยับ root transform ได้เพราะปิด Root Motion แล้ว) ─
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            transform.position = Vector3.Lerp(
                surfacePos + Vector3.down * startDepth,
                surfacePos,
                riseCurve.Evaluate(t));
            yield return null;
        }

        transform.position = surfacePos;

        // รอนิ่งก่อนไล่
        yield return new WaitForSeconds(idleAfterRise);

        // ─ คืนค่า Root Motion และปลดล็อค AI ─
        if (anim != null)
            anim.applyRootMotion = originalRootMotion;

        isRising = false;

        if (agent != null)
        {
            agent.Warp(surfacePos); // sync NavMeshAgent กลับ
            agent.isStopped = false;
        }

        if (vfx != null)
            Destroy(vfx, groundVFXDestroyDelay);

        Destroy(this);
    }
}
