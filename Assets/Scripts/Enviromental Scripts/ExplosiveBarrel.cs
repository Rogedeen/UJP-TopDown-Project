using System.Collections;
using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Varil Sağlığı")]
    public int barrelHealth = 2;

    [Header("Patlama Ayarları")]
    public float explosionRadius = 4f;
    public int explosionDamage = 2;

    [Header("Kritik Durum Ayarları")]
    public float criticalAutoExplodeTime = 2.5f; 
    public float pulseSpeed = 5f;                
    public float pulseAmount = 0.15f;            

    [Header("Referanslar")]
    public GameObject explosionVFX;

    // Dahili durum takibi
    private bool isCritical = false;    
    private bool hasExploded = false;   
    private Vector3 originalScale;      

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isCritical)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        if (other.CompareTag("Weapon") || other.CompareTag("OrbitWeapon"))
        {
            int damageValue = 1;
            if (other.TryGetComponent<Weapon>(out var w)) damageValue = w.damage;
            else if (other.TryGetComponent<OrbitWeapon>(out var ow)) damageValue = ow.damage;

            TakeBarrelDamage(damageValue);
        }

        if (isCritical && other.CompareTag("Enemy"))
        {
            Explode();
        }
    }

    void TakeBarrelDamage(int damage)
    {
        if (isCritical)
        {
            Explode();
            return;
        }

        barrelHealth -= damage;

        if (barrelHealth <= 0)
        {
            Explode();
        }
        else
        {
            EnterCriticalState();
        }
    }

    void EnterCriticalState()
    {
        if (isCritical) return; 

        isCritical = true;
        StartCoroutine(CriticalCountdown());
    }

    IEnumerator CriticalCountdown()
    {
        yield return new WaitForSeconds(criticalAutoExplodeTime);

        if (!hasExploded)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return; 
        hasExploded = true;
        isCritical = false;

        transform.localScale = originalScale;

        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        Collider[] affected = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var col in affected)
        {
            if (col.TryGetComponent<EnemyBase>(out var enemy))
                enemy.TakeDamage(explosionDamage, transform.position);

            if (col.TryGetComponent<PlayerHealth>(out var player))
                player.TakeDamage(1);
        }

        Destroy(gameObject);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}