using UnityEngine;

/// <summary>
/// Can, Enerji, Yenilenme hızı veya Hareket hızı gibi
/// sürekli (pasif) istatistik artışları sağlamak için kullanılan upgrade sınıfıdır.
/// </summary>
[CreateAssetMenu(fileName = "New Stat Upgrade", menuName = "Upgrades/Stat Upgrade")]
public class StatUpgrade : BaseUpgrade
{
    // Olası arttırılabilir stat'ların tipleri
    public enum StatType
    {
        MaxHealth,        // Maximum can (ve mevcut can) artışı
        HealDirect,       // Sadece can yenileme
        MaxEnergy,        // Enerji kapasitesi artışı
        EnergyRegenRate,  // Saniyede dolan enerji hızı artışı
        MovementSpeed     // Normal yürüyüş hızını artırma
    }

    [Header("Stat Modification")]
    public StatType statToModify;
    public float amountToAdd; // Eklenecek değer (örneğin can için +1, hız için +1.5f vs.)

    public override void ApplyUpgrade(PlayerController player)
    {
        // Oyuncunun gerekli bileşenlerini al
        PlayerHealth healthComponent = player.GetComponent<PlayerHealth>();

        switch (statToModify)
        {
            case StatType.MaxHealth:
                if (healthComponent != null)
                {
                    healthComponent.IncreaseMaxHealth((int)amountToAdd);
                }
                break;
                
            case StatType.HealDirect:
                if (healthComponent != null)
                {
                    healthComponent.Heal((int)amountToAdd);
                }
                break;

            case StatType.MaxEnergy:
                player.maxEnergy += amountToAdd;
                player.RestoreEnergy(amountToAdd); // Limiti artırınca bir miktar da enerji verelim
                break;

            case StatType.EnergyRegenRate:
                // Normal "energyRegenRate" private olduğu için onu arttıracak bir public metod yazacağız.
                // Veya değerlerini okuyabildiğimiz farklı bir sisteme bakacağız.
                // [GÜNCELLENECEK] PlayerController.cs tarafında bir IncreaseRegenRate(amount) fonksiyonu şart.
                player.IncreaseRegenRate(amountToAdd);
                break;

            case StatType.MovementSpeed:
                player.speed += amountToAdd;
                break;
        }

        Debug.Log($"Upgrade Applied: {upgradeName} -> {statToModify} increased by {amountToAdd}");
    }
}
