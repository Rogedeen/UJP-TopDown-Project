using UnityEngine;
using System.Collections;

[System.Serializable]
public struct TimePhase
{
    [Tooltip("Örn: Öğle, İkindi, Gece")]
    public string phaseName;

    [Header("Skybox")]
    [Tooltip("Bu aşamada gökyüzünün kullanacağı materyal (Day, Blend, Night)")]
    public Material phaseSkyboxMaterial;

    [Header("Directional Light (Güneş)")]
    [Tooltip("Güneşin parlaklığı. Gündüz 1.0, Gece 0.15")]
    public float lightIntensity;
    [Tooltip("Güneşin rengi. Hafif sıcak/soğuk ton. ASLA parlak turuncu veya mor yapma!")]
    public Color lightColor;
    [Tooltip("Güneşin X açısı. Yüksek = Öğle, Düşük = Gün batımı, Negatif = Gece")]
    public float sunAngleX;

    [Header("Çevre Aydınlatması")]
    [Tooltip("Gölgelerin ve karanlık alanların rengi. Çok koyu ve az doygun olmalı.")]
    public Color ambientColor;
    [Tooltip("Sis rengi. Açık gri veya koyu lacivert gibi doğal tonlar.")]
        public Color fogColor;
    [Tooltip("Sis yoğunluğu. Gündüz 0, Gece 0.015 civarı.")]
    public float fogDensity;
    [Tooltip("Linear sisin başladığı mesafe (Linear Mode)")]
    public float fogStartDistance;
    [Tooltip("Linear sisin tam kapandığı mesafe (Linear Mode)")]
    public float fogEndDistance;

    [Header("Düşman Zorluk")]
    [Tooltip("1.0 = normal hız.")]
    public float enemySpeedMultiplier;
    [Tooltip("1.0 = normal hasar.")]
    public float enemyDamageMultiplier;
}

