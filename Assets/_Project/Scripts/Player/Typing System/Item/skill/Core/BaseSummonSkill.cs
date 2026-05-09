using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseSummonSkill : BaseItemSkill
{
    [Header("─── Summon Settings ───")]
    [Tooltip("Prefab ของมอนสเตอร์/ตัวช่วย ที่จะเรียกออกมา")]
    public GameObject summonPrefab;
    [Tooltip("ให้ตัวช่วยอยู่ได้นานแค่ไหน (0 = อยู่จนตาย)")]
    public float summonDuration = 0f;
    [Tooltip("เสกให้ลอยกลางอากาศหรือร่วงลงพื้น")]
    public bool dropToGround = true;

    [Tooltip("เวลาดีเลย์ก่อนเสกมอนสเตอร์ (เช่น รอให้เอฟเฟกต์ประตูมิติเปิดก่อน)")]
    public float spawnDelay = 0f;

    [Tooltip("จุดเกิดเฉพาะเจาะจง (ถ้าไม่ใส่ จะเกิดตรงตำแหน่งของไอเทม)")]
    public Transform customSpawnPoint;

    [Tooltip("รายชื่อ Object ใน Hierarchy ที่จะสุ่มเป็นจุดเกิด (เช่น Portal_1, Portal_2)")]
    public List<string> spawnPointNames = new List<string>();

    [Header("─── VFX & Audio ───")]
    public GameObject spawnVFX;
    public AudioClip spawnSFX;

    public override void Activate(Transform playerTransform)
    {
        // คำนวณจุดเกิดครั้งเดียว เพื่อให้ Pos และ Rot ตรงกัน
        (Vector3 pos, Quaternion rot) spawnPoint = GetFinalSpawnPoint();

        // ย้ายตัวเองไปที่จุดเกิดทันที
        // ถ้ามี NavMeshAgent ต้องใช้ Warp เท่านั้น ไม่เช่นนั้นตำแหน่งจะไม่เปลี่ยน
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(spawnPoint.pos);
        }
        else
        {
            transform.position = spawnPoint.pos;
        }
        transform.rotation = spawnPoint.rot;

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        PlayVoice(spawnPos);
        
        // 1. เล่นเอฟเฟกต์เสก
        if (spawnVFX != null) Instantiate(spawnVFX, spawnPos, spawnRot);
        if (spawnSFX != null) AudioSource.PlayClipAtPoint(spawnSFX, spawnPos);

        // 2. เสก (แบบมีดีเลย์ หรือ ทันที)
        if (spawnDelay > 0f)
        {
            StartCoroutine(SpawnRoutine(playerTransform));
        }
        else
        {
            DoSpawn(playerTransform);
            Destroy(gameObject); 
        }
    }

    private System.Collections.IEnumerator SpawnRoutine(Transform playerTransform)
    {
        yield return new WaitForSeconds(spawnDelay);
        DoSpawn(playerTransform);
        Destroy(gameObject); 
    }

    private void DoSpawn(Transform playerTransform)
    {
        if (summonPrefab != null)
        {
            Vector3 spawnPos = transform.position; 
            Quaternion spawnRot = transform.rotation;

            // ─── Ground Detection ───
            if (dropToGround)
            {
                if (Physics.Raycast(spawnPos + Vector3.up, Vector3.down, out RaycastHit hit, 10f))
                {
                    spawnPos = hit.point;
                }
            }

            Debug.Log($"[BaseSummonSkill] กำลังเสก: {summonPrefab.name} ที่ตำแหน่ง {spawnPos}");
            
            GameObject minion = Instantiate(summonPrefab, spawnPos, spawnRot);
            minion.SetActive(true);
            
            OnSummonCreated(minion, playerTransform);

            if (summonDuration > 0f)
            {
                Destroy(minion, summonDuration);
            }
        }
        else
        {
            Debug.LogWarning("[BaseSummonSkill] ❌ ไม่สามารถเสกได้! ช่อง Summon Prefab ใน Inspector ว่างเปล่าอยู่");
        }
    }

    private (Vector3 pos, Quaternion rot) GetFinalSpawnPoint()
    {
        // รายงานจำนวนชื่อใน List เพื่อเช็คว่า Unity เห็นข้อมูลไหม
        int rawCount = (spawnPointNames != null) ? spawnPointNames.Count : 0;
        Debug.Log($"[BaseSummonSkill] >>> Checking Spawn Point Names List... Found {rawCount} entries.");

        // กรองหารายชื่อ Portal ทั้งหมดที่มีอยู่จริงในฉากก่อน
        List<GameObject> activePortals = new List<GameObject>();
        if (spawnPointNames != null)
        {
            foreach (string pName in spawnPointNames)
            {
                if (string.IsNullOrEmpty(pName)) continue;
                GameObject pObj = FindObjectByName(pName);
                if (pObj != null) 
                {
                    activePortals.Add(pObj);
                }
                else
                {
                    Debug.LogWarning($"[BaseSummonSkill] ⚠️ Could not find portal named '{pName}' in the Hierarchy! (Please check for typos)");
                }
            }
        }

        // 1. ถ้าเป็นการ Aim (มีตำแหน่งเป้าหมาย)
        if (TargetPosition.HasValue)
        {
            Vector3 targetPos = TargetPosition.Value;

            // ถ้ามี Portal ใน List ให้เลือกตัวที่ใกล้เป้าหมายที่สุด
            if (activePortals.Count > 0)
            {
                GameObject closestPortal = activePortals[0];
                float minDistance = Vector3.Distance(targetPos, closestPortal.transform.position);

                foreach (GameObject portal in activePortals)
                {
                    float dist = Vector3.Distance(targetPos, portal.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPortal = portal;
                    }
                }

                Debug.Log($"[BaseSummonSkill] >>> MODE: Aimed | Target at {targetPos} | Picking NEAREST portal: '{closestPortal.name}'");
                // เกิดด้านหน้า Portal 1.5 เมตร เพื่อไม่ให้ค้างข้างใน
                Vector3 spawnPos = closestPortal.transform.position + closestPortal.transform.forward * 1.5f;
                return (spawnPos, closestPortal.transform.rotation);
            }

            // ถ้าไม่มี Portal เลย ให้ใช้จุดเป้าหมายเดิม (Aimed Target)
            Debug.Log($"[BaseSummonSkill] >>> MODE: Aimed | No portals found, spawning directly at target: {targetPos}");
            return (targetPos, transform.rotation);
        }

        // 2. ถ้าเป็นการพิมพ์เอง (Manual) ให้สุ่มจาก Portal ที่หาเจอ
        if (activePortals.Count > 0)
        {
            GameObject randomPortal = activePortals[Random.Range(0, activePortals.Count)];
            Vector3 spawnPos = randomPortal.transform.position + randomPortal.transform.forward * 1.5f;
            
            Debug.Log($"[BaseSummonSkill] >>> MODE: Manual (List) | Target Portal: '{randomPortal.name}' | Final Pos: {spawnPos}");
            return (spawnPos, randomPortal.transform.rotation);
        }

        // 3. Fallback: Custom Spawn Point
        if (customSpawnPoint != null)
        {
            Vector3 spawnPos = customSpawnPoint.position + customSpawnPoint.forward * 1.5f;
            Debug.Log($"[BaseSummonSkill] >>> MODE: Manual (Fallback) | Target Point: '{customSpawnPoint.name}' | Final Pos: {spawnPos}");
            return (spawnPos, customSpawnPoint.rotation);
        }

        // 4. Default: Spawning near player
        string searchedNames = (spawnPointNames != null) ? string.Join(", ", spawnPointNames) : "None";
        Debug.LogWarning($"[BaseSummonSkill] >>> MODE: Manual (Default) | No Portals found in Hierarchy! (Searched for: {searchedNames}) | Spawning at Player position: {transform.position}");
        
        // สแกนหาความผิดพลาดในชื่อให้ User ดูทาง Console
        LogHierarchyDebug();
        
        return (transform.position, transform.rotation);
    }

    private GameObject FindObjectByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // 1. ลองหาแบบตรงตัวก่อน (เร็ว)
        GameObject obj = GameObject.Find(name);
        if (obj != null) return obj;

        // 2. ถ้าไม่เจอ ให้ลองหาแบบไม่สนตัวพิมพ์เล็ก-ใหญ่ (Case-Insensitive) และรวม Inactive
        string targetLower = name.ToLower().Trim();
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.ToLower().Trim() == targetLower) return root;
            GameObject child = FindChildRecursive(root, targetLower);
            if (child != null) return child;
        }

        return null;
    }

    private GameObject FindChildRecursive(GameObject parent, string lowerName)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name.ToLower().Trim() == lowerName) return child.gameObject;
            GameObject found = FindChildRecursive(child.gameObject, lowerName);
            if (found != null) return found;
        }
        return null;
    }

    private void LogHierarchyDebug()
    {
        Debug.Log("[BaseSummonSkill] 🔍 --- Hierarchy Portal Scanner ---");
        int foundCount = 0;
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            // พิมพ์ชื่อวัตถุที่มีคำว่า 'Portal' หรือ 'Spawn' เพื่อช่วย User หาชื่อที่ถูกต้อง
            if (root.name.Contains("Portal") || root.name.Contains("Spawn"))
            {
                Debug.Log($"[BaseSummonSkill] Found potential portal: '{root.name}'");
                foundCount++;
            }
        }
        if (foundCount == 0) Debug.Log("[BaseSummonSkill] No objects with 'Portal' or 'Spawn' in their name found.");
    }

    /// <summary>
    /// ถูกเรียกเมื่อตัวช่วยเกิดมาแล้ว ให้คลาสลูกนำไปสั่งงานต่อ (เช่น สั่งให้โจมตีศัตรูตัวไหน)
    /// </summary>
    protected abstract void OnSummonCreated(GameObject summonedEntity, Transform playerTransform);
}
