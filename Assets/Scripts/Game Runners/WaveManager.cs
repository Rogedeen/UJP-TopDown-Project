using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public int firstWaveCount = 2;
    public int enemyToAddByWave = 1;

    [Header("Enemy Prefabs")]
    public GameObject normalEnemyPrefab;
    public GameObject strongEnemyPrefab;
    public GameObject wizardEnemyPrefab; 

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

            if (roll < difficultyScore * 0.5f)
            {
                prefabToSpawn = wizardEnemyPrefab; 
            }
            else if (roll < difficultyScore)
            {
                prefabToSpawn = strongEnemyPrefab; 
            }
            else
            {
                prefabToSpawn = normalEnemyPrefab; 
            }

            SpawnAtGate(prefabToSpawn, randomGate.transform.position);

            yield return new WaitForSeconds(1.0f);
        }

        firstWaveCount += enemyToAddByWave;
        isSpawning = false;
    }
    public void CheckForVictory()
    {
        foreach (var gate in gates)
        {
            if (gate.isActive) return;
        }

        Debug.Log("Tebrikler çırak, tüm kapıları kapattın!");
        gameManager.WinGame();
    }
    void SpawnAtGate(GameObject enemyPrefab, Vector3 spawnPosition)
    {
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemyCount++;
    }
}