/// <summary>
/// Kapılar kapandığında Skybox materyalini, güneşi, ortam ışığını ve sisi
/// pürüzsüzce değiştirir. Tüm renkler DOĞAL ve az doygun tutulmalıdır.
/// </summary>
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    [Header("References")]
    [Tooltip("Sahnedeki Directional Light (Güneş)")]
    public Light directionalLight;

    [Header("Time Phases")]
    public TimePhase[] timePhases;
    [Tooltip("Geçiş süresi (saniye)")]
    public float transitionDuration = 5f;

    public int CurrentPhaseIndex { get; private set; }
    public event System.Action<int> OnPhaseChanged;

    public float CurrentEnemySpeedMultiplier { get; private set; } = 1f;
    public float CurrentEnemyDamageMultiplier { get; private set; } = 1f;

    private Material runtimeSkybox;
    private Coroutine activeTransition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (RenderSettings.skybox != null)
        {
            runtimeSkybox = new Material(RenderSettings.skybox);
            RenderSettings.skybox = runtimeSkybox;
        }

        if (timePhases != null && timePhases.Length > 0)
        {
            CurrentPhaseIndex = 0;
            ApplyPhaseInstant(timePhases[0]);
        }
    }

    public void AdvanceTimePhase()
    {
        if (timePhases == null || timePhases.Length == 0) return;

        int nextIndex = Mathf.Min(CurrentPhaseIndex + 1, timePhases.Length - 1);
        if (nextIndex == CurrentPhaseIndex) return;

        CurrentPhaseIndex = nextIndex;

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(TransitionRoutine(timePhases[nextIndex]));
    }

    private void ApplyPhaseInstant(TimePhase phase)
    {
        // Skybox materyali
        if (runtimeSkybox != null && phase.phaseSkyboxMaterial != null)
        {
            runtimeSkybox.CopyPropertiesFromMaterial(phase.phaseSkyboxMaterial);
            DynamicGI.UpdateEnvironment();
        }

        // Güneş
        if (directionalLight != null)
        {
            directionalLight.intensity = phase.lightIntensity;
            directionalLight.color = phase.lightColor;
            directionalLight.transform.rotation = Quaternion.Euler(phase.sunAngleX, directionalLight.transform.eulerAngles.y, 0f);
        }

                // Çevre
        RenderSettings.ambientLight = phase.ambientColor;
        RenderSettings.fogColor = phase.fogColor;
        RenderSettings.fogDensity = phase.fogDensity;
        RenderSettings.fogStartDistance = phase.fogStartDistance == 0 ? 15f : phase.fogStartDistance;
        RenderSettings.fogEndDistance = phase.fogEndDistance == 0 ? 40f : phase.fogEndDistance;

        // Düşman
        CurrentEnemySpeedMultiplier = phase.enemySpeedMultiplier == 0 ? 1f : phase.enemySpeedMultiplier;
        CurrentEnemyDamageMultiplier = phase.enemyDamageMultiplier == 0 ? 1f : phase.enemyDamageMultiplier;

        Debug.Log($"[DayNightManager] Faz: {phase.phaseName}");
        OnPhaseChanged?.Invoke(CurrentPhaseIndex);
    }

    private IEnumerator TransitionRoutine(TimePhase targetPhase)
    {
        Debug.Log($"[DayNightManager] Geçiş: {targetPhase.phaseName}");

        float elapsed = 0f;

        Material startSnapshot = runtimeSkybox != null ? new Material(runtimeSkybox) : null;

        // Başlangıç değerlerini kaydet
        float startLightInt = directionalLight != null ? directionalLight.intensity : 1f;
        Color startLightCol = directionalLight != null ? directionalLight.color : Color.white;
        float startSunAngle = directionalLight != null ? directionalLight.transform.eulerAngles.x : 50f;
        float sunY = directionalLight != null ? directionalLight.transform.eulerAngles.y : 0f;

                Color startAmbient = RenderSettings.ambientLight;
        Color startFogCol = RenderSettings.fogColor;
        float startFogDens = RenderSettings.fogDensity;
        float startFogStart = RenderSettings.fogStartDistance;
        float startFogEnd = RenderSettings.fogEndDistance;

        float startSpeed = CurrentEnemySpeedMultiplier;
        float startDamage = CurrentEnemyDamageMultiplier;
        float targetSpeed = targetPhase.enemySpeedMultiplier == 0 ? 1f : targetPhase.enemySpeedMultiplier;
        float targetDamage = targetPhase.enemyDamageMultiplier == 0 ? 1f : targetPhase.enemyDamageMultiplier;

        bool giUpdated = false;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            // Skybox materyal harmanlama
            if (runtimeSkybox != null && startSnapshot != null && targetPhase.phaseSkyboxMaterial != null)
            {
                runtimeSkybox.Lerp(startSnapshot, targetPhase.phaseSkyboxMaterial, t);
                if (t >= 0.5f && !giUpdated)
                {
                    DynamicGI.UpdateEnvironment();
                    giUpdated = true;
                }
            }

            // Güneş
            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(startLightInt, targetPhase.lightIntensity, t);
                directionalLight.color = Color.Lerp(startLightCol, targetPhase.lightColor, t);
                float angle = Mathf.Lerp(startSunAngle, targetPhase.sunAngleX, t);
                directionalLight.transform.rotation = Quaternion.Euler(angle, sunY, 0f);
            }

            // Çevre
                        RenderSettings.ambientLight = Color.Lerp(startAmbient, targetPhase.ambientColor, t);
            RenderSettings.fogColor = Color.Lerp(startFogCol, targetPhase.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDens, targetPhase.fogDensity, t);
            RenderSettings.fogStartDistance = Mathf.Lerp(startFogStart, targetPhase.fogStartDistance == 0 ? 15f : targetPhase.fogStartDistance, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, targetPhase.fogEndDistance == 0 ? 40f : targetPhase.fogEndDistance, t);

            // Düşman
            CurrentEnemySpeedMultiplier = Mathf.Lerp(startSpeed, targetSpeed, t);
            CurrentEnemyDamageMultiplier = Mathf.Lerp(startDamage, targetDamage, t);

            yield return null;
        }

        if (startSnapshot != null) Destroy(startSnapshot);
        ApplyPhaseInstant(targetPhase);
        activeTransition = null;
    }
}
