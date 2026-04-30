using UnityEngine;

[System.Serializable]
public struct OrbitLevelStats
{
    public float damage;
    public float radius;
    public float rotationSpeed;
}

[CreateAssetMenu(fileName = "NewOrbitEffect", menuName = "Rouge-like/Code for Buff/Orbit Effect")]
public class OrbitEffect : UpgradeEffect
{
    [Header("Prefabs")]
    public GameObject projectilePrefab;

    [Header("Level Data (Index 0 = Lv.1, Index 1 = Lv.2, ...)")]
    public System.Collections.Generic.List<OrbitLevelStats> levels;

    public override void Apply(GameObject player)
    {
        // 1. ค้นหาหรือเพิ่ม OrbitBuffManager เข้าไปที่ตัว Player
        OrbitBuffManager manager = player.GetComponent<OrbitBuffManager>();
        if (manager == null)
        {
            manager = player.AddComponent<OrbitBuffManager>();
            manager.projectilePrefab = projectilePrefab;
        }

        // 2. อัปเดตข้อมูลเลเวลให้กับ Manager (เพื่อให้ใช้ค่าที่ตั้งจาก SO นี้)
        manager.UpdateLevelStats(levels);

        // 3. สั่งอัพเลเวล
        manager.LevelUp();
    }
}
