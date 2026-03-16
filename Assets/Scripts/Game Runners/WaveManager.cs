using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int firstWaveCount = 2;
    public int enemyToAddByWave = 1;

    [Header("Melee Enemy Prefabs")]
    public GameObject normalEnemyPrefab;
    public GameObject strongEnemyPrefab;

    [Header("Wizard Enemy Prefabs")]
    public GameObject fireWizardPrefab;
    public GameObject iceWizardPrefab;
    public GameObject supportWizardPrefab;

    [Header("References")]
    [SerializeField] private PowerUpManager powerUpManager;
    [SerializeField] private GameManager gameManager;

    public Gates[] gates;

    private int activeEnemyCount = 0;
    private bool isSpawning = false;
    private int currentWave = 0;

    // ─── HUD İÇİN PUBLIC GETTER'LAR ───
    public int CurrentWave => currentWave;
    public int ActiveEnemyCount => activeEnemyCount;

    /// <summary>
    /// Kapatılmış kapı sayısını döndürür (HUD için).
    /// </summary>
    public int ClosedGateCount
    {
        get
        {
            int count = 0;
            foreach (var gate in gates)
            {
                if (!gate.isActive) count++;
            }
            return count;
        }
    }

    public int TotalGateCount => gates.Length;

    void OnEnable()
    {
        GameEvents.OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyDied -= HandleEnemyDied;
    }

    void Start()
    {
        activeEnemyCount = 0;
        currentWave = 0;
    }

    void Update()
    {
        if (!GameManager.isGameActive) return;

        if (activeEnemyCount == 0 && !isSpawning)
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    private void HandleEnemyDied()
    {
        activeEnemyCount--;
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;
        powerUpManager.SpawnPowerUp();

        List<Gates> activeGates = new List<Gates>();
        foreach (var gate in gates)
        {
            if (gate.isActive) activeGates.Add(gate);
        }

        if (activeGates.Count == 0)
        {
            CheckForVictory();
            yield break;
        }

        int closedGates = gates.Length - activeGates.Count;
        float difficultyScore = (float)closedGates / gates.Length;

        // Kademeli (Phased) Zorluk Sistemi:
        // Aşama 1 (0-1 kapı kapalı): SADECE normal düşmanlar. Sayıları biraz fazla olabilir ama öldürmesi kolay.
        // Aşama 2 (2-3 kapı kapalı): Güçlü düşmanlar (şövalyeler vs.) ve ufaktan büyücüler başlar.
        // Aşama 3 (Son 1-2 kapı): Her şey serbest, max zorluk.
        
        for (int i = 0; i < firstWaveCount; i++)
        {
            Gates randomGate = activeGates[Random.Range(0, activeGates.Count)];
            GameObject prefabToSpawn = normalEnemyPrefab; // Varsayılanı her zaman normaldir
            float roll = Random.value;

            // Aşama 3 (Sonlara doğru)
            if (closedGates >= gates.Length - 2)
            {
                if (roll < 0.25f) prefabToSpawn = GetRandomWizardPrefab(); // %25 Büyücü
                else if (roll < 0.65f) prefabToSpawn = strongEnemyPrefab;  // %40 Güçlü
                                                                           // Kalan %35 Normal
            }
            // Aşama 2 (Oyunun ortaları - en az 2 kapı kapalı)
            else if (closedGates >= 2)
            {
                if (roll < 0.10f) prefabToSpawn = GetRandomWizardPrefab(); // Sadece %10 Büyücü (tadımlık)
                else if (roll < 0.35f) prefabToSpawn = strongEnemyPrefab;  // %25 Güçlü
                                                                           // Kalan %65 Normal
            }
            // Aşama 1 (Oyunun başı - 0 veya 1 kapı kapalı)
            else
            {
                // Her zaman %100 normal düşman
                prefabToSpawn = normalEnemyPrefab;
            }

            SpawnAtGate(prefabToSpawn, randomGate.spawnPoint.position);
            yield return new WaitForSeconds(0.6f); // Doğma hızını 0.8'den 0.6'ya çektim ki başlardaki tempo artsın
        }

        firstWaveCount += enemyToAddByWave;
        isSpawning = false;
    }

    GameObject GetRandomWizardPrefab()
    {
        float wizardRoll = Random.value;

        if (wizardRoll < 0.4f) return fireWizardPrefab;
        if (wizardRoll < 0.8f) return iceWizardPrefab;
        return supportWizardPrefab;
    }

    public void HandleGateClosed()
    {
        bool allClosed = true;
        foreach (var gate in gates)
        {
            if (gate.isActive) 
            {
                allClosed = false;
                break;
            }
        }

        if (allClosed)
        {
            StartCoroutine(gameManager.WinGame());
        }
        else
        {
            // Tüm kapılar henüz kapanmadıysa, atmosferi ve zorluğu ilerlet (Geceye yaklaş)
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.AdvanceTimePhase();
            }

            // Oyuncuya Level Up (Upgrade) seçeneği sun.
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.TriggerUpgradeSelection();
            }
        }
    }

    public void CheckForVictory()
    {
        foreach (var gate in gates)
        {
            if (gate.isActive) return;
        }
        StartCoroutine(gameManager.WinGame());
    }

    void SpawnAtGate(GameObject enemyPrefab, Vector3 spawnPosition)
    {
        if (enemyPrefab == null) return;
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemyCount++;
    }
}