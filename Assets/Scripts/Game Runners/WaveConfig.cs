using UnityEngine;

[CreateAssetMenu(menuName = "Game/Wave Config", fileName = "NewWaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Tooltip("HUD'da gösterilecek isim (Örn: Wave 1)")]
    public string waveName = "Wave 1";

    [Header("Zamanlama")]
    [Tooltip("Bu wave kaç saniye sürecek")]
    public float waveDuration = 60f;
    [Tooltip("Kaç saniyede bir düşman doğacak")]
    public float spawnInterval = 2f;

    [Header("Düşman Sayı Kontrolü (Quota Sistemi)")]
    [Tooltip("Ekranda EN AZ bu kadar düşman olmalı. Altına düşerse hızlıca doldurulur.")]
    public int minAliveEnemies = 3;
    [Tooltip("Ekranda EN FAZLA bu kadar düşman olabilir. Sınıra ulaşılırsa spawn durur.")]
    public int maxAliveEnemies = 12;

    [Header("Spawn Tablosu")]
    [Tooltip("Hangi düşmanın hangi ağırlıkla doğacağı")]
    public SpawnEntry[] spawnTable;
}

[System.Serializable]
public struct SpawnEntry
{
    public GameObject enemyPrefab;
    [Tooltip("Ağırlık değeri. Yüksek = daha sık. Örn: Normal=60, Güçlü=25, Büyücü=10")]
    public int spawnWeight;
}
