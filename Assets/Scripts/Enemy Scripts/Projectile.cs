using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum ProjectileType { Damage, Slow, Heal }

    [Header("Settings")]
    public ProjectileType type = ProjectileType.Damage;
    public float speed = 15f;
    public int value = 1;
    public float lifeTime = 4f;

    // Slow efekti için ek alanlar
    [Header("Slow Settings")]
    public float slowAmount = 0.4f;   // Oyuncunun hızı %40'a düşer
    public float slowDuration = 2f;

    [Header("Effects")]
    public GameObject explosionPrefab;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Düşmanlara çarparsa geç (hem normal hem heal mermisi için)
        if (other.CompareTag("EnemyProjectile")) return;

        // Heal mermisi oyuncuya çarparsa hasar verme, sadece yok ol
        if (type == ProjectileType.Heal)
        {
            if (other.CompareTag("Enemy"))
            {
                EnemyBase target = other.GetComponent<EnemyBase>();
                // Bileşen direkt bulunamazsa parent'ta ara (collider child objedeyse)
                if (target == null) target = other.GetComponentInParent<EnemyBase>();
                if (target != null) target.Heal(value);
            }
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy")) return;

        SpawnExplosion();

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
        if (pHealth == null) return;

        switch (type)
        {
            case ProjectileType.Damage:
                pHealth.TakeDamage(value);
                break;

            case ProjectileType.Slow:
                pHealth.TakeDamage(value);
                // PlayerHealth'e ApplySlow eklediğinde bu satırı aç:
                // pHealth.ApplySlow(slowAmount, slowDuration);
                break;

            case ProjectileType.Heal:
                // Support wizard'ın takım arkadaşlarını iyileştirmesi için
                // şimdilik boş, ileride genişletilecek
                break;
        }
    }

    private void SpawnExplosion()
    {
        if (explosionPrefab != null)
        {
            GameObject expo = Instantiate(explosionPrefab, transform.position, transform.rotation);
            Destroy(expo, 2f);
        }
    }
}