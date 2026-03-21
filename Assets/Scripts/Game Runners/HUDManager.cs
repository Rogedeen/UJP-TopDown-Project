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

    [Header("Skill Display")]
    [Tooltip("Orbit weapon cooldown bar (Slider olarak)")]
    [SerializeField] private Slider orbitCooldownSlider;
    [Tooltip("Orbit weapon cooldown bar (Image Fill Amount olarak) - İkisinden birini atayın")]
    [SerializeField] private Image orbitCooldownImage;
    [SerializeField] private float skillPulseSpeed = 5f;
    [SerializeField] private float skillPulseAmount = 0.15f;

    // Cache — gereksiz UI güncellemelerini önlemek için
    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private int lastClosedGates = -1;
    private float lastMaxEnergy = -1f;

    private RectTransform energyRectTransform;
    private float initialEnergyWidth = -1f;
    private float initialMaxEnergy = -1f;

    private Vector3 originalSkillScale = Vector3.one;

    void Start()
    {
        if (energySlider != null)
        {
            energyRectTransform = energySlider.GetComponent<RectTransform>();
            initialEnergyWidth = energyRectTransform.sizeDelta.x;
            
            if (playerController != null)
                initialMaxEnergy = playerController.maxEnergy;
        }

        if (orbitCooldownSlider != null) originalSkillScale = orbitCooldownSlider.transform.localScale;
        else if (orbitCooldownImage != null) originalSkillScale = orbitCooldownImage.transform.localScale;
    }

    void Update()
    {
        if (!GameManager.isGameActive) return;

        UpdateHealthDisplay();
        UpdateWaveDisplay();
        UpdateGateDisplay();
        UpdateEnergyDisplay();
        UpdateSkillDisplay();
    }

    void UpdateHealthDisplay()
    {
        if (playerHealth == null || heartImages.Length == 0) return;

        int currentHP = playerHealth.playerHealth;
        int maxHP = playerHealth.maxPlayerHealth;

        // Aynıysa güncelleme yapma (performans)
        if (currentHP == lastHealth && maxHP == lastMaxHealth) return;
        
        lastHealth = currentHP;
        lastMaxHealth = maxHP;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            // Upgrade mekaniğine göre: Sadece maxHealth kadar olan kalpler aktif (görünür) olsun
            if (i < maxHP)
            {
                heartImages[i].gameObject.SetActive(true);
                heartImages[i].texture = (i < currentHP) ? fullHeartTexture : emptyHeartTexture;
            }
            else
            {
                // Max health'i geçen kalpler UI'da gizlenir. Upgrade alınınca açılır.
                heartImages[i].gameObject.SetActive(false);
            }
        }
    }

    void UpdateWaveDisplay()
    {
        if (waveManager == null || waveText == null) return;

        int wave = waveManager.CurrentWave;
        float timeLeft = waveManager.WaveTimeRemaining;
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);

        waveText.text = $"Wave {wave} — {minutes}:{seconds:00}";
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

        float currentMax = playerController.maxEnergy;
        float currentEnergy = playerController.CurrentEnergy;

        // Max enerji değiştiyse slider'a yansıt ve görseli uzat
        if (Mathf.Abs(currentMax - lastMaxEnergy) > 0.1f)
        {
            lastMaxEnergy = currentMax;
            energySlider.maxValue = currentMax;

            if (energyRectTransform != null && initialEnergyWidth > 0 && initialMaxEnergy > 0)
            {
                // Mevcut Max enerjinin, ilk andaki max enerjiye oranını hesapla (örn 120 / 100 = 1.2x)
                float ratio = currentMax / initialMaxEnergy;
                float newWidth = initialEnergyWidth * ratio;

                // Slider'ın RectTransform Width (Genişlik) değerini güncelle
                energyRectTransform.sizeDelta = new Vector2(newWidth, energyRectTransform.sizeDelta.y);
            }
        }

        // Değer değişmemişse atlamaya gerek yok çünkü Slider.value ataması Unity içinde zaten optimize çalışır,
        // ancak yine de performans için değer farkı var mı diye bakabiliriz
        if (Mathf.Abs(energySlider.value - currentEnergy) > 0.1f)
        {
            energySlider.value = currentEnergy;
        }
    }

    void UpdateSkillDisplay()
    {
        if (playerController == null) return;
        
        OrbitWeapon skill = playerController.ActiveOrbitWeapon;
        // Eğer OrbitWeapon henüz bulunamadıysa işlem yapma
        if (skill == null) return;

        float ratio = skill.currentCooldownRatio;
        bool isReady = skill.canUseSkill;

        Transform activeUI = null;

        if (orbitCooldownSlider != null)
        {
            orbitCooldownSlider.minValue = 0f;
            orbitCooldownSlider.maxValue = 1f;
            orbitCooldownSlider.value = ratio;
            activeUI = orbitCooldownSlider.transform;
        }
        else if (orbitCooldownImage != null)
        {
            if (orbitCooldownImage.type != Image.Type.Filled)
            {
                orbitCooldownImage.type = Image.Type.Filled;
                orbitCooldownImage.fillMethod = Image.FillMethod.Horizontal;
            }
            orbitCooldownImage.fillAmount = ratio;
            activeUI = orbitCooldownImage.transform;
        }

        // Pulse / Nefes animasyonu (Kullanıma hazır olduğunda)
        if (activeUI != null)
        {
            if (isReady && ratio >= 1f)
            {
                float pulse = 1f + Mathf.Sin(Time.time * skillPulseSpeed) * skillPulseAmount;
                activeUI.localScale = originalSkillScale * pulse;
            }
            else
            {
                activeUI.localScale = originalSkillScale;
            }
        }
    }
}
