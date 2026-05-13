using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
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
        public GameObject gameEndUI;
        public UnityEvent OnAllWavesCleared;

        public static bool IsGameFinished { get; private set; } = false;

        private bool isTransitioning = false;
        private bool waveReadyToCheck = false;
        private GameObject activeBoss;

        void Awake()
        {
            // Force start from the beginning regardless of Inspector value
            currentWaveIndex = -1;
            IsGameFinished = false;
        }

        void Start()
        {
            if (gameEndUI != null) gameEndUI.SetActive(false);
            
            // Start the first wave
            NextWave();
        }

        void Update()
        {
            if (isTransitioning || !waveReadyToCheck || currentWaveIndex < 0 || currentWaveIndex >= waves.Count) return;

            // Check if all normal enemies are dead
            bool enemiesDead = EnemyRegistry.All.Count == 0;
            
            // Check if it's a boss wave and if boss is dead
            bool bossDead = true;
            if (waves[currentWaveIndex].isBossWave)
            {
                if (activeBoss != null)
                {
                    var miniBoss = activeBoss.GetComponent<MiniBoss>();
                    if (miniBoss != null)
                    {
                        bossDead = miniBoss.IsDead;
                        
                        // If boss dies, kill all remaining minions/summoned enemies immediately
                        if (bossDead && !enemiesDead)
                        {
                            KillAllEnemiesInRegistry();
                            enemiesDead = true; // Update flag to proceed to clear sequence
                        }
                    }
                    else bossDead = false;
                }
                else
                {
                    bossDead = true; // Boss was destroyed or never spawned
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
                // All waves finished
                EndGame();
            }
        }

        private void UpdateWaveUI()
        {
            if (waveText == null) return;

            if (currentWaveIndex >= 0 && currentWaveIndex < waves.Count)
            {
                if (waves[currentWaveIndex].isBossWave)
                {
                    waveText.text = "Final Wave";
                }
                else
                {
                    waveText.text = $"Wave {currentWaveIndex + 1}/{waves.Count}";
                }
            }
        }

        private IEnumerator StartWaveRoutine(EnemyWave wave)
        {
            isTransitioning = true;
            waveReadyToCheck = false;
            Debug.Log($"[WaveManager] Starting Wave {currentWaveIndex + 1}. IsBossWave: {wave.isBossWave}");

            // 1. Play Start Timeline if exists
            if (wave.startTimeline != null)
            {
                Debug.Log("[WaveManager] Playing Start Timeline...");
                wave.startTimeline.Play();
                
                // Robust wait logic using duration (Fixed per user suggestion)
                float timeout = (float)wave.startTimeline.duration + 0.5f;
                float elapsed = 0f;
                while (wave.startTimeline.state == PlayState.Playing && elapsed < timeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                Debug.Log("[WaveManager] Start Timeline Finished.");
            }
            else if (wave.isBossWave)
            {
                // Only wait for delay if no timeline
                yield return new WaitForSecondsRealtime(wave.bossSpawnDelay);
            }

            // 2. Handle Boss Spawn if Boss Wave
            if (wave.isBossWave)
            {
                SpawnBoss(wave);
            }

            // 3. Spawn Normal Enemies (Now spawns in both normal and boss waves if defined)
            SpawnWaveEnemies(wave);

            // Wait a bit to ensure enemies have registered and are stable
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
            if (wave.bossPrefab == null)
            {
                Debug.LogWarning($"[WaveManager] Boss Prefab is MISSING in Wave {currentWaveIndex + 1}!");
                return;
            }

            Transform spawnPoint = wave.bossSpawnPoint != null ? wave.bossSpawnPoint : GetRandomGlobalSpawnPoint();
            activeBoss = Instantiate(wave.bossPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log($"[WaveManager] Boss {activeBoss.name} Spawned at {spawnPoint.position}");
        }

        private IEnumerator HandleWaveCleared()
        {
            isTransitioning = true;
            EnemyWave currentWave = waves[currentWaveIndex];

            Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} Cleared!");

            // Play End Timeline if exists
            if (currentWave.endTimeline != null)
            {
                Debug.Log("[WaveManager] Playing End Timeline...");
                currentWave.endTimeline.Play();
                
                // Robust wait logic using duration
                float timeout = (float)currentWave.endTimeline.duration + 0.5f;
                float elapsed = 0f;
                while (currentWave.endTimeline.state == PlayState.Playing && elapsed < timeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // Small delay before next wave
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
            Debug.Log("[WaveManager] Boss defeated! Killing all remaining enemies.");
            var enemies = new List<EnemyController>(EnemyRegistry.All);
            foreach (var e in enemies)
            {
                if (e != null) e.ApplyDamage(999999f); // Kill instantly
            }
        }

        /// <summary>
        /// Forces the current wave to end by killing all registered enemies.
        /// Called by DevPanelController.
        /// </summary>
        public void SkipWave()
        {
            if (isTransitioning) return;

            Debug.Log("<color=orange>[DEV]</color> Skipping Wave...");
            
            // Kill all normal enemies
            var enemies = new List<EnemyController>(EnemyRegistry.All);
            foreach (var e in enemies)
            {
                if (e != null) e.ApplyDamage(999999f); // Kill instantly
            }

            // Kill boss if active
            if (activeBoss != null)
            {
                var miniBoss = activeBoss.GetComponent<MiniBoss>();
                if (miniBoss != null)
                {
                    miniBoss.ApplyDamage(999999f);
                }
                else 
                {
                    Destroy(activeBoss);
                }
            }
        }

        private void EndGame()
        {
            Debug.Log("[WaveManager] All Waves Cleared! Showing Game End UI.");
            
            // Hide All Game UI
            if (waveText != null) waveText.gameObject.SetActive(false);
            if (mainHUD != null) mainHUD.SetActive(false);

            // Show End UI
            if (gameEndUI != null) gameEndUI.SetActive(true);
            
            OnAllWavesCleared?.Invoke();
            IsGameFinished = true;
            
            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            isTransitioning = false;
        }
    }
}
