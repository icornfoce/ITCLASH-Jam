using UnityEngine;
using System.Collections.Generic;

public class OrbitBuffManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject projectilePrefab;
    
    private int currentLevel = 0;
    private List<GameObject> activeProjectiles = new List<GameObject>();
    private List<OrbitLevelStats> levelStatsList;

    public void UpdateLevelStats(List<OrbitLevelStats> stats)
    {
        levelStatsList = stats;
    }

    public void LevelUp()
    {
        currentLevel++;
        RefreshProjectiles();
    }

    private void RefreshProjectiles()
    {
        if (levelStatsList == null || levelStatsList.Count == 0)
        {
            Debug.LogWarning("[OrbitBuffManager] Level stats list is empty!");
            return;
        }

        // ป้องกันกรณีเวลเกินจำนวนข้อมูลที่มี
        int statsIndex = Mathf.Clamp(currentLevel - 1, 0, levelStatsList.Count - 1);
        OrbitLevelStats currentStats = levelStatsList[statsIndex];

        // ลบของเก่าออกก่อน
        foreach (var p in activeProjectiles)
        {
            if (p != null) Destroy(p);
        }
        activeProjectiles.Clear();

        // สร้างลูกแก้ววนรอบตัว (ใช้ currentLevel เป็นจำนวนลูก)
        int count = currentLevel; 
        for (int i = 0; i < count; i++)
        {
            GameObject p = Instantiate(projectilePrefab, transform);
            OrbitProjectile orbit = p.GetComponent<OrbitProjectile>();
            if (orbit == null) orbit = p.AddComponent<OrbitProjectile>();

            float startAngle = (360f / count) * i;
            orbit.Setup(currentStats.radius, currentStats.damage, currentStats.rotationSpeed, startAngle);
            activeProjectiles.Add(p);
        }

        Debug.Log($"[OrbitBuff] Level {currentLevel} Applied: Dmg={currentStats.damage}, Radius={currentStats.radius}");
    }
}
