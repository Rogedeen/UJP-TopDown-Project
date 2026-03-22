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

    public Gates[] gates;

    // ─── DURUM DEĞİŞKENLERİ ───
    private int activeEnemyCount = 0;
    private int currentWaveIndex = -1; // -1 = henüz başlamadı
    private float waveTimer = 0f;
    private float spawnTimer = 0f;
    private bool waveActive = false;

    // ─── HUD İÇİN PUBLIC GETTER'LAR ───
    public int CurrentWave => currentWaveIndex + 1; // 1-indexed
    public int ActiveEnemyCount => activeEnemyCount;
    public float WaveTimeRemaining => waveActive ? Mathf.Max(0, CurrentConfig.waveDuration - waveTimer) : 0f;
    public float WaveDuration => CurrentConfig != null ? CurrentConfig.waveDuration : 0f;
    public bool IsWaveActive => waveActive;

    private WaveConfig CurrentConfig =>
        (currentWaveIndex >= 0 && currentWaveIndex < waveConfigs.Length)
            ? waveConfigs[currentWaveIndex]
            : null;

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
        StartNextWave();
    }

    void Update()
    {
        if (!GameManager.isGameActive || !waveActive || CurrentConfig == null) return;

        // ─── WAVE ZAMANLAYICISI ───
        waveTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        // ─── QUOTA SİSTEMİ ───
        // 1) Düşman sayısı minimumun altındaysa → HIZLI DOLUM (her frame kontrol)
        if (activeEnemyCount < CurrentConfig.minAliveEnemies)
        {
            // minAlive'a ulaşana kadar her frame'de bir düşman doğur
            SpawnRandomEnemy();
            spawnTimer = 0f; // Normal spawn zamanlayıcısını sıfırla
        }
        // 2) Normal spawn aralığı geldiyse ve max'ın altındaysak → doğur
        else if (spawnTimer >= CurrentConfig.spawnInterval && activeEnemyCount < CurrentConfig.maxAliveEnemies)
        {
            SpawnRandomEnemy();
            spawnTimer = 0f;
        }

        // ─── WAVE SÜRESİ DOLDU MU? ───
        if (waveTimer >= CurrentConfig.waveDuration)
        {
            EndCurrentWave();
        }
    }

    // ─── WAVE YÖNETİMİ ───

    private void StartNextWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waveConfigs.Length)
        {
            // Tüm wave'ler bitti, son wave'i tekrar et (sonsuz mod)
            currentWaveIndex = waveConfigs.Length - 1;
        }

        waveTimer = 0f;
        spawnTimer = 0f;
        waveActive = true;

        // Her wave başında powerup
        if (currentWaveIndex > 0) // İlk wave'de değilse
        {
            powerUpManager.SpawnPowerUp();
        }

        Debug.Log($"[WaveManager] {CurrentConfig.waveName} başladı! Süre: {CurrentConfig.waveDuration}s");
    }

    private void EndCurrentWave()
    {
        waveActive = false;
        Debug.Log($"[WaveManager] {CurrentConfig.waveName} süresi doldu. Yeni wave bekleniyor...");

        // Süre dolunca bir sonraki wave'i başlat (kapı kapatmayı beklemiyor)
        StartNextWave();
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