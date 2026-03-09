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

    public static int activeEnemyCount = 0;
    public Gates[] gates;

    private PowerUpManager powerUpManager;
    private GameManager gameManager;
    private bool isSpawning = false;

    void Start()
    {
        activeEnemyCount = 0;
        powerUpManager = GameObject.Find("Power Up Manager").GetComponent<PowerUpManager>();
        gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
    }

    void Update()
    {
        if (!GameManager.isGameActive) return;

        if (activeEnemyCount == 0 && !isSpawning)
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
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

            // --- YENİ SPAWN MANTIĞI ---
            // Difficulty arttıkça wizard gelme şansı artıyor (max %50 wizard şansı gibi)
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

    // Wizard seçimini kendi içinde rastgele yapıyoruz
    GameObject GetRandomWizardPrefab()
    {
        float wizardRoll = Random.value;

        // %40 Fire, %40 Ice, %20 Support (Support biraz daha nadir olsun ki kıymetli olsun)
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