using UnityEngine;

/// <summary>
/// Tüm Rogue-Lite güçlendirmelerinin (upgrades) türetileceği ana sınıftır.
/// Bu sayede Unity Editor içinde kartları veri olarak tasarlayabiliriz.
/// </summary>
public abstract class BaseUpgrade : ScriptableObject
{
    [Header("UI Information")]
    public string upgradeName = "New Upgrade";
    
    [TextArea(2, 4)]
    public string description = "Upgrade Description";
    
    public Sprite icon; // Kartın üzerinde görünecek resim

    [Header("Juice (Polishing & Feedback)")]
    [Tooltip("Bu güçlendirme alındığında oyuncunun üzerinde patlayacak görsel efekt (VFX).")]
    public GameObject vfxPrefab;
    
    [Tooltip("Bu güçlendirme alındığında çalacak özel ses (SFX).")]
    public AudioClip pickupSound;

    /// <summary>
    /// Bu upgrade seçildiğinde çalışacak asıl fonksiyondur.
    /// Her bir alt sınıf (örn: StatUpgrade) bu fonksiyonu kendine göre "override" eder.
    /// </summary>
    /// <param name="player">Oyuncu karakterinin ana Controller objesi</param>
    public abstract void ApplyUpgrade(PlayerController player);
}
