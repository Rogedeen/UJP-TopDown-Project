using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyundaki tüm olası Upgrade (güçlendirme) kartlarını tutan ve 
/// kapı kapandığında aralarından rastgele 3 tanesini seçen sistem.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Upgrade Database")]
    // Inspector'dan sürüklenecek tüm ScriptableObject kartları
    public List<BaseUpgrade> allAvailableUpgrades = new List<BaseUpgrade>();

    [Header("References")]
    public UpgradeUI upgradeUI; // Seçilen kartları çizecek UI kodu
    private PlayerController playerController;

    [Header("XP & Leveling System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 10;
    [Tooltip("Her seviyede gereken XP miktarının artış çarpanı")]
    public float xpScalingFactor = 1.5f;

    private void Awake()
    {
        // Basit Singleton yapısı
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Oyuncuyu dinamik olarak bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyDiedWithXP += HandleEnemyDiedXP;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyDiedWithXP -= HandleEnemyDiedXP;
    }

    private void HandleEnemyDiedXP(int xpAmount)
    {
        if (!GameManager.isGameActive) return;

        currentXP += xpAmount;
        Debug.Log($"[UpgradeManager] Düşman Öldü! Kazanılan XP: {xpAmount}. Mevcut XP: {currentXP}/{xpToNextLevel}");

        if (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpScalingFactor);
            
            Debug.Log($"[UpgradeManager] SEVİYE ATLANDI! Yeni Level: {currentLevel}");
            TriggerUpgradeSelection();
        }
    }

    /// <summary>
    /// Kapı kapatıldığında WaveManager veya Gates objesi tarafından çağrılır.
    /// Rastgele 3 eşsiz yetenek seçip UI'a gönderir.
    /// </summary>
    public void TriggerUpgradeSelection()
    {
        if (allAvailableUpgrades.Count == 0)
        {
            Debug.LogWarning("UpgradeManager: Havuzda (List) hiç upgrade kartı yok!");
            return;
        }

        // Oyun zamanını durdur
        Time.timeScale = 0f;

        List<BaseUpgrade> selectedUpgrades = GetRandomUpgrades(3);
        
        if (upgradeUI != null)
        {
            upgradeUI.ShowUpgradeScreen(selectedUpgrades);
        }
        else
        {
            Debug.LogError("UpgradeManager: UpgradeUI referansı boş!");
        }
    }

    /// <summary>
    /// Verilen sayı kadar rastgele ve birbirinden farklı kart çeker.
    /// </summary>
    private List<BaseUpgrade> GetRandomUpgrades(int count)
    {
        List<BaseUpgrade> selected = new List<BaseUpgrade>();
        List<BaseUpgrade> pool = new List<BaseUpgrade>(allAvailableUpgrades);

        // Eğer havuzdaki sayımız istenenden azsa, hepsini ver
        int fetchCount = Mathf.Min(count, pool.Count);

        for (int i = 0; i < fetchCount; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            selected.Add(pool[randomIndex]);
            // Aynı kartın birden fazla kez çıkmasını engellemek için pool'dan silinir
            pool.RemoveAt(randomIndex); 
        }

        return selected;
    }

    /// <summary>
    /// Oyuncu UI'dan bir kartı seçtiğinde (Click) bu tetiklenecektir.
    /// </summary>
    /// <param name="selectedUpgrade">Oyuncunun seçtiği ScriptableObject</param>
    public void OnUpgradeSelected(BaseUpgrade selectedUpgrade)
    {
        // 1. Etkiyi (Upgrade) oyuncuya uygula
        if (playerController != null && selectedUpgrade != null)
        {
            selectedUpgrade.ApplyUpgrade(playerController);

            // GÖRSEL VE İŞİTSEL GERİ BİLDİRİM (JUICE)
            Animator anim = playerController.GetComponent<Animator>();
            if (anim != null)
            {
                // Animator parameter kontrolünü güvenli yapıyoruz (yoksa hata atar)
                foreach (var param in anim.parameters)
                {
                    if (param.name == "PowerUpReceived")
                    {
                        anim.SetTrigger("PowerUpReceived");
                        break;
                    }
                }
            }

            if (selectedUpgrade.vfxPrefab != null)
            {
                // Efekti oyuncunun içine at, onu takip etsin
                GameObject vfx = Instantiate(selectedUpgrade.vfxPrefab, playerController.transform.position, Quaternion.identity, playerController.transform);
                Destroy(vfx, 4f); // 4 saniye sonra temizle
            }

            if (selectedUpgrade.pickupSound != null)
            {
                AudioSource audioSource = playerController.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(selectedUpgrade.pickupSound);
                }
            }

            // Seçim sonrası dokunulmazlık (yanıp sönme) başlat
            PlayerHealth ph = playerController.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.StartCoroutine(ph.PostUpgradeInvincibilityRoutine());
            }
        }

        // 2. Oyunu kaldığı yerden devam ettir
        Time.timeScale = 1f;

        // 3. UI modülünden yetenek kartlarını kapat
        if (upgradeUI != null)
        {
            upgradeUI.HideUpgradeScreen();
        }

        // İleride burada Juice/Polishing için oyuncunun üstünde anlık "Level Up" patlama efekti (VFX) oynatılabilir.
    }
}
