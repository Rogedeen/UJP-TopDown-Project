using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Visual Layers")]
    public GameObject cardFrontContainer; // Animasyon sırasında dönene kadar kapalı kalacak ön yüz
    public Image cardBackImage;           // Arka taraf grafiği
    public Image backgroundImage;
    public Image frameImage;
    public Image namePlateImage;
    public Image descriptionPlateImage;
    public Image rarityGemImage;
    public Image iconImage;
    public Image glowOutlineImage;

    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("Hover Settings")]
    public float hoverScale = 1.1f;
    public float scaleSpeed = 10f;
    private Vector3 targetScale = Vector3.one;

    private BaseUpgrade currentUpgrade;
    private UpgradeUI parentUI;
    private bool isClickable = false;

    private void Update()
    {
        // Smooth scaling for hover effect
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void SetupCard(BaseUpgrade upgrade, CardVisualSettings visualSettings, UpgradeUI uiManager)
    {
        currentUpgrade = upgrade;
        parentUI = uiManager;
        isClickable = false; // Başlangıçta arkası dönük ve tıklanamaz
        
        // Reset scale and glow
        targetScale = Vector3.one;
        transform.localScale = Vector3.one;
        if (glowOutlineImage != null) glowOutlineImage.enabled = false;

        if (upgrade == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Bind Texts
        if (titleText != null) titleText.text = upgrade.upgradeName;
        if (descriptionText != null) descriptionText.text = upgrade.description;

        // Bind Icon
        if (iconImage != null)
        {
            if (upgrade.icon != null)
            {
                iconImage.sprite = upgrade.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        // Apply Rarity and Global Visuals
        if (visualSettings != null)
        {
            // Hazırlık: Önce arka yüzü gösteriyoruz (Sprite ataması Rarity bloğunda yapılacak)
            if (cardFrontContainer != null) cardFrontContainer.SetActive(false);
            if (cardBackImage != null) cardBackImage.gameObject.SetActive(true);

            // Sabit Grafik Kısımları (Frame, Plate'ler)
            if (frameImage != null) frameImage.sprite = visualSettings.globalFrame;
            if (namePlateImage != null) namePlateImage.sprite = visualSettings.globalNamePlate;
            if (descriptionPlateImage != null) descriptionPlateImage.sprite = visualSettings.globalDescriptionPlate;

            // Nadirliğe (Rarity) Özel Kısımlar
            RarityVisualData data = visualSettings.GetVisualData(upgrade.rarity);
            if (data != null)
            {
                if (cardBackImage != null && data.cardBack != null) cardBackImage.sprite = data.cardBack;
                if (backgroundImage != null && data.cardBackground != null) backgroundImage.sprite = data.cardBackground;
                
                if (rarityGemImage != null)
                {
                    if (data.rarityGem != null)
                    {
                        rarityGemImage.sprite = data.rarityGem;
                        rarityGemImage.enabled = true;
                    }
                    else
                    {
                        rarityGemImage.enabled = false;
                    }
                }

                if (glowOutlineImage != null)
                {
                    glowOutlineImage.color = data.hoverGlowColor;
                }
            }
        }
    }

    public System.Collections.IEnumerator FlipRoutine(float delay)
    {
        // Ekstra 1 saniye beklet (oyuncunun kazara tıklamasını önlemek için)
        yield return new WaitForSecondsRealtime(delay + 1.25f);

        float duration = 0.25f;
        float elapsed = 0f;

        // 1. Aşama: Kartı Y ekseninde (veya X'te) daraltarak incelt (Arka Yüz)
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scaleX = Mathf.Lerp(1f, 0f, elapsed / duration);
            transform.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }

        // Kart tamamen ince (X=0) olduğunda görseli ön yüze çevir
        transform.localScale = new Vector3(0f, 1f, 1f);
        if (cardBackImage != null) cardBackImage.gameObject.SetActive(false);
        if (cardFrontContainer != null) cardFrontContainer.SetActive(true);

        elapsed = 0f;

        // 2. Aşama: Kartı tekrar genişlet (Ön Yüz)
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scaleX = Mathf.Lerp(0f, 1f, elapsed / duration);
            transform.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        targetScale = Vector3.one; // Hover sistemini senkronize et
        
        isClickable = true; // Kart tamamen döndükten sonra tıklanabilir
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Hover Start
        targetScale = Vector3.one * hoverScale;
        if (glowOutlineImage != null)
        {
            glowOutlineImage.enabled = true;
        }
        
        // Optional: Play hover sound here
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Hover End
        targetScale = Vector3.one;
        if (glowOutlineImage != null)
        {
            glowOutlineImage.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isClickable) return;

        if (currentUpgrade != null && parentUI != null)
        {
            parentUI.OnCardSelected(currentUpgrade);
        }
    }
}
