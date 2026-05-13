using UnityEngine;
using ITCLASH.Enemies;

/// <summary>
/// BlankSkill — โยนภาพวาดเปล่า (ไร้สี) ใส่ศัตรู
/// ชนแล้วทำให้ศัตรูมองไม่เห็น (Blind) ชั่วคราว ไม่มีดาเมจ ทำเฉพาะ CC
/// </summary>
public class BlankSkill : BaseProjectileSkill
{
    [Header("─── Blank Settings ───")]
    [Tooltip("ระยะเวลาที่ศัตรูมองไม่เห็น (วินาที)")]
    public float blindDuration = 4f;

    [Tooltip("รัศมีของ Collider ที่จะเพิ่มให้อัตโนมัติ ถ้า prefab ไม่มี Collider")]
    public float autoColliderRadius = 0.6f;

    protected override void Awake()
    {
        // base.Awake() จะใส่ Rigidbody ให้ถ้าไม่มี (kinematic = true ก่อน Activate)
        base.Awake();

        // ─── ถ้า prefab ไม่มี Collider เลย ใส่ SphereCollider ให้อัตโนมัติ ───
        // (ไม่งั้นจะชนศัตรูไม่ได้ และ AddForce อาจไม่ทำงานเต็มที่)
        Collider anyCol = GetComponent<Collider>();
        if (anyCol == null) anyCol = GetComponentInChildren<Collider>();
        if (anyCol == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = autoColliderRadius;
            sc.isTrigger = false;
        }

        // ─── ตั้ง Rigidbody ให้ไม่หยุดเองตอนยิง ───
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public override void Activate(Transform playerTransform)
    {
        base.Activate(playerTransform);

        // ─── ไม่ให้ชนกับ Collider ของผู้เล่นเอง ───
        if (playerTransform != null)
        {
            Collider myCol = GetComponent<Collider>();
            if (myCol == null) myCol = GetComponentInChildren<Collider>();
            if (myCol != null)
            {
                foreach (Collider pc in playerTransform.root.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(myCol, pc, true);
                }
            }
        }

        Debug.Log($"[BlankSkill] ยิง Blank ออกไป (force={launchForce}, lifetime={lifetime})");
    }

    protected override void OnHit(Collision collision)
    {
        EnemyController enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            BlindEffect existing = enemy.GetComponent<BlindEffect>();
            if (existing != null)
            {
                existing.Refresh(blindDuration);
            }
            else
            {
                enemy.gameObject.AddComponent<BlindEffect>().Setup(enemy, blindDuration);
            }

            Debug.Log($"[BlankSkill] {collision.gameObject.name} โดน Blank! มองไม่เห็น {blindDuration} วิ");
        }

        SpawnHitVFX(collision.contacts[0].point);
        PlayHitSFX(transform.position);
        Destroy(gameObject);
    }
}
