using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using TMPro;
using ITCLASH.Enemies;

namespace ITCLASH.Spawners
{
    [System.Serializable]
    public class EnemyWave
    {
        [Header("Enemy Pool")]
        public List<GameObject> possibleEnemies;
        public int totalEnemiesToSpawn;

        [Header("Boss Wave Settings")]
        public bool isBossWave;
        public GameObject bossPrefab;
        public Transform bossSpawnPoint;
        public float bossSpawnDelay = 2f;

        [Header("Cutscenes")]
        [Tooltip("Timeline played at the start of this wave (e.g., boss intro)")]
        public PlayableDirector startTimeline;
        [Tooltip("Timeline played after clearing this wave (e.g., boss death cutscene)")]
        public PlayableDirector endTimeline;
    }

    public class WaveManager : MonoBehaviour
    {
        [Header("Spawn Points")]
        public Transform[] globalSpawnPoints;

        [Header("Waves Configuration")]
        public List<EnemyWave> waves;
        public int currentWaveIndex = -1;

        [Header("UI & Game End")]
        public TextMeshProUGUI waveText;
        public GameObject mainHUD;
        public UnityEvent OnAllWavesCleared;
        
        [Header("Scene Transition")]
        public string nextSceneName;
        public float sceneLoadDelay = 1f;

        [HideInInspector] public bool IsGameFinished = false;

        private bool isTransitioning = false;
        private bool waveReadyToCheck = false;
        private GameObject activeBoss;

        void Awake()
        {
            currentWaveIndex = -1;
            IsGameFinished = false;
        }

        void Start()
        {
            // Start the first wave
            NextWave();
        }

        void Update()
        {
            if (isTransitioning || !waveReadyToCheck || currentWaveIndex < 0 || currentWaveIndex >= waves.Count) return;

            bool enemiesDead = EnemyRegistry.All.Count == 0;
            bool bossDead = true;

            if (waves[currentWaveIndex].isBossWave)
            {
                if (activeBoss != null)
                {
                    var miniBoss = activeBoss.GetComponent<MiniBoss>();
                    if (miniBoss != null)
                    {
                        bossDead = miniBoss.IsDead;
                        if (bossDead && !enemiesDead)
                        {
                            KillAllEnemiesInRegistry();
                            enemiesDead = true;
                        }
                    }
                    else bossDead = false;
                }
            }

            if (enemiesDead && bossDead)
            {
                StartCoroutine(HandleWaveCleared());
            }
        }

        public void NextWave()
        {
            currentWaveIndex++;

            if (currentWaveIndex < waves.Count)
            {
                UpdateWaveUI();
                StartCoroutine(StartWaveRoutine(waves[currentWaveIndex]));
            }
            else
            {
                EndGame();
            }
        }

        private void UpdateWaveUI()
        {
            if (waveText == null) return;
            if (currentWaveIndex >= 0 && currentWaveIndex < waves.Count)
            {
                waveText.text = waves[currentWaveIndex].isBossWave ? "Final Wave" : $"Wave {currentWaveIndex + 1}/{waves.Count}";
            }
        }

        private IEnumerator StartWaveRoutine(EnemyWave wave)
        {
            isTransitioning = true;
            waveReadyToCheck = false;

            if (wave.startTimeline != null)
            {
                wave.startTimeline.Play();
                float timeout = (float)wave.startTimeline.duration + 0.5f;
                float elapsed = 0f;
                while (wave.startTimeline.state == PlayState.Playing && elapsed < timeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else if (wave.isBossWave)
            {
                yield return new WaitForSecondsRealtime(wave.bossSpawnDelay);
            }

            if (wave.isBossWave) SpawnBoss(wave);
            SpawnWaveEnemies(wave);

            yield return new WaitForSecondsRealtime(0.1f);
            waveReadyToCheck = true;
            isTransitioning = false;
        }

        private void SpawnWaveEnemies(EnemyWave wave)
        {
            if (wave.possibleEnemies == null || wave.possibleEnemies.Count == 0) return;
            for (int i = 0; i < wave.totalEnemiesToSpawn; i++)
            {
                GameObject randomPrefab = wave.possibleEnemies[Random.Range(0, wave.possibleEnemies.Count)];
                Transform spawnPoint = GetRandomGlobalSpawnPoint();
                if (randomPrefab != null && spawnPoint != null)
                {
                    Instantiate(randomPrefab, spawnPoint.position, spawnPoint.rotation);
                }
            }
        }

        private void SpawnBoss(EnemyWave wave)
        {
            if (wave.bossPrefab == null) return;
            Transform spawnPoint = wave.bossSpawnPoint != null ? wave.bossSpawnPoint : GetRandomGlobalSpawnPoint();
            activeBoss = Instantiate(wave.bossPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        private IEnumerator HandleWaveCleared()
        {
            isTransitioning = true;
            EnemyWave currentWave = waves[currentWaveIndex];

            if (currentWave.endTimeline != null)
            {
                currentWave.endTimeline.Play();
                float timeout = (float)currentWave.endTimeline.duration + 0.5f;
                float elapsed = 0f;
                while (currentWave.endTimeline.state == PlayState.Playing && elapsed < timeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.2f);
            NextWave();
        }

        private Transform GetRandomGlobalSpawnPoint()
        {
            if (globalSpawnPoints == null || globalSpawnPoints.Length == 0) return transform;
            return globalSpawnPoints[Random.Range(0, globalSpawnPoints.Length)];
        }

        private void KillAllEnemiesInRegistry()
        {
            var enemies = new List<EnemyController>(EnemyRegistry.All);
            foreach (var e in enemies) if (e != null) e.ApplyDamage(999999f);
        }

        public void SkipWave()
        {
            if (isTransitioning) return;
            var enemies = new List<EnemyController>(EnemyRegistry.All);
            foreach (var e in enemies) if (e != null) e.ApplyDamage(999999f);
            if (activeBoss != null)
            {
                var miniBoss = activeBoss.GetComponent<MiniBoss>();
                if (miniBoss != null) miniBoss.ApplyDamage(999999f);
                else Destroy(activeBoss);
            }
        }

        private void EndGame()
        {
            IsGameFinished = true;
            if (waveText != null) waveText.gameObject.SetActive(false);
            if (mainHUD != null) mainHUD.SetActive(false);
            
            OnAllWavesCleared?.Invoke();

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                StartCoroutine(LoadNextScene());
            }
        }

        private IEnumerator LoadNextScene()
        {
            yield return new WaitForSecondsRealtime(sceneLoadDelay);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
