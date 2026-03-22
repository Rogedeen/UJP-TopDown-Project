using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUIManager : MonoBehaviour
{
    public static BossUIManager Instance;

    [Header("UI Elements")]
    [Tooltip("Boss can barının ana Container'ı (Ekranda ortada veya en altta duran obje)")]
    public GameObject bossHealthPanel; 
    public Slider healthSlider;
    public TextMeshProUGUI bossNameText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (bossHealthPanel != null)
            bossHealthPanel.SetActive(false);
    }

    /// <summary>
    /// Boss doğduğunda çağrılır. Karartılmış ekranın altından havalı bir şekilde çıkması için animasyon da eklenebilir.
    /// </summary>
    public void ShowBoss(string name, int maxHealth)
    {
        if (bossHealthPanel == null) return;
        
        if (bossNameText != null) bossNameText.text = name;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }
        
        bossHealthPanel.SetActive(true);
    }

    public void UpdateHealth(int currentHealth)
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }

    public void HideBoss()
    {
        if (bossHealthPanel != null)
            bossHealthPanel.SetActive(false);
    }
}
