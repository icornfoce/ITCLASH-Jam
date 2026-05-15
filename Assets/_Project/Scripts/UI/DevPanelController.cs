using UnityEngine;
using ITCLASH.Enemies;

public class DevPanelController : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("UI Panel ที่ต้องการให้เปิด/ปิดเวลาเปิดโหมด Dev (ถ้าไม่มีปล่อยว่างได้ จะทำงานแบบไร้ UI)")]
    public GameObject devPanelUI;

    [Header("Settings")]
    public float mergeRadius = 20f; // รัศมีที่จะดึง Gem มารวมเวลาใช้คีย์ลัด L

    private bool _isPanelActive = false;

    private void Start()
    {
        // ปิด Panel ไว้ตั้งแต่เริ่ม
        if (devPanelUI != null)
        {
            devPanelUI.SetActive(false);
        }
    }

    private void Update()
    {
        // ------------------------------------------------
        // Toggle System (F)
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.F))
        {
            _isPanelActive = !_isPanelActive;
            
            if (devPanelUI != null)
            {
                devPanelUI.SetActive(_isPanelActive);
            }
            
            Debug.Log($"<color=orange>[DEV PANEL]</color> {( _isPanelActive ? "Activated" : "Deactivated")}!");
        }

        // อนุญาตให้กดคีย์ลัดอื่นๆ เฉพาะตอน Panel Active
        if (!_isPanelActive) return;

        // ------------------------------------------------
        // U: Damage ALL enemies 20% Max HP
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.U))
        {
            int count = 0;

            // Old Enemy System
            Enemy[] allOldEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in allOldEnemies)
            {
                if (e != null)
                {
                    int dmg = Mathf.RoundToInt(e.maxHealth * 0.2f);
                    e.TakeDamage(dmg);
                    count++;
                }
            }

            // New Enemy System (v2)
            EnemyController[] allV2Enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var e in allV2Enemies)
            {
                if (e != null && e.Stats != null)
                {
                    float dmg = e.Stats.maxHealth * 0.2f;
                    e.ApplyDamage(dmg);
                    count++;
                }
            }

            // MiniBoss — ต้องใช้ ApplyBossDamage (ApplyDamage ปกติเป็น Immune)
            MiniBoss[] miniBosses = Object.FindObjectsByType<MiniBoss>(FindObjectsSortMode.None);
            foreach (var b in miniBosses)
            {
                if (b != null)
                {
                    float dmg = b.maxHealth * 0.2f;
                    b.ApplyBossDamage(dmg); // ✅ ใช้ method จริง ไม่ใช่ Immune stub
                    count++;
                }
            }

            Debug.Log($"<color=orange>[DEV]</color> Damaged {count} enemies for 20% of their MAX HP!");
        }


        // ------------------------------------------------
        // J: Kill ALL enemies
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.J))
        {
            Enemy[] allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            int count = 0;
            
            foreach (Enemy e in allEnemies)
            {
                Destroy(e.gameObject);
                count++;
            }
            
            Debug.Log($"<color=orange>[DEV]</color> Killed all {count} enemies!");
        }


        // ------------------------------------------------
        // V: Force ALL MiniBoss to use Summon Skill
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.V))
        {
            MiniBoss[] bosses = Object.FindObjectsByType<MiniBoss>(FindObjectsSortMode.None);
            if (bosses.Length == 0)
            {
                Debug.Log("<color=orange>[DEV]</color> No MiniBoss found in scene.");
            }
            else
            {
                foreach (var boss in bosses)
                {
                    if (boss != null) boss.ForceUseSummon();
                }
                Debug.Log($"<color=orange>[DEV]</color> Forced {bosses.Length} MiniBoss(es) to use Summon!");
            }
        }

        // ------------------------------------------------
        // Y: Force ALL MiniBoss to play a skill then die
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.Y))
        {
            MiniBoss[] bosses = Object.FindObjectsByType<MiniBoss>(FindObjectsSortMode.None);
            if (bosses.Length == 0)
            {
                Debug.Log("<color=orange>[DEV]</color> No MiniBoss found in scene.");
            }
            else
            {
                foreach (var boss in bosses)
                {
                    if (boss != null) boss.ForceSkillThenDie();
                }
                Debug.Log($"<color=orange>[DEV]</color> Forced {bosses.Length} MiniBoss(es) to play skill then die!");
            }
        }

        // ------------------------------------------------
        // 1: Force ALL MiniBoss to use Void Zone Skill
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MiniBoss[] bosses = Object.FindObjectsByType<MiniBoss>(FindObjectsSortMode.None);
            if (bosses.Length == 0)
            {
                Debug.Log("<color=orange>[DEV]</color> No MiniBoss found in scene.");
            }
            else
            {
                foreach (var boss in bosses)
                {
                    if (boss != null) boss.ForceUseVoidZone();
                }
                Debug.Log($"<color=orange>[DEV]</color> Forced {bosses.Length} MiniBoss(es) to use Void Zone!");
            }
        }

        // ------------------------------------------------
        // 2: Skip Wave
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var manager = FindFirstObjectByType<ITCLASH.Spawners.WaveManager>();
            if (manager != null) manager.SkipWave();
        }
    }
}
