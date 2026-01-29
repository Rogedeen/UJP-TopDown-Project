using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class WaveManager : MonoBehaviour
{
    public int firstWave = 2;
    public int enemyToAddByWave = 1;
    public GameObject enemyPrefab;
    public Gates[] gates;
    public static int activeEnemyCount = 0;

    private PowerUpManager powerUpManager;
    private bool isSpawning = false; 

    void Start()
    {
        activeEnemyCount = 0;
        powerUpManager = GameObject.Find("Power Up Manager").GetComponent<PowerUpManager>();
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
        //Kilidi kapat
        isSpawning = true; 

        powerUpManager.SpawnPowerUp();

        // Wave başlarken 2-3 saniye hazırlanma süresi verebiliriz
        // sonra açarsın yield return new WaitForSeconds(2.0f);

        for (int i = 0; i < firstWave; i++)
        {
            SpawnSingleEnemy();

            //Her spawn sonrası 1.5 saniye bekle
            yield return new WaitForSeconds(1.0f);
        }

        firstWave += enemyToAddByWave;
        isSpawning = false; //İşlem bitti, kilidi aç
    }

    void SpawnSingleEnemy()
    {
        int randomIndex = Random.Range(0, gates.Length);

        if (gates[randomIndex].isActive)
        {
            Vector3 gatePosition = gates[randomIndex].gameObject.transform.position;
            Instantiate(enemyPrefab, gatePosition, enemyPrefab.transform.rotation);
            activeEnemyCount++;
        }
    }
}