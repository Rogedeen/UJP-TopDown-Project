using UnityEngine;

/// <summary>
/// Oyuncunun kombo sayısını artırarak 4. (Finisher) vuruşa erişmesini sağlayan
/// güçlendirme (upgrade) kartı.
/// </summary>
[CreateAssetMenu(fileName = "New Combo Upgrade", menuName = "Upgrades/Combo Upgrade")]
public class ComboUpgrade : BaseUpgrade
{
    [Header("Combo Modification")]
    [Tooltip("Yeni eklenecek kombo sayısı (Örn: 1 yazarsan Max Combo 3'ten 4'e çıkar)")]
    public int extraComboSteps = 1;

    public override void ApplyUpgrade(PlayerController player)
    {
        if (player != null)
        {
            player.maxCombo += extraComboSteps;
            Debug.Log($"Upgrade Applied: {upgradeName} -> Max Combo increased to {player.maxCombo}");
        }
    }
}
