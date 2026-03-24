using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyun içi HUD yöneticisi — Souls-Style bar sistemi.
/// 
/// Her bar 3 katmandan oluşur:
///   Frame   (Image)         → Magic for UI çerçeve sprite'ı (sadece görsel)
///   Fill    (Image.Filled)  → fillAmount ile dolup boşalan bar
///   Text    (TMP)           → "3/5" sayısal gösterim
///
/// KURULUM:
/// 1. Ingame Screen objesine bu scripti ekle
/// 2. Her bar için Hierarchy yapısı:
///      BarRoot (Empty — RectTransform)
///      ├── Frame  (Image, Source = HUD_default_dark sprite)
///      └── Fill   (Image, Type=Filled, Fill Method=Horizontal, Fill Origin=Left)
///          └── ValueText (TextMeshProUGUI, Alignment=Center)
///    Fill'in anchor'ı stretch–stretch olmalı, Left/Right/Top/Bottom'a
///    çerçeve kalınlığına göre ~8-12 px padding verin.
/// 3. Inspector'dan referansları atayın.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private WaveManager waveManager;

    // ─────────────────── HEALTH BAR ───────────────────
    [Header("Health Bar — Souls Style")]
    [Tooltip("Barın tamamını kapsayan root RectTransform. Max health değiştiğinde genişlik buradan ayarlanır.")]
    [SerializeField] private RectTransform healthBarRoot;
    [Tooltip("Fill Image (Image Type = Filled, Fill Method = Horizontal). Çerçevenin child'ı olmalı.")]
    [SerializeField] private Image healthFillImage;
    [Tooltip("Sayısal gösterim TMP — örn. 3/5")]
    [SerializeField] private TextMeshProUGUI healthValueText;

    [Header("Health Color Gradient")]
    [Tooltip("true ise renk otomatik gradient ile belirlenir; false ise sabit kalır.")]
    [SerializeField] private bool useHealthGradient = true;

    // ─────────────────── ENERGY BAR ───────────────────
    [Header("Energy (Charge) Bar — Souls Style")]
    [SerializeField] private RectTransform energyBarRoot;
    [SerializeField] private Image energyFillImage;
    [SerializeField] private TextMeshProUGUI energyValueText;

    [Header("Energy Colors")]
    [SerializeField] private Color energyFullColor  = new Color(0.20f, 0.60f, 1.00f, 1f);  // Parlak mavi
    [SerializeField] private Color energyLowColor   = new Color(0.10f, 0.25f, 0.55f, 1f);  // Koyu mavi

    // ─────────────────── SKILL BAR ────────────────────
    [Header("Skill (Cooldown) Bar — Souls Style")]
    [SerializeField] private RectTransform skillBarRoot;
    [SerializeField] private Image skillFillImage;
    [SerializeField] private TextMeshProUGUI skillValueText;
    [SerializeField] private float skillPulseSpeed = 5f;
    [SerializeField] private float skillPulseAmount = 0.15f;

    [Header("Skill Colors")]
    [SerializeField] private Color skillReadyColor    = new Color(0.65f, 0.20f, 0.85f, 1f); // Parlak mor
    [SerializeField] private Color skillCooldownColor = new Color(0.30f, 0.10f, 0.45f, 1f); // Koyu mor

    // ─────────────────── XP BAR ───────────────────────
    [Header("XP & Level Display")]
    [SerializeField] private Image xpFillImage;
    [SerializeField] private TextMeshProUGUI xpValueText; // Örn: 7/10
    [SerializeField] private TextMeshProUGUI levelText;   // Örn: Lv. 2

    // ─────────────────── WAVE & GATE ──────────────────
    [Header("Wave Display")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Gate Display")]
    [SerializeField] private TextMeshProUGUI gateText;

    // ─────────────────── ESKI KALP SİSTEMİ (Opsiyonel, geriye uyumluluk) ──────
    [Header("Legacy Heart Display (İsteğe Bağlı)")]
    [SerializeField] private RawImage[] heartImages;
    [SerializeField] private Texture fullHeartTexture;
    [SerializeField] private Texture emptyHeartTexture;

    // ─────────────────── İÇ DEĞİŞKENLER ──────────────
    // Cache — gereksiz UI güncellemelerini önlemek için
    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private int lastClosedGates = -1;
    private float lastEnergy = -1f;
    private float lastMaxEnergy = -1f;
    private float lastSkillRatio = -1f;
    private float lastXP = -1f;
    private float lastMaxXP = -1f;

    // Başlangıç genişlikleri — max değer değiştiğinde bar uzar/kısalır
    private float initialHealthWidth;
    private int   initialMaxHealth;
    private float initialEnergyWidth;
    private float initialMaxEnergy;

    // Skill pulse
    private Vector3 originalSkillScale = Vector3.one;

    // Health gradient (yeşil → sarı → kırmızı)
    private Gradient healthGradient;

    // ══════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════

    void Start()
    {
        // ── Health Bar başlangıç ayarları ──
        if (healthBarRoot != null && playerHealth != null)
        {
            initialHealthWidth = healthBarRoot.sizeDelta.x;
            initialMaxHealth = playerHealth.maxPlayerHealth;
        }

        // ── Energy Bar başlangıç ayarları ──
        if (energyBarRoot != null && playerController != null)
        {
            initialEnergyWidth = energyBarRoot.sizeDelta.x;
            initialMaxEnergy = playerController.maxEnergy;
        }

        // ── Skill Bar başlangıç ayarları ──
        if (skillBarRoot != null)
            originalSkillScale = skillBarRoot.localScale;
        else if (skillFillImage != null)
            originalSkillScale = skillFillImage.transform.localScale;

        // ── Health Gradient: Kırmızı (%0) → Sarı (%40) → Yeşil (%100) ──
        healthGradient = new Gradient();
        healthGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.80f, 0.13f, 0.13f), 0.0f),  // Kırmızı
                new GradientColorKey(new Color(0.90f, 0.77f, 0.13f), 0.4f),  // Sarı
                new GradientColorKey(new Color(0.20f, 0.80f, 0.33f), 1.0f),  // Yeşil
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );

        // ── İlk frame'de barları doğru değerlerle başlat ("New Text" kalmasını engelle) ──
        ForceInitialUpdate();
    }

    void Update()
    {
        if (!GameManager.isGameActive) return;

        UpdateHealthBar();
        UpdateEnergyBar();
        UpdateSkillBar();
        UpdateWaveDisplay();
        UpdateGateDisplay();
        UpdateXPBar();
    }

    // ══════════════════════════════════════════════════
    //  HEALTH BAR
    // ══════════════════════════════════════════════════

    void UpdateHealthBar()
    {
        if (playerHealth == null || healthFillImage == null) return;

        int currentHP = playerHealth.playerHealth;
        int maxHP = playerHealth.maxPlayerHealth;

        // Cache — değişmediyse güncelleme yapma
        if (currentHP == lastHealth && maxHP == lastMaxHealth) return;

        lastHealth = currentHP;
        lastMaxHealth = maxHP;

        // ── Boyut sabit tutuluyor (ResizeBar kaldırıldı) ──

        // ── Fill amount (0-1 arası) ──
        float ratio = (maxHP > 0) ? (float)currentHP / maxHP : 0f;
        healthFillImage.fillAmount = ratio;

        // ── Renk geçişi ──
        if (useHealthGradient)
            healthFillImage.color = healthGradient.Evaluate(ratio);

        // ── Sayısal gösterim ──
        if (healthValueText != null)
            healthValueText.text = $"{currentHP}/{maxHP}";
    }

    // ══════════════════════════════════════════════════
    //  LEGACY HEART DISPLAY (geriye uyumluluk)
    // ══════════════════════════════════════════════════

    void UpdateLegacyHearts()
    {
        if (playerHealth == null || heartImages == null || heartImages.Length == 0) return;

        int currentHP = playerHealth.playerHealth;
        int maxHP = playerHealth.maxPlayerHealth;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            if (i < maxHP)
            {
                heartImages[i].gameObject.SetActive(true);
                heartImages[i].texture = (i < currentHP) ? fullHeartTexture : emptyHeartTexture;
            }
            else
            {
                heartImages[i].gameObject.SetActive(false);
            }
        }
    }

    // ══════════════════════════════════════════════════
    //  ENERGY BAR
    // ══════════════════════════════════════════════════

    void UpdateEnergyBar()
    {
        if (playerController == null || energyFillImage == null) return;

        float currentEnergy = playerController.CurrentEnergy;
        float currentMax = playerController.maxEnergy;

        // Cache
        bool maxChanged = Mathf.Abs(currentMax - lastMaxEnergy) > 0.1f;
        bool valueChanged = Mathf.Abs(currentEnergy - lastEnergy) > 0.1f;
        if (!maxChanged && !valueChanged) return;

        lastEnergy = currentEnergy;
        lastMaxEnergy = currentMax;

        // ── Boyut sabit tutuluyor (ResizeBar kaldırıldı) ──

        // ── Fill amount ──
        float ratio = (currentMax > 0) ? currentEnergy / currentMax : 0f;
        energyFillImage.fillAmount = ratio;

        // ── Renk (mavi tonları) ──
        energyFillImage.color = Color.Lerp(energyLowColor, energyFullColor, ratio);

        // ── Sayısal gösterim ──
        if (energyValueText != null)
            energyValueText.text = $"{(int)currentEnergy}/{(int)currentMax}";
    }

    // ══════════════════════════════════════════════════
    //  SKILL (COOLDOWN) BAR
    // ══════════════════════════════════════════════════

    void UpdateSkillBar()
    {
        if (playerController == null || skillFillImage == null) return;

        OrbitWeapon skill = playerController.ActiveOrbitWeapon;
        if (skill == null) return;

        float ratio = skill.currentCooldownRatio;
        bool isReady = skill.canUseSkill;

        // Cache
        if (Mathf.Abs(ratio - lastSkillRatio) < 0.005f && !isReady) return;
        lastSkillRatio = ratio;

        // ── Fill amount ──
        skillFillImage.fillAmount = ratio;

        // ── Renk ──
        skillFillImage.color = Color.Lerp(skillCooldownColor, skillReadyColor, ratio);

        // ── Sayısal gösterim (yüzde) ──
        if (skillValueText != null)
            skillValueText.text = $"{Mathf.RoundToInt(ratio * 100)}%";

        // ── Pulse (hazır olduğunda nefes animasyonu) ──
        Transform pulseTarget = skillBarRoot != null ? (Transform)skillBarRoot : skillFillImage.transform;
        if (isReady && ratio >= 1f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * skillPulseSpeed) * skillPulseAmount;
            pulseTarget.localScale = originalSkillScale * pulse;
        }
        else
        {
            pulseTarget.localScale = originalSkillScale;
        }
    }

    // ══════════════════════════════════════════════════
    //  XP BAR & LEVEL
    // ══════════════════════════════════════════════════

    void UpdateXPBar()
    {
        if (UpgradeManager.Instance == null) return;

        int currentXP = UpgradeManager.Instance.currentXP;
        int maxXP = UpgradeManager.Instance.xpToNextLevel;

        if (currentXP == lastXP && maxXP == lastMaxXP) return;
        lastXP = currentXP;
        lastMaxXP = maxXP;

        if (xpFillImage != null)
        {
            float ratio = (maxXP > 0) ? (float)currentXP / maxXP : 0f;
            xpFillImage.fillAmount = ratio;
        }

        if (xpValueText != null)
        {
            xpValueText.text = $"{currentXP}/{maxXP}";
        }

        if (levelText != null)
        {
            levelText.text = $"Lv. {UpgradeManager.Instance.currentLevel}";
        }
    }

    // ══════════════════════════════════════════════════
    //  WAVE & GATE
    // ══════════════════════════════════════════════════

    void UpdateWaveDisplay()
    {
        if (waveText != null && waveText.gameObject.activeSelf) 
            waveText.gameObject.SetActive(false);
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

    // ══════════════════════════════════════════════════
    //  YARDIMCI METODLAR
    // ══════════════════════════════════════════════════

    /// <summary>
    /// Max değer değiştiğinde barın genişliğini orantılı olarak günceller.
    /// Souls oyunlarındaki gibi, max health/energy arttıkça bar uzar.
    /// </summary>
    void ResizeBar(RectTransform barRoot, float currentMax, float initialMax, float initialWidth)
    {
        if (barRoot == null || initialMax <= 0 || initialWidth <= 0) return;

        float ratio = currentMax / initialMax;
        float newWidth = initialWidth * ratio;
        barRoot.sizeDelta = new Vector2(newWidth, barRoot.sizeDelta.y);
    }

    /// <summary>
    /// Oyun başlamadan (GameManager.isGameActive olmasa bile) barları doğru değerlerle başlatır.
    /// Bu sayede editörde "New Text" veya yanlış renk kalması engellenir.
    /// </summary>
    void ForceInitialUpdate()
    {
        // Cache'i sıfırla ki Update metodları ilk çağrıda kesinlikle çalışsın
        lastHealth = -1;
        lastMaxHealth = -1;
        lastEnergy = -1f;
        lastMaxEnergy = -1f;
        lastSkillRatio = -1f;

        UpdateHealthBar();
        UpdateLegacyHearts();
        UpdateEnergyBar();
        UpdateSkillBar();
    }
}
