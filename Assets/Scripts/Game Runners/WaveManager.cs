using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Configs (Her kapı arası için bir tane)")]
    [Tooltip("Sırasıyla her wave'in ayarlarını buraya ekle")]
    public WaveConfig[] waveConfigs;

    [Header("References")]
    [SerializeField] private PowerUpManager powerUpManager;
    [SerializeField] private GameManager gameManager;

    [Header("Boss Configuration")]
    [Tooltip("Oyunun son aşamasında (Örn: 3 kapı kapandığında) patronu tam 1 kez çağırır.")]
    public int bossSpawnGateCount = 3;
    public GameObject bossPrefab;
    private bool hasSpawnedBoss = false;

    public Gates[] gates;

    // ─── DURUM DEĞİŞKENLERİ ───
    private int activeEnemyCount = 0;
    private float spawnTimer = 0f;

    // ─── HUD İÇİN PUBLIC GETTER'LAR ───
    public int ActiveEnemyCount => activeEnemyCount;

    private WaveConfig CurrentConfig
    {
        get
        {
            int index = ClosedGateCount;
            if (waveConfigs == null || waveConfigs.Length == 0) return null;
            if (index >= waveConfigs.Length) index = waveConfigs.Length - 1; // Tüm kapılar kapanıyorsa en son zora kilitlen
            return waveConfigs[index];
        }
    }

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
        spawnTimer = 0f;
        hasSpawnedBoss = false;
    }

    void Update()
    {
        if (!GameManager.isGameActive || CurrentConfig == null) return;

        spawnTimer += Time.deltaTime;

        // ─── SÜREKLİ DOĞUŞ (CONTINUOUS SPAWN) SİSTEMİ ───
        
        // --- ÖZEL: BOSS DOGUŞU ---
        if (!hasSpawnedBoss && ClosedGateCount >= bossSpawnGateCount && bossPrefab != null)
        {
            hasSpawnedBoss = true;
            SpawnBoss();
        }

        // 1) Düşman sayısı minimumun altındaysa → HIZLI DOLUM (her frame kontrol)
        if (activeEnemyCount < CurrentConfig.minAliveEnemies)
        {
            // minAlive'a ulaşana kadar her frame'de bir düşman doğur
            SpawnRandomEnemy();
            spawnTimer = 0f; // Normal spawn zamanlayıcısını sıfırla
        }
        // 2) Normal spawn aralığı geldiğinde ve ekrandaki düşman sayısı max limitinden az olduğunda (Wave Cap)
        else if (spawnTimer >= CurrentConfig.spawnInterval && activeEnemyCount < CurrentConfig.maxAliveEnemies)
        {
            SpawnRandomEnemy();
            spawnTimer = 0f;
        }
    }

    // ─── SPAWN MEKANİĞİ ───

    private void SpawnRandomEnemy()
    {
        WaveConfig config = CurrentConfig;
        if (config == null || config.spawnTable == null || config.spawnTable.Length == 0) return;

        // Aktif kapılardan birini seç
        List<Gates> activeGates = new List<Gates>();
        foreach (var gate in gates)
        {
            if (gate.isActive) activeGates.Add(gate);
        }

        if (activeGates.Count == 0) return;

        Gates randomGate = activeGates[Random.Range(0, activeGates.Count)];
        GameObject prefab = GetWeightedRandomPrefab(config.spawnTable);

        if (prefab != null)
        {
            SpawnAtGate(prefab, randomGate.spawnPoint.position);
        }
    }

    private void SpawnBoss()
    {
        List<Gates> activeGates = new List<Gates>();
        foreach (var gate in gates)
        {
            if (gate.isActive) activeGates.Add(gate);
        }

        if (activeGates.Count == 0) return;

        Gates randomGate = activeGates[Random.Range(0, activeGates.Count)];
        SpawnAtGate(bossPrefab, randomGate.spawnPoint.position);
        Debug.Log("[WaveManager] BOSS YARATILDI!");
    }

    /// <summary>
    /// Ağırlıklı rastgele seçim. spawnWeight'i yüksek olan düşman daha sık çıkar.
    /// </summary>
    private GameObject GetWeightedRandomPrefab(SpawnEntry[] table)
    {
        int totalWeight = 0;
        foreach (var entry in table)
        {
            totalWeight += entry.spawnWeight;
        }

        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in table)
        {
            cumulative += entry.spawnWeight;
            if (roll < cumulative)
            {
                return entry.enemyPrefab;
            }
        }

        return table[0].enemyPrefab; // Fallback
    }

    // ─── KAPI SİSTEMİ (AYNEN KALIYOR) ───

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
            // Atmosferi ilerlet (Gece/Gündüz)
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.AdvanceTimePhase();
            }

            // Upgrade seçimi sun
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

    // ─── YARDIMCI METODLAR ───

    private void HandleEnemyDied()
    {
        activeEnemyCount--;
    }

    private void SpawnAtGate(GameObject enemyPrefab, Vector3 spawnPosition)
    {
        if (enemyPrefab == null) return;
        
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(enemyPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
        activeEnemyCount++;
    }
}