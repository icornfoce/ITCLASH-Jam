using UnityEngine;

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
            ITCLASH.Enemies.EnemyController[] allV2Enemies = Object.FindObjectsByType<ITCLASH.Enemies.EnemyController>(FindObjectsSortMode.None);
            foreach (var e in allV2Enemies)
            {
                if (e != null && e.Stats != null)
                {
                    float dmg = e.Stats.maxHealth * 0.2f;
                    e.ApplyDamage(dmg);
                    count++;
                }
            }

            // MiniBoss
            MiniBoss[] miniBosses = Object.FindObjectsByType<MiniBoss>(FindObjectsSortMode.None);
            foreach (var b in miniBosses)
            {
                if (b != null)
                {
                    float dmg = b.maxHealth * 0.2f;
                    b.ApplyDamage(dmg);
                    count++;
                }
            }

            Debug.Log($"<color=orange>[DEV]</color> Damaged {count} enemies for 20% of their MAX HP!");
        }

        // ------------------------------------------------
        // G: Level + 1
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (PlayerExperience.Instance != null)
            {
                float neededExp = PlayerExperience.Instance.maxExpForNextLevel - PlayerExperience.Instance.currentExp;
                PlayerExperience.Instance.currentExp += neededExp;
                PlayerExperience.Instance.AddExperience(0); // Trigger check กึ่งบังคับอัพเวล
                Debug.Log($"<color=orange>[DEV]</color> Forced Level Up! Now level {PlayerExperience.Instance.currentLevel}");
            }
        }

        // ------------------------------------------------
        // H: Add 500 EXP
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (PlayerExperience.Instance != null)
            {
                PlayerExperience.Instance.AddExperience(500f);
                Debug.Log("<color=orange>[DEV]</color> Added 500 Base EXP.");
            }
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
        // K: Kill ONLY closest enemy
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.K))
        {
            Enemy[] allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            Enemy closest = null;
            float minDistance = float.MaxValue;

            Transform playerT = PlayerExperience.Instance != null ? PlayerExperience.Instance.transform : Camera.main.transform;

            foreach (Enemy e in allEnemies)
            {
                float dist = Vector3.Distance(playerT.position, e.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = e;
                }
            }

            if (closest != null)
            {
                Destroy(closest.gameObject);
                Debug.Log($"<color=orange>[DEV]</color> Killed closest enemy at {minDistance:F2} unit distance.");
            }
            else
            {
                Debug.Log("<color=orange>[DEV]</color> No enemy found on map to kill.");
            }
        }

        // ------------------------------------------------
        // L: Force merge nearby gems into Rare Gem
        // ------------------------------------------------
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (GemManager.Instance != null)
            {
                Transform playerT = PlayerExperience.Instance != null ? PlayerExperience.Instance.transform : Camera.main.transform;
                GemManager.Instance.ForceMergeGems(playerT.position, mergeRadius);
            }
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

        // 2: Skip Wave
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var manager = FindFirstObjectByType<ITCLASH.Spawners.WaveManager>();
            if (manager != null) manager.SkipWave();
        }
    }
}
