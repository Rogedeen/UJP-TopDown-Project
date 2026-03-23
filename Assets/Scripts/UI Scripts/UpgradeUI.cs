using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ekranda beliren 3 rastgele kartı (Upgrade) kontrol eden UI yöneticisidir.
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Kartların hepsini kapsayan ekran tasarımı (Siyah arka plan vs.)")]
    public GameObject upgradeContainer;

    [Header("UI System")]
    [Tooltip("Görsel temalar ve nadirlik ayarları")]
    public CardVisualSettings cardVisualSettings;

    [Header("Card Arrays")]
    [Tooltip("Sahnede tanımlanmış kart arayüzleri")]
    public CardUI[] cards;

    // Seçilen 3 aktif kartın listesini tutar (Index takibi için)
    private List<BaseUpgrade> activeUpgrades;

    private void Start()
    {
        // Başlangıçta ekran kapalı olacak
        HideUpgradeScreen();
    }

    /// <summary>
    /// Manager rastgele 3 kartı çektiğinde bu fonksiyonu çağırır.
    /// UI'ı ekranda belirginleştirir ve kartları günceller.
    /// </summary>
    public void ShowUpgradeScreen(List<BaseUpgrade> upgradesToDisplay)
    {
        activeUpgrades = upgradesToDisplay;
        upgradeContainer.SetActive(true);
        StartCoroutine(ShowAnimationRoutine());

        // Maksimum desteklenen kart sayısı kadar (örn 3) döngü yap
        for (int i = 0; i < cards.Length; i++)
        {
            if (i < activeUpgrades.Count)
            {
                BaseUpgrade currentUpgrade = activeUpgrades[i];
                cards[i].SetupCard(currentUpgrade, cardVisualSettings, this);
                
                // Kap düşme/açılma animasyonuna (0.4f) ek olarak her karta 0.15f gecikme ekle
                StartCoroutine(cards[i].FlipRoutine(0.4f + (i * 0.15f)));
            }
            else
            {
                // Havuzda yeterli kart kalmamışsa fazla butonları kapat
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// CardUI üzerinden kullanıcı tıkladığında tetiklenir.
    /// </summary>
    public void OnCardSelected(BaseUpgrade selectedUpgrade)
    {
        if (selectedUpgrade != null)
        {
            UpgradeManager.Instance.OnUpgradeSelected(selectedUpgrade);
        }
    }

    /// <summary>
    /// Ekranı Kapat
    /// </summary>
    public void HideUpgradeScreen()
    {
        if (upgradeContainer != null)
        {
            upgradeContainer.SetActive(false);
        }
    }

    private System.Collections.IEnumerator ShowAnimationRoutine()
    {
        CanvasGroup cg = upgradeContainer.GetComponent<CanvasGroup>();
        if (cg == null) cg = upgradeContainer.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        upgradeContainer.transform.localScale = Vector3.one * 0.8f;

        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Oyun süresi 0 olsa da çalışır
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            upgradeContainer.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
        upgradeContainer.transform.localScale = Vector3.one;
    }
}
