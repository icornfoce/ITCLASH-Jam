using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// จัดการ Flow การต่อสู้กับบอสทั้งหมด
    ///
    /// FLOW:
    ///   [Intro Timeline เล่น]
    ///       → เล่นจบ → Destroy GameObject ที่มี introTimeline
    ///       → SetActive(true) บอสจริง + StartFight()
    ///   [บอสตาย]
    ///       → MiniBoss.Die() → Destroy ตัวเอง → ส่ง event OnBossDeathEvent
    ///       → เล่น dead timeline
    ///       → เล่นจบ → ปิด UI ทุกอย่าง + เปิด Victory UI
    ///   [ผู้เล่นตาย]
    ///       → เฟดดำ → เปิด Defeat UI
    /// </summary>
    public class BossManager : MonoBehaviour
    {
        public static BossManager Instance { get; private set; }

        // ─────────────────────────────────────────────
        // Inspector Fields
        // ─────────────────────────────────────────────

        [Header("=== Boss Reference ===")]
        [Tooltip("ลาก MiniBoss (บอสจริง) ที่วางใน Scene มาใส่ — จะถูกซ่อนไว้จนกว่า Intro จะจบ")]
        public MiniBoss realBoss;

        [Header("=== Timelines ===")]
        [Tooltip("Intro cutscene — GameObject นี้จะถูก Destroy หลังเล่นจบ")]
        public PlayableDirector introTimeline;

        [Tooltip("Dead cutscene — เล่นหลังบอสตาย")]
        public PlayableDirector deadTimeline;

        [Header("=== UI ===")]
        [Tooltip("Root ของ UI ทั้งหมดระหว่างเล่น (BossHP, Crosshair, ฯลฯ)")]
        public GameObject gameUIContainer;

        [Tooltip("UI แสดงเมื่อชนะ")]
        public GameObject victoryUI;

        [Tooltip("UI แสดงเมื่อแพ้")]
        public GameObject defeatUI;

        [Tooltip("ชื่อ Scene ที่จะโหลดเมื่อแพ้ (ปล่อยว่างไว้ถ้าจะใช้แค่ UI ด้านบน)")]
        public string defeatSceneName;

        [Header("=== Death Fade ===")]
        [Tooltip("CanvasGroup สีดำสำหรับ Fade ตอนผู้เล่นตาย")]
        public CanvasGroup deathFadeGroup;
        public float deathFadeDuration = 2f;

        [Header("=== Audio ===")]
        [Tooltip("AudioSource สำหรับเล่น BGM")]
        public AudioSource bgmSource;
        
        [Tooltip("BGM ปกติที่เล่นในฉาก")]
        public AudioClip normalBGM;
        
        [Tooltip("BGM ที่เล่นตอน Game Over")]
        public AudioClip gameOverBGM;

        // ─────────────────────────────────────────────
        // Private State
        // ─────────────────────────────────────────────

        private bool _bossDeadHandled  = false;
        private bool _playerDeadHandled = false;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // ซ่อนบอสจริงไว้ก่อน เพื่อไม่ให้โผล่ระหว่าง Intro
            if (realBoss != null)
                realBoss.gameObject.SetActive(false);

            // ซ่อน UI เกมและหน้าจบ
            SetActive(gameUIContainer, false);
            SetActive(victoryUI, false);
            SetActive(defeatUI, false);

            // ซ่อน Fade Group
            if (deathFadeGroup != null)
            {
                deathFadeGroup.alpha = 0f;
                deathFadeGroup.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            Debug.Log("[BossManager] Start() — เริ่มต้น Flow");

            // เล่น BGM ปกติเมื่อเริ่ม
            if (bgmSource != null && normalBGM != null)
            {
                bgmSource.clip = normalBGM;
                bgmSource.loop = true;
                bgmSource.Play();
            }

            // Subscribe ตายของ Player
            PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null)
            {
                ph.OnDeath.AddListener(OnPlayerDied);
                Debug.Log("[BossManager] Subscribe PlayerHealth.OnDeath ✓");
            }
            else
            {
                Debug.LogWarning("[BossManager] ไม่พบ PlayerHealth ใน Scene!");
            }

            // เริ่ม Flow
            if (introTimeline != null)
            {
                Debug.Log("[BossManager] พบ introTimeline → เริ่ม IntroSequence");
                StartCoroutine(IntroSequence());
            }
            else
            {
                Debug.Log("[BossManager] ไม่มี introTimeline → BeginFight ทันที");
                BeginFight();
            }
        }

        // ─────────────────────────────────────────────
        // Main Sequences
        // ─────────────────────────────────────────────

        private IEnumerator IntroSequence()
        {
            Debug.Log("[BossManager] IntroSequence — ล็อคผู้เล่น + เล่น Intro");
            SetPlayerControl(false);

            float duration = (float)introTimeline.duration;
            introTimeline.Play();
            Debug.Log($"[BossManager] introTimeline.Play() — รอ {duration:F2}s");

            // รอให้ครบ duration ของ Timeline (+ buffer นิดนึง)
            yield return new WaitForSeconds(duration + 0.1f);

            Debug.Log("[BossManager] Intro เล่นจบแล้ว → ลบ introTimeline GameObject");

            if (introTimeline != null)
            {
                Destroy(introTimeline.gameObject);
                introTimeline = null;
                Debug.Log("[BossManager] Destroy introTimeline.gameObject ✓");
            }

            yield return null; // รอ 1 frame ให้ Destroy เสร็จ
            BeginFight();
        }

        private void BeginFight()
        {
            Debug.Log("[BossManager] BeginFight() called");

            if (realBoss == null)
            {
                Debug.LogError("[BossManager] ❌ 'realBoss' เป็น null! ลาก MiniBoss มาใส่ใน Inspector");
                return;
            }

            Debug.Log($"[BossManager] SetActive(true) → {realBoss.name}");
            realBoss.gameObject.SetActive(true);
            realBoss.OnBossDeathEvent += OnBossDied;
            realBoss.StartFight();

            SetActive(gameUIContainer, true);
            SetPlayerControl(true);

            Debug.Log("[BossManager] ✅ Fight Started!");
        }

        // ─────────────────────────────────────────────
        // Event Handlers
        // ─────────────────────────────────────────────

        /// <summary>
        /// ถูกเรียกจาก MiniBoss.Die() ผ่าน event
        /// MiniBoss จะ Destroy ตัวเองหลัง invoke event นี้
        /// </summary>
        private void OnBossDied()
        {
            if (_bossDeadHandled || _playerDeadHandled) return;
            _bossDeadHandled = true;
            StartCoroutine(VictorySequence());
        }

        /// <summary>
        /// ถูกเรียกจาก PlayerHealth.OnDeath
        /// </summary>
        private void OnPlayerDied()
        {
            if (_bossDeadHandled || _playerDeadHandled) return;
            _playerDeadHandled = true;
            StartCoroutine(DefeatSequence());
        }

        // ─────────────────────────────────────────────
        // Outcome Sequences
        // ─────────────────────────────────────────────

        /// <summary>
        /// บอสตาย → เล่น dead timeline → ปิด UI ทุกอย่าง → Victory UI
        /// </summary>
        private IEnumerator VictorySequence()
        {
            SetPlayerControl(false);
            SetActive(gameUIContainer, false);

            // เล่น Dead Timeline (ใช้ duration แบบเดียวกับ intro)
            if (deadTimeline != null)
            {
                float duration = (float)deadTimeline.duration;
                deadTimeline.Play();
                Debug.Log($"[BossManager] deadTimeline.Play() — รอ {duration:F2}s");
                yield return new WaitForSeconds(duration + 0.1f);
            }

            Debug.Log("[BossManager] Victory!");

            // ปิด UI ทั้งหมด
            SetActive(gameUIContainer, false);
            SetActive(defeatUI, false);

            // ปลดล็อคเมาส์ก่อนโชว์ Victory UI
            // เพื่อให้ผู้เล่นกดปุ่มใน UI ได้ทันที
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            SetActive(victoryUI, true);
        }


        /// <summary>
        /// ผู้เล่นตาย → เฟดดำ → Defeat UI
        /// </summary>
        private IEnumerator DefeatSequence()
        {
            SetPlayerControl(false);
            SetActive(gameUIContainer, false);

            // หยุดทุกอย่างในเกม (ศัตรู, physics, animation, ฯลฯ)
            Time.timeScale = 0f;
            Debug.Log("[BossManager] Time.timeScale = 0 — เกมหยุดแล้ว");

            // ปิด BGM ปกติ
            if (bgmSource != null)
            {
                bgmSource.Stop();
                
                // เปลี่ยน BGM เป็น Game Over BGM
                if (gameOverBGM != null)
                {
                    bgmSource.clip = gameOverBGM;
                    bgmSource.loop = true;
                    bgmSource.ignoreListenerPause = true; // เผื่อกรณีมีการ pause
                    bgmSource.Play();
                }
            }

            // เฟดดำ — ต้องใช้ unscaledDeltaTime เพราะ timeScale = 0
            if (deathFadeGroup != null)
            {
                deathFadeGroup.gameObject.SetActive(true);
                float t = 0f;
                while (t < deathFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    deathFadeGroup.alpha = Mathf.Clamp01(t / deathFadeDuration);
                    yield return null;
                }
                deathFadeGroup.alpha = 1f;
            }
            else
            {
                yield return new WaitForSecondsRealtime(1.5f);
            }

            Debug.Log("[BossManager] Defeat!");

            // ปลดล็อคเมาส์
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            // คืน timeScale ให้ปกติ
            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(defeatSceneName))
            {
                // โหลด Scene แพ้
                UnityEngine.SceneManagement.SceneManager.LoadScene(defeatSceneName);
            }
            else
            {
                // โชว์ Defeat UI ใน Scene เดิม
                SetActive(gameUIContainer, false);
                SetActive(victoryUI, false);
                SetActive(defeatUI, true);
            }
        }


        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        private void SetPlayerControl(bool enable)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = enable;

            var cam = player.GetComponentInChildren<FirstPersonCamera>();
            if (cam != null) cam.enabled = enable;

            if (enable)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
        }

        private static void SetActive(GameObject go, bool state)
        {
            if (go != null) go.SetActive(state);
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ─────────────────────────────────────────────
        // Dev / Debug
        // ─────────────────────────────────────────────

        /// <summary>เรียกจาก Dev Panel เพื่อข้าม Intro แล้วเริ่มสู้ทันที</summary>
        public void DEV_SkipIntroAndFight()
        {
            StopAllCoroutines();
            if (introTimeline != null) Destroy(introTimeline.gameObject);
            BeginFight();
        }

        /// <summary>เรียกจาก Dev Panel เพื่อบังคับ Victory</summary>
        public void DEV_ForceVictory()
        {
            if (_bossDeadHandled || _playerDeadHandled) return;
            _bossDeadHandled = true;
            StartCoroutine(VictorySequence());
        }

        /// <summary>เรียกจาก Dev Panel เพื่อบังคับ Defeat</summary>
        public void DEV_ForceDefeat()
        {
            if (_bossDeadHandled || _playerDeadHandled) return;
            _playerDeadHandled = true;
            StartCoroutine(DefeatSequence());
        }
    }
}
