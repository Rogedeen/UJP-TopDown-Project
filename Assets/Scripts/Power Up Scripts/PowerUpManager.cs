using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    [Header("Loot Drop Sistemi")]
    [Tooltip("Düşman öldüğünde power-up düşme ihtimali (0-1). 0.25 = %25")]
    [Range(0f, 1f)]
    public float baseDropChance = 0.25f;

    [Tooltip("Düşman ağırlığına göre drop tablosu")]
    public LootTable[] lootTables;

    [Header("Eski Sistem (Opsiyonel)")]
    [Tooltip("Wave başında rastgele spawn hâlâ kullanılsın mı?")]
    public bool useWaveSpawn = false;
    public int firstWavePowerUpCount;
    public int powerUpToAddByWave;
    public float spawnRange;
    public List<GameObject> powerUpTypes;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Düşman öldüğünde EnemyBase tarafından çağrılır.
    /// enemyWeight: düşmanın zorluk ağırlığı (normal=1, strong=2, wizard=3)
    /// </summary>
    public void TryDropLoot(Vector3 position, int enemyWeight)
    {
        // Ağırlık arttıkça drop şansı artar (weight=1 → %25, weight=3 → %75)
        float adjustedChance = Mathf.Clamp01(baseDropChance * enemyWeight);
        
        if (Random.value > adjustedChance) return; // Şans tutmadı

        // Ağırlığa uygun loot tablosunu bul
        int tableIndex = GetTableIndexForWeight(enemyWeight);
        if (tableIndex < 0 || lootTables[tableIndex].entries == null || lootTables[tableIndex].entries.Length == 0) return;

        // Ağırlıklı rastgele seçim
        GameObject prefab = GetWeightedDrop(lootTables[tableIndex].entries);
        if (prefab != null)
        {
            Vector3 spawnPos = position;
            spawnPos.y = 1f; // Yere gömülmesin
            
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Spawn(prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }
    }

    private int GetTableIndexForWeight(int enemyWeight)
    {
        if (lootTables == null || lootTables.Length == 0) return -1;

        int bestIndex = -1;
        foreach (var table in lootTables)
        {
            if (enemyWeight >= table.minEnemyWeight)
            {
                if (bestIndex < 0 || table.minEnemyWeight > lootTables[bestIndex].minEnemyWeight)
                    bestIndex = System.Array.IndexOf(lootTables, table);
            }
        }
        // Hiç eşleşme yoksa ilk tabloyu kullan
        return bestIndex >= 0 ? bestIndex : 0;
    }

    private GameObject GetWeightedDrop(LootEntry[] entries)
    {
        int totalWeight = 0;
        foreach (var e in entries)
            totalWeight += e.dropWeight;

        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var e in entries)
        {
            cumulative += e.dropWeight;
            if (roll < cumulative) return e.powerUpPrefab;
        }
        return entries[0].powerUpPrefab;
    }

    /// <summary>
    /// Eski sistem: Wave başında rastgele spawn (opsiyonel, useWaveSpawn açıksa çalışır)
    /// </summary>
    public void SpawnPowerUp()
    {
        if (!useWaveSpawn || powerUpTypes == null || powerUpTypes.Count == 0) return;

        for (int i = 0; i < firstWavePowerUpCount; i++)
        {
            int randIndex = Random.Range(0, powerUpTypes.Count);
            Vector3 randPos = new Vector3(
                Random.Range(-spawnRange, spawnRange),
                1f,
                Random.Range(-spawnRange, spawnRange)
            );
            
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Spawn(powerUpTypes[randIndex], randPos, powerUpTypes[randIndex].transform.rotation);
            }
            else
            {
                Instantiate(powerUpTypes[randIndex], randPos, powerUpTypes[randIndex].transform.rotation);
            }
        }
        firstWavePowerUpCount += powerUpToAddByWave;
    }
}

[System.Serializable]
public struct LootTable
{
    [Tooltip("Bu tablo en az bu ağırlıktaki düşmanlar için geçerli (1=Normal, 2=Strong, 3=Wizard)")]
    public int minEnemyWeight;
    [Tooltip("Bu tablodaki drop seçenekleri")]
    public LootEntry[] entries;
}

[System.Serializable]
public struct LootEntry
{
    public GameObject powerUpPrefab;
    [Tooltip("Ağırlık. Yüksek = daha sık düşer. Örn: Health=50, DamageBoost=10")]
    public int dropWeight;
}
