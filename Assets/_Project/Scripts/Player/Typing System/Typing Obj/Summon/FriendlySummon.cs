using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FriendlySummon : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("ความเร็วในการเดิน")]
    public float moveSpeed = 5f;
    [Tooltip("ระยะการมองเห็นศัตรู (500 = ทั่วทั้งแมพ)")]
    public float detectionRange = 500f;
    [Tooltip("ระยะหยุด (ระยะโจมตี)")]
    public float attackRange = 2f;
    [Tooltip("Tag ของศัตรูที่ต้องการให้ไล่ล่า")]
    public string enemyTag = "Enemy";

    [Header("Animation Names")]
    public string walkBoolParam = "IsWalking";
    public string attackTriggerParam = "Attack";

    private NavMeshAgent agent;
    private Animator anim;
    private Transform targetEnemy;
    private float scanTimer = 0f;

    void Awake()
    {
        // บังคับให้ Tag เป็น Player เพื่อป้องกันการถูกเข้าใจผิดว่าเป็นศัตรู
        gameObject.tag = "Player";

        // ปิดสคริปต์ศัตรูเดิม (ถ้ามี) เพื่อไม่ให้มันหันมาทำร้ายเรา
        MonoBehaviour enemyCtrl = GetComponent("EnemyController") as MonoBehaviour;
        if (enemyCtrl != null) enemyCtrl.enabled = false;

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange;
        }
    }

    void Start()
    {
        // Warp ให้ลงไปอยู่บน NavMesh ทันทีที่เกิด (ป้องกันการค้างที่ประตูมิติ)
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    void Update()
    {
        // สแกนหาศัตรูทุกๆ 0.5 วินาที (เพื่อประหยัดพลังงานและอัปเดตเป้าหมาย)
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            FindNearestEnemy();
            scanTimer = 0.5f;
        }

        if (targetEnemy != null)
        {
            float distance = Vector3.Distance(transform.position, targetEnemy.position);
            
            // Log เป้าหมายปัจจุบัน (เอาออกทีหลังได้)
            // Debug.Log($"[{name}] Chasing: {targetEnemy.name} (Dist: {distance:F1})");

            if (distance <= detectionRange)
            {
                if (distance > attackRange)
                {
                    // ระยะไกล -> ไล่ตาม
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(targetEnemy.position);
                    }
                    SetWalking(true);
                }
                else
                {
                    // ถึงระยะโจมตี -> หยุดและหันหน้าหา
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    SetWalking(false);
                    FaceTarget();
                }
            }
            else
            {
                StopAI();
            }
        }
        else
        {
            StopAI();
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            // ตรวจสอบว่าศัตรูยังมีชีวิตอยู่ (ถ้ามีสคริปต์ HP)
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        targetEnemy = closestEnemy;
    }

    void FaceTarget()
    {
        if (targetEnemy == null) return;
        Vector3 direction = (targetEnemy.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void SetWalking(bool walking)
    {
        if (anim != null) anim.SetBool(walkBoolParam, walking);
    }

    void StopAI()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
        SetWalking(false);
    }
}
