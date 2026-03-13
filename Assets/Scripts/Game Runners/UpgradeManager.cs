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
