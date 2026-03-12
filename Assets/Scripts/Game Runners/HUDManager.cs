using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyun içi HUD yöneticisi.
/// Kalpler, wave sayacı ve kapı durumunu gösterir.
/// 
/// KURULUM:
/// 1. Ingame Screen objesine bu scripti ekle
/// 2. Inspector'dan referansları ata:
///    - playerHealth → Player'daki PlayerHealth component
///    - waveManager → WaveManager objesi
///    - heartImages → HealthDisplay altındaki kalp Image'ları (sırasıyla)
///    - fullHeartSprite / emptyHeartSprite → kalp sprite asset'leri
///    - waveText → WaveDisplay altındaki TextMeshPro
///    - gateText → GateDisplay altındaki TextMeshPro
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WaveManager waveManager;

    [Header("Health Display")]
    [SerializeField] private RawImage[] heartImages;
    [SerializeField] private Texture fullHeartTexture;
    [SerializeField] private Texture emptyHeartTexture;

    [Header("Wave Display")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Gate Display")]
    [SerializeField] private TextMeshProUGUI gateText;

    [Header("Energy Display")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Slider energySlider;

    // Cache — gereksiz UI güncellemelerini önlemek için
    private int lastHealth = -1;
    private int lastWave = -1;
    private int lastClosedGates = -1;

    void Update()
    {
        if (!GameManager.isGameActive) return;

        UpdateHealthDisplay();
        UpdateWaveDisplay();
        UpdateGateDisplay();
        UpdateEnergyDisplay();
    }

    void UpdateHealthDisplay()
    {
        if (playerHealth == null || heartImages.Length == 0) return;

        int currentHP = playerHealth.playerHealth;

        // Aynıysa güncelleme yapma (performans)
        if (currentHP == lastHealth) return;
        lastHealth = currentHP;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            // i < currentHP → dolu kalp, değilse → boş kalp
            heartImages[i].texture = (i < currentHP) ? fullHeartTexture : emptyHeartTexture;
        }
    }

    void UpdateWaveDisplay()
    {
        if (waveManager == null || waveText == null) return;

        int wave = waveManager.CurrentWave;

        if (wave == lastWave) return;
        lastWave = wave;

        waveText.text = $"Wave {wave}";
    }

    void UpdateGateDisplay()
    {
        if (waveManager == null || gateText == null) return;

        int closed = waveManager.ClosedGateCount;

        if (closed == lastClosedGates) return;
        lastClosedGates = closed;

        int total = waveManager.TotalGateCount;
        gateText.text = $"Kapılar: {closed}/{total}";
    }

    void UpdateEnergyDisplay()
    {
        if (playerController == null || energySlider == null) return;

        // Slider değerini PlayerController'daki CurrentEnergy ile eşle
        // Değer değişmemişse atlamaya gerek yok çünkü Slider.value ataması Unity içinde zaten optimize çalışır,
        // ancak yine de performans için değer farkı var mı diye bakabiliriz
        if (Mathf.Abs(energySlider.value - playerController.CurrentEnergy) > 0.1f)
        {
            energySlider.value = playerController.CurrentEnergy;
        }
    }
}
