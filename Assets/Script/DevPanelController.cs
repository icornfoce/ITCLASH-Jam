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
            Enemy[] allEnemies = FindObjectsOfType<Enemy>();
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
            Enemy[] allEnemies = FindObjectsOfType<Enemy>();
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
    }
}
