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

    [Header("Card Arrays")]
    [Tooltip("3 adet Kartın (Butonun) kendisi")]
    public Button[] cardButtons;

    [Tooltip("3 adet Kartın İsim (TMP_Text) Teksleri")]
    public TMP_Text[] titleTexts;
    
    [Tooltip("3 adet Kartın Açıklama (TMP_Text) Textleri")]
    public TMP_Text[] descriptionTexts;
    
    [Tooltip("3 adet Kartın Resim Yeri (Image) Referansları")]
    public Image[] iconImages;

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

        // Maksimum desteklenen kart sayısı kadar (örn 3) döngü yap
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (i < activeUpgrades.Count)
            {
                BaseUpgrade currentUpgrade = activeUpgrades[i];
                cardButtons[i].gameObject.SetActive(true);

                // Kart UI elemanlarını doldur ("Index Out of Range" koruması ile)
                if (i < titleTexts.Length && titleTexts[i] != null) 
                    titleTexts[i].text = currentUpgrade.upgradeName;
                
                if (i < descriptionTexts.Length && descriptionTexts[i] != null) 
                    descriptionTexts[i].text = currentUpgrade.description;
                
                if (i < iconImages.Length && iconImages[i] != null)
                {
                    if (currentUpgrade.icon != null)
                    {
                        iconImages[i].sprite = currentUpgrade.icon;
                        iconImages[i].enabled = true;
                    }
                    else
                    {
                        iconImages[i].enabled = false;
                    }
                }
            }
            else
            {
                // Havuzda yeterli kart kalmamışsa fazla butonları kapat
                cardButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Kullanıcı UI butonlarından birine tıkladığında EventSystem üzerinden tetiklenir!
    /// Unity Editor'de butonun OnClick() fonksiyonuna bu metodu verip Index (0, 1 veya 2) vereceğiz.
    /// </summary>
    public void OnCardClicked(int cardIndex)
    {
        if (cardIndex >= 0 && cardIndex < activeUpgrades.Count)
        {
            // Tıklanan kartı al ve Game Manager / Upgrade Manager'a bildir
            BaseUpgrade selected = activeUpgrades[cardIndex];
            UpgradeManager.Instance.OnUpgradeSelected(selected);
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
}
