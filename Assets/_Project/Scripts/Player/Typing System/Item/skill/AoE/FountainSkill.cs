using UnityEngine;
using UnityEngine.Serialization;
using ITCLASH.Enemies;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// น้ำพุ — ป้อม AoE ที่ทำดาเมจ "ศัตรูทุกตัวในวงกลม" ซ้ำๆ ทุก actionInterval วินาที
/// VFX จะ Loop ตลอดอายุของป้อม แล้วค่อยๆ จางหายตอนหมดเวลา
/// </summary>
public class FountainSkill : BaseTurretSkill
{
    [Header("─── Fountain Damage ───")]
    [Tooltip("ดาเมจต่อ Tick ต่อศัตรู 1 ตัว")]
    [FormerlySerializedAs("damage")]
    public float damagePerTick = 15f;

    [Tooltip("รัศมีพื้นที่ทำ Damage (จะ sync ไปที่ targetingRange ของ Base)")]
    public float radius = 5f;

    [Tooltip("แรงผลักศัตรูออกเบาๆ ทุก Tick (0 = ไม่ผลัก)")]
    public float pushForce = 3f;

    [Header("─── Layers ───")]
    [Tooltip("Layer ของศัตรู (ปล่อยว่างได้ — จะใช้ Tag 'Enemy' แทน)")]
    public LayerMask enemyLayer;
    [Tooltip("Layer ของ Player — จะไม่โดนสกิลตัวเอง")]
    public LayerMask playerLayer;

    [Header("─── Spawn ───")]
    [Tooltip("ระยะข้างหน้า Player ตอนวาง (ถ้าไม่ได้เล็ง)")]
    public float spawnForwardOffset = 2.5f;

    [Header("─── VFX & Audio ───")]
    [Tooltip("Prefab VFX ของน้ำพุ — จะถูก Spawn แล้ว Loop ตลอดอายุของสกิล")]
    [FormerlySerializedAs("explosionVFX")]
    public GameObject fountainVFX;

    [Tooltip("เสียงตอนเปิดสกิล")]
    [FormerlySerializedAs("explosionSFX")]
    public AudioClip activateSFX;

    [Tooltip("ให้ VFX ขยายตามขนาด Radius")]
    public bool autoScaleVFX = true;

    [Tooltip("เวลาที่เผื่อให้ VFX จางหายหลังหมดอายุ (วินาที)")]
    public float vfxFadeOutTime = 1.0f;

    // ─── Internal ───
    private GameObject _vfxInstance;
    private readonly List<ParticleSystem> _particles = new List<ParticleSystem>();
    private readonly Collider[] _hitBuffer = new Collider[64];
    private readonly HashSet<EnemyController> _hitThisTick = new HashSet<EnemyController>();
    private bool _fading;

    /// <summary>
    /// เรียกครั้งเดียวตอนป้อมลงพื้น (ก่อนเริ่ม tick)
    /// </summary>
    protected override void OnTurretDeployed(Transform playerTransform)
    {
        // ── sync ระยะของ Gizmo / Base ให้ตรงกับ radius ของเรา ──
        targetingRange = radius;

        // ── ปรับตำแหน่งตาม spawnForwardOffset ถ้าไม่ได้เล็งไว้ก่อน ──
        if (!TargetPosition.HasValue && playerTransform != null)
        {
            Vector3 spawnOrigin = playerTransform.position
                                  + playerTransform.forward * spawnForwardOffset
                                  + Vector3.up * 3f;
            if (Physics.Raycast(spawnOrigin, Vector3.down, out RaycastHit groundHit, 30f))
                transform.position = groundHit.point;
        }

        // ── หยุดฟิสิกส์ (Base เปิดมา → เราปิดกลับเพราะน้ำพุไม่ตก ไม่กลิ้ง) ──
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        // ── ไม่ให้ชน Player ──
        if (playerTransform != null)
        {
            Collider playerCol = playerTransform.GetComponentInChildren<Collider>();
            if (playerCol != null)
            {
                foreach (var col in GetComponentsInChildren<Collider>())
                    if (col != null) Physics.IgnoreCollision(col, playerCol, true);
            }
        }

        // ── Spawn VFX และบังคับให้ Loop ──
        SpawnAndLoopVFX();

        // ── เล่นเสียง ──
        if (activateSFX != null)
            AudioSource.PlayClipAtPoint(activateSFX, transform.position);
    }

    /// <summary>
    /// ถูกเรียกจาก Base ทุกๆ actionInterval วินาที
    /// </summary>
    protected override void PerformTurretAction()
    {
        if (_fading) return;
        DamageAllEnemiesInRadius();
    }

    /// <summary>
    /// หาศัตรูทั้งหมดในรัศมี แล้วยิงดาเมจใส่พร้อมกันในเฟรมเดียว
    /// </summary>
    private void DamageAllEnemiesInRadius()
    {
        bool useLayer = enemyLayer.value != 0;
        int count = useLayer
            ? Physics.OverlapSphereNonAlloc(transform.position, radius, _hitBuffer, enemyLayer)
            : Physics.OverlapSphereNonAlloc(transform.position, radius, _hitBuffer);

        _hitThisTick.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hitBuffer[i];
            if (hit == null) continue;

            // กรอง Player ออกตอนใช้ fallback mode (ไม่มี enemyLayer)
            if (!useLayer && playerLayer.value != 0 &&
                (playerLayer.value & (1 << hit.gameObject.layer)) != 0)
                continue;

            EnemyController enemy = hit.GetComponentInParent<EnemyController>();

            if (enemy != null)
            {
                // กันยิงซ้ำในเฟรมเดียวกรณีศัตรูตัวเดียวมีหลาย Collider
                if (!_hitThisTick.Add(enemy)) continue;

                enemy.ApplyDamage(damagePerTick);

                if (pushForce > 0f)
                {
                    Vector3 dir = enemy.transform.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
                    dir = dir.normalized;
                    dir.y = 0.2f;
                    enemy.ApplyKnockback(dir.normalized * pushForce, 0.2f);
                }
            }
            else if (hit.CompareTag("Enemy"))
            {
                // Fallback: ไม่มี EnemyController → ใช้ SendMessage
                hit.SendMessage("TakeDamage", damagePerTick, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    /// <summary>
    /// เรียกตอน Base พบว่า duration หมดเวลา — เราจะ fade VFX ก่อนค่อย Destroy
    /// </summary>
    protected override void OnDurationExpired()
    {
        // อย่าเรียก base.OnDurationExpired() เพราะมัน Destroy ทันที
        isActive = false;
        if (!_fading) StartCoroutine(FadeOutAndDestroy());
    }

    /// <summary>
    /// Spawn VFX, ปรับขนาด, แล้วบังคับให้ ParticleSystem ทั้งหมด Loop ต่อเนื่อง
    /// </summary>
    private void SpawnAndLoopVFX()
    {
        if (fountainVFX == null) return;

        _vfxInstance = Instantiate(fountainVFX, transform.position, Quaternion.identity, transform);

        if (autoScaleVFX)
            _vfxInstance.transform.localScale = Vector3.one * (radius * 2f);

        _vfxInstance.GetComponentsInChildren(true, _particles);
        foreach (ParticleSystem ps in _particles)
        {
            if (ps == null) continue;

            var main = ps.main;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;

            if (!ps.isPlaying) ps.Play(true);
        }
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (_fading) yield break;
        _fading = true;

        foreach (ParticleSystem ps in _particles)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = false;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, vfxFadeOutTime));

        if (_vfxInstance != null) Destroy(_vfxInstance);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_vfxInstance != null) Destroy(_vfxInstance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.35f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
