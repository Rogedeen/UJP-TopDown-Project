using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int firstWaveCount = 2;
    public int enemyToAddByWave = 1;
    public GameObject normalEnemyPrefab;
    public GameObject strongEnemyPrefab;
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

        // Eğer hiç düşman kalmadıysa ve yeni dalga doğurulmuyorsa başlat
        if (activeEnemyCount == 0 && !isSpawning)
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        powerUpManager.SpawnPowerUp();

        // 1. Aktif kapıları filtrele
        List<Gates> activeGates = new();
        foreach (var gate in gates)
        {
            if (gate.isActive) activeGates.Add(gate);
        }

        // Oyun Kazanma Kontrolü
        if (activeGates.Count == 0)
        {
            Debug.Log("Tebrikler çırak, tüm kapıları kapattın!");
            gameManager.WinGame(); // Kazandın ekranı için
            yield break;
        }

        // 2. Zorluk Hesabı (Kapılar kapandıkça artan 0-1 arası değer)
        float difficultyScore = (float)(gates.Length - activeGates.Count) / gates.Length;

        for (int i = 0; i < firstWaveCount; i++)
        {
            // Rastgele bir AKTİF kapı seç
            Gates randomGate = activeGates[Random.Range(0, activeGates.Count)];

            // Hangi düşmanı doğuralım? (Zorluğa göre karar ver)
            GameObject prefabToSpawn = (Random.value < difficultyScore) ? strongEnemyPrefab : normalEnemyPrefab;

            // Güncel spawn fonksiyonumuzu çağırıyoruz
            SpawnAtGate(prefabToSpawn, randomGate.transform.position);

            yield return new WaitForSeconds(1.0f);
        }

        firstWaveCount += enemyToAddByWave;
        isSpawning = false;
    }

    // Eski SpawnSingleEnemy yerine bu daha esnek fonksiyonu koyduk
    void SpawnAtGate(GameObject enemyPrefab, Vector3 spawnPosition)
    {
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemyCount++;
    }
}