using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class ExplosiveBarrel : MonoBehaviour, IDamageable
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
    public GameObject[] explosionVFX;
    public AudioSource audioSource;
    public AudioClip[] explosionSoundEffects;
    private float offset = 1.2f;


    [Header("Hit Flash")]
    public Material hitFlashMaterial;      
    public Material originalMaterial;

    [Header("Knockback Ayarları")]
    public float knockbackSpeed = 6f;
    public float knockbackDuration = 0.3f;

    private Renderer barrelRenderer;

    // Dahili durum takibi
    private bool isCritical = false;    
    private bool hasExploded = false;   
    private Vector3 originalScale;
    private FollowPlayer camScript;

    private bool isKnockedBack = false;
    private UnityEngine.AI.NavMeshObstacle navObstacle;

    void Start()
    {
        originalScale = transform.localScale;
        barrelRenderer = GetComponent<Renderer>();
        camScript = Camera.main.GetComponent<FollowPlayer>();
        navObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
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

        if (other.CompareTag("Weapon") || other.CompareTag("OrbitWeapon") || other.CompareTag("Barrel"))
        {
            int damageValue = 1;
            if (other.TryGetComponent<Weapon>(out var w)) damageValue = w.damage;
            else if (other.TryGetComponent<OrbitWeapon>(out var ow)) damageValue = ow.damage;
            else if (other.TryGetComponent<ExplosiveBarrel>(out var eb)) damageValue = eb.explosionDamage;

            TakeBarrelDamage(damageValue);
            ApplyKnockback(other.transform.position);
        }



        if (other.CompareTag("Enemy"))
        {
            // Kritik modda düşman teması anında patlat, bu mantık zaten vardı
            if (isCritical)
            {
                Explode();
                return;
            }

            // Normal modda düşman varile çarparsa hafif hasar ve knockback
            // Düşmanın kendi pozisyonunu kaynak veriyoruz ki
            // varil düşmandan uzağa doğru itilsin
            TakeBarrelDamage(1);
            ApplyKnockback(other.transform.position);
        }
    }

    public void ApplyKnockback(Vector3 source, float knockbackMultiplier = 1f)
    {
        // Zaten knockback'teyse veya patladıysa tekrar başlatma
        if (hasExploded || isKnockedBack) return;
        StartCoroutine(KnockbackRoutine(source, knockbackMultiplier));
    }

    IEnumerator KnockbackRoutine(Vector3 source, float knockbackMultiplier = 1f)
    {
        isKnockedBack = true;
        if (navObstacle != null) navObstacle.enabled = false;

        Vector3 pushDir = (transform.position - source).normalized;
        pushDir.y = 0;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / knockbackDuration);
            Vector3 targetPos = transform.position + (knockbackSpeed * knockbackMultiplier) * t * Time.deltaTime * pushDir;

            // NavMesh üzerinde geçerli bir pozisyon var mı diye kontrol et
            // SamplePosition "bu noktaya en yakın NavMesh yüzeyi neresi?" diye sorar
            // Bu sayede varil NavMesh dışına veya collider içine giremez
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                // Y eksenini koru! SamplePosition zeminin altına çekiyordu.
                Vector3 safePos = hit.position;
                safePos.y = transform.position.y;
                transform.position = safePos;
            }

            yield return null;
        }

        if (navObstacle != null) navObstacle.enabled = true;
        isKnockedBack = false;
    }

    // IDamageable interface implementasyonu
    // Hasar veren kodların "IDamageable mısın?" diye sormasına olanak tanır
    public void TakeDamage(int damage, Vector3 knockbackSource, float knockbackMultiplier = 1f)
    {
        TakeBarrelDamage(damage);
        ApplyKnockback(knockbackSource, knockbackMultiplier);
    }

    public void TakeBarrelDamage(int damage)
    {
        if (hasExploded) return;

        StartCoroutine(HitFlashRoutine());

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

        if (explosionVFX != null && explosionSoundEffects!= null)
        {
            int rand = Random.Range(0, explosionVFX.Length);

            Vector3 spawnPos = transform.position; 
            spawnPos.y = transform.position.y + offset;

            GameObject vfx = Instantiate(explosionVFX[rand], spawnPos, Quaternion.identity);
            audioSource.PlayOneShot(explosionSoundEffects[Random.Range(0, explosionSoundEffects.Length)]);
            Destroy(vfx, 2f);
        }

        Collider[] affected = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var col in affected)
        {
            if (col.TryGetComponent<EnemyBase>(out var enemy))
                enemy.TakeDamage(explosionDamage, transform.position);
            if (col.TryGetComponent<PlayerHealth>(out var player))
                player.TakeDamage(1);
            if (col.TryGetComponent<ExplosiveBarrel>(out var barrel))
                barrel.TakeBarrelDamage(1);
        }

        StartCoroutine(ExplodeAndDestroy());
    }

    IEnumerator ExplodeAndDestroy()
    {
        foreach (var col in GetComponents<Collider>())
            col.enabled = false;

        if (barrelRenderer != null)
            barrelRenderer.enabled = false;

        var lightComp = GetComponent<Light>();
        if (lightComp != null) lightComp.enabled = false;

        yield return new WaitForSeconds(0.15f);
        camScript.TriggerShake(0.5f, 0.4f);
        yield return new WaitForSeconds(4.0f);
        Destroy(gameObject);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
    IEnumerator HitFlashRoutine()
    {
        barrelRenderer.material = hitFlashMaterial;
        yield return new WaitForSeconds(0.1f);
        barrelRenderer.material = originalMaterial;
    }
}