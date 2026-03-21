using UnityEngine;

/// <summary>
/// Hasar alabilecek tüm objeler için ortak arayüz (interface).
/// EnemyBase, ExplosiveBarrel gibi sınıflar bunu implement eder.
/// 
/// Interface = "Bu obje şu işlevi yapabilir" sözleşmesi.
/// Hasar veren kodun (PlayerController, Projectile vs.) hedefin 
/// tam olarak ne olduğunu bilmesine gerek kalmaz.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage, Vector3 knockbackSource, float knockbackMultiplier = 1f);
}
