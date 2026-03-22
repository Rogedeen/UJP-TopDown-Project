using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum ProjectileType { Damage, Slow, Heal }

    [Header("Settings")]
    public ProjectileType type = ProjectileType.Damage;
    public int value = 1;

    [Header("Slow Settings")]
    public float slowAmount = 0.4f;   
    public float slowDuration = 2f;

    private void Awake()
    {
        // ÖNEMLİ: Unity'de OnTriggerEnter'ın çalışması için çarpan objelerden
        // en az birinde Rigidbody olması zorunludur. Büyücü mermilerinde (VFX)
        // Rigidbody yoksa kapıların (static collider) içinden geçer.
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyProjectile")) return;

        if (type == ProjectileType.Heal)
        {
            if (other.CompareTag("Enemy"))
            {
                EnemyBase target = other.GetComponent<EnemyBase>();
                if (target == null) target = other.GetComponentInParent<EnemyBase>();
                if (target != null) target.Heal(value);
            }
            if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject);
            else Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy")) return;

        if (other.CompareTag("Player"))
        {
            ApplyEffect(other);
            if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject);
            else Destroy(gameObject);
        }
        else if (other.CompareTag("Barrier") || other.CompareTag("Barrel"))
        {
            if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject);
            else Destroy(gameObject);
        }
        else
        {
            if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject);
            else Destroy(gameObject);
        }
    }

    private void ApplyEffect(Collider other)
    {
        PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
        PlayerController pController = other.GetComponent<PlayerController>();
        if (pHealth == null) return;

        switch (type)
        {
            case ProjectileType.Damage:
                float damageMultiplier = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentEnemyDamageMultiplier : 1f;
                pHealth.TakeDamage(Mathf.RoundToInt(value * damageMultiplier));
                break;

            case ProjectileType.Slow:
                float slowDamageMultiplier = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentEnemyDamageMultiplier : 1f;
                pHealth.TakeDamage(Mathf.RoundToInt(value * slowDamageMultiplier));
                pController.StartSlow(slowAmount, slowDuration);
                break;

            case ProjectileType.Heal:
                // Support wizard'ın takım arkadaşlarını iyileştirmesi için
                // şimdilik boş, ileride genişletilecek
                break;
        }
    }

}