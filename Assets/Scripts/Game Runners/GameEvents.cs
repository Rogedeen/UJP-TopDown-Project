using System;

/// <summary>
/// Oyundaki global olayları (event) yayınlayan statik sınıf.
/// 
/// EVENT SİSTEMİ NASIL ÇALIŞIR?
/// ─────────────────────────────
/// Düşman öldüğünde artık doğrudan WaveManager.activeEnemyCount-- yazmıyoruz.
/// Bunun yerine:
///   1. Düşman: "Ben öldüm!" sinyali yayınlar    → GameEvents.OnEnemyDied?.Invoke()
///   2. WaveManager: Bu sinyali dinler              → GameEvents.OnEnemyDied += HandleEnemyDied
///   3. WaveManager.HandleEnemyDied() içinde        → activeEnemyCount--
/// 
/// AVANTAJ: Düşmanın WaveManager'ı tanımasına gerek yok. 
/// İleride skor sistemi, ses efekti vs. eklemek istersen sadece bu event'e abone olursun.
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// Bir düşman öldüğünde tetiklenir.
    /// Dinleyen: WaveManager (düşman sayısını azaltmak için)
    /// </summary>
    public static event Action OnEnemyDied;

    /// <summary>
    /// Bir düşman öldüğünde XP değeriyle tetiklenir.
    /// Dinleyen: UpgradeManager (Level Up sistemi için)
    /// </summary>
    public static event Action<int> OnEnemyDiedWithXP;

    /// <summary>
    /// Oyuncu hasar aldığında tetiklenir.
    /// Dinleyen: İleride UI, ses sistemi vs. eklenebilir
    /// </summary>
    public static event Action<int> OnPlayerDamaged;

    // Event'leri tetiklemek için yardımcı metodlar
    public static void EnemyDied(int xpAmount = 1) 
    {
        OnEnemyDied?.Invoke(); 
        OnEnemyDiedWithXP?.Invoke(xpAmount);
    }
    public static void PlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);
}
