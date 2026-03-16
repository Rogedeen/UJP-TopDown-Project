using UnityEngine;
using System.Collections;

[System.Serializable]
public struct TimePhase
{
    [Tooltip("Örn: Öğle, İkindi, Gece")]
    public string phaseName; 
    
    [Header("Skybox Settings")]
    [Tooltip("Bu aşamaya geçilirken gökyüzü materyali tamamen değişsin istiyorsanız buraya koyun (Örn: Gece materyali)")]
    public Material phaseSkyboxMaterial;
    [ColorUsage(true, true)] public Color skyTint;
    public float skyExposure;
    
    [Header("Fog Settings (Skybox Extended)")]
    [Range(0f, 1f)] public float fogIntensity;
    public float fogHeight;

    [Header("Directional Light")]
    public Color lightColor;
    public float lightIntensity;

    [Header("Enemy Global Buffs")]
    [Tooltip("1.0 normal hız. Geceleri 1.5 yapılabilir.")]
    public float enemySpeedMultiplier;
    [Tooltip("1.0 normal hasar. Geceleri 2.0 yapılabilir.")]
    public float enemyDamageMultiplier;
}

/// <summary>
/// Skybox Extended asseti ile uyumlu Gece/Gündüz döngüsü yöneticisi.
/// WaveManager'dan (Kapılar kapandığında) tetiklenerek atmosferi ve oyun zorluğunu Lerp ile pürüzsüzce değiştirir.
/// </summary>
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    [Header("References")]
    [Tooltip("Sahnede Skybox Extended shader'ını kullanan material")]
    public Material skyboxMaterial;
    [Tooltip("Sahnedeki ana Güneş / Ay ışığı")]
    public Light directionalLight;
    
    [Header("Settings")]
    public float transitionDuration = 3f;
    public TimePhase[] timePhases;

    public int CurrentPhaseIndex { get; private set; }

    // Shader özellik ID'leri (Reflection yerine performanslı ID kullanımı)
    private readonly int tintColorId = Shader.PropertyToID("_TintColor");
    private readonly int exposureId = Shader.PropertyToID("_Exposure");
    private readonly int fogIntensityId = Shader.PropertyToID("_FogIntensity");
    private readonly int fogHeightId = Shader.PropertyToID("_FogHeight");

    // Global zorluk çarpanlarını diğer scriptlerin (EnemyAI vb) kolayca okuması için field'lar
    public float CurrentEnemySpeedMultiplier { get; private set; } = 1f;
    public float CurrentEnemyDamageMultiplier { get; private set; } = 1f;

    private Coroutine activeTransition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (timePhases.Length > 0)
        {
            CurrentPhaseIndex = 0;
            ApplyPhaseInstant(timePhases[0]);
        }
    }

    /// <summary>
    /// Bir sonraki zaman dilimine geçişi başlatır. (Örn: Öğle -> Akşam)
    /// </summary>
    public void AdvanceTimePhase()
    {
        if (timePhases == null || timePhases.Length == 0) return;

        int nextPhaseIndex = Mathf.Min(CurrentPhaseIndex + 1, timePhases.Length - 1);
        
        // Eğer zaten son aşamadaysak (Gece) bir daha ilerleme
        if (nextPhaseIndex != CurrentPhaseIndex)
        {
            CurrentPhaseIndex = nextPhaseIndex;
            
            if (activeTransition != null) StopCoroutine(activeTransition);
            activeTransition = StartCoroutine(TransitionRoutine(timePhases[nextPhaseIndex]));
        }
    }

    private void ApplyPhaseInstant(TimePhase phase)
    {
        // 1- İSTERSEN SKYBOX MATERYALİNİ KOMPLE DEĞİŞTİR:
        // Eğer bu aşama (phase) kendine özel bir Skybox Materyaline sahipse onu kullan:
        if (phase.phaseSkyboxMaterial != null && RenderSettings.skybox != phase.phaseSkyboxMaterial)
        {
            RenderSettings.skybox = phase.phaseSkyboxMaterial;
            DynamicGI.UpdateEnvironment(); // Çevresel yansımaları (Lighting) yeni materyale uyarla
            skyboxMaterial = phase.phaseSkyboxMaterial; // Lerp referansını güncelle
        }

        // 2- İŞTE BURADA DA RENK VE SİSLER (FOG) ANINDA EŞİTLENİR
        if (skyboxMaterial != null)
        {
            if (skyboxMaterial.HasProperty(tintColorId)) skyboxMaterial.SetColor(tintColorId, phase.skyTint);
            if (skyboxMaterial.HasProperty(exposureId)) skyboxMaterial.SetFloat(exposureId, phase.skyExposure);
            if (skyboxMaterial.HasProperty(fogIntensityId)) skyboxMaterial.SetFloat(fogIntensityId, phase.fogIntensity);
            if (skyboxMaterial.HasProperty(fogHeightId)) skyboxMaterial.SetFloat(fogHeightId, phase.fogHeight);
        }

        if (directionalLight != null)
        {
            directionalLight.color = phase.lightColor;
            directionalLight.intensity = phase.lightIntensity;
        }

        CurrentEnemySpeedMultiplier = phase.enemySpeedMultiplier == 0 ? 1f : phase.enemySpeedMultiplier;
        CurrentEnemyDamageMultiplier = phase.enemyDamageMultiplier == 0 ? 1f : phase.enemyDamageMultiplier;
        
        Debug.Log($"[DayNightManager] Zaman anında atlandı: {phase.phaseName}");
    }

    private IEnumerator TransitionRoutine(TimePhase targetPhase)
    {
        Debug.Log($"[DayNightManager] Zaman ilerliyor: {targetPhase.phaseName}");

        float elapsed = 0f;

        // EĞER HEDEF FAZIN MATERYALİ FARKLIYSA, GEÇİŞİN EN BAŞINDA MATERYALİ DEĞİŞTİR:
        // (Çünkü yıldızların ve bulutların dokusu (Texture) Lerp edilemez, anında değişmeli)
        if (targetPhase.phaseSkyboxMaterial != null && RenderSettings.skybox != targetPhase.phaseSkyboxMaterial)
        {
            RenderSettings.skybox = targetPhase.phaseSkyboxMaterial;
            DynamicGI.UpdateEnvironment();
            skyboxMaterial = targetPhase.phaseSkyboxMaterial; // Artık yeni materyalin renkleriyle oynayacağız
        }

        // Mevcut Material Değerlerini Oku
        Color startSkyTint = skyboxMaterial != null && skyboxMaterial.HasProperty(tintColorId) ? skyboxMaterial.GetColor(tintColorId) : Color.white;
        float startExposure = skyboxMaterial != null && skyboxMaterial.HasProperty(exposureId) ? skyboxMaterial.GetFloat(exposureId) : 1f;
        float startFogInt = skyboxMaterial != null && skyboxMaterial.HasProperty(fogIntensityId) ? skyboxMaterial.GetFloat(fogIntensityId) : 0f;
        float startFogHeight = skyboxMaterial != null && skyboxMaterial.HasProperty(fogHeightId) ? skyboxMaterial.GetFloat(fogHeightId) : 0f;

        // Mevcut Işık Değerlerini Oku
        Color startLightColor = directionalLight != null ? directionalLight.color : Color.white;
        float startLightInt = directionalLight != null ? directionalLight.intensity : 1f;

        // Mevcut Buff Değerleri
        float startSpeedMult = CurrentEnemySpeedMultiplier;
        float startDamageMult = CurrentEnemyDamageMultiplier;

        float targetSpeedMult = targetPhase.enemySpeedMultiplier == 0 ? 1f : targetPhase.enemySpeedMultiplier;
        float targetDamageMult = targetPhase.enemyDamageMultiplier == 0 ? 1f : targetPhase.enemyDamageMultiplier;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration); // Smooth curve için eklenebilir

            if (skyboxMaterial != null)
            {
                if (skyboxMaterial.HasProperty(tintColorId)) skyboxMaterial.SetColor(tintColorId, Color.Lerp(startSkyTint, targetPhase.skyTint, t));
                if (skyboxMaterial.HasProperty(exposureId)) skyboxMaterial.SetFloat(exposureId, Mathf.Lerp(startExposure, targetPhase.skyExposure, t));
                if (skyboxMaterial.HasProperty(fogIntensityId)) skyboxMaterial.SetFloat(fogIntensityId, Mathf.Lerp(startFogInt, targetPhase.fogIntensity, t));
                if (skyboxMaterial.HasProperty(fogHeightId)) skyboxMaterial.SetFloat(fogHeightId, Mathf.Lerp(startFogHeight, targetPhase.fogHeight, t));
            }

            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startLightColor, targetPhase.lightColor, t);
                directionalLight.intensity = Mathf.Lerp(startLightInt, targetPhase.lightIntensity, t);
            }

            CurrentEnemySpeedMultiplier = Mathf.Lerp(startSpeedMult, targetSpeedMult, t);
            CurrentEnemyDamageMultiplier = Mathf.Lerp(startDamageMult, targetDamageMult, t);

            yield return null;
        }

        ApplyPhaseInstant(targetPhase);
        activeTransition = null;
    }
}
