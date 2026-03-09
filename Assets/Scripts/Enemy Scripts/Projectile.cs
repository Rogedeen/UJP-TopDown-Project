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
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy")) return;

        if (other.CompareTag("Player"))
        {
            ApplyEffect(other);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Barrier") || other.CompareTag("Barrel"))
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
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
                pHealth.TakeDamage(value);
                break;

            case ProjectileType.Slow:
                pHealth.TakeDamage(value);
                Debug.Log("Yavaşlatma mermisi çarptı, efekt uygulanıyor...");
                pController.StartCoroutine(pController.ApplySlow(slowAmount, slowDuration));
                // PlayerHealth'e ApplySlow eklediğinde bu satırı aç:
                // pHealth.ApplySlow(slowAmount, slowDuration);
                break;

            case ProjectileType.Heal:
                // Support wizard'ın takım arkadaşlarını iyileştirmesi için
                // şimdilik boş, ileride genişletilecek
                break;
        }
    }

}