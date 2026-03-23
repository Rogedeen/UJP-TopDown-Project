using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RarityVisualData
{
    public CardRarity rarity;
    public Sprite cardBack;           // Nadirliğe (örn Epic) özel arka kapak
    public Sprite cardBackground;     // Ön yüzdeki kağıt/zemin
    public Sprite rarityGem;
    public Color hoverGlowColor = Color.white;
}

[CreateAssetMenu(fileName = "CardVisualSettings", menuName = "UJP Project/UI/Card Visual Settings")]
public class CardVisualSettings : ScriptableObject
{
    [Header("Constant Layouts (Her Kartta Aynı)")]
    public Sprite globalFrame; // Yetenek ikonu çerçevesi (Skill slot)
    public Sprite globalNamePlate; // İsim Banner'ı
    public Sprite globalDescriptionPlate; // Açıklama Banner'ı

    [Header("Rarity Settings")]
    public List<RarityVisualData> rarityVisuals = new List<RarityVisualData>();

    public RarityVisualData GetVisualData(CardRarity rarity)
    {
        foreach (var visual in rarityVisuals)
        {
            if (visual.rarity == rarity)
            {
                return visual;
            }
        }
        return null;
    }
}
