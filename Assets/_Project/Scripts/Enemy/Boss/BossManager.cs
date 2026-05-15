using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

namespace ITCLASH.Enemies
{
    public class BossManager : MonoBehaviour
    {
        public static BossManager Instance { get; private set; }

        [Header("--- Boss Configuration ---")]
        public GameObject bossPrefab;
        public Transform bossSpawnPoint;

        [Header("--- Timelines ---")]
        public PlayableDirector introTimeline;
        public PlayableDirector deathTimeline;

        [Header("--- UI Management ---")]
        public GameObject gameUIContainer;
        public GameObject victoryUI;

        private MiniBoss spawnedBoss;
        private bool isBossDead = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (introTimeline != null)
            {
                StartCoroutine(FullBossSequence());
            }
            else
            {
                // ถ้าไม่มีคัทซีน ให้เสกบอสทันที
                SpawnBoss();
            }
        }

        private IEnumerator FullBossSequence()
        {
            // 1. เตรียมฉาก (ซ่อน UI และล็อคตัวละคร)
            if (gameUIContainer != null) gameUIContainer.SetActive(false);
            if (victoryUI != null) victoryUI.SetActive(false);
            SetPlayerControl(false);

            // 2. เล่นคัทซีนเปิดตัว
            if (introTimeline != null)
            {
                introTimeline.Play();
                yield return new WaitUntil(() => introTimeline.state != PlayState.Playing);
            }

            // 3. เสกบอสและเริ่มการต่อสู้
            SpawnBoss();
            if (gameUIContainer != null) gameUIContainer.SetActive(true);
            SetPlayerControl(true);
        }

        private void SpawnBoss()
        {
            if (bossPrefab == null || bossSpawnPoint == null)
            {
                Debug.LogError("[BossManager] Missing Boss Prefab or Spawn Point!");
                return;
            }

            GameObject bossObj = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            spawnedBoss = bossObj.GetComponent<MiniBoss>();

            if (spawnedBoss != null)
            {
                // ลงทะเบียน Event ตอนบอสตาย
                spawnedBoss.OnBossDeathEvent += HandleBossDeath;
                spawnedBoss.StartFight();
            }
        }

        private void HandleBossDeath()
        {
            if (isBossDead) return;
            isBossDead = true;

            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            // 1. ซ่อน UI และล็อคตัวละครระหว่างคัทซีนตาย
            if (gameUIContainer != null) gameUIContainer.SetActive(false);
            SetPlayerControl(false);

            // 2. เล่นคัทซีนตอนตาย
            if (deathTimeline != null)
            {
                deathTimeline.Play();
                yield return new WaitUntil(() => deathTimeline.state != PlayState.Playing);
            }

            // 3. โชว์ Victory Screen
            if (victoryUI != null) victoryUI.SetActive(true);
            
            // ปลดล็อคเมาส์มาให้กดปุ่มตอนจบ
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetPlayerControl(bool enabled)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var controller = player.GetComponent<PlayerController>();
                if (controller != null) controller.enabled = enabled;

                var cam = player.GetComponentInChildren<FirstPersonCamera>();
                if (cam != null) cam.enabled = enabled;

                if (!enabled)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        public void ForceStartBoss()
        {
            StopAllCoroutines();
            StartCoroutine(FullBossSequence());
        }
    }
}
