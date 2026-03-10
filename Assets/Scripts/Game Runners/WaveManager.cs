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

        float difficultyScore = (float)(gates.Length - activeGates.Count) / gates.Length;

        for (int i = 0; i < firstWaveCount; i++)
        {
            Gates randomGate = activeGates[Random.Range(0, activeGates.Count)];
            GameObject prefabToSpawn;
            float roll = Random.value;

            if (roll < difficultyScore * 0.5f)
            {
                prefabToSpawn = GetRandomWizardPrefab();
            }
            else if (roll < difficultyScore)
            {
                prefabToSpawn = strongEnemyPrefab;
            }
            else
            {
                prefabToSpawn = normalEnemyPrefab;
            }

            SpawnAtGate(prefabToSpawn, randomGate.spawnPoint.position);
            yield return new WaitForSeconds(0.8f);
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