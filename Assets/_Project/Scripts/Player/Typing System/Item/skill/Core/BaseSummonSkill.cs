using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // ย้ายตัวเองไปที่จุดเกิดทันที เพื่อให้ VFX หรือลูกๆ อยู่ถูกที่
        transform.position = spawnPoint.pos;
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
        // 1. ถ้าเป็นการ Aim (TargetPosition มีค่า) ให้ใช้ตำแหน่งนั้นเลย
        if (TargetPosition.HasValue)
        {
            Debug.Log($"[BaseSummonSkill] >>> MODE: Aimed Target | Pos: {TargetPosition.Value}");
            return (TargetPosition.Value, transform.rotation);
        }

        // 2. ถ้าเป็นการพิมพ์เอง (Manual) ให้สุ่มจากรายชื่อใน List
        int listCount = (spawnPointNames != null) ? spawnPointNames.Count : 0;
        if (listCount > 0)
        {
            List<string> validNames = spawnPointNames.FindAll(s => !string.IsNullOrEmpty(s));
            
            if (validNames.Count > 0)
            {
                string randomName = validNames[Random.Range(0, validNames.Count)];
                GameObject obj = FindObjectByName(randomName);

                if (obj != null)
                {
                    Debug.Log($"[BaseSummonSkill] >>> MODE: Manual (List) | Found Portal: '{randomName}' at {obj.transform.position}");
                    return (obj.transform.position, obj.transform.rotation);
                }
                else
                {
                    Debug.LogWarning($"[BaseSummonSkill] ⚠️ Could not find any object named '{randomName}' in the Hierarchy! (Mode: Manual)");
                }
            }
            else
            {
                Debug.LogWarning("[BaseSummonSkill] ⚠️ The Spawn Point Names list exists but all entries are empty!");
            }
        }

        // 3. Fallback: Custom Spawn Point
        if (customSpawnPoint != null)
        {
            Debug.Log($"[BaseSummonSkill] >>> MODE: Manual (Fallback) | Using Custom Spawn Point: {customSpawnPoint.name}");
            return (customSpawnPoint.position, customSpawnPoint.rotation);
        }

        // 4. Default: Spawning near player
        Debug.Log("[BaseSummonSkill] >>> MODE: Manual (Default) | No portals or custom points found. Spawning at player offset.");
        return (transform.position, transform.rotation);
    }

    private GameObject FindObjectByName(string name)
    {
        // ลองหาแบบปกติ (เร็ว)
        GameObject obj = GameObject.Find(name);
        if (obj != null) return obj;

        // ถ้าไม่เจอ ลองหาแบบรวม Inactive (ช้ากว่านิดหน่อยแต่ชัวร์)
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
            GameObject child = FindChildRecursive(root, name);
            if (child != null) return child;
        }

        return null;
    }

    private GameObject FindChildRecursive(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == name) return child.gameObject;
            GameObject found = FindChildRecursive(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// ถูกเรียกเมื่อตัวช่วยเกิดมาแล้ว ให้คลาสลูกนำไปสั่งงานต่อ (เช่น สั่งให้โจมตีศัตรูตัวไหน)
    /// </summary>
    protected abstract void OnSummonCreated(GameObject summonedEntity, Transform playerTransform);
}
